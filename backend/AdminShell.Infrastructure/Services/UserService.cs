using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;

namespace AdminShell.Infrastructure.Services;

public class UserService : BaseService<User, UserDto, CreateUserRequest, UpdateUserRequest>, IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository, IAuditLogService auditLog)
        : base(userRepository, auditLog)
    {
        _userRepository = userRepository;
    }

    public override async Task<PagedResult<UserDto>> GetAllAsync(QuerySpecification query, string? currentUser, CancellationToken ct = default)
    {
        var email = query.Filters.FirstOrDefault(f => string.Equals(f.Field, "email", StringComparison.OrdinalIgnoreCase))?.Value;
        var username = query.Filters.FirstOrDefault(f => string.Equals(f.Field, "username", StringComparison.OrdinalIgnoreCase))?.Value;
        var displayName = query.Filters.FirstOrDefault(f => string.Equals(f.Field, "displayName", StringComparison.OrdinalIgnoreCase))?.Value;
        return await GetAllAsync(query.Skip, query.Take, email, username, displayName, currentUser, ct);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int skip, int take, string? email, string? username, string? displayName, string? currentUser, CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(skip, take, email, username, displayName, ct);
        var total = await _userRepository.GetCountAsync(email, username, displayName, ct);

        var dtos = MapToDtos(users);
        return new PagedResult<UserDto>(dtos, total, skip, take);
    }

    public override async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return null;
        return MapToDto(user);
    }

    public override async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, string? currentUser, CancellationToken ct = default)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existingEmail is not null)
            return Result<UserDto>.Failure("Email already in use");

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, ct);
        if (existingUsername is not null)
            return Result<UserDto>.Failure("Username already in use");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            DisplayName = request.DisplayName,
            IsActive = true,
            CreatedBy = currentUser ?? "system",
            CreatedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ExtensionFields = request.ExtensionFields ?? new()
        };

        var created = await _userRepository.AddAsync(user, ct);
        await LogAsync("USER_CREATE", "User", created.Id.ToString(), currentUser,
            details: $"Created user {created.Email}", ct: ct);

        return Result<UserDto>.Success(MapToDto(created));
    }

    public override async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, string? currentUser, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return Result<UserDto>.Failure("User not found");

        var previousEmail = user.Email ?? string.Empty;

        if (request.Email is not null && request.Email != user.Email)
        {
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email, ct);
            if (existingEmail is not null)
                return Result<UserDto>.Failure("Email already in use");
            user.Email = request.Email;
        }

        if (request.Username is not null && request.Username != user.Username)
        {
            var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, ct);
            if (existingUsername is not null)
                return Result<UserDto>.Failure("Username already in use");
            user.Username = request.Username;
        }

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.ExtensionFields = request.ExtensionFields ?? user.ExtensionFields;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser ?? "system";

        await _userRepository.UpdateAsync(user, ct);
        await LogAsync("USER_UPDATE", "User", id.ToString(), currentUser,
            previousValue: previousEmail, newValue: user.Email, ct: ct);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public override async Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return Result.Failure("User not found");

        await _userRepository.DeleteAsync(user, ct);
        await LogAsync("USER_DELETE", "User", id.ToString(), currentUser,
            details: $"Deleted user {user.Email}", ct: ct);

        return Result.Success();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Username = user.Username ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty }).ToList(),
            ExtensionFields = user.ExtensionFields
        };
    }

    private static List<UserDto> MapToDtos(IEnumerable<User> users)
    {
        return users.Select(MapToDto).ToList();
    }
}