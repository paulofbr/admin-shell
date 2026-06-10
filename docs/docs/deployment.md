# Deployment Guide

This guide covers deploying Admin Shell to various environments.

---

## Prerequisites

- .NET 9.0 Runtime
- PostgreSQL 15+ (production)
- Reverse proxy (nginx, Caddy, or IIS)
- SSL certificate (Let's Encrypt recommended)

---

## Production Build

### Backend

```bash
# Publish the backend as a self-contained deployment
dotnet publish src/AdminShell.Host/AdminShell.Host.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output ./publish/backend

# Verify the build
./publish/backend/AdminShell.Host --version
```

### Frontend

```bash
cd frontend
npm ci
npm run build

# The output is in frontend/dist/
# Copy this to your web server or the backend's wwwroot
```

---

## Deployment Options

### Option 1: Single Server (Backend + Frontend)

Serve the frontend static files from the backend's `wwwroot/` directory.

```bash
# Copy frontend build to backend
cp -r frontend/dist/* publish/backend/wwwroot/

# Configure appsettings.Production.json
cat > publish/backend/appsettings.Production.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.example.com;Database=adminshell;Username=app;Password=..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "AdminShell",
    "Audience": "AdminShell"
  },
  "Cors": {
    "Origins": "https://admin.example.com"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  }
}
EOF

# Run as a service
sudo cp admin-shell.service /etc/systemd/system/
sudo systemctl enable admin-shell
sudo systemctl start admin-shell
```

### Option 2: Separate Servers (Backend API + Static Frontend)

Deploy the frontend to a CDN or static file server (nginx, Cloudflare Pages, Vercel), and the backend as a separate API server.

**nginx configuration for frontend:**

```nginx
server {
    listen 443 ssl;
    server_name admin.example.com;

    ssl_certificate /etc/letsencrypt/live/admin.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/admin.example.com/privkey.pem;

    root /var/www/admin-shell/dist;
    index index.html;

    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API requests to backend
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Option 3: Docker

```dockerfile
# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/AdminShell.Host/AdminShell.Host.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["./AdminShell.Host"]
```

```yaml
# docker-compose.yml
version: "3.8"

services:
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: adminshell
      POSTGRES_USER: adminshell
      POSTGRES_PASSWORD: changeme
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  backend:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Database=adminshell;Username=adminshell;Password=changeme"
      Jwt__Secret: "your-256-bit-secret-key-here"
    ports:
      - "5000:8080"
    depends_on:
      - db

volumes:
  postgres_data:
```

---

## Environment Configuration

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=adminshell;Username=...;Password=..."
  },
  "Jwt": {
    "Secret": "64-char-hex-or-base64-secret",
    "Issuer": "AdminShell",
    "Audience": "AdminShell",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "Origins": "https://admin.example.com"
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/admin-shell/log-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:5000" }
    }
  }
}
```

---

## systemd Service

```ini
[Unit]
Description=Admin Shell API
After=network.target postgresql.service

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/admin-shell
ExecStart=/opt/admin-shell/AdminShell.Host
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

---

## Database Migrations

```bash
# Apply migrations manually (production)
dotnet ef database update \
    --project src/AdminShell.Infrastructure \
    --startup-project src/AdminShell.Host

# Or generate an SQL script
dotnet ef migrations script \
    --project src/AdminShell.Infrastructure \
    --startup-project src/AdminShell.Host \
    --output migrate.sql

# Apply the script
psql -h db.example.com -U adminshell -d adminshell -f migrate.sql
```

---

## CI/CD Pipeline

The project includes a GitHub Actions workflow (`.github/workflows/ci.yml`) that:

1. **On push/PR to main:** Builds backend, builds frontend, builds plugins, runs tests
2. **On version tag (v*):** Packs and pushes NuGet packages, publishes frontend to npm, creates a GitHub release

For custom deployment, extend the pipeline with deployment steps:

```yaml
deploy:
  name: Deploy to Production
  needs: [backend, frontend, plugins]
  runs-on: ubuntu-latest
  if: github.ref == 'refs/heads/main'

  steps:
    - uses: actions/checkout@v4
    - name: Deploy via SSH
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.DEPLOY_HOST }}
        username: ${{ secrets.DEPLOY_USER }}
        key: ${{ secrets.DEPLOY_KEY }}
        script: |
          cd /opt/admin-shell
          git pull
          dotnet publish -c Release
          sudo systemctl restart admin-shell
```

---

## Health Checks

The application exposes `/api/health` for monitoring:

```bash
curl https://admin.example.com/api/health
# {"status":"Healthy","checks":[...]}
```

Configure your monitoring tool (Prometheus, Datadog, UptimeRobot, etc.) to check this endpoint.

---

## Logging

Logs are written by **Serilog** to:

- **Console** (all environments)
- **File** (production, with daily rolling)
- Additional sinks can be configured (ElasticSearch, Datadog, Seq)

Log level can be adjusted via configuration:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```