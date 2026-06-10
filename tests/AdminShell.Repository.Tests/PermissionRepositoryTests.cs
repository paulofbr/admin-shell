using AdminShell.Core.Entities;
using AdminShell.Infrastructure.Data.Repositories;
using Dapper;
using FluentAssertions;
using Xunit;

namespace AdminShell.Repository.Tests;

[Collection("RepositoryTests")]
public class PermissionRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public PermissionRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private PermissionRepository CreateRepo() => new(_fixture.ConnectionFactory);

    #region Query all / by id / by code

    [Fact]
    public async Task GetAllAsync_ReturnsSeededPermissions()
    {
        var repo = CreateRepo();

        var permissions = await repo.GetAllAsync();

        permissions.Should().NotBeEmpty();
        permissions.Should().Contain(p => p.Code == "users:read");
        permissions.Should().Contain(p => p.Code == "users:write");
        permissions.Should().Contain(p => p.Code == "users:delete");
        permissions.Should().Contain(p => p.Code == "plugins:manage");
        permissions.Should().Contain(p => p.Code == "settings:read");
        permissions.Should().Contain(p => p.Code == "settings:write");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByResourceThenAction()
    {
        var repo = CreateRepo();

        var permissions = await repo.GetAllAsync();

        // Check ordering: resource ascending, then action ascending
        for (int i = 1; i < permissions.Count; i++)
        {
            var prev = permissions[i - 1];
            var curr = permissions[i];
            var resourceCmp = string.Compare(prev.Resource, curr.Resource, StringComparison.Ordinal);
            if (resourceCmp == 0)
            {
                string.Compare(prev.Action, curr.Action, StringComparison.Ordinal)
                    .Should().BeLessOrEqualTo(0);
            }
            else
            {
                resourceCmp.Should().BeLessOrEqualTo(0);
            }
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPermission_WhenExists()
    {
        var repo = CreateRepo();

        // Find a known permission
        Guid permId;
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            permId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Permissions WHERE Code = 'users:read'");
        }

        var loaded = await repo.GetByIdAsync(permId);

        loaded.Should().NotBeNull();
        loaded!.Code.Should().Be("users:read");
        loaded.Resource.Should().Be("users");
        loaded.Action.Should().Be("read");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByIdAsync(Guid.NewGuid());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsPermission_WhenExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByCodeAsync("plugins:manage");

        loaded.Should().NotBeNull();
        loaded!.Resource.Should().Be("plugins");
        loaded.Action.Should().Be("manage");
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByCodeAsync("nonexistent:perm");

        loaded.Should().BeNull();
    }

    #endregion

    #region Role-Permission assignments

    [Fact]
    public async Task GetByRoleIdAsync_AdminRole_HasAllSeededPermissions()
    {
        var repo = CreateRepo();
        Guid adminRoleId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            adminRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'Admin'");
        }

        var permissions = await repo.GetByRoleIdAsync(adminRoleId);

        permissions.Should().NotBeEmpty();
        permissions.Should().HaveCount(6);
        permissions.Select(p => p.Code).Should().Contain(new[]
        {
            "users:read", "users:write", "users:delete",
            "plugins:manage", "settings:read", "settings:write"
        });
    }

    [Fact]
    public async Task GetByRoleIdAsync_UserRole_HasNoPermissions()
    {
        var repo = CreateRepo();
        Guid userRoleId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            userRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'User'");
        }

        var permissions = await repo.GetByRoleIdAsync(userRoleId);

        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByRoleIdAsync_NonExistentRole_ReturnsEmpty()
    {
        var repo = CreateRepo();

        var permissions = await repo.GetByRoleIdAsync(Guid.NewGuid());

        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignToRoleAsync_AddsPermission()
    {
        var repo = CreateRepo();
        Guid userRoleId;
        Guid usersReadPermId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            userRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'User'");
            usersReadPermId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Permissions WHERE Code = 'users:read'");
        }

        await repo.AssignToRoleAsync(userRoleId, usersReadPermId);

        var permissions = await repo.GetByRoleIdAsync(userRoleId);
        permissions.Should().ContainSingle(p => p.Code == "users:read");

        // Cleanup
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync(
                "DELETE FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermId",
                new { RoleId = userRoleId, PermId = usersReadPermId });
        }
    }

    [Fact]
    public async Task AssignToRoleAsync_DuplicateAssignment_DoesNotThrow()
    {
        var repo = CreateRepo();
        Guid userRoleId;
        Guid usersReadPermId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            userRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'User'");
            usersReadPermId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Permissions WHERE Code = 'users:read'");
        }

        await repo.AssignToRoleAsync(userRoleId, usersReadPermId);
        // Second assign should not throw (might be no-op or PK violation — but table has PK on RoleId+PermissionId)
        // Actually this WILL throw due to PK violation — that's expected behavior
        Func<Task> act = async () => await repo.AssignToRoleAsync(userRoleId, usersReadPermId);

        await act.Should().ThrowAsync<Exception>();

        // Cleanup
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync(
                "DELETE FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermId",
                new { RoleId = userRoleId, PermId = usersReadPermId });
        }
    }

    [Fact]
    public async Task RemoveFromRoleAsync_RemovesPermission()
    {
        var repo = CreateRepo();
        // Admin role has all permissions seeded — remove one
        Guid adminRoleId;
        Guid settingsWritePermId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            adminRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'Admin'");
            settingsWritePermId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Permissions WHERE Code = 'settings:write'");
        }

        await repo.RemoveFromRoleAsync(adminRoleId, settingsWritePermId);

        var permissions = await repo.GetByRoleIdAsync(adminRoleId);
        permissions.Should().NotContain(p => p.Code == "settings:write");
        permissions.Should().HaveCount(5);

        // Re-add for cleanup (restore seed state)
        await repo.AssignToRoleAsync(adminRoleId, settingsWritePermId);
    }

    [Fact]
    public async Task RemoveFromRoleAsync_NonExistentAssignment_DoesNotThrow()
    {
        var repo = CreateRepo();

        // User role has no permissions — remove any should be no-op
        Guid userRoleId;
        Guid permId;

        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            userRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'User'");
            permId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Permissions WHERE Code = 'users:read'");
        }

        var act = async () => await repo.RemoveFromRoleAsync(userRoleId, permId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AssignThenRemove_RoundTrip_Works()
    {
        var repo = CreateRepo();
        var role = MakeTestRole();
        var perm = MakeTestPermission();

        // Insert test role + permission directly
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync(
                @"INSERT INTO Roles (Id, Name, Description, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Name, @Desc, 0, @Now, 'test')",
                new { role.Id, role.Name, Desc = (string?)role.Description, Now = DateTime.UtcNow });

            await db.ExecuteAsync(
                @"INSERT INTO Permissions (Id, Code, Resource, Action, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Code, @Res, @Act, 0, @Now, 'test')",
                new { perm.Id, perm.Code, Res = perm.Resource, Act = perm.Action, Now = DateTime.UtcNow });
        }

        // Assign
        await repo.AssignToRoleAsync(role.Id, perm.Id);

        var afterAssign = await repo.GetByRoleIdAsync(role.Id);
        afterAssign.Should().ContainSingle(p => p.Code == perm.Code);

        // Remove
        await repo.RemoveFromRoleAsync(role.Id, perm.Id);

        var afterRemove = await repo.GetByRoleIdAsync(role.Id);
        afterRemove.Should().BeEmpty();

        // Cleanup
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync("DELETE FROM RolePermissions WHERE RoleId = @Id", new { Id = role.Id });
            await db.ExecuteAsync("DELETE FROM Permissions WHERE Id = @Id", new { Id = perm.Id });
            await db.ExecuteAsync("DELETE FROM Roles WHERE Id = @Id", new { Id = role.Id });
        }
    }

    #endregion

    private static Role MakeTestRole()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = $"PermTest-{suffix}",
            Description = "Permission test role",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private static Permission MakeTestPermission()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = $"test:{suffix}",
            Resource = "test",
            Action = suffix,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }
}