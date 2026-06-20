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
using AdminShell.Host.Services;
using Asp.Versioning;
using FluentValidation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

var eventBus = new InMemoryEventBus();
builder.Services.AddSingleton<IEventBus>(eventBus);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

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

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("ADMIN_SHELL_JWT_SECRET")
    ?? throw new InvalidOperationException(
        "JWT Secret required. Set ADMIN_SHELL_JWT_SECRET env var or 'dotnet user-secrets set Jwt:Secret <value>'.");

builder.Configuration["Jwt:Secret"] = jwtSecret;

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSPA", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://192.168.1.72:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ILogFileReader, SerilogLogFileReader>();
builder.Services.AddSingleton<INotificationBroadcaster, NotificationBroadcaster>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

builder.Services.AddHealthChecks();

var queryRegistry = new PluginQueryRegistry();
builder.Services.AddSingleton<IQueryRegistry>(queryRegistry);

// Plugin loading — synchronous phase (register services before Build)
var prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "plugins");
if (!Directory.Exists(prePluginsDir))
    prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "plugins");
builder.Configuration["Plugins:Directory"] = prePluginsDir;

if (Directory.Exists(prePluginsDir))
{
    var pluginLoader = new PluginLoader(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginLoader>.Instance);
    pluginLoader.SetEventBus(eventBus);
    pluginLoader.LoadPluginsAsync(prePluginsDir).GetAwaiter().GetResult();
    pluginLoader.InitializePlugins(builder.Services, builder.Configuration, queryRegistry);
    builder.Services.AddSingleton<IPluginLoader>(pluginLoader);
    Log.Information("Plugins loaded and initialized from {Dir}", prePluginsDir);
}
else
{
    builder.Services.AddSingleton<IPluginLoader>(sp =>
        new PluginLoader(sp.GetRequiredService<ILogger<PluginLoader>>()));
    Log.Information("No plugins directory at {Dir}, running without plugins", prePluginsDir);
}

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseSerilogRequestLogging();
app.UseCors("AllowSPA");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminShell.Host.Hubs.NotificationHub>("/api/v{version:apiVersion}/hub/notifications");

var pluginLoader2 = app.Services.GetRequiredService<IPluginLoader>();
pluginLoader2.ConfigurePlugins(app, app.Environment);
pluginLoader2.MapPluginEndpoints(app);

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Admin Shell API"));
app.MapHealthChecks("/healthz");

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

_ = eventBus.PublishAsync(new ApplicationStartedEvent(DateTime.UtcNow, pluginLoader2.LoadedPlugins.Count));

Log.Information("Admin Shell API started successfully");
app.Run();
