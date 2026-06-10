using AdminShell.Contracts;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AdminShell.Infrastructure.PluginSystem;

/// <summary>
/// Registry that collects all extension contributions from loaded plugins.
/// Queries each plugin for supported extension interfaces on Refresh().
/// </summary>
public class PluginExtensionRegistry : IPluginExtensionRegistry
{
    private readonly IEnumerable<IAdminShellPlugin> _plugins;
    private readonly ILogger<PluginExtensionRegistry> _logger;

    private List<WidgetDescriptor> _widgets = new();
    private List<TabDescriptor> _tabs = new();
    private List<FormFieldDescriptor> _formFields = new();
    private List<HeaderActionDescriptor> _headerActions = new();
    private List<ReportDescriptor> _reports = new();
    private List<SidebarSectionDescriptor> _sidebarSections = new();
    private List<MenuItem> _menuItems = new();
    private List<PageResourceDescriptor> _pageResources = new();
    private List<IHealthContributor> _healthContributors = new();
    private List<ISearchProviderPlugin> _searchProviders = new();
    private List<IDataPlugin> _dataPlugins = new();

    public PluginExtensionRegistry(
        IEnumerable<IAdminShellPlugin> plugins,
        ILogger<PluginExtensionRegistry> logger)
    {
        _plugins = plugins;
        _logger = logger;
        Refresh();
    }

    public void Refresh()
    {
        _widgets = new List<WidgetDescriptor>();
        _tabs = new List<TabDescriptor>();
        _formFields = new List<FormFieldDescriptor>();
        _headerActions = new List<HeaderActionDescriptor>();
        _reports = new List<ReportDescriptor>();
        _sidebarSections = new List<SidebarSectionDescriptor>();
        _menuItems = new List<MenuItem>();
        _pageResources = new List<PageResourceDescriptor>();
        _healthContributors = new List<IHealthContributor>();
        _searchProviders = new List<ISearchProviderPlugin>();
        _dataPlugins = new List<IDataPlugin>();

        foreach (var plugin in _plugins)
        {
            try
            {
                CollectFromPlugin(plugin);
                _logger.LogDebug("Collected extensions from plugin {PluginId} ({PluginName})", plugin.Id, plugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect extensions from plugin {PluginId}", plugin.Id);
            }
        }

        var counts = (
            widgets: _widgets.Count,
            tabs: _tabs.Count,
            fields: _formFields.Count,
            actions: _headerActions.Count,
            reports: _reports.Count,
            sections: _sidebarSections.Count,
            menuItems: _menuItems.Count,
            resources: _pageResources.Count,
            health: _healthContributors.Count,
            search: _searchProviders.Count,
            data: _dataPlugins.Count
        );
        _logger.LogInformation(
            "Extension registry refreshed: {Widgets} widgets, {Tabs} tabs, {FormFields} form fields, " +
            "{HeaderActions} header actions, {Reports} reports, {SidebarSections} sidebar sections, " +
            "{MenuItems} menu items, {PageResources} page resources, {HealthContributors} health contributors, " +
            "{SearchProviders} search providers, {DataPlugins} data plugins",
            counts.widgets, counts.tabs, counts.fields, counts.actions, counts.reports,
            counts.sections, counts.menuItems, counts.resources, counts.health, counts.search, counts.data);
    }

    private void CollectFromPlugin(IAdminShellPlugin plugin)
    {
        if (plugin is IWidgetPlugin widgetPlugin)
            _widgets.AddRange(widgetPlugin.GetWidgets());

        if (plugin is ITabPlugin tabPlugin)
            _tabs.AddRange(tabPlugin.GetTabs());

        if (plugin is IFormFieldPlugin fieldPlugin)
            _formFields.AddRange(fieldPlugin.GetFormFields());

        if (plugin is IHeaderActionPlugin headerActionPlugin)
            _headerActions.AddRange(headerActionPlugin.GetHeaderActions());

        if (plugin is IReportPlugin reportPlugin)
            _reports.AddRange(reportPlugin.GetReports());

        if (plugin is ISidebarSectionPlugin sectionPlugin)
            _sidebarSections.AddRange(sectionPlugin.GetSidebarSections());

        if (plugin is IMenuPlugin menuPlugin)
            _menuItems.AddRange(menuPlugin.GetMenuItems());

        if (plugin is IPageExtensionPlugin pageExtPlugin)
            _pageResources.AddRange(pageExtPlugin.GetPageResources());

        if (plugin is IHealthContributor healthContributor)
            _healthContributors.Add(healthContributor);

        if (plugin is ISearchProviderPlugin searchPlugin)
            _searchProviders.Add(searchPlugin);

        if (plugin is IDataPlugin dataPlugin)
            _dataPlugins.Add(dataPlugin);
    }

    public IEnumerable<WidgetDescriptor> GetWidgets() => _widgets.AsReadOnly();
    public IEnumerable<TabDescriptor> GetTabs() => _tabs.AsReadOnly();
    public IEnumerable<FormFieldDescriptor> GetFormFields() => _formFields.AsReadOnly();
    public IEnumerable<HeaderActionDescriptor> GetHeaderActions() => _headerActions.AsReadOnly();
    public IEnumerable<ReportDescriptor> GetReports() => _reports.AsReadOnly();
    public IEnumerable<SidebarSectionDescriptor> GetSidebarSections() => _sidebarSections.AsReadOnly();
    public IEnumerable<MenuItem> GetMenuItems() => _menuItems.AsReadOnly();
    public IEnumerable<PageResourceDescriptor> GetPageResources() => _pageResources.AsReadOnly();
    public IEnumerable<IHealthContributor> GetHealthContributors() => _healthContributors.AsReadOnly();
    public IEnumerable<ISearchProviderPlugin> GetSearchProviders() => _searchProviders.AsReadOnly();
    public IEnumerable<IDataPlugin> GetDataPlugins() => _dataPlugins.AsReadOnly();

    /// <summary>
    /// Executes migrations for all IDataPlugin plugins.
    /// Called during database initialization.
    /// </summary>
    public async Task ApplyAllMigrationsAsync(IDbConnection connection, CancellationToken ct = default)
    {
        foreach (var dataPlugin in _dataPlugins)
        {
            try
            {
                await dataPlugin.ApplyMigrationsAsync(connection, ct);
                _logger.LogInformation("Applied migrations for data plugin {PluginId}", dataPlugin.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply migrations for plugin {PluginId}", dataPlugin.Id);
            }
        }
    }

    public ExtensionRegistrySnapshot GetSnapshot()
    {
        return new ExtensionRegistrySnapshot(
            _widgets.ToList(),
            _tabs.ToList(),
            _formFields.ToList(),
            _headerActions.ToList(),
            _reports.ToList(),
            _sidebarSections.ToList(),
            _menuItems.ToList(),
            _pageResources.ToList()
        );
    }
}