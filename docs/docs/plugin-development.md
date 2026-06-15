# Plugin Development Guide

This guide explains how to create, package, and distribute plugins for Admin Shell.

---

## Overview

Plugins are the primary extension mechanism for Admin Shell. A plugin is a .NET class library that implements one or more interfaces from `AdminShell.Contracts`. Plugins are discovered at startup by scanning the `plugins/` directory, resolved by dependency order, and loaded via reflection.

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
    -o plugins/MyPlugin \
    --pluginId myplugin \
    --pluginDisplayName "My Plugin" \
    --pluginDescription "A description of my plugin" \
    --pluginVersion 1.0.0
```

The template generates a backend project under the plugin root:

- `plugins/MyPlugin/manifest.json` — Source manifest with plugin metadata
- `plugins/MyPlugin/Backend/MyPlugin.csproj` — Project file targeting `net10.0` with a reference to `AdminShell.Contracts`
- `plugins/MyPlugin/Backend/Plugin.cs` — Main plugin class implementing `IAdminShellPlugin` and optional plugin interfaces
- `plugins/MyPlugin/Backend/dependencias/` — Backend dependency DLLs/PDBs copied by build/package
- `plugins/MyPlugin/FrontEnd/` — Optional Vue/TypeScript frontend source folder

---

## Manual Plugin Creation

### Step 1: Create the project

```bash
mkdir -p plugins/MyPlugin/Backend
cd plugins/MyPlugin/Backend
dotnet new classlib -n MyPlugin --framework net10.0
```

### Step 2: Add the project file

Edit `MyPlugin.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>MyPlugin</RootNamespace>
    <OutputPath>bin/$(Configuration)/</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../backend/AdminShell.Contracts/AdminShell.Contracts.csproj" />
  </ItemGroup>
  <Target Name="CopyPluginDependencies" AfterTargets="Build;Publish">
    <ItemGroup>
      <PluginDependency Include="$(OutputPath)*.dll" Exclude="$(OutputPath)$(AssemblyName).dll" />
      <PluginDependency Include="$(OutputPath)*.pdb" Exclude="$(OutputPath)$(AssemblyName).pdb" />
    </ItemGroup>
    <MakeDir Directories="$(MSBuildProjectDirectory)/dependencias" />
    <Copy SourceFiles="@(PluginDependency)" DestinationFolder="$(MSBuildProjectDirectory)/dependencias" SkipUnchangedFiles="true" />
  </Target>

</Project>
```

> **Note:** Setting `OutputPath` to `bin/$(Configuration)/` keeps build output inside `plugins/MyPlugin/Backend/bin/Release/net10.0/`. The loader walks up from the assembly path to `plugins/MyPlugin/` and reads `manifest.json`.

### Step 3: Create the plugin manifest

Create `manifest.json` in the plugin root:

```json
{
  "schemaVersion": 1,
  "id": "myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "Description of my plugin",
  "dependencies": []
}
```

Permissions are not declared in the manifest. Frontend plugins can export a `permissions` object from their frontend entry file when permissions are needed.

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
dotnet build plugins/MyPlugin/Backend/MyPlugin.csproj
```

The compiled assembly (`MyPlugin.dll`) will be placed in `plugins/MyPlugin/Backend/bin/Release/net10.0/`. Restart the application to see it loaded.

---

## Packaging

The deployable package must be a `.zip` with this structure:

```text
plugin.zip
  manifest.json
  backend/
    MyPlugin.dll
    MyPlugin.deps.json
    MyPlugin.runtimeconfig.json
    dependencies...
  frontend/
    index.js
    styles.css
    assets/
```

The `frontend/` folder is the compiled plugin frontend. Plugin source should follow:

```text
plugins/MyPlugin/
  manifest.json
  Backend/
    MyPlugin.csproj
    MyPlugin.cs
  FrontEnd/ (optional)
    package.json
    src/
    dist/
```

- Backend: .NET plugin code and API/data/widget/menu implementations.
- FrontEnd: Vue SFCs (`.vue`) for new pages/components and TypeScript (`.ts`) for non-UI code.
- Built `index.js` runtime entry in `FrontEnd/dist/` when the plugin has frontend assets.

A helper script is available:

```bash
scripts/package-plugin.sh <plugin-root> [output-dir]
```

Example:

```bash
scripts/package-plugin.sh plugins/OrderCreationPlugin dist/plugins
```

The browser should not extract tar files or upload frontend files separately. The backend owns package validation, extraction, dependency validation, migration execution, activation, and rollback.

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

Plugins declare dependencies in the manifest only:

```json
{
  "schemaVersion": 1,
  "id": "useraudit",
  "name": "User Audit Plugin",
  "version": "1.0.0",
  "dependencies": [
    {
      "id": "reporting",
      "version": ">= 1.0.0"
    }
  ]
}
```

---

## Plugin Manifest (manifest.json)

Every plugin source root must include a `manifest.json` next to `Backend/` and `FrontEnd/`. The deployable package also uses `manifest.json` at the package root.

| Field          | Type   | Required | Description                              |
|----------------|--------|----------|------------------------------------------|
| `schemaVersion` | number | Yes      | Manifest schema version                  |
| `id`            | string | Yes      | Unique plugin identifier (lowercase)     |
| `name`          | string | Yes      | Human-readable name                      |
| `version`       | string | Yes      | SemVer version                           |
| `description`   | string | Yes      | Short description                        |
| `dependencies`  | array  | No       | Array of dependency objects with `id` and `version` |

---

## Frontend Plugin Integration

When your backend plugin contributes a widget, menu item, tab, or page component, the frontend plugin needs corresponding Vue components.

Use this source convention inside the plugin frontend project:

```text
FrontEnd/
  backend/
    index.ts
    permissions.ts
    pages/
      ReportsPage.vue
    services/
      reportingApi.ts
    types/
      reporting.ts
```

Rules:

- New pages/components must be Vue SFCs (`.vue`).
- Non-UI code must be TypeScript (`.ts`): services, types, permissions, composables.
- The built runtime entry is `index.js` under `FrontEnd/dist/`.
- Routes must not be declared in the manifest.
- Permissions stay outside the manifest and are exported by the frontend entry or from `permissions.ts`.

Example entry:

```ts
import { ReportsPage } from './pages/ReportsPage.vue'
import { permissions } from './permissions'

export { permissions }

export default class ReportingPlugin {
  initialize(container, services) {
    services.components.register('ReportsPage', ReportsPage)
  }

  dispose() {}
}
```

---

## Best Practices

- **Keep plugins focused** — Each plugin should do one thing well
- **Use semantic versioning** — Follow SemVer for plugin versions
- **Declare dependencies explicitly** — Use manifest `dependencies[]` with `id` and `version`
- **Log at startup** — Log initialization to help with debugging
- **Isolate state** — Use scoped DI services rather than static state
- **Handle graceful degradation** — If a dependency is missing, log a warning and continue
- **Test independently** — Each plugin should have its own test suite