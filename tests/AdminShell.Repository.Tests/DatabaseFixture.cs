using AdminShell.Infrastructure.Data;
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
        var initializer = new DatabaseInitializer(ConnectionFactory, logger);
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
}