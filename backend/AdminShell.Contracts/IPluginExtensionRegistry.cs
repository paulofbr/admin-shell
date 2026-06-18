namespace AdminShell.Contracts;

/// <summary>
/// Central registry that collects all extension contributions from loaded plugins.
/// Provides a single source of truth for the frontend to discover what plugins contribute.
/// </summary>
public interface IPluginExtensionRegistry
{
    IEnumerable<WidgetDescriptor> GetWidgets();
    IEnumerable<TabDescriptor> GetTabs();
    IEnumerable<FormFieldDescriptor> GetFormFields();
    IEnumerable<HeaderActionDescriptor> GetHeaderActions();
    IEnumerable<ReportDescriptor> GetReports();
    IEnumerable<SidebarSectionDescriptor> GetSidebarSections();
    IEnumerable<MenuItem> GetMenuItems();
    IEnumerable<PageResourceDescriptor> GetPageResources();
    IEnumerable<IHealthContributor> GetHealthContributors();
    IEnumerable<ISearchProviderPlugin> GetSearchProviders();
    IEnumerable<IDataPlugin> GetDataPlugins();
    IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields();
    IReadOnlyList<EntityExtensionFieldDefinition> GetExtensionFieldsForEntity(string entityName);
    void Refresh();
    ExtensionRegistrySnapshot GetSnapshot();
    Task ApplyAllMigrationsAsync(IDbConnection connection, CancellationToken ct = default);
}

/// <summary>
/// Serializable snapshot of all extension contributions.
/// Note: IHealthContributor and ISearchProviderPlugin instances are NOT included
/// in the snapshot since they cannot be serialized — use the typed methods instead.
/// </summary>
public record ExtensionRegistrySnapshot(
    List<WidgetDescriptor> Widgets,
    List<TabDescriptor> Tabs,
    List<FormFieldDescriptor> FormFields,
    List<HeaderActionDescriptor> HeaderActions,
    List<ReportDescriptor> Reports,
    List<SidebarSectionDescriptor> SidebarSections,
    List<MenuItem> MenuItems,
    List<PageResourceDescriptor> PageResources
);