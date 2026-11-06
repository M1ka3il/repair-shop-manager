using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using RepairShop.Api.Middleware;
using RepairShop.Api.OpenApi;
using RepairShop.Application;
using RepairShop.Infrastructure;
using RepairShop.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging from the first line, so startup failures are readable too.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

var connectionString = builder.Configuration.GetConnectionString("RepairShopDb")
    ?? throw new InvalidOperationException(
        "Connection string 'RepairShopDb' is not configured. "
        + "Under docker compose it comes from the environment; locally use user secrets "
        + "or appsettings.Development.json.");

// One registration call per layer — the composition root stays readable and the
// dependency direction (API → Application → Domain) is visible at a glance.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Statuses travel as "InProgress", not as 2: the numeric value is a
        // storage detail and should not leak into the public contract.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Repair Shop API",
        Version = "v1",
        Description =
            "Repair shop management as a REST API. Customers own vehicles, vehicles have repair orders, "
            + "repair orders carry labour and parts lines. The order lifecycle "
            + "(Received → InProgress ⇄ WaitingForParts → Completed → Invoiced) is enforced in the domain layer.",
        License = new OpenApiLicense { Name = "MIT" }
    });

    // Swagger is the only UI this service has, so the XML docs are the
    // documentation — worth generating across layers and wiring up here.
    string[] xmlFiles =
    [
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
        "RepairShop.Application.xml",
        "RepairShop.Domain.xml"
    ];

    foreach (var xmlFile in xmlFiles)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }

    options.SupportNonNullableReferenceTypes();

    // Response DTOs always carry every field; say so, so the generated
    // frontend types are not optional everywhere.
    options.SchemaFilter<DtoPropertiesRequiredSchemaFilter>();
});

var app = builder.Build();

// First in the pipeline: nothing downstream can throw past it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Repair Shop API v1");
    // Served at the root: `docker compose up`, then open http://localhost:8080.
    options.RoutePrefix = string.Empty;
});

app.MapControllers();

// Liveness probe for the compose health check.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithTags("Health");

// Integration tests bring their own database and seed, so they opt out.
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitialiseAsync();
}

await app.RunAsync();

/// <summary>
/// Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can boot the real
/// pipeline in the integration tests.
/// </summary>
public partial class Program;
