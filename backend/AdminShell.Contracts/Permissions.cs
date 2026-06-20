using System.Security.Claims;

namespace AdminShell.Contracts;

public static class PermissionClaimTypes
{
    public const string Code = "permission";
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PermissionDefinitionsAttribute(string pluginId) : Attribute
{
    public string PluginId { get; } = pluginId;
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class PermissionDefinitionAttribute(string group, string name) : Attribute
{
    public string Group { get; } = group;
    public string Name { get; } = name;
}

public sealed record PermissionDefinition(
    string PluginId,
    string Code,
    string Group,
    string Name,
    string? Description = null)
{
    public string PolicyName => $"permission:{Code}";
}

public interface IPermissionDefinitionRegistry
{
    IReadOnlyList<PermissionDefinition> GetAll();
    IReadOnlyList<PermissionDefinition> GetByPlugin(string pluginId);
    PermissionDefinition? GetByCode(string code);
    void Discover(IEnumerable<System.Reflection.Assembly> assemblies);
    void DiscoverAssembly(System.Reflection.Assembly assembly);
}

public static class PermissionPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
        => principal.IsInRole("Admin") || principal.HasClaim(PermissionClaimTypes.Code, permission);
}

public sealed class RequirePermissionAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"permission:{permission}";
    }
}

public static class PermissionEndpointConventionBuilderExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            var existing = endpointBuilder.Metadata
                .OfType<RequirePermissionAttribute>()
                .ToArray();

            foreach (var attribute in existing)
            {
                endpointBuilder.Metadata.Remove(attribute);
            }

            endpointBuilder.Metadata.Add(new RequirePermissionAttribute(permission));
        });
        return builder;
    }
}
