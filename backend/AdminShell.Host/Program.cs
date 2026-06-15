using System.Text;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure;
using AdminShell.Infrastructure.Data;
using AdminShell.Infrastructure.PluginSystem;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using AdminShell.Host.Middleware;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Event Bus (singleton, shared across all plugins)
var eventBus = new InMemoryEventBus();
builder.Services.AddSingleton<IEventBus>(eventBus);

// Add controllers and OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            }
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
        });

        return Task.CompletedTask;
    });
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret required");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AdminShell",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AdminShell",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSPA", policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add infrastructure layer (Dapper, repositories, services, plugin loader)
builder.Services.AddInfrastructure(builder.Configuration);

// OpenTelemetry — tracing only
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

// Health checks
builder.Services.AddHealthChecks();

// Load & initialize plugins BEFORE building the app (so their services register in DI)
var prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "plugins");
if (!Directory.Exists(prePluginsDir))
    prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "plugins");

if (Directory.Exists(prePluginsDir))
{
    builder.Configuration["Plugins:Directory"] = prePluginsDir;

    var pluginLoader = new PluginLoader(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<PluginLoader>());
    var queryRegistry = new PluginQueryRegistry();
    pluginLoader.SetEventBus(eventBus);
    pluginLoader.LoadPluginsAsync(prePluginsDir).GetAwaiter().GetResult();
    pluginLoader.InitializePlugins(builder.Services, builder.Configuration, queryRegistry);
    builder.Services.AddSingleton<IPluginLoader>(pluginLoader);
    builder.Services.AddSingleton<IQueryRegistry>(queryRegistry);
}
else
{
    Log.Information("No plugins directory found at {Dir}, running without plugins", prePluginsDir);
    builder.Services.AddSingleton<IPluginLoader, PluginLoader>();
}

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseSerilogRequestLogging();
app.UseCors("AllowSPA");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Configure plugins and map their endpoints before OpenAPI is exposed.
var pluginLoader2 = app.Services.GetRequiredService<IPluginLoader>();
pluginLoader2.ConfigurePlugins(app, app.Environment);
pluginLoader2.MapPluginEndpoints(app);

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Admin Shell API"));
app.MapHealthChecks("/healthz");

// Initialize database (create tables + seed) using Dapper
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    var extensionRegistry = scope.ServiceProvider.GetRequiredService<IPluginExtensionRegistry>();
    using var db = connectionFactory.CreateConnection();
    db.Open();
    await extensionRegistry.ApplyAllMigrationsAsync(db);
}

// Fire application started event
_ = eventBus.PublishAsync(new ApplicationStartedEvent(DateTime.UtcNow, pluginLoader2.LoadedPlugins.Count));

Log.Information("Admin Shell API started successfully");
app.Run();