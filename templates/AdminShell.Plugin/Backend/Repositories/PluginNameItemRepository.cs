using AdminShell.Contracts;
using AdminShell.Infrastructure.Data;
using Dapper;
using PluginName.Entities;
using PluginName.Repositories;

namespace PluginName.Repositories;

public sealed class PluginNameItemRepository : IPluginNameItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PluginNameItemRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PluginNameItem>> ListAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        return (await db.QueryAsync<PluginNameItem>(
            """
            SELECT Id, Name, Description, IsActive, CreatedAt
            FROM PluginNameItems
            ORDER BY Name
            """)).ToList();
    }
}
