using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminShell.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLog;

    public AuthController(IAuthService authService, IAuditLogService auditLog)
    {
        _authService = authService;
        _auditLog = auditLog;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
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
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
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
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        await _authService.LogoutAsync(userId, ct);
        await _auditLog.LogAsync("LOGOUT", "User", userId.ToString(), email, ct: ct);
        return Ok(new { Message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(
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
            Roles = user.Roles.Select(r => new { r.Id, r.Name })
        });
    }
}