using System.Data;
using AdminShell.Contracts;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data;

public static class QuerySpecificationExtensions
{
    public static async Task<IReadOnlyList<T>> QueryBySpecAsync<T>(
        this IDbConnection connection,
        QuerySpecification spec,
        string tableName,
        Action<Query>? customWhere = null,
        string? defaultSortColumn = "CreatedAt",
        string? defaultSortDir = "ASC",
        Func<IDbConnection, T, CancellationToken, Task>? postProcess = null,
        CancellationToken ct = default)
    {
        var qf = new QueryFactory(connection, new SqlServerCompiler());

        var query = new Query(tableName);
        customWhere?.Invoke(query);

        foreach (var filter in spec.Filters)
        {
            query.WhereLike(filter.Field, $"%{filter.Value}%");
        }

        if (spec.Sorts.Count > 0)
        {
            var sort = spec.Sorts[0];
            query.OrderBy(sort.Field, sort.Order == SortOrder.Descending ? "desc" : "asc");
        }
        else if (defaultSortColumn is not null)
        {
            query.OrderBy(defaultSortColumn, defaultSortDir?.ToLower() == "desc" ? "desc" : "asc");
        }

        if (spec.Take > 0)
            query.Limit(spec.Take).Offset(spec.Skip);

        var result = (await qf.GetAsync<T>(query, null, null, ct)).ToList();

        if (postProcess is not null)
        {
            foreach (var item in result)
            {
                await postProcess(connection, item, ct);
            }
        }

        return result;
    }
}
