# Troubleshooting & FAQ

Common issues, solutions, and frequently asked questions.

---

## FAQ

### What is Admin Shell?

Admin Shell is an extensible administration shell framework built on ASP.NET Core 9.0 and Vue 3. It provides a plugin-driven architecture for building enterprise management applications.

### How do plugins work?

Plugins are .NET class libraries that implement `IAdminShellPlugin` (and optional derived interfaces). They are discovered from the `plugins/` directory at startup, ordered by dependency, and loaded via reflection. See the [Plugin Development](plugin-development.md) guide.

### Do I need PostgreSQL?

No. In development mode, the application automatically uses SQLite. PostgreSQL is only required in production.

### Can I use a different database?

Yes. EF Core supports multiple providers. Add the appropriate NuGet package and update the connection string in `appsettings.json`.

### How do I add authentication?

The shell includes JWT authentication out of the box. Configure the `Jwt:Secret`, `Jwt:Issuer`, and `Jwt:Audience` settings. Custom authentication providers can be added by extending the authentication middleware.

### Can plugins have dependencies?

Yes. Declare dependencies in the plugin manifest only. The manifest must use the new `dependencies[]` array with `id` and `version` for each dependency. The plugin loader uses topological sorting to initialize plugins in the correct order.

### How do I debug a plugin?

Build the plugin in Debug mode, place the assembly in the `plugins/` directory, and attach the Visual Studio/Rider debugger to the running `AdminShell.Host` process. Set breakpoints in plugin code.

### Can I hot-reload plugins?

The plugin system supports assembly unloading via custom `AssemblyLoadContext`. However, hot-reloading requires the application to be restarted or the plugin manager to be re-invoked. Full hot-reload support is planned for a future release.

---

## Troubleshooting

### Plugin Not Loaded

**Symptom:** Plugin doesn't appear in logs or `/api/plugins` endpoint.

**Checklist:**

1. **Verify the plugin is in the correct directory.** Plugins should be in `plugins/<PluginName>/Backend/`. The compiled `.dll` should be in `plugins/<PluginName>/Backend/bin/<Configuration>/net10.0/`, backend dependencies should be in `plugins/<PluginName>/Backend/dependencias/`, and `manifest.json` should be in `plugins/<PluginName>/`.

2. **Check manifest.json.** Ensure the file exists, is valid JSON, and has the required `id`, `name`, `version`, and `description` fields.

3. **Verify the assembly implements IAdminShellPlugin.** Open the assembly in a decompiler (like ILSpy or dotPeek) and confirm the plugin class implements `IAdminShellPlugin`.

4. **Check for dependency errors.** If your plugin depends on another plugin that failed to load, your plugin will also fail. Check the application logs for dependency resolution errors.

5. **Check target framework.** Ensure the plugin targets `net10.0`. Plugins targeting other frameworks will not load.

### Build Errors

#### "The framework 'Microsoft.NETCore.App', version '10.0.0' was not found"

Install the .NET 10.0 SDK:

```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

#### "NU1101: Unable to find package AdminShell.Contracts"

The AdminShell.Contracts package is a local project reference. Ensure:

1. The solution is restored with `dotnet restore`
2. The project reference path in `.csproj` is correct
3. You're building from the solution directory

### Runtime Errors

#### "InvalidOperationException: JWT Secret required"

The `Jwt:Secret` configuration key is missing. Add it to `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here-change-in-production"
  }
}
```

#### "Cannot resolve dependency: Plugin 'X' requires 'Y >= 1.0.0'"

The required plugin is missing or has an incompatible version. Ensure the dependency plugin is built and placed in the `plugins/` directory. Check its `manifest.json` for the correct version.

#### "Circular dependency detected between plugins"

Two plugins have mutual dependencies. Review the manifest `dependencies[]` fields to remove the cycle.

#### "The type 'X' exists in both 'Y.dll' and 'Z.dll'"

An assembly version conflict. This typically happens when two plugins reference different versions of the same library. Use binding redirects or ensure all plugins use the same version.

### Database Issues

#### "Cannot open database 'adminshell' requested by the login"

For SQLite: The database file path may be invalid. Check the connection string.

For PostgreSQL: Ensure PostgreSQL is running and the database exists:

```bash
sudo -u postgres psql -c "CREATE DATABASE adminshell;"
```

#### "Migrations are pending"

Run migrations manually:

```bash
dotnet ef database update --project backend/AdminShell.Infrastructure --startup-project backend/AdminShell.Host
```

### Frontend Issues

#### "Module not found: Can't resolve '...'"

Run `npm ci` from the `frontend/` directory to install dependencies. If the error persists, delete `node_modules/` and `package-lock.json`, then run `npm install` again.

#### Blank page on production build

Ensure the backend is configured to serve static files from `wwwroot/` or the frontend is deployed separately to a web server. Check the browser console for errors.

#### CORS errors in browser

Ensure the `Cors:Origins` configuration in the backend matches the frontend URL:

```json
{
  "Cors": {
    "Origins": "http://localhost:3000"
  }
}
```

---

## Getting Help

If you encounter an issue not covered here:

1. **Check application logs** — Serilog writes structured logs that often contain detailed error information.
2. **Enable debug logging** — Set `Serilog.MinimumLevel.Default` to `Debug` in configuration.
3. **Open an issue** — Report bugs and feature requests on the [GitHub Issues](https://github.com/nousresearch/admin-shell/issues) page.
4. **Check existing issues** — Your problem may already be reported and resolved.