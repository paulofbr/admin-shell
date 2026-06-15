using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminShell.Contracts;

/// <summary>
/// Base class for backend plugins.
/// Reduces boilerplate by providing default metadata and no-op lifecycle hooks.
/// </summary>
public abstract class AdminShellPluginBase : IAdminShellPlugin
{
    public abstract string Id { get; }

    public virtual string Name => FormatPluginName(GetType().Name);

    public virtual string Version => "1.0.0";

    public virtual string Description => string.Empty;

    public virtual void Initialize(IServiceCollection services, IConfiguration configuration)
    {
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    private static string FormatPluginName(string typeName)
    {
        var name = StripSuffix(typeName, "ForTests");
        name = StripSuffix(name, "Plugin");

        return InsertSpacesBeforeCapitalLetters(name);
    }

    private static string StripSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;
    }

    private static string InsertSpacesBeforeCapitalLetters(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            var previous = i > 0 ? value[i - 1] : '\0';

            if (i > 0 && char.IsUpper(current) && !char.IsUpper(previous))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
