using AdminShell.Contracts;
using AdminShell.Core.Entities;
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
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleDto>>> GetAll(CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync(ct);
        return Ok(roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            ExtensionFields = r.ExtensionFields
        }));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();
        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            ExtensionFields = role.ExtensionFields
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
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
            CreatedAt = DateTime.UtcNow,
            ExtensionFields = request.ExtensionFields ?? new()
        };

        var created = await _roleRepository.AddAsync(role, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new RoleDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            CreatedAt = created.CreatedAt,
            ExtensionFields = created.ExtensionFields
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
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

        role.ExtensionFields = request.ExtensionFields ?? role.ExtensionFields;
        role.UpdatedAt = DateTime.UtcNow;
        await _roleRepository.UpdateAsync(role, ct);

        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            ExtensionFields = role.ExtensionFields
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();
        await _roleRepository.DeleteAsync(role, ct);
        return NoContent();
    }

    // --- Permission management ---

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionDto>>> GetAllPermissions(CancellationToken ct)
    {
        var perms = await _permissionRepository.GetAllAsync(ct);
        return Ok(perms.Select(p => new { p.Id, p.Code, p.Resource, p.Action, p.Description }));
    }

    [HttpGet("{id:guid}/permissions")]
    [ProducesResponseType(typeof(RolePermissionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RolePermissionsResponse>> GetRolePermissions(Guid id, CancellationToken ct)
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
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> AssignPermission(Guid id, [FromBody] AssignPermissionRequest request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        var perm = await _permissionRepository.GetByIdAsync(request.PermissionId, ct);
        if (perm is null) return NotFound(new { Message = "Permission not found" });

        await _permissionRepository.AssignToRoleAsync(id, request.PermissionId, ct);
        return Ok(new { Message = "Permission assigned" });
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role is null) return NotFound();

        await _permissionRepository.RemoveFromRoleAsync(id, permissionId, ct);
        return NoContent();
    }
}

public record CreateRoleRequest(string Name, string? Description, List<ExtensionField>? ExtensionFields = null);
public record UpdateRoleRequest(string? Name, string? Description, List<ExtensionField>? ExtensionFields = null);
public record AssignPermissionRequest(Guid PermissionId);