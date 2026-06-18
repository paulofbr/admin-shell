using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Threading;

namespace AdminShell.Contracts;

public enum ManagedEntityPropertyType
{
    UniqueIdentifier = 0,
    NVarChar = 1,
    NVarCharMax = 2,
    Bit = 3,
    Int = 4,
    BigInt = 5,
    Decimal = 6,
    DateTime2 = 7,
    Json = 8
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class EntityColumnAttribute : Attribute
{
    public EntityColumnAttribute()
    {
    }

    public EntityColumnAttribute(int maxLength = 0, bool required = false, string? defaultSql = null)
    {
        MaxLenght = maxLength;
        Required = required;
        DefaultSql = defaultSql;
    }

    public int MaxLenght { get; set; }

    public bool Required { get; set; }

    public string? DefaultSql { get; set; }
}

public interface IManagedEntityProvider : IPluginComponent
{
    IEnumerable<Type> GetManagedEntityTypes();
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ManagedEntityAttribute : Attribute
{
    public string? TableName { get; set; }

    public int Order { get; set; } = 100;
}

public interface IManagedEntitySchemaManager
{
    Task EnsureAsync(IDbConnection connection, CancellationToken ct = default);
}

public static class ManagedEntityInference
{
    public static bool HasColumnAttribute(PropertyInfo property)
        => property.GetCustomAttribute<EntityColumnAttribute>() is not null
           || property.GetCustomAttribute<RequiredAttribute>() is not null;
}
