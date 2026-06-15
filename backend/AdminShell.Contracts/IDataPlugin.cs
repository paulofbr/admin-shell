namespace AdminShell.Contracts;

/// <summary>
/// Plugin que contribui com consultas e comandos de base de dados via Dapper.
/// Permite aos plugins registar migrações e queries personalizadas.
/// </summary>
public interface IDataPlugin : IPluginComponent
{
    /// <summary>
    /// Aplica migrações da base de dados usando Dapper.
    /// Chamado durante startup após todos os plugins estarem carregados.
    /// </summary>
    Task ApplyMigrationsAsync(IDbConnection connection, CancellationToken ct = default);

    /// <summary>
    /// Registra queries personalizadas que o plugin usa.
    /// Estas queries ficam disponíveis via IQueryRegistry no DI.
    /// </summary>
    void RegisterQueries(IQueryRegistry registry);
}

/// <summary>
/// Registro de queries para injeção de dependência.
/// Usado por plugins para expor queries que outros plugins podem consumir.
/// </summary>
public interface IQueryRegistry
{
    /// <summary>
    /// Regista uma query SQL com um dado identificador.
    /// </summary>
    void Register(string key, string sql);

    /// <summary>
    /// Obtém uma query registada pelo identificador.
    /// </summary>
    string? GetQuery(string key);

    /// <summary>
    /// Lista todos os keys de queries registadas.
    /// </summary>
    IEnumerable<string> ListQueries();
}