namespace UserDepartmentPlugin;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<string?> GetUserDepartmentAsync(Guid userId, CancellationToken ct = default);
    Task SetUserDepartmentAsync(Guid userId, string departmentId, CancellationToken ct = default);
}
