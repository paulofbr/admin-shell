using System.Security.Claims;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }

        var (token, expiresAt) = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        return new LoginResponse(token, refreshToken, expiresAt);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var principal = _jwtService.ValidateToken(request.AccessToken);
        if (principal is null)
        {
            throw new UnauthorizedAccessException("Invalid access token");
        }

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var (token, expiresAt) = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user, ct);

        return new LoginResponse(token, newRefreshToken, expiresAt);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _userRepository.UpdateAsync(user, ct);
            _logger.LogInformation("User {UserId} logged out", userId);
        }
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existingEmail is not null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, ct);
        if (existingUsername is not null)
        {
            throw new InvalidOperationException("Username already taken");
        }

        var defaultRole = await _roleRepository.GetByNameAsync("User", ct);
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            DisplayName = request.DisplayName ?? request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = "system",
            Roles = defaultRole is not null ? new List<Role> { defaultRole } : new List<Role>()
        };

        await _userRepository.AddAsync(user, ct);

        var (token, expiresAt) = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("User {Email} registered successfully", request.Email);
        return new LoginResponse(token, refreshToken, expiresAt);
    }
}
