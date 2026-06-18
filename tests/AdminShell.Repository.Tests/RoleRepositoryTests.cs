using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Infrastructure.Data.Repositories;
using Dapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AdminShell.Repository.Tests;

[Collection("RepositoryTests")]
public class RoleRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public RoleRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private RoleRepository CreateRepo()
    {
        var extensionRegistry = Substitute.For<IPluginExtensionRegistry>();
        extensionRegistry.GetExtensionFieldsForEntity(Arg.Any<string>())
            .Returns(Array.Empty<EntityExtensionFieldDefinition>());

        return new RoleRepository(_fixture.ConnectionFactory, extensionRegistry);
    }

    private static Role MakeUniqueRole(string namePrefix = "TestRole")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}-{suffix}",
            Description = $"Test role {suffix}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    #region CRUD

    [Fact]
    public async Task AddAsync_CreatesRole()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();

        var created = await repo.AddAsync(role);

        created.Should().NotBeNull();
        created.Id.Should().Be(role.Id);
        created.Name.Should().Be(role.Name);
        created.Description.Should().Be(role.Description);

        await CleanupRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRole_WhenExists()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);

        var loaded = await repo.GetByIdAsync(role.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(role.Id);
        loaded.Name.Should().Be(role.Name);

        await CleanupRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByIdAsync(Guid.NewGuid());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSoftDeleted()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);
        await repo.DeleteAsync(role);

        var loaded = await repo.GetByIdAsync(role.Id);

        loaded.Should().BeNull();

        await HardDeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsRole_WhenExists()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);

        var loaded = await repo.GetByNameAsync(role.Name);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(role.Id);

        await CleanupRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByNameAsync("NonExistentRole_" + Guid.NewGuid().ToString("N")[..8]);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenSoftDeleted()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);
        await repo.DeleteAsync(role);

        var loaded = await repo.GetByNameAsync(role.Name);

        loaded.Should().BeNull();

        await HardDeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetAllAsync_IncludesSeedRoles()
    {
        var repo = CreateRepo();

        var roles = await repo.GetAllAsync();

        roles.Should().NotBeEmpty();
        roles.Should().Contain(r => r.Name == "Admin");
        roles.Should().Contain(r => r.Name == "User");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        var repo = CreateRepo();
        // "Admin" and "User" already exist from seed
        // Add roles that sort before and after
        var roleA = MakeUniqueRole("AaaRole");
        var roleZ = MakeUniqueRole("ZzzRole");
        await repo.AddAsync(roleA);
        await repo.AddAsync(roleZ);

        var roles = await repo.GetAllAsync();
        var names = roles.Select(r => r.Name).ToList();

        // Check ordering (should be alphabetical by Name)
        for (int i = 1; i < names.Count; i++)
        {
            string.Compare(names[i - 1], names[i], StringComparison.Ordinal)
                .Should().BeLessThanOrEqualTo(0,
                    $"Expected '{names[i - 1]}' before '{names[i]}'");
        }

        await HardDeleteRoleAsync(roleA.Id);
        await HardDeleteRoleAsync(roleZ.Id);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeleted()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole("ToDelete");
        await repo.AddAsync(role);
        await repo.DeleteAsync(role);

        var roles = await repo.GetAllAsync();

        roles.Should().NotContain(r => r.Name == role.Name);

        await HardDeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesFields()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);

        role.Name = role.Name + "-UPDATED";
        role.Description = "Updated description";
        await repo.UpdateAsync(role);

        var loaded = await repo.GetByIdAsync(role.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(role.Name);
        loaded.Description.Should().Be("Updated description");

        await CleanupRoleAsync(role.Id);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);

        await repo.DeleteAsync(role);

        // Verify via direct Dapper - use dynamic to handle SQL Server types
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        var result = await db.QuerySingleAsync(
            "SELECT IsDeleted, DeletedAt FROM Roles WHERE Id = @Id",
            new { Id = role.Id });
        
        bool isDeleted = ((dynamic)result).IsDeleted;
        isDeleted.Should().BeTrue("IsDeleted should be true after soft delete");

        await HardDeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_DoesNotThrow()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);
        await repo.DeleteAsync(role);

        // Second delete should not throw
        await repo.DeleteAsync(role);

        await HardDeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_Throws()
    {
        var repo = CreateRepo();
        var role = MakeUniqueRole();
        await repo.AddAsync(role);

        var duplicate = MakeUniqueRole(role.Name); // same name but name won't change since we use prefix

        // Actually we need the exact same name — override it
        duplicate.Name = role.Name;

        Func<Task> act = async () => await repo.AddAsync(duplicate);

        // SQL Server unique constraint violation (2627)
        await act.Should().ThrowAsync<Exception>();

        await HardDeleteRoleAsync(role.Id);
    }

    #endregion

    #region Helpers

    private async Task CleanupRoleAsync(Guid roleId)
    {
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync("DELETE FROM RolePermissions WHERE RoleId = @Id", new { Id = roleId });
        await db.ExecuteAsync("DELETE FROM UserRoles WHERE RoleId = @Id", new { Id = roleId });
        await db.ExecuteAsync("DELETE FROM Roles WHERE Id = @Id", new { Id = roleId });
    }

    private async Task HardDeleteRoleAsync(Guid roleId)
    {
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync("DELETE FROM RolePermissions WHERE RoleId = @Id", new { Id = roleId });
        await db.ExecuteAsync("DELETE FROM UserRoles WHERE RoleId = @Id", new { Id = roleId });
        await db.ExecuteAsync("DELETE FROM Roles WHERE Id = @Id", new { Id = roleId });
    }

    #endregion
}