// Purpose: RED-phase tests for Phase 5 sub-cluster C-B — generation endpoints
//          (T5.4 /pdf, T5.5 /png, T5.6 /both, T5.7 /sample).
// Categories: Integration — WebApplicationFactory<Program> drives the in-memory host.
// Edge cases:
//   T5.4/5 — content-type assertions ignore the optional charset suffix; magic-byte
//            checks confirm a real PDF / PNG, not a stub or an error envelope.
//   T5.6 — both endpoint returns base64 strings; the test decodes and verifies the
//          PDF + PNG magic bytes per branch.
//   T5.7 — sample endpoint requires NO body; uses GET-or-POST POST per plan T5.7
//          ("POST /api/receipts/sample → 200 + application/pdf"). No payload.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReceiptToolkit.Api.Tests;

public sealed class GenerateEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly WebApplicationFactory<Program> _factory;

    public GenerateEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // T5.4 — valid sample → 200 + application/pdf + non-empty PDF bytes.
    [Fact]
    public async Task Post_Pdf_ValidSample_ReturnsPdfBytes()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(ApiTestBase.LoadSampleJson(), System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/pdf", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.True(bytes.Length > 0, "PDF body must be non-empty.");
        Assert.True(bytes.AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic), "Bytes must start with %PDF- magic.");
    }

    // T5.5 — valid sample → 200 + image/png + non-empty PNG bytes.
    [Fact]
    public async Task Post_Png_ValidSample_ReturnsPngBytes()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(ApiTestBase.LoadSampleJson(), System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/png", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.True(bytes.Length > 0, "PNG body must be non-empty.");
        Assert.True(bytes.AsSpan(0, PngMagic.Length).SequenceEqual(PngMagic), "Bytes must start with PNG magic.");
    }

    // T5.6 — valid sample → 200 + JSON { pdfBase64, pngBase64 } that decode to real PDF/PNG.
    [Fact]
    public async Task Post_Both_ValidSample_ReturnsBase64Json()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent body = new(ApiTestBase.LoadSampleJson(), System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/both", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        JsonDocument doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            ApiTestBase.JsonOptions,
            TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Response body was null.");

        using (doc)
        {
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("pdfBase64", out JsonElement pdfB64));
            Assert.Equal(JsonValueKind.String, pdfB64.ValueKind);
            byte[] pdfBytes = Convert.FromBase64String(pdfB64.GetString()!);
            Assert.True(pdfBytes.AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic));

            Assert.True(root.TryGetProperty("pngBase64", out JsonElement pngB64));
            Assert.Equal(JsonValueKind.String, pngB64.ValueKind);
            byte[] pngBytes = Convert.FromBase64String(pngB64.GetString()!);
            Assert.True(pngBytes.AsSpan(0, PngMagic.Length).SequenceEqual(PngMagic));
        }
    }

    // T5.7 — POST /api/receipts/sample with no body → 200 + application/pdf of the bundled fixture.
    [Fact]
    public async Task Post_Sample_NoBody_ReturnsBundledSamplePdf()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/receipts/sample", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.True(bytes.Length > 0, "Sample PDF body must be non-empty.");
        Assert.True(bytes.AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic), "Bytes must start with %PDF- magic.");
    }
}
