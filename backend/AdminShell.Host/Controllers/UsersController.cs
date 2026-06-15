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
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 20,
        [FromQuery] string? email = null,
        [FromQuery] string? username = null,
        [FromQuery] string? displayName = null,
        CancellationToken ct = default)
    {
        var result = await _userService.GetAllAsync(skip, take, email, username, displayName, User.Identity?.Name, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
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
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeleteAsync(id, User.Identity?.Name, ct);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }
}