using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Riok.Mapperly.Abstractions;

namespace AdminShell.Infrastructure.Mappings;

[Mapper]
public partial class AppMapper
{
    public partial UserDto UserToDto(User user);
    public partial RoleDto RoleToDto(Role role);
}
