namespace AdminShell.Contracts;

/// <summary>
/// Base interface for all admin-shell plugins.
/// </summary>
public interface IAdminShellPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    void Initialize(IServiceCollection services, IConfiguration configuration);
    void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}