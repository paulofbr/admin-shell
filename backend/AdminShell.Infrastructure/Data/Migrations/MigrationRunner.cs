using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Data.Migrations;

public static class MigrationRunner
{
    private static readonly Assembly Assembly = typeof(MigrationRunner).Assembly;

    public static DatabaseUpgradeResult RunMigrations(string connectionString, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly, script =>
                script.StartsWith($"{Assembly.GetName().Name}.Data.Migrations.Scripts.", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .LogTo(new DbUpLogger(logger))
            .Build();

        var result = upgrader.PerformUpgrade();

        if (result.Successful)
        {
            logger.LogInformation("DbUp migrations applied successfully");
        }
        else
        {
            logger.LogError(result.Error, "DbUp migration failed");
        }

        return result;
    }

    /// <summary>
    /// Returns only new scripts that haven't been run yet (for informational purposes).
    /// </summary>
    public static IEnumerable<string> GetPendingScripts(string connectionString, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly, script =>
                script.StartsWith($"{Assembly.GetName().Name}.Data.Migrations.Scripts.", StringComparison.OrdinalIgnoreCase))
            .LogTo(new DbUpLogger(logger))
            .Build();

        return upgrader.GetScriptsToExecute().Select(s => s.Name);
    }

    private sealed class DbUpLogger(ILogger logger) : DbUp.Engine.Output.IUpgradeLog
    {
        public void WriteInformation(string format, params object[] args)
            => logger.LogInformation(format, args);

        public void WriteError(string format, params object[] args)
            => logger.LogError(format, args);

        public void WriteWarning(string format, params object[] args)
            => logger.LogWarning(format, args);
    }
}
