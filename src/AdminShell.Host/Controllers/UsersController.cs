using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var result = await _userService.GetAllAsync(skip, take, User.Identity?.Name, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateAsync(request, User.Identity?.Name, ct);
        if (!result.IsSuccess) return result.Error switch
        {
            "Email already in use" or "Username already in use" => Conflict(new { Message = result.Error }),
            _ => BadRequest(new { Message = result.Error })
        };
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateAsync(id, request, User.Identity?.Name, ct);
        if (!result.IsSuccess) return result.Error switch
        {
            "User not found" => NotFound(),
            "Email already in use" or "Username already in use" => Conflict(new { Message = result.Error }),
            _ => BadRequest(new { Message = result.Error })
        };
        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeleteAsync(id, User.Identity?.Name, ct);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }
}