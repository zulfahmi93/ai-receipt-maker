// Purpose: RED-phase tests for Phase 5 sub-cluster C-A — root endpoint (T5.1).
//          Pin GET / contract: 200 OK + JSON body with {service, version, status:"ok"}.
// Categories: Integration — WebApplicationFactory<Program> drives the in-memory host.
// Edge cases: status field must be the literal string "ok" so health probes can pin it.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReceiptToolkit.Api.Tests;

public sealed class RootEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RootEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // T5.1 — GET / returns 200 + JSON {service, version, status:"ok"}.
    [Fact]
    public async Task Get_Root_Returns200WithServiceJson()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");

        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);

            Assert.True(root.TryGetProperty("service", out JsonElement service));
            Assert.Equal(JsonValueKind.String, service.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(service.GetString()));

            Assert.True(root.TryGetProperty("version", out JsonElement version));
            Assert.Equal(JsonValueKind.String, version.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(version.GetString()));

            Assert.True(root.TryGetProperty("status", out JsonElement status));
            Assert.Equal("ok", status.GetString());
        }
    }
}
