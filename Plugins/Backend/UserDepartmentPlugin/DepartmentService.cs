namespace UserDepartmentPlugin;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<string?> GetUserDepartmentAsync(Guid userId, CancellationToken ct = default);
}

public record DepartmentDto(string Id, string Name, string Color);

public class DepartmentService : IDepartmentService
{
    private static readonly List<DepartmentDto> Departments =
    [
        new("eng", "Engineering", "#6366f1"),
        new("marketing", "Marketing", "#10b981"),
        new("sales", "Sales", "#f59e0b"),
        new("hr", "Human Resources", "#ef4444"),
        new("finance", "Finance", "#3b82f6"),
        new("ops", "Operations", "#8b5cf6"),
    ];

    // Deterministic "assignment" based on userId hash — no DB needed
    private static readonly Dictionary<string, string> Overrides = new()
    {
        // Seed data so the admin user shows Engineering
        { "00000000-0000-0000-0000-000000000000", "eng" },
    };

    public Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Departments);
    }

    public Task<string?> GetUserDepartmentAsync(Guid userId, CancellationToken ct = default)
    {
        var key = userId.ToString();
        if (Overrides.TryGetValue(key, out var dept))
            return Task.FromResult<string?>(dept);

        var idx = Math.Abs(userId.GetHashCode()) % Departments.Count;
        return Task.FromResult<string?>(Departments[idx].Id);
    }
}