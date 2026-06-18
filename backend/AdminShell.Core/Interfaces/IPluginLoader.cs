using AdminShell.Contracts;

namespace AdminShell.Core.Interfaces;

public interface IPluginLoader
{
    IReadOnlyList<PluginDescriptor> LoadedPlugins { get; }
    IReadOnlyList<IAdminShellPlugin> GetPluginInstances();
    IReadOnlyList<IPluginComponent> GetPluginComponents();
    IReadOnlyList<IManagedEntityProvider> GetManagedEntityProviders();
    IReadOnlyList<IWidgetPlugin> GetWidgetPlugins();
    IReadOnlyList<IMenuPlugin> GetMenuPlugins();
    EmbeddedFrontendManifest? GetEmbeddedFrontendManifest(string pluginId);
    EmbeddedFrontendAsset? GetEmbeddedFrontendAsset(string pluginId, string path);
    Task LoadPluginsAsync(string pluginsDirectory, CancellationToken ct = default);
    Task<PluginDescriptor?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default);
    void InitializePlugins(IServiceCollection services, IConfiguration configuration);
    void ConfigurePlugins(IApplicationBuilder app, IWebHostEnvironment env);
    void MapPluginEndpoints(IApplicationBuilder app);
    void RefreshPluginEndpoints();
    Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default);
    Task<bool> DisablePluginAsync(string pluginId, CancellationToken ct = default);
}