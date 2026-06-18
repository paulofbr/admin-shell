# Desenvolvimento local

## Pré-requisitos

- .NET SDK 10
- Node.js 22+
- Docker, para o SQL Server local

## Base de dados local

A aplicação espera o SQL Server em `localhost,1434`.

```bash
docker rm -f adminshell-sql 2>/dev/null || true
docker run -d \
  --name adminshell-sql \
  -p 1434:1433 \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='Admin123!' \
  mcr.microsoft.com/mssql/server:2022-latest
```

A connection string usada pela aplicação é:

```text
Server=localhost,1434;Database=AdminShell;User Id=sa;Password=Admin123!;TrustServerCertificate=true;Encrypt=false;
```

## Frontend

```bash
cd frontend
npm install
npm run lint
npm test -- --run
npm run build
```

O build do frontend faz, nesta ordem:

1. build do plugin `OrderCreationPlugin`
2. cópia do bundle do plugin para `frontend/public/plugins/order-creation`
3. geração do cliente API Orval
4. `vue-tsc`
5. build Vite

Para correr apenas partes específicas:

```bash
npm run build:plugin
npm run build:app
npm run generate:api-client
```

## Plugin OrderCreationPlugin

```bash
cd plugins/OrderCreationPlugin/FrontEnd
npm install
npm run build
```

## Backend e testes

```bash
dotnet restore AdminShell.sln
dotnet test AdminShell.sln
```

## Checklist antes de commitar

- `dotnet test AdminShell.sln`
- `cd frontend && npm run lint`
- `cd frontend && npm test -- --run`
- `cd frontend && npm run build`
- `cd plugins/OrderCreationPlugin/FrontEnd && npm run build`
