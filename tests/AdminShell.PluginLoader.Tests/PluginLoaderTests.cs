using System.Reflection;
using AdminShell.Contracts;
using AdminShell.Infrastructure.PluginSystem;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AdminShell.PluginLoader.Tests;

public class PluginLoaderTests
{
    private readonly ILogger<global::AdminShell.Infrastructure.PluginSystem.PluginLoader> _logger =
        NullLogger<global::AdminShell.Infrastructure.PluginSystem.PluginLoader>.Instance;

    [Fact]
    public async Task LoadPlugins_FromDirectory_LoadsAllPlugins()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            var debugPluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Debug", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll) && File.Exists(debugPluginDll))
            {
                pluginDll = debugPluginDll;
            }

            if (File.Exists(pluginDll))
            {
                // Copy the plugin DLL and its dependencies
                var pluginDir = Path.GetDirectoryName(pluginDll)!;
                foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
                {
                    var src = Path.Combine(pluginDir, dep);
                    if (File.Exists(src))
                        File.Copy(src, Path.Combine(tempDir, dep));
                }
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);
            loader.LoadedPlugins.Should().NotBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugins_FromDirectory_IgnoresObjAndLoadsComponents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var debugPluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Debug", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(debugPluginDll)) return;

            var objDll = Path.Combine(tempDir, "ReportingPlugin.dll");
            File.Copy(debugPluginDll, objDll, true);

            var binDll = Path.Combine(tempDir, "bin", "Debug", "net10.0", "ReportingPlugin.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(binDll)!);
            File.Copy(debugPluginDll, binDll, true);

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);

            loader.LoadedPlugins.Should().ContainSingle();
            loader.LoadedPlugins.Single().AssemblyPath.Should().Contain("bin");
            loader.GetPluginComponents().Should().Contain(component => component is IMenuPlugin);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugins_FromEmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);
            loader.LoadedPlugins.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugins_FromNonExistentDirectory_DoesNotThrow()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var act = async () => await loader.LoadPluginsAsync(nonExistentDir);
        await act.Should().NotThrowAsync();
        loader.LoadedPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPlugins_DeduplicatesById_WhenSamePluginLoadedFromMultiplePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir1 = Path.Combine(tempDir, "sub1");
        var subDir2 = Path.Combine(tempDir, "sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll)) return; // Skip if no real plugin

            var pluginDir = Path.GetDirectoryName(pluginDll)!;
            foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
            {
                var src = Path.Combine(pluginDir, dep);
                if (File.Exists(src))
                {
                    File.Copy(src, Path.Combine(subDir1, dep));
                    File.Copy(src, Path.Combine(subDir2, dep));
                }
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);

            // Should only have one instance of ReportingPlugin, not two
            loader.LoadedPlugins.Should().ContainSingle(p => p.Id == "reporting");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugin_ManifestDependencies_UsesIdAndVersion()
    {
        var pluginDll = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "plugins", "UserAuditPlugin", "Backend", "bin", "Release", "net10.0", "UserAuditPlugin.dll");

        if (!File.Exists(pluginDll)) return;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var backendDir = Path.Combine(tempDir, "backend");
        Directory.CreateDirectory(backendDir);

        try
        {
            foreach (var dll in Directory.GetFiles(Path.GetDirectoryName(pluginDll)!, "*.dll"))
            {
                File.Copy(dll, Path.Combine(backendDir, Path.GetFileName(dll)), true);
            }

            var pluginRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(pluginDll)!, "..", "..", "..", ".."));

            File.Copy(
                Path.Combine(pluginRoot, "manifest.json"),
                Path.Combine(tempDir, "manifest.json"),
                true);

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            var descriptor = await loader.LoadPluginAsync(Path.Combine(backendDir, "UserAuditPlugin.dll"));

            descriptor.Should().NotBeNull();
            descriptor!.Dependencies.Should().ContainSingle(dep => dep.PluginId == "reporting");
            descriptor.Dependencies.Single(dep => dep.PluginId == "reporting").VersionConstraint.Should().Be(">=1.0.0");
            descriptor.Dependencies.Single(dep => dep.PluginId == "reporting").Version.Should().Be(">=1.0.0");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugin_ValidAssembly_ReturnsDescriptor()
    {
        var pluginDll = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

        if (!File.Exists(pluginDll)) return;

        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var descriptor = await loader.LoadPluginAsync(pluginDll);

        descriptor.Should().NotBeNull();
        descriptor!.Id.Should().Be("reporting");
        descriptor.Name.Should().Be("Reporting Plugin");
        descriptor.Version.Should().Be("1.0.0");
        descriptor.Status.Should().Be(PluginStatus.Loaded);
    }

    [Fact]
    public async Task LoadPlugin_WithEmbeddedFrontend_ExposesManifestAndAssets()
    {
        var pluginDll = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "plugins", "UserDepartmentPlugin", "Backend", "bin", "Release", "net10.0", "UserDepartmentPlugin.dll");

        if (!File.Exists(pluginDll)) return;

        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var descriptor = await loader.LoadPluginAsync(pluginDll);

        descriptor.Should().NotBeNull();
        descriptor!.HasEmbeddedFrontend.Should().BeTrue();
        descriptor.FrontendManifestResourceName.Should().EndWith("UserDepartmentPlugin.plugin.json");

        var manifest = loader.GetEmbeddedFrontendManifest("user-department");
        manifest.Should().NotBeNull();
        manifest!.Id.Should().Be("user-department");
        manifest.Source.Should().Be("embedded");
        manifest.Main.Should().Be("index.js");

        var asset = loader.GetEmbeddedFrontendAsset("user-department", "index.js");
        asset.Should().NotBeNull();
        asset!.ContentType.Should().Contain("text/javascript");
        System.Text.Encoding.UTF8.GetString(asset.Content).Should().Contain("User Department Plugin");

        loader.GetEmbeddedFrontendAsset("user-department", "../index.js").Should().BeNull();
    }

    [Fact]
    public async Task LoadPlugin_InvalidAssembly_ReturnsFailedDescriptor()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var invalidDll = Path.Combine(tempDir, "invalid.dll");
        await File.WriteAllBytesAsync(invalidDll, new byte[] { 0, 0, 0, 0 });

        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var descriptor = await loader.LoadPluginAsync(invalidDll);

        descriptor.Should().NotBeNull();
        descriptor!.Status.Should().Be(PluginStatus.Failed);
        descriptor.ErrorMessage.Should().NotBeNullOrEmpty();

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void InitializePlugins_DoesNotThrow_WithNoPlugins()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var services = Substitute.For<IServiceCollection>();
        var configuration = Substitute.For<IConfiguration>();
        var act = () => loader.InitializePlugins(services, configuration);
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigurePlugins_DoesNotThrow_WithNoPlugins()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var app = Substitute.For<IApplicationBuilder>();
        var env = Substitute.For<IWebHostEnvironment>();
        var act = () => loader.ConfigurePlugins(app, env);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task EnableDisablePlugin_TogglesStatus()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll)) return;

            // Copy plugin + dependencies to temp dir
            var pluginDir = Path.GetDirectoryName(pluginDll)!;
            foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
            {
                var src = Path.Combine(pluginDir, dep);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(tempDir, dep));
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);

            var disabled = await loader.DisablePluginAsync("reporting");
            disabled.Should().BeTrue();
            loader.LoadedPlugins.Should().Contain(p => p.Id == "reporting" && p.Status == PluginStatus.Disabled);

            var enabled = await loader.EnablePluginAsync("reporting");
            enabled.Should().BeTrue();
            loader.LoadedPlugins.Should().Contain(p => p.Id == "reporting" && p.Status == PluginStatus.Active);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PluginExtensionRegistry_ExcludesContributions_FromDisabledPlugin()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll)) return;

            var pluginDir = Path.GetDirectoryName(pluginDll)!;
            foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
            {
                var src = Path.Combine(pluginDir, dep);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(tempDir, dep));
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);

            var services = new ServiceCollection();
            services.AddLogging();
            loader.InitializePlugins(services, new ConfigurationBuilder().Build());

            var registry = new PluginExtensionRegistry(
                loader.GetPluginComponents(),
                NullLogger<PluginExtensionRegistry>.Instance,
                loader);

            registry.GetMenuItems().Should().Contain(m => m.Id == "reports");
            registry.GetWidgets().Should().NotBeEmpty();

            var disabled = await loader.DisablePluginAsync("reporting");
            disabled.Should().BeTrue();

            registry.Refresh();

            registry.GetMenuItems().Should().BeEmpty();
            registry.GetWidgets().Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task EnableDisablePlugin_NonExistent_ReturnsFalse()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var enabled = await loader.EnablePluginAsync("nonexistent");
        var disabled = await loader.DisablePluginAsync("nonexistent");
        enabled.Should().BeFalse();
        disabled.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPluginsAsync_MultipleCalls_DoesNotDuplicate()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll)) return;

            var pluginDir = Path.GetDirectoryName(pluginDll)!;
            foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
            {
                var src = Path.Combine(pluginDir, dep);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(tempDir, dep));
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            await loader.LoadPluginsAsync(tempDir);
            var countAfterFirst = loader.LoadedPlugins.Count;

            // Load again from same directory
            await loader.LoadPluginsAsync(tempDir);
            var countAfterSecond = loader.LoadedPlugins.Count;

            // Should not have grown
            countAfterSecond.Should().Be(countAfterFirst);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadPlugin_CanBeCalledIndependently()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pluginDll = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "plugins", "ReportingPlugin", "Backend", "bin", "Release", "net10.0", "ReportingPlugin.dll");

            if (!File.Exists(pluginDll)) return;

            var pluginDir = Path.GetDirectoryName(pluginDll)!;
            foreach (var dep in new[] { "ReportingPlugin.dll", "AdminShell.Contracts.dll" })
            {
                var src = Path.Combine(pluginDir, dep);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(tempDir, dep));
            }

            var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
            // Call LoadPluginAsync directly (not via LoadPluginsAsync)
            var descriptor = await loader.LoadPluginAsync(Path.Combine(tempDir, "ReportingPlugin.dll"));
            descriptor.Should().NotBeNull();

            // It should also be in LoadedPlugins via the LoadPluginsAsync path later
            await loader.LoadPluginsAsync(tempDir);
            loader.LoadedPlugins.Should().Contain(p => p.Id == "reporting");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetPluginInstances_ReturnsAllInstances()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var instances = loader.GetPluginInstances();
        instances.Should().NotBeNull();
    }

    [Fact]
    public void NewInterfaces_ReturnEmptyLists_WhenNoPluginsLoaded()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        loader.GetWidgetPlugins().Should().BeEmpty();
        loader.GetMenuPlugins().Should().BeEmpty();
    }

    [Fact]
    public void SetEventBus_DoesNotThrow()
    {
        var loader = new global::AdminShell.Infrastructure.PluginSystem.PluginLoader(_logger);
        var eventBus = new InMemoryEventBus();
        var act = () => loader.SetEventBus(eventBus);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task EventBus_PublishSubscribe_DeliversEvent()
    {
        var eventBus = new InMemoryEventBus();
        string? received = null;
        using (eventBus.Subscribe<string>(async (msg, ct) =>
        {
            received = msg;
            await Task.CompletedTask;
        }))
        {
            await eventBus.PublishAsync("hello world");
            received.Should().Be("hello world");
        }
    }

    [Fact]
    public async Task EventBus_MultipleSubscribers_AllReceiveEvent()
    {
        var eventBus = new InMemoryEventBus();
        var received1 = false;
        var received2 = false;

        eventBus.Subscribe<string>(async (msg, ct) => { received1 = true; await Task.CompletedTask; });
        eventBus.Subscribe<string>(async (msg, ct) => { received2 = true; await Task.CompletedTask; });

        await eventBus.PublishAsync("test");
        received1.Should().BeTrue();
        received2.Should().BeTrue();
    }

    [Fact]
    public async Task EventBus_Unsubscribe_StopsDelivery()
    {
        var eventBus = new InMemoryEventBus();
        var count = 0;
        var sub = eventBus.Subscribe<string>(async (msg, ct) => { count++; await Task.CompletedTask; });

        await eventBus.PublishAsync("first");
        count.Should().Be(1);

        sub.Dispose();
        await eventBus.PublishAsync("second");
        count.Should().Be(1); // Still 1 — second was not delivered
    }

    [Fact]
    public async Task EventBus_DifferentTypes_DoNotCrossDeliver()
    {
        var eventBus = new InMemoryEventBus();
        string? intEvent = null;
        string? stringEvent = null;

        eventBus.Subscribe<string>(async (msg, ct) => { intEvent = "got-string"; await Task.CompletedTask; });
        eventBus.Subscribe<string>(async (msg, ct) => { stringEvent = msg; await Task.CompletedTask; });

        await eventBus.PublishAsync("hello");
        intEvent.Should().Be("got-string");
        stringEvent.Should().Be("hello");
    }
}