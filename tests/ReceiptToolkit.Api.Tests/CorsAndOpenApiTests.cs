// Purpose: RED-phase tests for Phase 5 sub-cluster C-D — CORS preflight (T5.11)
//          + OpenAPI document (T5.12).
// Categories: Integration — WebApplicationFactory<Program> drives the in-memory host;
//             T5.11 toggles ASPNETCORE_ENVIRONMENT via WithWebHostBuilder.UseEnvironment
//             to assert dev-vs-prod CORS behaviour without touching real config files.
// Edge cases:
//   T5.11a — Development env: OPTIONS preflight from arbitrary origin returns 2xx +
//            Access-Control-Allow-Origin echoing the request origin (or "*").
//   T5.11b — Production env: OPTIONS preflight from a non-allow-listed origin returns
//            with NO Access-Control-Allow-Origin header (browser-side fail).
//   T5.11c — Production env: OPTIONS preflight from an allow-listed origin returns
//            ACAO matching the origin. Allow-list seeded via in-memory configuration.
//   T5.12  — GET /openapi/v1.json returns 200 + JSON containing an "openapi" key
//            (the version string, e.g. "3.0.4").

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ReceiptToolkit.Api.Tests;

public sealed class CorsAndOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsAndOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // T5.11a — Development env: any origin permitted on preflight.
    [Fact]
    public async Task Cors_Preflight_DevelopmentAllowsAnyOrigin()
    {
        using WebApplicationFactory<Program> dev = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Microsoft.Extensions.Hosting.Environments.Development);
        });
        using HttpClient client = dev.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/receipts/validate");
        preflight.Headers.Add("Origin", "https://random-origin.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        using HttpResponseMessage response = await client.SendAsync(
            preflight,
            TestContext.Current.CancellationToken);

        Assert.True(
            (int)response.StatusCode is >= 200 and < 300,
            $"Expected 2xx, got {(int)response.StatusCode}");

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Development preflight must include ACAO header.");

        string acao = response.Headers.GetValues("Access-Control-Allow-Origin").First();
        Assert.True(acao == "*" || acao == "https://random-origin.example",
            $"Unexpected ACAO value '{acao}'.");
    }

    // T5.11b — Production env, non-allow-listed origin: NO ACAO header on preflight.
    [Fact]
    public async Task Cors_Preflight_ProductionRejectsUnknownOrigin()
    {
        using WebApplicationFactory<Program> prod = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Microsoft.Extensions.Hosting.Environments.Production);
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "https://allowed.example",
                });
            });
        });
        using HttpClient client = prod.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/receipts/validate");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        using HttpResponseMessage response = await client.SendAsync(
            preflight,
            TestContext.Current.CancellationToken);

        Assert.False(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Production preflight from unknown origin must NOT carry ACAO.");
    }

    // T5.11c — Production env, allow-listed origin: ACAO matches.
    [Fact]
    public async Task Cors_Preflight_ProductionAcceptsAllowListedOrigin()
    {
        using WebApplicationFactory<Program> prod = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Microsoft.Extensions.Hosting.Environments.Production);
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "https://allowed.example",
                });
            });
        });
        using HttpClient client = prod.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/receipts/validate");
        preflight.Headers.Add("Origin", "https://allowed.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        using HttpResponseMessage response = await client.SendAsync(
            preflight,
            TestContext.Current.CancellationToken);

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Allow-listed origin must yield ACAO header on preflight.");

        string acao = response.Headers.GetValues("Access-Control-Allow-Origin").First();
        Assert.Equal("https://allowed.example", acao);
    }

    // T5.12 — OpenAPI document at /openapi/v1.json returns 200 + valid OpenAPI JSON.
    [Fact]
    public async Task OpenApi_DocumentAvailable_AtV1Json()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("OpenAPI body was null.");
        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("openapi", out JsonElement openapi));
            Assert.False(string.IsNullOrWhiteSpace(openapi.GetString()));

            Assert.True(root.TryGetProperty("paths", out JsonElement paths));
            Assert.Equal(JsonValueKind.Object, paths.ValueKind);
        }
    }
}
