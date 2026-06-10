# Plugin Development Guide

This guide explains how to create, package, and distribute plugins for Admin Shell.

---

## Overview

Plugins are the primary extension mechanism for Admin Shell. A plugin is a .NET class library that implements one or more interfaces from `AdminShell.Contracts`. Plugins are discovered at startup by scanning the `Plugins/` directory, resolved by dependency order, and loaded via reflection.

### Plugin Types

| Interface          | Purpose                                  | Required |
|--------------------|------------------------------------------|----------|
| `IAdminShellPlugin` | Base lifecycle and metadata              | Yes      |
| `IApiPlugin`       | Contribute minimal API endpoints         | No       |
| `IDataPlugin`      | Add EF Core entity configuration and seed data | No  |
| `IWidgetPlugin`    | Provide dashboard widgets                | No       |
| `IMenuPlugin`      | Contribute sidebar and navigation items  | No       |

---

## Using the Plugin Template

The quickest way to start is with the `adminshell-plugin` dotnet new template.

### Install the template

```bash
dotnet new install ./templates/AdminShellPlugin
```

### Create a new plugin

```bash
dotnet new adminshell-plugin \
    -n MyPlugin \
    -o Plugins/Backend/MyPlugin \
    --pluginId myplugin \
    --pluginDisplayName "My Plugin" \
    --pluginDescription "A description of my plugin" \
    --pluginVersion 1.0.0
```

The template generates:

- `MyPlugin.csproj` — Project file targeting `net9.0` with a reference to `AdminShell.Contracts`
- `Plugin.cs` — Main plugin class implementing `IAdminShellPlugin` and `IApiPlugin`
- `plugin.json` — Manifest with plugin metadata
- `GlobalUsings.cs` — Common using directives

---

## Manual Plugin Creation

### Step 1: Create the project

```bash
mkdir -p Plugins/Backend/MyPlugin
cd Plugins/Backend/MyPlugin
dotnet new classlib -n MyPlugin --framework net9.0
```

### Step 2: Add the project file

Edit `MyPlugin.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>MyPlugin</RootNamespace>
    <OutputPath>../</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/AdminShell.Contracts/AdminShell.Contracts.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

> **Note:** Setting `OutputPath` to `../` places the compiled assembly alongside `Plugins/Backend/` so the plugin loader can find it.

### Step 3: Create the plugin manifest

Create `plugin.json`:

```json
{
  "id": "myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "Description of my plugin",
  "dependencies": {},
  "permissions": ["myplugin:read"]
}
```

### Step 4: Implement the plugin

Create `Plugin.cs`:

```csharp
using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyPlugin;

public class MyPlugin : IAdminShellPlugin, IApiPlugin
{
    public string Id => "myplugin";
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Description => "Description of my plugin";

    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        // Register services here
        var logger = services.BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(Name);
        logger.LogInformation("{Plugin} initialized", Name);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Add middleware if needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/plugins/myplugin")
            .WithTags("My Plugin");

        group.MapGet("/hello", () => Results.Ok(new { message = "Hello from MyPlugin!" }));
    }
}
```

### Step 5: Build and test

```bash
dotnet build Plugins/Backend/MyPlugin/MyPlugin.csproj
```

The compiled assembly (`MyPlugin.dll`) will be placed in `Plugins/Backend/`. Restart the application to see it loaded.

---

## Plugin Interfaces

### IAdminShellPlugin (Required)

```csharp
public interface IAdminShellPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    void Initialize(IServiceCollection services, IConfiguration configuration);
    void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}
```

| Member        | Description                                      |
|---------------|--------------------------------------------------|
| `Id`          | Unique identifier (lowercase, no spaces)          |
| `Name`        | Human-readable display name                       |
| `Version`     | SemVer version string                             |
| `Description` | Short description of the plugin                   |
| `Initialize`  | Called at startup — register DI services          |
| `Configure`   | Called after pipeline is built — add middleware   |

---

### API Plugin (IApiPlugin)

```csharp
public interface IApiPlugin : IAdminShellPlugin
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

Use `MapEndpoints` to register minimal API endpoints. Follow the convention of grouping under `/api/plugins/{pluginId}`:

```csharp
public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("/api/plugins/myplugin")
        .WithTags("My Plugin (Plugin)");

    group.MapGet("/items", async (CancellationToken ct) =>
    {
        // return items
    });

    group.MapPost("/items", async (CreateItemRequest request, CancellationToken ct) =>
    {
        // create item
        return Results.Created($"/api/plugins/myplugin/items/{item.Id}", item);
    });
}
```

---

### Data Plugin (IDataPlugin)

```csharp
public interface IDataPlugin : IAdminShellPlugin
{
    void ConfigureEntities(ModelBuilder modelBuilder);
    void SeedData(ModelBuilder modelBuilder);
}
```

Used to contribute entity configurations and seed data to EF Core's model builder:

```csharp
public void ConfigureEntities(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MyEntity>(entity =>
    {
        entity.ToTable("MyEntities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(200);
    });
}

public void SeedData(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MyEntity>().HasData(
        new MyEntity { Id = 1, Name = "Default Item" }
    );
}
```

---

### Widget Plugin (IWidgetPlugin)

```csharp
public interface IWidgetPlugin : IAdminShellPlugin
{
    IEnumerable<WidgetDescriptor> GetWidgets();
}
```

Provides dashboard widgets:

```csharp
public IEnumerable<WidgetDescriptor> GetWidgets()
{
    yield return new WidgetDescriptor
    {
        Id = "myplugin-summary",
        Title = "My Summary",
        Zone = "dashboard",
        Order = 100,
        Width = 4,
        Height = 4,
        ComponentName = "MyWidget"
    };
}
```

`WidgetDescriptor` properties:

| Property        | Type     | Default       | Description                     |
|-----------------|----------|---------------|---------------------------------|
| `Id`            | string   | (required)    | Unique widget identifier        |
| `Title`         | string   | (required)    | Display title                   |
| `Zone`          | string   | `"dashboard"` | Placement zone                  |
| `Order`         | int      | `100`         | Sort order within zone          |
| `Width`         | int      | `4`           | Grid width (CSS grid columns)   |
| `Height`        | int      | `4`           | Grid height                     |
| `ComponentName` | string?  | null          | Frontend component identifier   |
| `Settings`      | dict?    | null          | Optional configuration          |

---

### Menu Plugin (IMenuPlugin)

```csharp
public interface IMenuPlugin : IAdminShellPlugin
{
    IEnumerable<MenuItem> GetMenuItems();
}
```

Contributes sidebar and navigation items:

```csharp
public IEnumerable<MenuItem> GetMenuItems()
{
    yield return new MenuItem
    {
        Id = "myplugin",
        Label = "My Plugin",
        Icon = "extension",
        Path = "/plugins/myplugin",
        Order = 100,
        Permissions = new[] { "myplugin:read" }
    };
}
```

`MenuItem` properties:

| Property      | Type      | Default  | Description                      |
|---------------|-----------|----------|----------------------------------|
| `Id`          | string    | (required)| Unique menu item identifier     |
| `Label`       | string    | (required)| Display label                   |
| `Icon`        | string?   | null     | Icon identifier                  |
| `Path`        | string?   | null     | Route path (for links)           |
| `Order`       | int       | `100`    | Sort order                       |
| `ParentId`    | string?   | null     | Parent menu item (for nesting)   |
| `Permissions` | string[]? | null     | Required permissions             |
| `Children`    | list?     | null     | Nested sub-menu items            |

---

## Plugin Dependencies

Plugins can declare dependencies on other plugins using the `[PluginDependency]` assembly attribute:

```csharp
[assembly: PluginDependency(typeof(ReportingPlugin.ReportingPlugin), ">= 1.0.0")]
```

The plugin loader uses **topological sorting** to initialize plugins in the correct order. Circular dependencies are detected and reported at startup.

Dependencies can also be declared in `plugin.json`:

```json
{
  "id": "useraudit",
  "name": "User Audit Plugin",
  "version": "1.0.0",
  "dependencies": {
    "reporting": ">= 1.0.0"
  }
}
```

---

## Plugin Manifest (plugin.json)

Every plugin must include a `plugin.json` manifest file:

| Field          | Type   | Required | Description                              |
|----------------|--------|----------|------------------------------------------|
| `id`           | string | Yes      | Unique plugin identifier (lowercase)     |
| `name`         | string | Yes      | Human-readable name                      |
| `version`      | string | Yes      | SemVer version                           |
| `description`  | string | Yes      | Short description                        |
| `dependencies` | object | No       | Map of plugin ID to version constraint   |
| `permissions`  | array  | No       | Array of permission strings              |

---

## Frontend Plugin Integration

When your backend plugin contributes a widget or menu, the frontend needs corresponding components:

1. Add a frontend plugin entry in `frontend/src/plugins/{pluginId}/`
2. Export React components matching the `ComponentName` in `WidgetDescriptor`
3. Register routes matching the `Path` in `MenuItem`

This is covered in the frontend plugin documentation (coming soon).

---

## Best Practices

- **Keep plugins focused** — Each plugin should do one thing well
- **Use semantic versioning** — Follow SemVer for plugin versions
- **Declare dependencies explicitly** — Use `[PluginDependency]` and `plugin.json`
- **Log at startup** — Log initialization to help with debugging
- **Isolate state** — Use scoped DI services rather than static state
- **Handle graceful degradation** — If a dependency is missing, log a warning and continue
- **Test independently** — Each plugin should have its own test suite