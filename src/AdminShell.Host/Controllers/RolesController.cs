using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public RolesController(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync(ct);
        return Ok(roles.Select(r => new
        {
            r.Id, r.Name, r.Description, r.CreatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();
        return Ok(new { role.Id, role.Name, role.Description, role.CreatedAt });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var existing = await _roleRepository.GetByNameAsync(request.Name, ct);
        if (existing is not null)
            return Conflict(new { Message = "Role already exists" });

        var role = new AdminShell.Core.Entities.Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _roleRepository.AddAsync(role, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            new { created.Id, created.Name, created.Description, created.CreatedAt });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        if (request.Name is not null && request.Name != role.Name)
        {
            var existing = await _roleRepository.GetByNameAsync(request.Name, ct);
            if (existing is not null)
                return Conflict(new { Message = "Role name already in use" });
            role.Name = request.Name;
        }

        if (request.Description is not null)
            role.Description = request.Description;

        role.UpdatedAt = DateTime.UtcNow;
        await _roleRepository.UpdateAsync(role, ct);

        return Ok(new { role.Id, role.Name, role.Description, role.CreatedAt });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();
        await _roleRepository.DeleteAsync(role, ct);
        return NoContent();
    }

    // --- Permission management ---

    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var perms = await _permissionRepository.GetAllAsync(ct);
        return Ok(perms.Select(p => new { p.Id, p.Code, p.Resource, p.Action, p.Description }));
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        var assigned = await _permissionRepository.GetByRoleIdAsync(id, ct);
        var all = await _permissionRepository.GetAllAsync(ct);

        return Ok(new
        {
            RoleId = id,
            Assigned = assigned.Select(p => new { p.Id, p.Code, p.Resource, p.Action }),
            Available = all.Select(p => new { p.Id, p.Code, p.Resource, p.Action })
        });
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] AssignPermissionRequest request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        var perm = await _permissionRepository.GetByIdAsync(request.PermissionId, ct);
        if (perm is null) return NotFound(new { Message = "Permission not found" });

        await _permissionRepository.AssignToRoleAsync(id, request.PermissionId, ct);
        return Ok(new { Message = "Permission assigned" });
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        await _permissionRepository.RemoveFromRoleAsync(id, permissionId, ct);
        return NoContent();
    }
}

public record CreateRoleRequest(string Name, string? Description);
public record UpdateRoleRequest(string? Name, string? Description);
public record AssignPermissionRequest(Guid PermissionId);