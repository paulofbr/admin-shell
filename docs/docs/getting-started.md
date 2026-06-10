# Getting Started

This guide walks you through setting up the Admin Shell development environment, building the project, and running it for the first time.

---

## Prerequisites

| Tool            | Version | Notes                                      |
|-----------------|---------|--------------------------------------------|
| .NET SDK        | 9.0+    | Required for backend                       |
| Node.js         | 20+     | Required for frontend                      |
| npm             | 10+     | Ships with Node.js                         |
| PostgreSQL      | 15+     | Optional — SQLite used in development      |
| Git             | 2.40+   | For version control                        |

---

## 1. Clone the Repository

```bash
git clone https://github.com/nousresearch/admin-shell.git
cd admin-shell
```

---

## 2. Backend Setup

### Restore and Build

```bash
# Restore NuGet packages
dotnet restore

# Build the entire solution
dotnet build

# Run all backend tests
dotnet test
```

All **9 tests** should pass.

### Configuration

The backend reads configuration from `src/AdminShell.Host/appsettings.json` and `appsettings.Development.json`. Key settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=adminshell;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here-change-in-production",
    "Issuer": "AdminShell",
    "Audience": "AdminShell"
  },
  "Cors": {
    "Origins": "http://localhost:3000"
  }
}
```

> **Tip:** For local development, the application falls back to SQLite if PostgreSQL is unavailable.

### Run the Backend

```bash
cd src/AdminShell.Host
dotnet run
```

The API will be available at `http://localhost:5000`.  
Swagger UI is at `http://localhost:5000/swagger`.

---

## 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm ci

# Build for production
npm run build

# Start development server
npm run dev
```

The frontend development server runs at `http://localhost:3000` and proxies API requests to the backend.

---

## 4. Verify the Setup

Open your browser and navigate to `http://localhost:3000`. You should see:

- The Admin Shell login page
- Swagger documentation at `http://localhost:5000/swagger`
- Health check responding at `http://localhost:5000/api/health`

---

## 5. Running Plugins

Backend plugins are automatically discovered from the `Plugins/` directory. The application includes two sample plugins:

| Plugin            | Description                                       |
|-------------------|---------------------------------------------------|
| **ReportingPlugin** | Adds reporting API endpoints with report generation |
| **UserAuditPlugin** | Provides user audit trail via reporting infrastructure |

These are automatically loaded when the application starts. Log output will confirm plugin initialization:

```
[INF] ReportingPlugin v1.0.0 initialized
[INF] UserAuditPlugin v1.0.0 initialized - depends on ReportingPlugin
```

---

## 6. Creating Your First Plugin

Use the `adminshell-plugin` dotnet new template:

```bash
# Install the template
dotnet new install ./templates/AdminShellPlugin

# Create a new plugin
dotnet new adminshell-plugin -n MyPlugin -o Plugins/Backend/MyPlugin

# Build it
dotnet build Plugins/Backend/MyPlugin/MyPlugin.csproj
```

See the [Plugin Development](plugin-development.md) guide for detailed instructions.

---

## Next Steps

- Explore the [Architecture](architecture.md) deep-dive
- Learn about [Plugin Development](plugin-development.md)
- Review the [API Reference](api-reference.md)
- Read the [Deployment Guide](deployment.md)