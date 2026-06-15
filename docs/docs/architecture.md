# Architecture

This document provides a deep-dive into the Admin Shell architecture, covering layering, the plugin system, dependency injection, and key design decisions.

---

## Clean Architecture

Admin Shell follows **Clean Architecture** principles, with four distinct layers:

```
┌────────────────────────────────────────────────────────┐
│                    Presentation                         │
│                (AdminShell.Host)                        │
│   Controllers · Middleware · Configuration · Startup    │
├────────────────────────────────────────────────────────┤
│                    Application                          │
│               (AdminShell.Core)                         │
│   Services · Interfaces · DTOs · Use Cases · Events     │
├────────────────────────────────────────────────────────┤
│                    Domain                               │
│              (AdminShell.Contracts)                     │
│   Plugin Interfaces · Records · Enums · Attributes      │
├────────────────────────────────────────────────────────┤
│                  Infrastructure                         │
│           (AdminShell.Infrastructure)                   │
│   EF Core · Repositories · PluginLoader · Auth · Log   │
└────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer              | Project                 | Responsibility                                 |
|-------------------|-------------------------|-------------------------------------------------|
| **Presentation**  | `AdminShell.Host`       | HTTP entry point, middleware pipeline, config    |
| **Application**   | `AdminShell.Core`       | Business logic, service orchestration, use cases |
| **Domain**        | `AdminShell.Contracts`  | Shared contracts, plugin interfaces, records     |
| **Infrastructure**| `AdminShell.Infrastructure` | Data access, external services, plugin loader |

---

## Plugin System Architecture

The plugin system is the most architecturally significant component. It follows a **discovery → resolution → loading → initialization** pipeline.

### Discovery Phase

```
plugins/
├── ReportingPlugin/
│   ├── manifest.json
│   ├── Backend/
│   │   ├── bin/
│   │   ├── dependencias/
│   │   └── ReportingPlugin.csproj
│   └── FrontEnd/
├── UserAuditPlugin/
│   ├── manifest.json
│   ├── Backend/
│   │   └── UserAuditPlugin.csproj
│   └── FrontEnd/
└── OrderCreationPlugin/
    ├── manifest.json
    ├── Backend/
    │   └── OrderCreationPlugin.csproj
    └── FrontEnd/
```

1. `PluginLoader` scans the configured plugins directory
2. Each subdirectory containing `.dll` files is a candidate plugin
3. `manifest.json` is read for metadata (ID, version, dependencies)
4. Assemblies are loaded into a custom `AssemblyLoadContext` for isolation

### Dependency Resolution

```
      ┌──────────────┐
      │ Reporting    │
      │   v1.0.0     │
      └──────────────┘
            ▲
            │ depends on
      ┌──────────────┐
      │ UserAudit    │
      │   v1.0.0     │
      └──────────────┘
```

Dependencies are declared only in the manifest:

```json
{
  "id": "useraudit",
  "dependencies": [
    {
      "id": "reporting",
      "version": ">= 1.0.0"
    }
  ]
}
```

The loader uses **topological sorting** (Kahn's algorithm) to determine initialization order. Circular dependencies are detected and reported as errors.

### Loading Phase

1. Types implementing `IAdminShellPlugin` are discovered via reflection
2. Each type is instantiated with `Activator.CreateInstance`
3. Plugin instances are paired with their `PluginDescriptor` metadata
4. The ordered list is passed to the initialization phase

### Initialization Phase

```
foreach plugin in topologicalOrder:
    plugin.Initialize(services, configuration)   ← Register DI services

// Application builds the service provider

foreach plugin in topologicalOrder:
    plugin.Configure(app, env)                   ← Register middleware

foreach apiPlugin in topologicalOrder:
    apiPlugin.MapEndpoints(endpoints)            ← Register API endpoints
```

### Assembly Isolation

Plugins are loaded into a **custom `AssemblyLoadContext`** (not the default context) to:

- Allow unloading plugin assemblies at runtime
- Prevent assembly version conflicts between plugins
- Enable security boundaries between plugin code

---

## Dependency Injection

Admin Shell uses the built-in .NET DI container. Plugins register their services during `Initialize()`:

```csharp
public void Initialize(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IReportService, ReportService>();
    services.Configure<ReportOptions>(configuration.GetSection("Reporting"));
}
```

### Service Lifetimes

| Lifetime    | Use Case                         |
|-------------|----------------------------------|
| `Singleton` | Caching, configuration, logging  |
| `Scoped`    | Per-request services, DbContext  |
| `Transient` | Lightweight, stateless services  |

---

## Event Bus

The `IEventBus` interface enables loosely-coupled communication between plugins:

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;
}
```

### Usage Example

```csharp
// In ReportingPlugin
public class ReportingPlugin : IAdminShellPlugin
{
    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
    }
}

// In UserAuditPlugin
public class UserAuditPlugin : IAdminShellPlugin
{
    private IDisposable? _subscription;

    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();
        _subscription = eventBus.Subscribe<ReportGeneratedEvent>(async (evt, ct) =>
        {
            // Log audit entry
        });
    }
}
```

---

## Authentication & Authorization

### JWT Flow

```
┌──────┐     POST /api/auth/login     ┌──────────┐
│Client│ ──────────────────────────►  │  Backend  │
│      │ ◄──────────────────────────  │          │
└──────┘     { token, expiresAt }     └──────────┘

┌──────┐     GET /api/users           ┌──────────┐
│Client│ ──── Authorization: Bearer──►│  Backend  │
│      │ ◄──────────────────────────  │          │
└──────┘     200 OK + data           └──────────┘
```

1. User authenticates with email/password (or other provider)
2. Backend validates credentials and returns a JWT
3. Client includes the token in subsequent requests
4. Middleware validates the token on every request
5. `[Authorize]` attributes enforce role-based access

### Permission Model

Permissions are string-based and follow the convention `{resource}:{action}`:

- `users:read`, `users:create`, `users:update`, `users:delete`
- `reports:read`, `reports:create`
- `audit:read`

---

## Frontend Architecture

```text
frontend/
├── src/
│   ├── core/
│   │   ├── layout/          ← Sidebar, Header, Content area
│   │   ├── services/        ← API client, Auth, EventBus
│   │   └── plugin-system/   ← Frontend plugin loader
│   ├── components/
│   │   ├── common/          ← DataTable, Modal, etc.
│   │   └── dashboard/       ← Dashboard layout
│   ├── pages/               ← Route pages (.vue)
│   │   ├── auth/            ← Login page
│   │   ├── dashboard/       ← Main dashboard
│   │   └── users/           ← User management
│   ├── composables/         ← Vue composables
│   ├── stores/              ← Pinia stores
│   └── types/               ← TypeScript interfaces
```

### Plugin Frontend Convention

Plugin frontend packages are built by the plugin author and deployed under `frontend/`.

- New pages/components: use Vue SFCs (`.vue`).
- Non-UI code: use TypeScript (`.ts`), e.g. services, types, permissions, composables.
- Entry file: `index.js` or compiled bundle produced from the plugin frontend build.
- Permissions stay outside the manifest and are exported by the frontend entry:

```ts
export const permissions = {
  reportsRead: ['reports:read'],
}
```

### State Management

Pinia is used for client-side state.

```typescript
interface AuthState {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: () => boolean;
}
```

### API Client

Axios is configured with interceptors for automatic token injection and refresh:

```typescript
const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use(config => {
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
```

---

## Database

### Development (SQLite)

Auto-configured in `appsettings.Development.json`. No external database server needed.

### Production (PostgreSQL)

Configured via `ConnectionStrings:DefaultConnection`. EF Core migrations are applied automatically on startup (development only) or via the `dotnet ef database update` command.

### Migration Strategy

- Backend uses EF Core Code-First with migrations in `AdminShell.Infrastructure`
- Migrations are auto-applied in development mode
- Production uses explicit migration via CI/CD pipeline

---

## Middleware Pipeline

```
Request
  │
  ▼
Security Headers ───────────────────── app.UseSecurityHeaders()
  │
  ▼
Serilog Request Logging ────────────── app.UseSerilogRequestLogging()
  │
  ▼
CORS ───────────────────────────────── app.UseCors("AllowSPA")
  │
  ▼
Authentication ─────────────────────── app.UseAuthentication()
  │
  ▼
Authorization ──────────────────────── app.UseAuthorization()
  │
  ▼
Controllers ────────────────────────── app.MapControllers()
  │
  ▼
Health Checks ──────────────────────── app.MapHealthChecks("/api/health")
  │
  ▼
plugins/ ───────────────────────────── Plugin loader (runtime)
  │
  ▼
Response
```