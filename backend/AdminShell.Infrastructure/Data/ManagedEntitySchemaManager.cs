using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Data;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Data;

public sealed class ManagedEntitySchemaManager : IManagedEntitySchemaManager
{
    private readonly IReadOnlyList<IManagedEntityProvider> _hostProviders;
    private readonly IPluginLoader? _pluginLoader;
    private readonly ILogger<ManagedEntitySchemaManager> _logger;

    public ManagedEntitySchemaManager(
        IEnumerable<IManagedEntityProvider> hostProviders,
        IPluginLoader? pluginLoader = null,
        ILogger<ManagedEntitySchemaManager> logger = null!)
    {
        _hostProviders = hostProviders.ToImmutableArray();
        _pluginLoader = pluginLoader;
        _logger = logger;
    }

    public async Task EnsureAsync(IDbConnection connection, CancellationToken ct = default)
    {
        foreach (var entity in GetManagedEntities())
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await EnsureTableAsync(connection, entity, ct);
                _logger.LogInformation("Ensured managed entity {EntityName} as table {TableName}",
                    entity.Type.Name,
                    entity.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure managed entity {EntityName} as table {TableName}",
                    entity.Type.Name,
                    entity.TableName);
            }
        }
    }

    private IEnumerable<ResolvedManagedEntity> GetManagedEntities()
    {
        var explicitPluginProviders = _pluginLoader?.GetPluginInstances().OfType<IManagedEntityProvider>() ?? Array.Empty<IManagedEntityProvider>();
        var conventionalPluginProviders = _pluginLoader?.GetManagedEntityProviders() ?? Array.Empty<IManagedEntityProvider>();
        var pluginProviders = explicitPluginProviders
            .Concat(conventionalPluginProviders)
            .ToList();
        var pluginTypes = pluginProviders
            .SelectMany(provider =>
            {
                try
                {
                    return provider.GetManagedEntityTypes();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect managed entity types from provider {ProviderType}",
                        provider.GetType().FullName);
                    return Enumerable.Empty<Type>();
                }
            }).ToList();

        var entityTypes = _hostProviders
            .SelectMany(provider =>
            {
                try
                {
                    return provider.GetManagedEntityTypes();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect managed entity types from provider {ProviderType}",
                        provider.GetType().FullName);
                    return Enumerable.Empty<Type>();
                }
            })
            .Concat(pluginTypes)
            .Distinct()
            .ToList();

        return entityTypes
            .Select(ResolveEntity)
            .Where(entity => entity is not null)
            .Cast<ResolvedManagedEntity>()
            .GroupBy(entity => entity.TableName, StringComparer.OrdinalIgnoreCase)
            .Select(MergeEntities)
            .OrderBy(entity => entity.Order)
            .ToList();
    }

    private ResolvedManagedEntity? ResolveEntity(Type type)
    {
        try
        {
            var attribute = type.GetCustomAttribute<ManagedEntityAttribute>();
            var tableName = attribute?.TableName ?? Pluralize(type.Name);
            var order = attribute?.Order ?? 100;
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite)
                .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                .Select(ResolveProperty)
                .Where(property => property is not null)
                .Cast<ResolvedManagedEntityProperty>()
                .OrderBy(property => property.Order)
                .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (properties.Count == 0)
                return null;

            return new ResolvedManagedEntity(
                type,
                tableName,
                properties,
                order);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve managed entity {TypeName}", type.FullName);
            return null;
        }
    }

    private static ResolvedManagedEntityProperty? ResolveProperty(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<EntityColumnAttribute>();

        if (!IsSupportedColumn(property.PropertyType))
            return null;

        var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>() is not null;
        var primaryKey = string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)
            && IsSimplePrimaryKey(property.PropertyType);
        var required = columnAttribute is not null && columnAttribute.Required
                       || columnAttribute is null && (requiredAttribute || IsRequiredByClrType(property.PropertyType));
        var type = InferType(property.PropertyType);
        int? maxLength = columnAttribute?.MaxLenght is > 0 ? columnAttribute.MaxLenght : null;
        if (maxLength is null && type == ManagedEntityPropertyType.NVarChar)
            maxLength = 256;
        var defaultSql = columnAttribute?.DefaultSql;

        return new ResolvedManagedEntityProperty(
            property.Name,
            type,
            maxLength,
            required,
            primaryKey,
            defaultSql,
            100);
    }

    private static bool IsSupportedColumn(Type type)
    {
        var nonNullable = Nullable.GetUnderlyingType(type) ?? type;

        if (nonNullable.IsEnum)
            return true;

        return nonNullable == typeof(Guid)
               || nonNullable == typeof(string)
               || nonNullable == typeof(bool)
               || nonNullable == typeof(int)
               || nonNullable == typeof(long)
               || nonNullable == typeof(decimal)
               || nonNullable == typeof(double)
               || nonNullable == typeof(float)
               || nonNullable == typeof(DateTime)
               || nonNullable == typeof(DateTimeOffset)
               || nonNullable == typeof(object)
               || nonNullable == typeof(JsonElement);
    }

    private static bool IsSimplePrimaryKey(Type type)
    {
        var nonNullable = Nullable.GetUnderlyingType(type) ?? type;
        return nonNullable == typeof(Guid) || nonNullable == typeof(int) || nonNullable == typeof(long);
    }

    private static ManagedEntityPropertyType InferType(Type type)
    {
        var nonNullable = Nullable.GetUnderlyingType(type) ?? type;

        if (nonNullable.IsEnum)
            return ManagedEntityPropertyType.NVarChar;

        return nonNullable switch
        {
            Type value when value == typeof(Guid) => ManagedEntityPropertyType.UniqueIdentifier,
            Type value when value == typeof(string) => ManagedEntityPropertyType.NVarChar,
            Type value when value == typeof(bool) => ManagedEntityPropertyType.Bit,
            Type value when value == typeof(int) => ManagedEntityPropertyType.Int,
            Type value when value == typeof(long) => ManagedEntityPropertyType.BigInt,
            Type value when value == typeof(decimal) || value == typeof(double) || value == typeof(float) => ManagedEntityPropertyType.Decimal,
            Type value when value == typeof(DateTime) || value == typeof(DateTimeOffset) => ManagedEntityPropertyType.DateTime2,
            Type value when value == typeof(object) || value == typeof(JsonElement) => ManagedEntityPropertyType.Json,
            _ => ManagedEntityPropertyType.NVarCharMax
        };
    }

    private static bool IsRequiredByClrType(Type type)
    {
        var nonNullable = Nullable.GetUnderlyingType(type) ?? type;
        return nonNullable.IsValueType && nonNullable != typeof(Guid);
    }

    private static ResolvedManagedEntity MergeEntities(IGrouping<string, ResolvedManagedEntity> group)
    {
        var first = group.OrderBy(entity => entity.Order).First();
        var properties = group
            .SelectMany(entity => entity.Properties)
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(propertyGroup => propertyGroup.OrderBy(property => property.Order).First())
            .OrderBy(property => property.Order)
            .ToList();

        return first with { Properties = properties };
    }

    private static async Task EnsureTableAsync(IDbConnection connection, ResolvedManagedEntity entity, CancellationToken ct)
    {
        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = @TableName",
            new { entity.TableName });

        if (exists == 0)
        {
            await connection.ExecuteAsync(CreateTableSql(entity));
            return;
        }

        foreach (var property in entity.Properties)
        {
            ct.ThrowIfCancellationRequested();
            await EnsureColumnAsync(connection, entity.TableName, property, ct);
        }
    }

    private static async Task EnsureColumnAsync(IDbConnection connection, string tableName, ResolvedManagedEntityProperty property, CancellationToken ct)
    {
        var exists = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName",
            new { TableName = tableName, ColumnName = property.Name });

        if (exists > 0)
            return;

        await connection.ExecuteAsync(
            $"ALTER TABLE {QuoteIdentifier(tableName)} ADD {QuoteIdentifier(property.Name)} {property.SqlType}{Nullability(property)}{DefaultValue(property)}");
    }

    private static string CreateTableSql(ResolvedManagedEntity entity)
    {
        var primaryKeys = entity.Properties
            .Where(property => property.PrimaryKey)
            .Select(property => QuoteIdentifier(property.Name))
            .ToList();

        var columns = entity.Properties.Select(property =>
            $"    {QuoteIdentifier(property.Name)} {property.SqlType}{Nullability(property)}{DefaultValue(property)}");

        var primaryKeyClause = primaryKeys.Count > 0
            ? $",\n    CONSTRAINT PK_{entity.TableName} PRIMARY KEY ({string.Join(", ", primaryKeys)})"
            : string.Empty;

        return $@"IF OBJECT_ID(N'{entity.TableName}', N'U') IS NULL
CREATE TABLE {QuoteIdentifier(entity.TableName)} (
{string.Join(",\n", columns)}{primaryKeyClause}
);";
    }

    private static string Nullability(ResolvedManagedEntityProperty property)
        => property.IsNullable ? " NULL" : " NOT NULL";

    private static string DefaultValue(ResolvedManagedEntityProperty property)
    {
        if (!string.IsNullOrWhiteSpace(property.DefaultSql))
            return $" DEFAULT {property.DefaultSql}";

        return string.Empty;
    }

    private static string EscapeSqlString(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static string QuoteIdentifier(string identifier)
        => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string Pluralize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Entity type name is required.");

        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return value;

        if (value.EndsWith("y", StringComparison.OrdinalIgnoreCase)
            && value.Length > 1
            && !IsVowel(value[^2]))
        {
            return $"{value[..^1]}ies";
        }

        return $"{value}s";
    }

    private static bool IsVowel(char value) => "aeiou".Contains(char.ToLowerInvariant(value), StringComparison.Ordinal);

    private sealed record ResolvedManagedEntity(
        Type Type,
        string TableName,
        IReadOnlyList<ResolvedManagedEntityProperty> Properties,
        int Order);

    private sealed record ResolvedManagedEntityProperty(
        string Name,
        ManagedEntityPropertyType Type,
        int? MaxLength,
        bool Required,
        bool PrimaryKey,
        string? DefaultSql,
        int Order)
    {
        public string SqlType => Type switch
        {
            ManagedEntityPropertyType.UniqueIdentifier => "UNIQUEIDENTIFIER",
            ManagedEntityPropertyType.NVarChar => $"NVARCHAR({MaxLength ?? 256})",
            ManagedEntityPropertyType.NVarCharMax or ManagedEntityPropertyType.Json => "NVARCHAR(MAX)",
            ManagedEntityPropertyType.Bit => "BIT",
            ManagedEntityPropertyType.Int => "INT",
            ManagedEntityPropertyType.BigInt => "BIGINT",
            ManagedEntityPropertyType.Decimal => "DECIMAL(18,4)",
            ManagedEntityPropertyType.DateTime2 => "DATETIME2",
            _ => "NVARCHAR(MAX)"
        };

        public bool IsNullable => !Required && !PrimaryKey;
    }
}
