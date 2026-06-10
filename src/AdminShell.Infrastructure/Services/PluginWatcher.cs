using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Services;

/// <summary>
/// Watches the plugins directory for changes and hot-reloads plugins during development.
/// </summary>
public class PluginWatcher : BackgroundService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly string _pluginsDirectory;
    private readonly ILogger<PluginWatcher> _logger;

    public PluginWatcher(
        IPluginLoader pluginLoader,
        IConfiguration configuration,
        ILogger<PluginWatcher> logger)
    {
        _pluginLoader = pluginLoader;
        // Path resolved by Program.cs and stored in config.Plugins:Directory
        _pluginsDirectory = configuration["Plugins:Directory"] ?? "Plugins";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            _logger.LogInformation("PluginWatcher: directory {Dir} not found, skipping", _pluginsDirectory);
            return;
        }

        _logger.LogInformation("PluginWatcher: watching {Dir} for changes", _pluginsDirectory);

        using var watcher = new FileSystemWatcher(_pluginsDirectory, "*.dll")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        var debounceTimer = new System.Timers.Timer(2000) { AutoReset = false };

        void OnChanged(object s, FileSystemEventArgs e)
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnChanged;

        debounceTimer.Elapsed += async (_, _) =>
        {
            _logger.LogInformation("PluginWatcher: change detected, reloading plugins...");
            try
            {
                // Reload all plugins
                var services = new ServiceCollection();
                var config = new ConfigurationBuilder().Build();
                await _pluginLoader.LoadPluginsAsync(_pluginsDirectory, stoppingToken);
                _pluginLoader.InitializePlugins(services, config);
                _logger.LogInformation("PluginWatcher: plugins reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PluginWatcher: failed to reload plugins");
            }
        };

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }
}
