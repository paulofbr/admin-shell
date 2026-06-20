using AdminShell.Contracts;
using AdminShell.Infrastructure.Data;
using AdminShell.Infrastructure.PluginSystem;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;

namespace AdminShell.Repository.Tests;

/// <summary>
/// Shared xUnit fixture that initialises the SQL Server database once
/// and provides transaction-scoped connections for test isolation.
/// Requires the adminshell-sql Docker container to be running on localhost:1434.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }
    public IDbConnectionFactory ConnectionFactory { get; }

    public DatabaseFixture()
    {
        ConnectionString = "Server=localhost,1434;Database=AdminShell;User Id=sa;Password=Admin123!;TrustServerCertificate=true;";
        ConnectionFactory = new SqlConnectionFactory(ConnectionString);
    }

    public async Task InitializeAsync()
    {
        var logger = NullLogger<DatabaseInitializer>.Instance;
        var extensionRegistry = new NoOpExtensionRegistry();
        var managedEntitySchemaManager = new NoOpManagedEntitySchemaManager();
        var permissionDefinitionRegistry = new PermissionDefinitionRegistry();
        var settingsRegistry = new NoOpSettingsRegistry();
        var initializer = new DatabaseInitializer(ConnectionFactory, extensionRegistry, managedEntitySchemaManager, permissionDefinitionRegistry, settingsRegistry, logger);
        await initializer.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Opens a new connection and begins a transaction that will be rolled back
    /// when disposed — perfect for test isolation.
    /// </summary>
    public (IDbConnection Connection, IDbTransaction Transaction) CreateIsolatedConnection()
    {
        var db = ConnectionFactory.CreateConnection();
        db.Open();
        var tx = db.BeginTransaction();
        return (db, tx);
    }

    private sealed class NoOpExtensionRegistry : IPluginExtensionRegistry
    {
        public IEnumerable<WidgetDescriptor> GetWidgets() => [];
        public IEnumerable<TabDescriptor> GetTabs() => [];
        public IEnumerable<FormFieldDescriptor> GetFormFields() => [];
        public IEnumerable<HeaderActionDescriptor> GetHeaderActions() => [];
        public IEnumerable<ReportDescriptor> GetReports() => [];
        public IEnumerable<SidebarSectionDescriptor> GetSidebarSections() => [];
        public IEnumerable<MenuItem> GetMenuItems() => [];
        public IEnumerable<PageResourceDescriptor> GetPageResources() => [];
        public IEnumerable<IHealthContributor> GetHealthContributors() => [];
        public IEnumerable<ISearchProviderPlugin> GetSearchProviders() => [];
        public IEnumerable<IDataPlugin> GetDataPlugins() => [];
        public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields() => [];
        public IReadOnlyList<EntityExtensionFieldDefinition> GetExtensionFieldsForEntity(string entityName) => [];
        public IEnumerable<Type> GetManagedEntityTypes() => [];
        public void Refresh() { }
        public ExtensionRegistrySnapshot GetSnapshot() => new([], [], [], [], [], [], [], []);
        public Task ApplyAllMigrationsAsync(IDbConnection connection, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpManagedEntitySchemaManager : IManagedEntitySchemaManager
    {
        public Task EnsureAsync(IDbConnection connection, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpSettingsRegistry : ISettingsRegistry
    {
        public IReadOnlyList<SettingDefinition> GetAll() => [];
        public IReadOnlyList<SettingDefinition> GetForCategory(string category) => [];
        public Task EnsureDefaultsAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<SettingsResponse> GetSettingsAsync(string category, CancellationToken ct = default)
            => Task.FromResult(new SettingsResponse(category, category, []));
        public Task<SettingsResponse> UpdateAsync(
            string category,
            IEnumerable<UpdateSettingRequest> requests,
            CancellationToken ct = default)
            => Task.FromResult(new SettingsResponse(category, category, []));
        public Task<TSettings> GetOptionsAsync<TSettings>(CancellationToken ct = default)
            where TSettings : class, new()
            => Task.FromResult(new TSettings());
        public Task SetOptionsAsync<TSettings>(TSettings options, CancellationToken ct = default)
            where TSettings : class
            => Task.CompletedTask;
    }
}