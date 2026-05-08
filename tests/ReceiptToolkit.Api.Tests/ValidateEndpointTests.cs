// Purpose: RED-phase tests for Phase 5 sub-cluster C-A — POST /api/receipts/validate
//          (T5.2 valid path, T5.3 invalid path).
// Categories: Integration — WebApplicationFactory<Program> drives the in-memory host.
// Edge cases:
//   T5.2 — valid sample fixture returns {valid:true, errors:[]}; never 4xx.
//   T5.3 — empty business name fires BusinessNameRule (single-rule trigger).
//          Endpoint MUST return 200, NOT 400 — validation errors are reported in body
//          per plan T5.3 ("validate endpoint does not 400 on validation failures;
//          structural JSON failure → 400"). Body shape: {valid:false, errors:[...]}
//          where each error carries Field + Message.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Api.Tests;

public sealed class ValidateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValidateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // T5.2 — valid sample fixture → 200 {valid:true, errors:[]}.
    [Fact]
    public async Task Post_Validate_ValidSample_Returns200WithValidTrue()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(ApiTestBase.LoadSampleJson(), System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/validate", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");

        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("valid", out JsonElement valid));
            Assert.Equal(JsonValueKind.True, valid.ValueKind);

            Assert.True(root.TryGetProperty("errors", out JsonElement errors));
            Assert.Equal(JsonValueKind.Array, errors.ValueKind);
            Assert.Equal(0, errors.GetArrayLength());
        }
    }

    // T5.3 — invalid data (empty businessName) → 200 {valid:false, errors:[...]}.
    [Fact]
    public async Task Post_Validate_InvalidData_Returns200WithErrors()
    {
        ReceiptData invalid = ApiTestBase.LoadSampleData() with
        {
            Business = new BusinessInfo { BusinessName = string.Empty },
        };
        string json = invalid.ToJson();

        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(json, System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/validate", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");

        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("valid", out JsonElement valid));
            Assert.Equal(JsonValueKind.False, valid.ValueKind);

            Assert.True(root.TryGetProperty("errors", out JsonElement errors));
            Assert.Equal(JsonValueKind.Array, errors.ValueKind);
            Assert.True(errors.GetArrayLength() > 0);

            // First error should carry field + message.
            JsonElement first = errors[0];
            Assert.True(first.TryGetProperty("field", out JsonElement field));
            Assert.Equal(JsonValueKind.String, field.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(field.GetString()));

            Assert.True(first.TryGetProperty("message", out JsonElement message));
            Assert.Equal(JsonValueKind.String, message.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(message.GetString()));

            // At least one error must reference business.businessName.
            bool businessHit = false;
            foreach (JsonElement err in errors.EnumerateArray())
            {
                string? f = err.GetProperty("field").GetString();
                if (f is not null && f.Contains("business", StringComparison.OrdinalIgnoreCase))
                {
                    businessHit = true;
                    break;
                }
            }

            Assert.True(businessHit, "Expected at least one error referencing the business field.");
        }
    }
}
