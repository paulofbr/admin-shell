using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Name = @Name AND IsDeleted = 0",
            new { Name = name });
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var roles = await db.QueryAsync<Role>(
            "SELECT * FROM Roles WHERE IsDeleted = 0 ORDER BY Name");
        return roles.ToList();
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Roles WHERE IsDeleted = 0");
    }

    public async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            @"INSERT INTO Roles (Id, Name, Description, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Id, @Name, @Description, 0, @CreatedAt, @CreatedBy)",
            new { role.Id, role.Name, role.Description, role.CreatedAt, role.CreatedBy });
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE Roles SET Name = @Name, Description = @Description WHERE Id = @Id",
            new { role.Id, role.Name, role.Description });
    }

    public async Task DeleteAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE Roles SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { role.Id, DeletedAt = DateTime.UtcNow });
    }
}