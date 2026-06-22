using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using AdminShell.Contracts;
using AdminShell.Core;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data;

public abstract class RepositoryBase<T> : IBaseRepository<T> where T : BaseEntity
{
    protected IDbConnectionFactory ConnectionFactory { get; }
    protected IPluginExtensionRegistry? ExtensionRegistry { get; }
    protected string EntityName { get; }
    protected string TableName { get; }
    protected bool HasExtensionFields => ExtensionRegistry is not null;
    private static readonly bool IsSoftDeletable = typeof(IDeletable).IsAssignableFrom(typeof(T));

    private static readonly HashSet<string> NavigationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExtensionFields"
    };

    protected RepositoryBase(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
    {
        ConnectionFactory = connectionFactory;
        ExtensionRegistry = extensionRegistry;
        EntityName = ResolveEntityName();
        TableName = ResolveTableName();
    }

    private string ResolveEntityName()
    {
        var attr = typeof(T).GetCustomAttribute<EntityNameAttribute>();
        return attr?.Name ?? typeof(T).Name;
    }

    private string ResolveTableName() => EntityName;

    protected IDbConnection CreateConnection()
    {
        var db = ConnectionFactory.CreateConnection();
        db.Open();
        return db;
    }

    protected QueryFactory CreateQueryFactory(IDbConnection connection)
        => new(connection, new SqlServerCompiler());

    protected void ApplySoftDelete(Query query)
    {
        if (IsSoftDeletable)
            query.Where("IsDeleted", 0);
    }

    protected virtual List<string> GetBaseColumns()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !IsNavigation(p))
            .Select(p => p.Name)
            .Where(n => n is not ("DeletedAt" or "UpdatedAt" or "UpdatedBy"))
            .ToList();
    }

    protected virtual List<string> GetBaseAssignments()
    {
        return GetBaseColumns()
            .Where(n => n is not ("Id" or "CreatedAt" or "CreatedBy" or "IsDeleted" or "DeletedAt"))
            .Select(n => $"{n} = @{n}")
            .ToList();
    }

    private static bool IsNavigation(PropertyInfo prop)
    {
        if (NavigationNames.Contains(prop.Name)) return true;
        var t = prop.PropertyType;
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(ICollection<>) || def == typeof(List<>)) return true;
        }
        return false;
    }

    protected static string SoftDeleteFilter(string? tableAlias = null)
    {
        if (!IsSoftDeletable) return "1=1";
        var prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{tableAlias}.";
        return $"{prefix}IsDeleted = 0";
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Id", id);
        ApplySoftDelete(query);
        var entity = await qf.FirstOrDefaultAsync<T>(query, null, null, ct);
        if (entity is not null)
        {
            await AfterLoadAsync(db, entity, ct);
        }
        return entity;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, Expression<Func<T, object>> includes, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Id", id);
        ApplySoftDelete(query);
        var entity = await qf.FirstOrDefaultAsync<T>(query, null, null, ct);
        if (entity is not null)
        {
            await AfterLoadAsync(db, entity, ct);
        }
        return entity;
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(QuerySpecification query, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.QueryBySpecAsync<T>(query, TableName, ApplySoftDelete,
            DefaultSortColumn, DefaultSortDir,
            postProcess: async (conn, entity, token) =>
            {
                await AfterLoadAsync(conn, entity, token);
            },
            ct: ct);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).OrderBy(DefaultSortColumn, DefaultSortDir == "DESC" ? "desc" : "asc");
        ApplySoftDelete(query);
        var rows = (await qf.GetAsync<T>(query, null, null, ct)).ToList();
        foreach (var item in rows)
        {
            await AfterLoadAsync(db, item, ct);
        }
        return rows;
    }

    public virtual async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await GetCountAsync((QuerySpecification?)null, ct);
    }

    public virtual async Task<int> GetCountAsync(QuerySpecification? query = null, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var q = new Query(TableName).AsCount();
        ApplySoftDelete(q);

        if (query is not null)
        {
            foreach (var filter in query.Filters)
                q.WhereLike(filter.Field, $"%{filter.Value}%");
        }

        var row = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(q, null, null, ct);
        if (row is null) return 0;
        var val = row.Values.FirstOrDefault();
        return val is not null ? Convert.ToInt32(val) : 0;
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var props = GetPropertyMap();
        var columns = GetBaseColumns();
        var data = new Dictionary<string, object>();

        foreach (var col in columns)
        {
            if (props.TryGetValue(col, out var prop))
                data[col] = prop.GetValue(entity) ?? DBNull.Value;
        }

        if (HasExtensionFields)
        {
            var definitions = GetDefinitions();
            foreach (var def in definitions)
            {
                var field = entity.ExtensionFields.FirstOrDefault(f =>
                    string.Equals(f.Name, def.Name, StringComparison.OrdinalIgnoreCase));
                var value = field?.Value ?? def.DefaultValue;
                data[def.ColumnName] = NormalizeExtensionValue(value, def.Type) ?? DBNull.Value;
            }
        }

        await qf.ExecuteAsync(new Query(TableName).AsInsert(data), null, null, ct);

        if (HasExtensionFields)
            await HydrateExtensionFieldsAsync(db, entity, ct);

        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var props = GetPropertyMap();
        var assignments = GetBaseAssignments();
        var data = new Dictionary<string, object>();

        foreach (var assign in assignments)
        {
            var col = assign.Split('=')[0].Trim();
            if (props.TryGetValue(col, out var prop))
                data[col] = prop.GetValue(entity) ?? DBNull.Value;
        }

        if (HasExtensionFields)
        {
            var definitions = GetDefinitions();
            foreach (var def in definitions)
            {
                var field = entity.ExtensionFields.FirstOrDefault(f =>
                    string.Equals(f.Name, def.Name, StringComparison.OrdinalIgnoreCase));
                var value = field?.Value ?? def.DefaultValue;
                data[def.ColumnName] = NormalizeExtensionValue(value, def.Type) ?? DBNull.Value;
            }
        }

        await qf.ExecuteAsync(new Query(TableName).Where("Id", entity.Id).AsUpdate(data), null, null, ct);

        if (HasExtensionFields)
            await HydrateExtensionFieldsAsync(db, entity, ct);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var data = new Dictionary<string, object>
        {
            ["IsDeleted"] = 1,
            ["DeletedAt"] = DateTime.UtcNow
        };
        await qf.ExecuteAsync(new Query(TableName).Where("Id", entity.Id).AsUpdate(data), null, null, ct);
    }

    public virtual async Task HardDeleteAsync(T entity, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        await qf.ExecuteAsync(new Query(TableName).Where("Id", entity.Id).AsDelete(), null, null, ct);
    }

    protected virtual string DefaultSortColumn => "CreatedAt";
    protected virtual string DefaultSortDir => "ASC";

    protected virtual async Task AfterLoadAsync(IDbConnection connection, T entity, CancellationToken ct)
    {
        if (HasExtensionFields)
            await HydrateExtensionFieldsAsync(connection, entity, ct);
    }

    private Dictionary<string, PropertyInfo> GetPropertyMap()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !IsNavigation(p))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    protected IReadOnlyList<EntityExtensionFieldDefinition> GetDefinitions()
        => ExtensionRegistry!.GetExtensionFieldsForEntity(EntityName).ToList();

    protected async Task HydrateExtensionFieldsAsync(IDbConnection db, T entity, CancellationToken ct)
    {
        var definitions = GetDefinitions();
        if (definitions.Count == 0)
            return;

        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .Select(definitions.Select(d => d.ColumnName).ToArray())
            .Where("Id", entity.Id);

        var row = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        if (row is null)
            return;

        entity.ExtensionFields = definitions.Select(definition =>
        {
            row.TryGetValue(definition.ColumnName, out var rawValue);
            return new ExtensionField
            {
                Name = definition.Name,
                Value = ConvertExtensionValue(rawValue, definition.Type),
                Type = definition.Type.ToString(),
                Required = definition.Required,
                DefaultValue = definition.DefaultValue,
                Label = definition.Label,
                PossibleValues = definition.PossibleValues,
                FrontEndValidator = definition.FrontEndValidator,
                Slot = definition.Slot
            };
        }).ToList();
    }

    private static object? NormalizeExtensionValue(object? value, EntityExtensionFieldType type)
    {
        if (value is null)
            return null;

        return type switch
        {
            EntityExtensionFieldType.Boolean => value is bool b ? b : bool.TryParse(Convert.ToString(value), out var pb) ? pb : value,
            EntityExtensionFieldType.Number => value is decimal or double or int or long ? value : decimal.TryParse(Convert.ToString(value), out var pd) ? pd : value,
            _ => value
        };
    }

    private static object? ConvertExtensionValue(object? value, EntityExtensionFieldType type)
    {
        if (value is null || value is DBNull)
            return null;

        return type switch
        {
            EntityExtensionFieldType.Boolean => value is bool b ? b : bool.TryParse(Convert.ToString(value), out var pb) && pb,
            EntityExtensionFieldType.Number => value is decimal d ? d : decimal.TryParse(Convert.ToString(value), out var pd) ? pd : value,
            _ => value
        };
    }
}
