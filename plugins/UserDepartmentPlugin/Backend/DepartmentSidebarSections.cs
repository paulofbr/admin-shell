using AdminShell.Contracts;

namespace UserDepartmentPlugin;

[PluginComponent("user-department")]
public sealed class DepartmentSidebarSections : ISidebarSectionPlugin
{
    public IEnumerable<SidebarSectionDescriptor> GetSidebarSections()
    {
        yield return new SidebarSectionDescriptor
        {
            Id = "department-tools",
            Label = "Departments",
            Icon = "OfficeBuilding",
            Order = 50,
            Items = new[]
            {
                new SidebarMenuItem
                {
                    Id = "dept-overview",
                    Label = "Department Overview",
                    Icon = "DataBoard",
                    Path = "/departments",
                    Order = 10
                },
                new SidebarMenuItem
                {
                    Id = "dept-members",
                    Label = "Members by Department",
                    Icon = "UserFilled",
                    Path = "/departments/members",
                    Order = 20
                },
                new SidebarMenuItem
                {
                    Id = "dept-report",
                    Label = "Department Report",
                    Icon = "DataAnalysis",
                    Path = "/departments/report",
                    Order = 30
                }
            }
        };
    }
}
