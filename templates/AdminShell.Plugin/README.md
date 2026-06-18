# AdminShell Plugin Template

Template para criar plugins AdminShell com backend, frontend, API, serviços, repositórios e entidades geridas automaticamente.

## Instalação local

A partir da raiz do repositório:

```bash
dotnet new install templates/AdminShell.Plugin
```

## Criar um plugin

```bash
dotnet new adminshell-plugin \
  --name ProjectPlugin \
  --PluginId project-plugin \
  --PluginTitle "Project Plugin" \
  --PluginDescription "Manages projects for AdminShell." \
  --output plugins/ProjectPlugin
```

Depois copia a parte frontend gerada para o frontend público:

```bash
cp -a plugins/ProjectPlugin/FrontEnd/public/plugins/project-plugin/. frontend/public/plugins/project-plugin/
```

Se não usares `--output`, move a pasta gerada para:

```txt
plugins/ProjectPlugin
```

## O que o template gera

```txt
ProjectPlugin/
  manifest.json
  Backend/
    ProjectPlugin.csproj
    ProjectPluginPlugin.cs
    Entities/
      ProjectPluginItem.cs
    Repositories/
      IProjectPluginItemRepository.cs
      ProjectPluginItemRepository.cs
    Services/
      IProjectPluginItemService.cs
      ProjectPluginItemService.cs
    Apis/
      ProjectPluginItemApi.cs
  FrontEnd/
    public/plugins/project-plugin/
      plugin.json
      index.js
```

## Convenções automáticas

O AdminShell descobre automaticamente:

- **Entidades geridas** em classes marcadas com `[ManagedEntity]`
- **Services** em pastas/namespaces `Services` ou classes terminadas em `Service`
- **Repositories** em pastas/namespaces `Repositories` ou classes terminadas em `Repository`
- **APIs** em pastas/namespaces `Apis` ou classes terminadas em `Api` que implementem `IApiPlugin`

Service:

```csharp
public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken ct = default);
}

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;

    public ProjectService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken ct = default)
        => _repository.ListAsync(ct);
}
```

Repository:

```csharp
public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct = default);
}

public sealed class ProjectRepository : IProjectRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProjectRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        return (await db.QueryAsync<Project>(
            """
            SELECT Id, Name, IsActive, CreatedAt
            FROM Projects
            ORDER BY Name
            """)).ToList();
    }
}
```

API:

```csharp
using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public sealed class ProjectApi : IApiPlugin
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapPluginApi("project-plugin");

        group.MapGet("/projects", async (IProjectService service, CancellationToken ct) =>
        {
            return Results.Ok(await service.ListAsync(ct));
        })
        .WithName("GetProjects")
        .Produces<List<ProjectDto>>(StatusCodes.Status200OK);
    }
}
```

Entidade gerida:

```csharp
[ManagedEntity]
public sealed class Project
{
    public Guid Id { get; set; }

    [EntityColumn(256, true)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [EntityColumn(defaultSql: "GETUTCDATE()")]
    public DateTime CreatedAt { get; set; }
}
```

Isto cria automaticamente a tabela `Projects` e as colunas em falta.
