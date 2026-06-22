using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Data;

namespace AdminShell.Infrastructure.PluginSystem;

public sealed record SettingRegistration(string Category, SettingDefinition Definition);

public class SettingsRegistry : ISettingsRegistry
{
    private readonly IPluginLoader? _pluginLoader;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SettingsRegistry> _logger;
    private List<SettingRegistration> _settings = [];

    public SettingsRegistry(
        IDbConnectionFactory connectionFactory,
        ILogger<SettingsRegistry> logger,
        IPluginLoader? pluginLoader = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _pluginLoader = pluginLoader;
        Refresh();
    }

    public void Refresh()
    {
        _settings = [];

        var activePluginIds = _pluginLoader?.LoadedPlugins
            .Where(p => p.Status == PluginStatus.Active || p.Status == PluginStatus.Loaded)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetLoadableTypes).Where(IsSettingsType))
        {
            var attribute = type.GetCustomAttribute<SettingsAttribute>()!;

            if (activePluginIds is not null && !activePluginIds.Contains(attribute.Category))
                continue;

            try
            {
                _settings.AddRange(GetDefinitions(type, attribute.Category)
                    .Select(definition => new SettingRegistration(attribute.Category, definition)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect settings from options type {OptionsType}", type.FullName);
            }
        }

        _logger.LogInformation("Settings registry refreshed: {Count} settings", _settings.Count);
    }

    public IReadOnlyList<SettingDefinition> GetAll()
        => _settings.Select(r => r.Definition).ToList().AsReadOnly();

    public IReadOnlyList<SettingDefinition> GetForCategory(string category)
        => _settings
            .Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Definition)
            .OrderBy(r => r.Order)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var qf = new QueryFactory(db, new SqlServerCompiler());
        var existingKeysQuery = new Query("Settings")
            .Where("IsDeleted", 0)
            .Select("Key");
        var existingKeys = (await qf.GetAsync<string>(existingKeysQuery, null, null, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        foreach (var definition in GetAll())
        {
            if (existingKeys.Contains(definition.Key))
                continue;

            var defaultValue = GetDefaultValue(definition) ?? string.Empty;
            var insertQuery = new Query("Settings").AsInsert(new
            {
                Id = Guid.NewGuid(),
                Key = definition.Key,
                Value = defaultValue,
                Category = definition.Category,
                Description = definition.Description,
                ValueType = definition.ValueType,
                IsDeleted = 0,
                CreatedAt = now,
                CreatedBy = "system"
            });
            await qf.ExecuteAsync(insertQuery, null, null, ct);

            existingKeys.Add(definition.Key);
            _logger.LogInformation("Ensured setting {Key} for category {Category}", definition.Key, definition.Category);
        }
    }

    public async Task<SettingsResponse> GetSettingsAsync(string category, CancellationToken ct = default)
    {
        var definitions = GetForCategory(category);
        if (definitions.Count == 0)
            throw new InvalidOperationException($"Category '{category}' does not define settings.");

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var qf = new QueryFactory(db, new SqlServerCompiler());
        var settingsQuery = new Query("Settings")
            .Where("IsDeleted", 0);
        var settings = (await qf.GetAsync<AppSetting>(settingsQuery, null, null, ct)).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        var dtos = definitions.Select(definition =>
        {
            settings.TryGetValue(definition.Key, out var setting);
            return new SettingOptionDto(
                definition.Key,
                definition.Name,
                definition.Label,
                definition.Description,
                definition.Type,
                setting?.Value,
                GetDefaultValue(definition) ?? string.Empty,
                definition.Required,
                definition.Order,
                definition.Validator);
        }).ToList();

        return new SettingsResponse(category, category, dtos);
    }

    public async Task<SettingsResponse> UpdateAsync(
        string category,
        IEnumerable<UpdateSettingRequest> requests,
        CancellationToken ct = default)
    {
        var definitions = GetForCategory(category);
        if (definitions.Count == 0)
            throw new InvalidOperationException($"Category '{category}' does not define settings.");

        var definitionsByKey = definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        var updates = requests.ToList();
        var unknown = updates
            .Select(r => r.Key)
            .Where(key => !definitionsByKey.ContainsKey(key))
            .ToArray();

        if (unknown.Length > 0)
            throw new InvalidOperationException($"Unknown setting(s): {string.Join(", ", unknown)}");

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        foreach (var update in updates)
        {
            var definition = definitionsByKey[update.Key];

            if (definition.Required && string.IsNullOrWhiteSpace(update.Value))
                throw new InvalidOperationException($"Setting '{definition.Key}' is required.");

            await UpsertSettingAsync(db, definition, update.Value);
        }

        return await GetSettingsAsync(category, ct);
    }

    public async Task<TSettings> GetOptionsAsync<TSettings>(CancellationToken ct = default)
        where TSettings : class, new()
    {
        var options = new TSettings();
        var settingsType = typeof(TSettings);
        var definitions = GetForOptionsType(settingsType);

        if (definitions.Count == 0)
            return options;

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var qf = new QueryFactory(db, new SqlServerCompiler());
        var valuesQuery = new Query("Settings")
                .Where("IsDeleted", 0)
                .Select("Key", "Value");
        var values = (await qf.GetAsync<SettingValueRow>(valuesQuery, null, null, ct))
            .ToDictionary(row => row.Key, row => row.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            var property = GetProperty(definition);
            if (property is null)
                continue;

            if (values.TryGetValue(definition.Key, out var value))
            {
                SetPropertyValue(property, options, value, definition.Type);
                continue;
            }

            var defaultValue = GetDefaultValue(definition);
            if (defaultValue is not null)
                SetPropertyValue(property, options, defaultValue, definition.Type);
        }

        return options;
    }

    public async Task SetOptionsAsync<TSettings>(TSettings options, CancellationToken ct = default)
        where TSettings : class
    {
        var settingsType = typeof(TSettings);
        var definitions = GetForOptionsType(settingsType);
        if (definitions.Count == 0)
            throw new InvalidOperationException($"Type '{settingsType.FullName}' is not a settings type.");

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        foreach (var definition in definitions)
        {
            var property = GetProperty(definition)
                ?? throw new InvalidOperationException($"Settings property '{definition.PropertyName}' was not found on '{settingsType.FullName}'.");

            var value = ConvertToString(property.GetValue(options), definition.Type);
            if (definition.Required && string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Setting '{definition.Key}' is required.");

            await UpsertSettingAsync(db, definition, value);
        }
    }

    private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static bool IsSettingsType(Type type)
        => !type.IsAbstract
           && !type.IsInterface
           && type.IsDefined(typeof(SettingsAttribute), inherit: false)
           && type.GetConstructor(Type.EmptyTypes) is not null;

    private static IEnumerable<SettingDefinition> GetDefinitions(Type type, string category)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attribute = property.GetCustomAttribute<SettingAttribute>();
            if (attribute is null)
                continue;

            yield return new SettingDefinition(
                category,
                property.Name,
                attribute.Label,
                attribute.Type,
                ConvertDefaultValueToString(attribute.DefaultValue, attribute.Type),
                attribute.Required,
                attribute.Order,
                attribute.Description,
                attribute.Validator,
                type,
                property.Name);
        }
    }

    private IReadOnlyList<SettingDefinition> GetForOptionsType(Type settingsType)
        => _settings
            .Where(r => r.Definition.OptionsType == settingsType)
            .Select(r => r.Definition)
            .OrderBy(r => r.Order)
            .ThenBy(r => r.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    private static PropertyInfo? GetProperty(SettingDefinition definition)
        => definition.OptionsType?.GetProperty(definition.PropertyName ?? string.Empty, BindingFlags.Instance | BindingFlags.Public);

    private static string? GetDefaultValue(SettingDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.DefaultValue))
            return definition.DefaultValue;

        if (definition.OptionsType is null || definition.PropertyName is null)
            return null;

        var instance = Activator.CreateInstance(definition.OptionsType);
        var property = instance?.GetType().GetProperty(definition.PropertyName, BindingFlags.Instance | BindingFlags.Public);
        return ConvertToString(property?.GetValue(instance), SettingType.String);
    }

    private async Task UpsertSettingAsync(IDbConnection db, SettingDefinition definition, string? value)
    {
        var qf = new QueryFactory(db, new SqlServerCompiler());
        var existingQuery = new Query("Settings")
            .Where("Key", definition.Key)
            .Where("IsDeleted", 0);
        var existing = await qf.FirstOrDefaultAsync<AppSetting>(existingQuery, null, null, default);

        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            var updateQuery = new Query("Settings")
                .Where("Id", existing.Id)
                .AsUpdate(new
                {
                    Value = value ?? string.Empty,
                    Category = definition.Category,
                    Description = definition.Description,
                    ValueType = definition.ValueType,
                    UpdatedAt = now,
                    UpdatedBy = "system"
                });
            await qf.ExecuteAsync(updateQuery, null, null, default);
            return;
        }

        var insertQuery = new Query("Settings").AsInsert(new
        {
            Id = Guid.NewGuid(),
            Key = definition.Key,
            Value = value ?? string.Empty,
            Category = definition.Category,
            Description = definition.Description,
            ValueType = definition.ValueType,
            IsDeleted = 0,
            CreatedAt = now,
            CreatedBy = "system",
            UpdatedAt = now,
            UpdatedBy = "system"
        });
        await qf.ExecuteAsync(insertQuery, null, null, default);
    }

    private static string? ConvertDefaultValueToString(object? value, SettingType type)
    {
        if (value is null)
            return null;

        return ConvertToString(value, type);
    }

    private static string? ConvertToString(object? value, SettingType type)
    {
        if (value is null)
            return null;

        if (value is bool boolValue)
            return boolValue ? "true" : "false";

        if (value is JsonElement jsonElement)
            return jsonElement.GetRawText();

        if (type == SettingType.Json && value is not string)
            return JsonSerializer.Serialize(value);

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static void SetPropertyValue(PropertyInfo property, object target, string value, SettingType type)
    {
        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (targetType == typeof(string))
        {
            property.SetValue(target, value);
            return;
        }

        if (targetType == typeof(bool))
        {
            property.SetValue(target, bool.TryParse(value, out var boolValue) ? boolValue : Convert.ToBoolean(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(int))
        {
            property.SetValue(target, int.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(long))
        {
            property.SetValue(target, long.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(decimal))
        {
            property.SetValue(target, decimal.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(double))
        {
            property.SetValue(target, double.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(float))
        {
            property.SetValue(target, float.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(DateTime))
        {
            property.SetValue(target, DateTime.Parse(value, CultureInfo.InvariantCulture));
            return;
        }

        if (targetType == typeof(JsonElement))
        {
            property.SetValue(target, JsonDocument.Parse(value).RootElement.Clone());
            return;
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            property.SetValue(target, converter.ConvertFromInvariantString(value));
            return;
        }

        property.SetValue(target, Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
    }

    private sealed record SettingValueRow(string Key, string? Value);
}
