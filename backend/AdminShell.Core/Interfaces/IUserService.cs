using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllAsync(int skip, int take, string? email, string? username, string? displayName, string? currentUser, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, string? currentUser, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, string? currentUser, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default);
}

public record CreateUserRequest(string Email, string Username, string Password, string? DisplayName, List<ExtensionField>? ExtensionFields = null);
public record UpdateUserRequest(string? Email, string? Username, string? DisplayName, bool? IsActive, List<ExtensionField>? ExtensionFields = null);

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? AvatarUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<RoleDto> Roles { get; init; } = new();
    public List<ExtensionField> ExtensionFields { get; init; } = new();
}

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ExtensionField> ExtensionFields { get; init; } = new();
}

public record PagedResult<T>(List<T> Data, int Total, int Skip, int Take);

public record Result(bool IsSuccess, string? Error = null)
{
    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
}

public record Result<T>(T Data, bool IsSuccess, string? Error = null)
{
    public static Result<T> Success(T data) => new(data, true);
    public static Result<T> Failure(string error) => new(default!, false, error);
}