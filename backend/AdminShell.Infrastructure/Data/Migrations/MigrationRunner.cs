using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Data.Migrations;

/// <summary>
/// Runs SQL migrations using DbUp.
/// Used by IDataPlugin for plugin migrations and core DB schema.
/// </summary>
public static class MigrationRunner
{
    public static bool RunMigrations(string connectionString, string migrationsPath, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(migrationsPath)
            .WithTransaction()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        
        if (result.Successful)
        {
            logger.LogInformation("DbUp migrations applied successfully from {Path}", migrationsPath);
            return true;
        }
        
        logger.LogError("DbUp migration failed: {Error}", result.Error);
        return false;
    }
}