using System.Text;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure;
using AdminShell.Infrastructure.Data;
using AdminShell.Infrastructure.PluginSystem;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AdminShell.Host.Middleware;
using OpenTelemetry.Trace;
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

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Admin Shell API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
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
var prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "Plugins");
if (!Directory.Exists(prePluginsDir))
    prePluginsDir = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Plugins");

if (Directory.Exists(prePluginsDir))
{
    builder.Configuration["Plugins:Directory"] = prePluginsDir;

    var pluginLoader = new PluginLoader(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<PluginLoader>());
    pluginLoader.SetEventBus(eventBus);
    pluginLoader.LoadPluginsAsync(prePluginsDir).GetAwaiter().GetResult();
    pluginLoader.InitializePlugins(builder.Services, builder.Configuration);
    builder.Services.AddSingleton<IPluginLoader>(pluginLoader);
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
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseSerilogRequestLogging();
app.UseCors("AllowSPA");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

// Configure plugins and map their endpoints (post-build)
var pluginLoader2 = app.Services.GetRequiredService<IPluginLoader>();
pluginLoader2.ConfigurePlugins(app, app.Environment);
pluginLoader2.MapPluginEndpoints(app);

// Initialize database (create tables + seed) using Dapper
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Fire application started event
_ = eventBus.PublishAsync(new ApplicationStartedEvent(DateTime.UtcNow, pluginLoader2.LoadedPlugins.Count));

Log.Information("Admin Shell API started successfully");
app.Run();