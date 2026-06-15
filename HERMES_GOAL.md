# 🏗️ Admin Shell — Goal Prompt

## Visão Geral

Criar uma **shell de administração completa e extensível** que serve como base para qualquer aplicação de gestão empresarial. A shell deve ser modular, permitindo que funcionalidades sejam adicionadas dinamicamente através de **plugins** sem modificar o código base.

---

## Stack Tecnológica

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | .NET 10+ ASP.NET Core Web API (Clean Architecture) |
| **ORM** | Dapper + Microsoft.Data.SqlClient em produção |
| **Database** | SQL Server produção / SQLite dev |
| **Frontend** | Vue 3+ com TypeScript |
| **State** | Pinia |
| **UI Kit** | Element Plus |
| **Roteamento** | Vue Router 4 |
| **Testes** | xUnit (backend) + Vitest (frontend) |
| **Packaging** | NuGet (backend) + npm (frontend) |

---

## Fases de Implementação

### 🟢 Fase 1 — Fundação (Base)

**Objetivo:** Solução base funcional com autenticação e gestão de utilizadores.

**Estrutura de projetos:**
```
admin-shell/
├── backend/
│   ├── AdminShell.Host/                    ← Web API (ponto de entrada)
│   ├── AdminShell.Contracts/               ← Interfaces e contratos (partilhado)
│   ├── AdminShell.Core/                    ← Lógica de domínio
│   ├── AdminShell.Infrastructure/          ← Acesso a dados, serviços externos
│   └── frontend/                           ← SPA Vue 3 (opcional dentro do Host)
├── plugins/
│   ├── auth/                               ← Plugin de autenticação base
│   └── user-management/                    ← Plugin de gestão de utilizadores
├── tests/
│   ├── AdminShell.Core.Tests/
│   └── AdminShell.Infrastructure.Tests/
└── docs/
```

**Especificações Técnicas:**
- Clean Architecture com 4 layers (Presentation, Application, Domain, Infrastructure)
- Autenticação JWT com refresh tokens
- Gestão de utilizadores (CRUD completo com roles)
- Base de dados SQL Server produção + SQLite dev
- Scalar/OpenAPI para documentação da API
- Logging estruturado com Serilog
- Health checks (aplicação, base de dados)
- Rate limiting básico

**Entregáveis:**
1. Solução compilável com `dotnet build`
2. API com `GET /api/health` e `POST /api/auth/login`
3. Página de login da SPA Vue
4. Layout base da shell com sidebar e header
5. Testes unitários para domínio e infraestrutura

---

### 🟡 Fase 2 — Sistema de Plugins Backend

**Objetivo:** Mecanismo de descoberta, carregamento e gestão de plugins no backend.

**Contratos Principais:**
```csharp
// Interface base para todos os plugins
public interface IAdminShellPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    void Initialize(IServiceCollection services, IConfiguration config);
    void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}

// Plugin que contribui endpoints de API
public interface IApiPlugin : IAdminShellPlugin
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

// Plugin que contribui componentes MVC/Razor
public interface IMvcPlugin : IAdminShellPlugin
{
    void ConfigureMvc(IMvcBuilder builder);
}

// Plugin que contribui entidades de dados
public interface IDataPlugin : IAdminShellPlugin
{
    void ConfigureEntities(ModelBuilder modelBuilder);
    void SeedData(ModelBuilder modelBuilder);
}

// Plugin que contribui widgets de UI
public interface IWidgetPlugin : IAdminShellPlugin
{
    IEnumerable<WidgetDescriptor> GetWidgets();
}

// Plugin que contribui itens de menu
public interface IMenuPlugin : IAdminShellPlugin
{
    IEnumerable<MenuItem> GetMenuItems();
}
```

**Sistema de Descoberta (sem MEF):**
- Scanner de assemblies por implementações de `IAdminShellPlugin`
- Ficheiro `manifest.json` em cada pacote de plugin com metadados no formato novo
- Dependências entre plugins declaradas em `dependencies[]` com `id` e `version`
- Ordenação topológica para resolver dependências
- Ciclos de dependência detectados e reportados ao iniciar

**Event Bus (comunicação entre plugins):**
```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;
}
```

**Entregáveis:**
1. `IPluginLoader` — serviço que descobre e carrega plugins
2. `PluginManager` — gestão do ciclo de vida dos plugins
3. `EventBus` — pub/sub para comunicação entre plugins
4. Plugin de exemplo (`ReportingPlugin`) que expõe endpoints e widgets
5. Testes de integração para descoberta, dependências e eventos

---

### 🟠 Fase 3 — Sistema de Plugins Frontend

**Objetivo:** SPA Vue 3 extensível que carrega plugins frontend dinamicamente.

**Arquitetura Frontend:**
```
AdminShell.Web/
├── backend/
│   ├── core/                   ← Código base da shell
│   │   ├── layout/             ← Layout (sidebar, header, content)
│   │   ├── services/           ← Serviços base
│   │   └── plugin-system/      ← Sistema de plugins frontend
│   ├── plugins/                ← Diretório de plugins
│   │   └── [plugin-id]/        ← Cada plugin na sua pasta
│   └── shared/                 ← Componentes partilhados
```

**Contrato Frontend:**
```typescript
interface AdminShellPlugin {
  id: string;
  name: string;
  version: string;
  initialize(container: HTMLElement, services: PluginServices): Promise<void>;
  dispose(): void;
}

interface PluginServices {
  http: HttpClient;
  eventBus: EventBus;
  router: RouterService;
  auth: AuthService;
  ui: UIService;
  notifications: NotificationService;
  storage: StorageService;
  theme: ThemeService;
}
```

**Manifesto do pacote (`manifest.json`):**
```json
{
  "schemaVersion": 1,
  "id": "reporting",
  "name": "Reporting Plugin",
  "version": "1.2.0",
  "description": "Plugin de reporting.",
  "dependencies": [
    {
      "id": "auth",
      "version": ">=1.0.0"
    }
  ]
}
```

**Frontend do plugin:**

- páginas/componentes novos: Vue SFCs (`.vue`);
- código não visual: TypeScript (`.ts`);
- entry compilada/runtime: `frontend/index.js`;
- permissões fora do manifesto, exportadas pelo frontend entry.

**Sistema de Composição de UI:**
- **Menu:** Plugins adicionam itens de menu hierárquicos com base em permissões
- **Widgets:** Plugins registam componentes Vue que aparecem no dashboard
- **Slots:** Pontos de extensão em páginas existentes (`<PluginSlot name="user-profile-actions" />`)
- **Zonas:** Áreas predefinidas (header, sidebar, footer, main) onde plugins podem contribuir

**Entregáveis:**
1. `PluginLoader` — carrega JS/CSS dinamicamente com lazy loading
2. `PluginRegistry` — gestão de plugins frontend registados
3. `PluginSlot` / `PluginZone` — componentes Vue para composição de UI
4. Sistema de isolamento (CSS scoped + sandbox de iframe)
5. Plugin de exemplo frontend (Dashboard Widget + Página de Reports)

---

### 🔴 Fase 4 — Integração + Features Avançadas

**Objetivo:** Integrar sistemas frontend e backend, adicionar funcionalidades enterprise.

**Funcionalidades a Implementar:**
1. **Admin UI de Gestão de Plugins**
   - Lista de plugins instalados (ativos/inativos)
   - Ativar/desativar plugins sem reiniciar
   - Ver dependências, versão, health status
   - Logs e diagnóstico por plugin

2. **Plugin Marketplace (simplificado)**
   - Instalar plugins via upload de zip/nupkg
   - Gestão de versões e compatibilidade
   - Notificações de atualizações disponíveis

3. **Sistema de Permissões Avançado**
   - RBAC (Role-Based Access Control)
   - Permissões por plugin: `plugin-name:action`
   - Políticas de autorização customizadas

4. **Observabilidade**
   - OpenTelemetry: traces e métricas por plugin
   - Logging estruturado por contexto de plugin
   - Dashboard de monitorização interna

5. **Cache Distribuído**
   - Redis como cache distribuído
   - Invalidação por plugin/entidade
   - Suporte a cache por utilizador

6. **Plugins em Hot-Reload (dev)**
   - File watcher para detetar alterações em plugins
   - Recarregar plugin sem reiniciar a aplicação

**Entregáveis:**
1. UI de administração de plugins
2. Sistema de upload e instalação de plugins
3. Integração OpenTelemetry
4. Cache com Redis
5. Hot-reload em desenvolvimento
6. Testes de carga com múltiplos plugins ativos

---

### 🟣 Fase 5 — Polimento + Ferramentas + Samples

**Objetivo:** Tornar o framework fácil de adotar por outros developers.

**Pacotes NuGet Distribuíveis:**
```
AdminShell.Core          ← Domínio base
AdminShell.Contracts     ← Contratos para plugins
AdminShell.Infrastructure ← Implementações base
AdminShell.Web.Core      ← Componentes Vue base
AdminShell.Templates     ← Project templates (dotnet new)
```

**Ferramentas CLI:**
```bash
# Criar novo projeto admin-shell
dotnet new admin-shell -n MyAdminApp

# Adicionar plugin
dotnet admin-shell plugin add MyPlugin

# Criar scaffold de plugin
dotnet admin-shell plugin scaffold com.mycompany.MyPlugin
```

**Documentação:**

- 📖 **Getting Started Guide** — "Cria a tua primeira aplicação admin em 15 minutos"
- 📖 **Plugin Developer Guide** — "Como criar plugins para a admin-shell"
- 📖 **API Reference** — Documentação completa das APIs internas e externas
- 📖 **Best Practices** — Padrões de código, segurança, performance
- 📖 **Migration Guide** — Como migrar de versão
- 📖 **Troubleshooting Guide** — Problemas comuns e soluções

**Plugins de Amostra (samples):**
1. **ReportingPlugin** — Relatórios com gráficos (Chart.js/Recharts)
2. **NotificationPlugin** — Notificações email + push + in-app
3. **AuditLogPlugin** — Log de auditoria com filtros
4. **BackupPlugin** — Backup e restauro da base de dados
5. **ThemePlugin** — Tema customizado com CSS variables
6. **ImportExportPlugin** — Import/Export CSV, Excel, JSON

**Pipelines CI/CD:**
```yaml
# build.yaml
- name: Build Backend
  run: dotnet build -c Release

- name: Test Backend
  run: dotnet test --collect:"XPlat Code Coverage"

- name: Test Frontend
  run: npm test -- --coverage

- name: Build Plugin NuGet
  run: dotnet pack -c Release -o ./nupkg

- name: Build Plugin npm
  run: npm run build --workspace=plugin

- name: Publish NuGet
  run: dotnet nuget push ./nupkg/*.nupkg

- name: Publish npm
  run: npm publish --workspace=plugin
```

**Template de Plugin Completo:**
```
MyPlugin/
├── backend/
│   ├── MyPlugin.csproj
│   ├── Plugin.cs                          ← Implementa IAdminShellPlugin
│   ├── Controllers/
│   ├── Services/
│   └── Data/
├── frontend/
│   ├── package.json
│   ├── backend/
│   │   ├── index.ts
│   │   ├── permissions.ts
│   │   ├── pages/
│   │   │   └── Page.vue
│   │   ├── services/
│   │   └── types/
│   └── vite.config.ts
├── tests/
├── README.md
└── LICENSE
```

---

## Critérios de Sucesso

### Funcionais
- [ ] A shell carrega e inicializa plugins do diretório configurado
- [ ] Plugins podem expor endpoints de API autenticados
- [ ] Plugins podem contribuir itens de menu na sidebar
- [ ] Plugins podem adicionar widgets ao dashboard
- [ ] Plugins comunicam entre si via EventBus
- [ ] Plugin pode ser desativado sem afetar os outros
- [ ] Conflitos de versão são detetados ao iniciar
- [ ] UI de administração mostra todos os plugins e o seu estado

### Não-Funcionais
- [ ] Startup time < 200ms com 5 plugins
- [ ] Overhead de memória < 15MB por plugin ativo
- [ ] UI mantém 60fps com 10 plugins concorrentes
- [ ] Falha de um plugin não quebra a aplicação
- [ ] Cobertura de testes > 80% no core framework
- [ ] Documentação permite criar plugin funcional em < 30 min

---

## Estrutura de Ficheiros Esperada (final)

```
admin-shell/
├── backend/
│   ├── AdminShell.Host/
│   ├── AdminShell.Contracts/
│   ├── AdminShell.Core/
│   ├── AdminShell.Infrastructure/
│   └── AdminShell.Web/
├── plugins/
│   ├── auth/
│   │   ├── backend/
│   │   ├── frontend/
│   │   └── manifest.json
│   ├── user-management/
│   ├── reporting/
│   └── ...
├── samples/
│   ├── hello-world-plugin/
│   └── fullstack-demo/
├── tests/
│   ├── unit/
│   └── integration/
├── docs/
├── .github/
│   └── workflows/
├── package.json           ← Workspaces npm (se monorepo)
├── admin-shell.sln
├── HERMES_GOAL.md
└── README.md
```

---

## Notas sobre MEF

❌ **Não usar MEF (Microsoft Extensibility Framework)** — problemas conhecidos:
- Fragilidade de composição
- Dificuldade de debugging
- Complexidade de versionamento
- Performance overhead desnecessário

✅ **Alternativa adotada:**  
Scanner de assemblies + injeção de dependência nativa do ASP.NET Core (`IServiceCollection`) + carregamento explícito via `Assembly.LoadFrom`. Simples, depurável e previsível.

---

*Goal criado automaticamente a partir dos requisitos em `/home/paulo/admin-shell-requirements.md`*
*Data: $(date +%Y-%m-%d)*
*Versão: 1.0.0*