# Admin Shell

> An extensible, modular administration shell built on **ASP.NET Core 9.0** (backend) and **React 18** (frontend), designed as a foundation for enterprise management applications.

---

## Overview

Admin Shell provides a **plugin-driven architecture** that allows features to be added dynamically — without modifying the core codebase. It includes:

- **🔌 Backend Plugin System** — Discover, load, and manage plugins at runtime via `IPluginLoader`
- **🎨 Frontend Plugin System** — Extend the SPA with dynamic menus, widgets, and routes
- **🔐 Authentication & Authorization** — JWT-based auth with role management
- **📊 Dashboard & Widgets** — Plugin-contributed dashboard widgets
- **🧩 Clean Architecture** — Domain-driven design with clearly separated layers
- **📦 CI/CD Ready** — GitHub Actions pipeline for build, test, and publish

### Technology Stack

| Layer       | Technology                                                 |
|-------------|------------------------------------------------------------|
| **Backend** | .NET 9.0, ASP.NET Core Web API, Clean Architecture         |
| **ORM**     | Entity Framework Core 9.0                                  |
| **Database**| PostgreSQL (primary), SQLite (development)                 |
| **Frontend**| React 18, TypeScript, Vite                                 |
| **State**   | Zustand                                                    |
| **UI**      | Custom CSS with CSS Modules                                |
| **Testing** | xUnit, FluentAssertions, NSubstitute (backend)             |
| **Packaging**| NuGet (backend), npm (frontend)                           |

---

## Architecture at a Glance

```
admin-shell/
├── src/                          ← Backend source
│   ├── AdminShell.Host/          ← Web API entry point
│   ├── AdminShell.Contracts/     ← Shared interfaces & contracts
│   ├── AdminShell.Core/          ← Domain logic
│   └── AdminShell.Infrastructure/← Data access, external services
├── frontend/                     ← React SPA
│   ├── src/
│   │   ├── core/                 ← Layout, routing, auth
│   │   └── components/           ← Reusable UI components
├── Plugins/                      ← Backend plugins
│   └── Backend/                  ← Plugin assemblies
├── tests/                        ← Backend test projects
├── templates/                    ← dotnet new templates
├── docs/                         ← Documentation site (MkDocs)
└── .github/workflows/            ← CI/CD pipelines
```

### Key Concepts

- **Plugin**: Any assembly implementing `IAdminShellPlugin` (or its derived interfaces)
- **Plugin Loader**: Scans directories, resolves dependencies, and loads plugins at startup
- **Plugin Descriptor**: Metadata (ID, name, version, dependencies) declared in `plugin.json`
- **Event Bus**: Pub/sub communication channel between plugins

---

## Quick Start

```bash
# Clone the repository
git clone https://github.com/nousresearch/admin-shell.git
cd admin-shell

# Build backend
dotnet restore
dotnet build

# Run tests
dotnet test

# Setup frontend
cd frontend
npm ci
npm run build

# Run the application
cd ../src/AdminShell.Host
dotnet run
```

See the [Getting Started](getting-started.md) guide for detailed setup instructions.

---

## Contributing Plugins

The plugin system is the heart of Admin Shell. To create a new plugin:

1. Use the `adminshell-plugin` dotnet new template
2. Implement `IAdminShellPlugin` and any optional interfaces
3. Add a `plugin.json` manifest
4. Drop the compiled assembly into the `Plugins/` directory

See the [Plugin Development](plugin-development.md) guide for complete documentation.

---

## Project Status

Admin Shell is currently in active development. The codebase is functional with:

- ✅ Backend plugin discovery and loading
- ✅ Dependency resolution with topological sorting
- ✅ API, Widget, Menu, and Data plugin interfaces
- ✅ JWT authentication
- ✅ EF Core with PostgreSQL/SQLite
- ✅ Frontend SPA with routing and dashboard
- ✅ CI/CD pipeline
- ✅ dotnet new plugin template

---

## License

This project is licensed under the MIT License.