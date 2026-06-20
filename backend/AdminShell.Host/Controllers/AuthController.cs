using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminShell.Host.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLog;

    public AuthController(IAuthService authService, IAuditLogService auditLog)
    {
        _authService = authService;
        _auditLog = auditLog;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request, ct);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditLog.LogAsync("LOGIN", "User", null,
                request.Email, details: "Login successful", ipAddress: ip, ct: ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditLog.LogAsync("LOGIN_FAILED", "User", null,
                request.Email, details: ex.Message, ipAddress: ip, ct: ct);
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, ct);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditLog.LogAsync("USER_REGISTER", "User", null,
                request.Email, details: "User registered", ipAddress: ip, ct: ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        await _authService.LogoutAsync(userId, ct);
        await _auditLog.LogAsync("LOGOUT", "User", userId.ToString(), email, ct: ct);
        return Ok(new { Message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(
        [FromServices] IUserRepository userRepository,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.IsActive,
            user.CreatedAt,
            Roles = user.Roles.Select(r => new { r.Id, r.Name }),
            Permissions = user.Roles
                .SelectMany(role => role.Permissions)
                .Select(permission => new { permission.Id, permission.Code, permission.Resource, permission.Action, permission.Description })
                .DistinctBy(permission => permission.Code)
        });
    }
}