// Purpose: RED-phase tests for Phase 5 sub-cluster C-C — RFC7807 ProblemDetails
//          error path (T5.8 malformed JSON, T5.9 validation failure on generation
//          endpoints, T5.10 generation exception with correlation id).
// Categories: Integration — WebApplicationFactory<Program> drives the in-memory host;
//             T5.10 swaps the IClock for a throwing stub via WithWebHostBuilder so
//             the generator pipeline raises mid-render.
// Edge cases:
//   T5.8 — body is the literal string "{not json" with Content-Type:application/json.
//          Default ASP.NET min-API JSON binder responds 400 — must surface as
//          RFC7807 with type/title/status.
//   T5.9 — invalid receipt (empty business name) on /pdf, /png, /both must each
//          return 400 ProblemDetails carrying the validator error list under
//          extensions["errors"].
//   T5.10 — IClock stub throws InvalidOperationException; pipeline catches and
//           returns 500 ProblemDetails. Body must include a non-empty "traceId" /
//           correlation extension so operators can grep logs.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Contracts.Time;

namespace ReceiptToolkit.Api.Tests;

public sealed class ProblemDetailsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProblemDetailsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // T5.8 — malformed JSON body returns 400 RFC7807 ProblemDetails.
    [Fact]
    public async Task Post_Validate_MalformedJson_Returns400ProblemDetails()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent body = new("{not json", System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/validate", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");
        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("type", out _));
            Assert.True(root.TryGetProperty("title", out _));
            Assert.True(root.TryGetProperty("status", out JsonElement status));
            Assert.Equal(400, status.GetInt32());
        }
    }

    // T5.9a — /pdf with invalid data → 400 ProblemDetails with errors[] extension.
    [Fact]
    public async Task Post_Pdf_InvalidData_Returns400ProblemDetails()
    {
        await AssertGenerationEndpointReturns400ProblemDetails("/api/receipts/pdf");
    }

    // T5.9b — /png with invalid data → 400 ProblemDetails with errors[] extension.
    [Fact]
    public async Task Post_Png_InvalidData_Returns400ProblemDetails()
    {
        await AssertGenerationEndpointReturns400ProblemDetails("/api/receipts/png");
    }

    // T5.9c — /both with invalid data → 400 ProblemDetails with errors[] extension.
    [Fact]
    public async Task Post_Both_InvalidData_Returns400ProblemDetails()
    {
        await AssertGenerationEndpointReturns400ProblemDetails("/api/receipts/both");
    }

    private async Task AssertGenerationEndpointReturns400ProblemDetails(string path)
    {
        ReceiptData invalid = ApiTestBase.LoadSampleData() with
        {
            Business = new BusinessInfo { BusinessName = string.Empty },
        };
        string json = invalid.ToJson();

        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(json, System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri(path, UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");
        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("status", out JsonElement status));
            Assert.Equal(400, status.GetInt32());

            // errors[] must be present and non-empty.
            Assert.True(root.TryGetProperty("errors", out JsonElement errors));
            Assert.Equal(JsonValueKind.Array, errors.ValueKind);
            Assert.True(errors.GetArrayLength() > 0);

            JsonElement first = errors[0];
            Assert.True(first.TryGetProperty("field", out _));
            Assert.True(first.TryGetProperty("message", out _));
        }
    }

    // T5.10 — generation exception → 500 ProblemDetails with correlation id (traceId).
    [Fact]
    public async Task Post_Pdf_GenerationException_Returns500ProblemDetailsWithTraceId()
    {
        using WebApplicationFactory<Program> faulty = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? clockDescriptor =
                    services.FirstOrDefault(d => d.ServiceType == typeof(IClock));
                if (clockDescriptor is not null)
                {
                    services.Remove(clockDescriptor);
                }
                services.AddSingleton<IClock, ThrowingClock>();
            });
        });

        using HttpClient client = faulty.CreateClient();
        using StringContent body = new(ApiTestBase.LoadSampleJson(), System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/pdf", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");
        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("status", out JsonElement status));
            Assert.Equal(500, status.GetInt32());

            // Correlation id surface — accept either "traceId" or a custom "correlationId".
            bool hasCorrelation =
                (root.TryGetProperty("traceId", out JsonElement traceId)
                    && !string.IsNullOrWhiteSpace(traceId.GetString()))
                || (root.TryGetProperty("correlationId", out JsonElement corrId)
                    && !string.IsNullOrWhiteSpace(corrId.GetString()));
            Assert.True(hasCorrelation, "Expected a non-empty traceId or correlationId extension.");
        }
    }

    private sealed class ThrowingClock : IClock
    {
        public DateTimeOffset UtcNow =>
            throw new InvalidOperationException("ThrowingClock: simulated generation failure.");
    }
}
