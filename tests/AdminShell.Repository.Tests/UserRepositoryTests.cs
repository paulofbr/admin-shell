using AdminShell.Core.Entities;
using AdminShell.Infrastructure.Data.Repositories;
using Dapper;
using FluentAssertions;
using Xunit;

namespace AdminShell.Repository.Tests;

[Collection("RepositoryTests")]
public class UserRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private UserRepository CreateRepo() => new(_fixture.ConnectionFactory);

    private static User MakeUniqueUser(string emailPrefix = "test")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"{emailPrefix}-{suffix}@test.com",
            Username = $"{emailPrefix}-{suffix}",
            DisplayName = $"Test User {suffix}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    #region CRUD

    [Fact]
    public async Task AddAsync_CreatesUser_AndCanBeRetrieved()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();

        var created = await repo.AddAsync(user);

        created.Should().NotBeNull();
        created.Id.Should().Be(user.Id);
        created.Email.Should().Be(user.Email);
        created.Username.Should().Be(user.Username);
        created.IsActive.Should().BeTrue();
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Cleanup — hard-delete via Dapper
        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(user.Id);
        loaded.Email.Should().Be(user.Email);
        loaded.Username.Should().Be(user.Username);

        await CleanupUserAsync(user.Id);
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
        var user = MakeUniqueUser();
        await repo.AddAsync(user);
        await repo.DeleteAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);

        loaded.Should().BeNull();

        await HardDeleteUserAsync(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        var loaded = await repo.GetByEmailAsync(user.Email);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(user.Id);

        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByEmailAsync("nonexistent@test.com");

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser_WhenExists()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        var loaded = await repo.GetByUsernameAsync(user.Username);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(user.Id);

        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepo();

        var loaded = await repo.GetByUsernameAsync("nonexistent-user");

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        var repo = CreateRepo();
        var userIds = new List<Guid>();
        // Seed users
        for (int i = 0; i < 5; i++)
        {
            var u = MakeUniqueUser($"page-{i}");
            await repo.AddAsync(u);
            userIds.Add(u.Id);
        }

        // Use the Admin user from seed for the first page
        var page1 = await repo.GetAllAsync(0, 20);

        page1.Should().NotBeEmpty();
        page1.Count.Should().BeGreaterThanOrEqualTo(5);

        // Test pagination
        var page2 = await repo.GetAllAsync(5, 3);
        page2.Count.Should().BeLessThanOrEqualTo(3);

        // Cleanup — only our users
        foreach (var id in userIds)
            await CleanupUserAsync(id);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        var repo = CreateRepo();
        var countBefore = await repo.GetCountAsync();

        var user = MakeUniqueUser("count-test");
        await repo.AddAsync(user);

        var countAfter = await repo.GetCountAsync();
        countAfter.Should().Be(countBefore + 1);

        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesFields()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        user.DisplayName = "Updated Name";
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = "test";
        await repo.UpdateAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);
        loaded.Should().NotBeNull();
        loaded!.DisplayName.Should().Be("Updated Name");
        loaded.IsActive.Should().BeFalse();
        loaded.UpdatedAt.Should().NotBeNull();

        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_SetsRefreshToken()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        user.RefreshToken = "test-refresh-token-value";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = "test";
        await repo.UpdateAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);
        loaded.Should().NotBeNull();
        loaded!.RefreshToken.Should().Be("test-refresh-token-value");
        loaded.RefreshTokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));

        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesUser()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        await repo.DeleteAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);
        loaded.Should().BeNull(); // Soft-deleted users are excluded

        // Verify via direct Dapper that IsDeleted = 1 and DeletedAt is set
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        // First check if the row exists at all
        var rowCount = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Users WHERE Id = @Id",
            new { Id = user.Id });
        rowCount.Should().Be(1, "row should still exist after soft delete");
        
        var direct = await db.QueryFirstOrDefaultAsync(
            "SELECT IsDeleted, DeletedAt FROM Users WHERE Id = @Id",
            new { Id = user.Id });
        Assert.NotNull(direct);
        bool isDeleted = ((dynamic)direct).IsDeleted;
        isDeleted.Should().BeTrue("IsDeleted should be true after soft delete");

        // Hard delete cleanup
        await HardDeleteUserAsync(user.Id);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_DoesNotThrow()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);
        await repo.DeleteAsync(user);

        // Delete again — should be idempotent
        await repo.DeleteAsync(user);

        await HardDeleteUserAsync(user.Id);
    }

    #endregion

    #region Roles

    [Fact]
    public async Task UserRoles_AreLoadedWhenRetrieved()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        // Fetch admin role (seeded by DatabaseInitializer)
        Guid adminRoleId;
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            adminRoleId = await db.QueryFirstOrDefaultAsync<Guid>(
                "SELECT Id FROM Roles WHERE Name = 'Admin'");
        }

        // Assign user to admin role
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync(
                "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                new { UserId = user.Id, RoleId = adminRoleId });
        }

        // Reload and verify roles are populated
        var loaded = await repo.GetByIdAsync(user.Id);
        loaded.Should().NotBeNull();
        loaded!.Roles.Should().ContainSingle(r => r.Id == adminRoleId);

        // Cleanup
        using (var db = _fixture.ConnectionFactory.CreateConnection())
        {
            db.Open();
            await db.ExecuteAsync(
                "DELETE FROM UserRoles WHERE UserId = @UserId",
                new { UserId = user.Id });
        }
        await CleanupUserAsync(user.Id);
    }

    [Fact]
    public async Task UserWithoutRoles_HasEmptyRolesCollection()
    {
        var repo = CreateRepo();
        var user = MakeUniqueUser();
        await repo.AddAsync(user);

        var loaded = await repo.GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Roles.Should().BeEmpty();

        await CleanupUserAsync(user.Id);
    }

    #endregion

    #region Helpers

    private async Task CleanupUserAsync(Guid userId)
    {
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync("DELETE FROM UserRoles WHERE UserId = @Id", new { Id = userId });
        await db.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = userId });
    }

    private async Task HardDeleteUserAsync(Guid userId)
    {
        using var db = _fixture.ConnectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync("DELETE FROM UserRoles WHERE UserId = @Id", new { Id = userId });
        await db.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = userId });
    }

    #endregion
}