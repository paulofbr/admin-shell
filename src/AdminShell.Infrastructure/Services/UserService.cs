using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;

namespace AdminShell.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLog;

    public UserService(IUserRepository userRepository, IAuditLogService auditLog)
    {
        _userRepository = userRepository;
        _auditLog = auditLog;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int skip, int take, string? currentUser, CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(skip, take, ct);
        var total = await _userRepository.GetCountAsync(ct);
        
        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            Username = u.Username ?? string.Empty,
            DisplayName = u.DisplayName ?? string.Empty,
            AvatarUrl = u.AvatarUrl,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            Roles = u.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty }).ToList()
        }).ToList();

        return new PagedResult<UserDto>(dtos, total, skip, take);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return null;
        
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Username = user.Username ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty }).ToList()
        };
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, string? currentUser, CancellationToken ct = default)
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        var created = await _userRepository.AddAsync(user, ct);
        
        await _auditLog.LogAsync("USER_CREATE", "User", created.Id.ToString(), currentUser ?? "system",
            details: $"Created user {created.Email}", ct: ct);

        var dto = new UserDto
        {
            Id = created.Id,
            Email = created.Email ?? string.Empty,
            Username = created.Username ?? string.Empty,
            DisplayName = created.DisplayName ?? string.Empty,
            AvatarUrl = created.AvatarUrl,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt,
            Roles = created.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty }).ToList()
        };

        return Result<UserDto>.Success(dto);
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, string? currentUser, CancellationToken ct = default)
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

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser ?? "system";

        await _userRepository.UpdateAsync(user, ct);
        
        await _auditLog.LogAsync("USER_UPDATE", "User", id.ToString(), currentUser ?? "system",
            previousValue: previousEmail, newValue: user.Email, ct: ct);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Username = user.Username ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty }).ToList()
        };

        return Result<UserDto>.Success(dto);
    }

    public async Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return Result.Failure("User not found");
        
        await _userRepository.DeleteAsync(user, ct);
        
        await _auditLog.LogAsync("USER_DELETE", "User", id.ToString(), currentUser ?? "system",
            details: $"Deleted user {user.Email}", ct: ct);
        
        return Result.Success();
    }
}