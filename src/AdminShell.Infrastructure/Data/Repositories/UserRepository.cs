using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
        if (user != null)
            user.Roles = await GetUserRolesAsync(db, id);
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0",
            new { Email = email });
        if (user != null)
            user.Roles = await GetUserRolesAsync(db, user.Id);
        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsDeleted = 0",
            new { Username = username });
        if (user != null)
            user.Roles = await GetUserRolesAsync(db, user.Id);
        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(int skip = 0, int take = 20, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var users = (await db.QueryAsync<User>(
            @"SELECT * FROM Users WHERE IsDeleted = 0
              ORDER BY CreatedAt ASC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { Skip = skip, Take = take })).ToList();
        foreach (var user in users)
            user.Roles = await GetUserRolesAsync(db, user.Id);
        return users;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE IsDeleted = 0");
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE IsDeleted = 0 AND IsActive = 1");
    }

    public async Task<List<MonthlyGrowthPoint>> GetMonthlyGrowthAsync(int months = 6, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var rows = await db.QueryAsync<MonthlyGrowthPoint>(
            @"SELECT
                FORMAT(CreatedAt, 'yyyy-MM') AS Month,
                COUNT(*) AS Count
              FROM Users
              WHERE IsDeleted = 0 AND CreatedAt >= DATEADD(MONTH, -@Months, GETUTCDATE())
              GROUP BY FORMAT(CreatedAt, 'yyyy-MM')
              ORDER BY Month",
            new { Months = months });
        return rows.ToList();
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            @"INSERT INTO Users (Id, Email, Username, DisplayName, PasswordHash, AvatarUrl, IsActive, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Id, @Email, @Username, @DisplayName, @PasswordHash, @AvatarUrl, @IsActive, 0, @CreatedAt, @CreatedBy)",
            new
            {
                user.Id, user.Email, user.Username, user.DisplayName,
                user.PasswordHash, user.AvatarUrl, user.IsActive,
                user.CreatedAt, user.CreatedBy
            });
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            @"UPDATE Users SET
                Email = @Email, Username = @Username, DisplayName = @DisplayName,
                AvatarUrl = @AvatarUrl, IsActive = @IsActive,
                RefreshToken = @RefreshToken, RefreshTokenExpiresAt = @RefreshTokenExpiresAt,
                UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id",
            new
            {
                user.Id, user.Email, user.Username, user.DisplayName,
                user.AvatarUrl, user.IsActive, user.RefreshToken, user.RefreshTokenExpiresAt,
                user.UpdatedAt, user.UpdatedBy
            });
    }

    public async Task DeleteAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE Users SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { user.Id, DeletedAt = DateTime.UtcNow });
    }

    private async Task<List<Role>> GetUserRolesAsync(System.Data.IDbConnection db, Guid userId)
    {
        var roles = await db.QueryAsync<Role>(
            @"SELECT r.Id, r.Name, r.Description, r.IsDeleted, r.DeletedAt, r.CreatedAt, r.CreatedBy
              FROM Roles r
              INNER JOIN UserRoles ON r.Id = UserRoles.RoleId
              WHERE UserRoles.UserId = @UserID",
            new { UserID = userId });
        return roles.ToList();
    }
}