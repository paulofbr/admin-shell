using AdminShell.Contracts;

namespace AdminShell.Core.Interfaces;

public interface IPluginLoader
{
    IReadOnlyList<PluginDescriptor> LoadedPlugins { get; }
    IReadOnlyList<IAdminShellPlugin> GetPluginInstances();
    IReadOnlyList<IWidgetPlugin> GetWidgetPlugins();
    IReadOnlyList<IMenuPlugin> GetMenuPlugins();
    Task LoadPluginsAsync(string pluginsDirectory, CancellationToken ct = default);
    Task<PluginDescriptor?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default);
    void InitializePlugins(IServiceCollection services, IConfiguration configuration);
    void ConfigurePlugins(IApplicationBuilder app, IWebHostEnvironment env);
    void MapPluginEndpoints(IApplicationBuilder app);
    Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default);
    Task<bool> DisablePluginAsync(string pluginId, CancellationToken ct = default);
}