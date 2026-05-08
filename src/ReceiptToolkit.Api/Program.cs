using ReceiptToolkit.Api.Endpoints;
using ReceiptToolkit.Api.Errors;
using ReceiptToolkit.Contracts.Time;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Time;
using ReceiptToolkit.Core.Validation;

const string DevCorsPolicy = "DevelopmentAnyOrigin";
const string ProdCorsPolicy = "ProductionAllowList";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---- Service registrations ---------------------------------------------------------
// FontProvider loads ~880KB of Inter VF bytes on first GetTypeface call. Singleton so
// the hit happens once per process; thread-safe per its <remarks> XML doc.
builder.Services.AddSingleton<FontProvider>();
builder.Services.AddSingleton<IClock, SystemClock>();
// Explicit factory: the (IEnumerable<IValidationRule>) ctor would be picked by DI and
// resolve to an empty rule set since no IValidationRule services are registered.
builder.Services.AddSingleton(_ => new ReceiptValidator());
// ReceiptGenerator is a stateless wrapper over the four singletons above; safe as
// singleton (the (IClock, FontProvider, ReceiptValidator) ctor sets ownsFonts=false so
// disposing it is a no-op for fonts).
builder.Services.AddSingleton(sp => new ReceiptGenerator(
    sp.GetRequiredService<IClock>(),
    sp.GetRequiredService<FontProvider>(),
    sp.GetRequiredService<ReceiptValidator>()));

// ---- ProblemDetails / RFC7807 ------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReceiptExceptionHandler>();

// ---- CORS --------------------------------------------------------------------------
// Development: any origin permitted (Flutter macOS demo, local Postman, etc.).
// Production: allow-list seeded from configuration key Cors:AllowedOrigins (string[]).
// Configuration is read at AddCors-policy-build time, NOT at app-builder time, so test
// overrides via WithWebHostBuilder.ConfigureAppConfiguration land before the policy
// resolves origins.
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
    options.AddPolicy(ProdCorsPolicy, policy =>
    {
        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// ---- OpenAPI -----------------------------------------------------------------------
// Microsoft.AspNetCore.OpenApi serves the spec at /openapi/{documentName}.json by default.
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Order matters: ExceptionHandler converts uncaught throws to RFC7807; StatusCodePages
// converts framework-issued status codes (e.g. 400 from the JSON body binder when the
// payload is malformed) to RFC7807. CORS must run before endpoint matching so preflight
// (OPTIONS) requests are answered without hitting the route table.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(app.Environment.IsDevelopment() ? DevCorsPolicy : ProdCorsPolicy);

app.MapOpenApi();
app.MapReceiptEndpoints();

app.Run();

/// <summary>
///   Marker partial declaration so test projects can reference the auto-generated
///   <c>Program</c> class via <c>WebApplicationFactory&lt;Program&gt;</c>
///   (Microsoft.AspNetCore.Mvc.Testing).
/// </summary>
public partial class Program;
