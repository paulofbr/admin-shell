namespace AdminShell.Core.Interfaces;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record RegisterRequest(string Email, string Username, string Password, string? DisplayName);

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
