using AdminShell.Contracts;
using AdminShell.Infrastructure.PluginSystem;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdminShell.PluginLoader.Tests;

public class PluginSystemContractTests
{
    [Fact]
    public void AdminShellPluginBase_UsesDefaults_ForMetadataAndLifecycle()
    {
        var plugin = new TestPlugin();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var app = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() }).Build();
        var env = app.Environment;

        plugin.Id.Should().Be("test-plugin");
        plugin.Name.Should().Be("Test");
        plugin.Version.Should().Be("1.0.0");
        plugin.Description.Should().BeEmpty();

        var act = () => plugin.Initialize(services, configuration);
        act.Should().NotThrow();

        var configure = () => plugin.Configure(app, env);
        configure.Should().NotThrow();
    }

    [Fact]
    public void PluginComponentMetadata_GetPluginId_ReadsPluginId_FromAttribute()
    {
        var component = new AuditComponentForTests();

        PluginComponentMetadata.GetPluginId(component).Should().Be("user-audit");
    }

    [Fact]
    public void PluginComponentMetadata_GetPluginId_Throws_WhenAttributeIsMissing()
    {
        var component = new ComponentWithoutAttribute();

        var act = () => PluginComponentMetadata.GetPluginId(component);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdminShellPluginBase_DerivesName_ByRemovingPluginSuffix()
    {
        new UserDepartmentPluginForTests().Name.Should().Be("User Department");
        new ReportingPluginForTests().Name.Should().Be("Reporting");
        new OnlyPluginForTests().Name.Should().Be("Only");
    }

    [Fact]
    public async Task PluginEndpointExtensions_MapsGroup_WithPluginApiPrefix()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        var group = app.MapPluginApi("user-audit");
        group.MapGet("/health", () => "ok");

        await app.StartAsync();
        try
        {
            var endpoint = app.Services
                .GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>()
                .Single(e => e.RoutePattern.RawText!.StartsWith("/api/plugins/user-audit", StringComparison.Ordinal)
                             && e.RoutePattern.RawText!.Contains("health", StringComparison.Ordinal));

            endpoint.Should().NotBeNull();
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PluginEndpointExtensions_RejectsEmptyPluginId(string pluginId)
    {
        var app = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() }).Build();

        var act = () => app.MapPluginApi(pluginId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PluginEndpointExtensions_RejectsNullPluginId()
    {
        var app = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() }).Build();

        var act = () => app.MapPluginApi(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PluginQueryRegistry_RegisterAndGetQuery_ReturnsRegisteredSql()
    {
        var registry = new PluginQueryRegistry();

        registry.Register("users.by-department", "SELECT * FROM Users WHERE DepartmentId = @DepartmentId");

        registry.GetQuery("users.by-department")
            .Should().Be("SELECT * FROM Users WHERE DepartmentId = @DepartmentId");
    }

    [Fact]
    public void PluginQueryRegistry_GetQuery_ReturnsNull_ForMissingKey()
    {
        var registry = new PluginQueryRegistry();

        registry.GetQuery("missing-key").Should().BeNull();
    }

    [Fact]
    public void PluginQueryRegistry_Register_OverwritesExistingKey()
    {
        var registry = new PluginQueryRegistry();

        registry.Register("audit.latest", "SELECT 1");
        registry.Register("audit.latest", "SELECT 2");

        registry.GetQuery("audit.latest").Should().Be("SELECT 2");
    }

    [Fact]
    public void PluginQueryRegistry_ListQueries_ReturnsKeysSortedCaseInsensitively()
    {
        var registry = new PluginQueryRegistry();

        registry.Register("zeta", "SELECT 3");
        registry.Register("Alpha", "SELECT 1");
        registry.Register("beta", "SELECT 2");

        registry.ListQueries().Should().Equal("Alpha", "beta", "zeta");
    }

    [Theory]
    [InlineData(null, "SELECT 1")]
    [InlineData("", "SELECT 1")]
    [InlineData(" ", "SELECT 1")]
    [InlineData("query", null)]
    [InlineData("query", "")]
    [InlineData("query", " ")]
    public void PluginQueryRegistry_Register_RejectsEmptyKeyOrSql(string? key, string? sql)
    {
        var registry = new PluginQueryRegistry();

        var act = () => registry.Register(key!, sql!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PluginQueryRegistry_GetQuery_RejectsEmptyKey(string? key)
    {
        var registry = new PluginQueryRegistry();

        var act = () => registry.GetQuery(key!);

        act.Should().Throw<ArgumentException>();
    }

    private sealed class TestPlugin : AdminShellPluginBase
    {
        public override string Id => "test-plugin";
    }

    [PluginComponent("user-audit")]
    private sealed class AuditComponentForTests : IMenuPlugin
    {
        public IEnumerable<MenuItem> GetMenuItems()
            => Enumerable.Empty<MenuItem>();
    }

    private sealed class ComponentWithoutAttribute : IMenuPlugin
    {
        public IEnumerable<MenuItem> GetMenuItems()
            => Enumerable.Empty<MenuItem>();
    }

    private sealed class UserDepartmentPluginForTests : AdminShellPluginBase
    {
        public override string Id => "user-department";
    }

    private sealed class ReportingPluginForTests : AdminShellPluginBase
    {
        public override string Id => "reporting";
    }

    private sealed class OnlyPluginForTests : AdminShellPluginBase
    {
        public override string Id => "only";
    }
}
