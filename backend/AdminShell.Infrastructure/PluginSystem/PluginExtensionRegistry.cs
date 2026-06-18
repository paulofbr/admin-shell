using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AdminShell.Infrastructure.PluginSystem;

public sealed record EntityExtensionFieldRegistration(string PluginId, EntityExtensionFieldDefinition Definition);

/// <summary>
/// Registry that collects all extension contributions from loaded plugins.
/// Queries each plugin for supported extension interfaces on Refresh().
/// </summary>
public class PluginExtensionRegistry : IPluginExtensionRegistry
{
    private readonly IEnumerable<IPluginComponent> _components;
    private readonly IPluginLoader? _pluginLoader;
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
    private List<EntityExtensionFieldRegistration> _extensionFields = new();

    public PluginExtensionRegistry(
        IEnumerable<IPluginComponent> components,
        ILogger<PluginExtensionRegistry> logger,
        IPluginLoader? pluginLoader = null)
    {
        _components = components;
        _pluginLoader = pluginLoader;
        _logger = logger;
        Refresh();
    }

    public PluginExtensionRegistry(
        IEnumerable<IAdminShellPlugin> plugins,
        ILogger<PluginExtensionRegistry> logger,
        IPluginLoader? pluginLoader = null)
        : this(plugins.OfType<IPluginComponent>(), logger, pluginLoader)
    {
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
        _extensionFields = new List<EntityExtensionFieldRegistration>();

        var activePluginIds = _pluginLoader?.LoadedPlugins
            .Where(p => p.Status == PluginStatus.Active || p.Status == PluginStatus.Loaded)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var components = _pluginLoader?.GetPluginComponents() ?? _components;
        foreach (var component in components)
        {
            var pluginId = PluginComponentMetadata.GetPluginId(component);

            if (activePluginIds is not null && !activePluginIds.Contains(pluginId))
                continue;

            try
            {
                CollectFromComponent(component, pluginId);
                _logger.LogDebug("Collected extensions from component {ComponentType} for plugin {PluginId}",
                    component.GetType().Name,
                    pluginId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect extensions from component {ComponentType} for plugin {PluginId}",
                    component.GetType().Name,
                    pluginId);
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
            extensionFields: _extensionFields.Count,
            health: _healthContributors.Count,
            search: _searchProviders.Count,
            data: _dataPlugins.Count
        );
        _logger.LogInformation(
            "Extension registry refreshed: {Widgets} widgets, {Tabs} tabs, {FormFields} form fields, " +
            "{HeaderActions} header actions, {Reports} reports, {SidebarSections} sidebar sections, " +
            "{MenuItems} menu items, {PageResources} page resources, {ExtensionFields} extension fields, " +
            "{HealthContributors} health contributors, {SearchProviders} search providers, {DataPlugins} data plugins",
            counts.widgets, counts.tabs, counts.fields, counts.actions, counts.reports,
            counts.sections, counts.menuItems, counts.resources, counts.extensionFields, counts.health, counts.search, counts.data);
    }

    private void CollectFromComponent(IPluginComponent component, string pluginId)
    {
        if (component is IWidgetPlugin widgetPlugin)
            _widgets.AddRange(widgetPlugin.GetWidgets());

        if (component is ITabPlugin tabPlugin)
            _tabs.AddRange(tabPlugin.GetTabs());

        if (component is IFormFieldPlugin fieldPlugin)
            _formFields.AddRange(fieldPlugin.GetFormFields());

        if (component is IHeaderActionPlugin headerActionPlugin)
            _headerActions.AddRange(headerActionPlugin.GetHeaderActions());

        if (component is IReportPlugin reportPlugin)
            _reports.AddRange(reportPlugin.GetReports());

        if (component is ISidebarSectionPlugin sectionPlugin)
            _sidebarSections.AddRange(sectionPlugin.GetSidebarSections());

        if (component is IMenuPlugin menuPlugin)
            _menuItems.AddRange(menuPlugin.GetMenuItems());

        if (component is IPageExtensionPlugin pageExtPlugin)
            _pageResources.AddRange(pageExtPlugin.GetPageResources());

        if (component is IExtensionFieldPlugin extensionFieldPlugin)
            _extensionFields.AddRange(extensionFieldPlugin.GetExtensionFields()
                .Select(field => new EntityExtensionFieldRegistration(pluginId, field)));

        if (component is IHealthContributor healthContributor)
            _healthContributors.Add(healthContributor);

        if (component is ISearchProviderPlugin searchPlugin)
            _searchProviders.Add(searchPlugin);

        if (component is IDataPlugin dataPlugin)
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

    public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
        => _extensionFields.Select(r => r.Definition).ToList().AsReadOnly();

    public IReadOnlyList<EntityExtensionFieldDefinition> GetExtensionFieldsForEntity(string entityName)
        => _extensionFields
            .Where(r => string.Equals(r.Definition.EntityName, entityName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(EntityExtensionFieldDefinition.GetTableName(r.Definition.EntityName), EntityExtensionFieldDefinition.GetTableName(entityName), StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Definition)
            .OrderBy(r => r.Order)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Creates extension field columns for loaded plugins.
    /// Definitions stay in plugin code; only entity columns are created automatically.
    /// Called during database initialization and plugin installation.
    /// </summary>
    public async Task ApplyAllMigrationsAsync(IDbConnection connection, CancellationToken ct = default)
    {
        foreach (var dataPlugin in _dataPlugins)
        {
            try
            {
                await dataPlugin.ApplyMigrationsAsync(connection, ct);
                _logger.LogInformation("Applied migrations for data plugin {PluginId}", PluginComponentMetadata.GetPluginId(dataPlugin));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply migrations for plugin {PluginId}", PluginComponentMetadata.GetPluginId(dataPlugin));
            }
        }

        foreach (var registration in _extensionFields)
        {
            try
            {
                await EnsureExtensionFieldColumnAsync(connection, registration.Definition, ct);
                _logger.LogInformation("Ensured extension field {EntityName}.{ColumnName} for plugin {PluginId}",
                    registration.Definition.EntityName,
                    registration.Definition.ColumnName,
                    registration.PluginId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure extension field {EntityName}.{ColumnName} for plugin {PluginId}",
                    registration.Definition.EntityName,
                    registration.Definition.ColumnName,
                    registration.PluginId);
            }
        }
    }

    private static async Task EnsureExtensionFieldColumnAsync(IDbConnection connection, EntityExtensionFieldDefinition definition, CancellationToken ct)
    {
        var exists = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName",
            new { definition.TableName, ColumnName = definition.ColumnName });

        if (exists > 0)
            return;

        await connection.ExecuteAsync(
            $"ALTER TABLE {definition.QuotedTableName} ADD {definition.QuotedColumnName} {definition.SqlType} NULL");
    }

    public ExtensionRegistrySnapshot GetSnapshot()
    {
        Refresh();
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
