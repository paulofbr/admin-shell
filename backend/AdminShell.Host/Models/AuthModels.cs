namespace AdminShell.Host.Models;

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record RegisterRequest(string Email, string Username, string Password, string? DisplayName);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
