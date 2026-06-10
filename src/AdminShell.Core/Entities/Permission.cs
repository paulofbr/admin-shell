namespace AdminShell.Core.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
