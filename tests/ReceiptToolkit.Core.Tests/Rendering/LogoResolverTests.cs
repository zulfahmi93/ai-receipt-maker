// Purpose: RED-phase tests for Phase 3 (T3.1–T3.4) — LogoResolver.Resolve.
// Categories: Unit — pure in-process asset loading; tests null passthrough, local PNG
//             file loading, base64 data-URI decoding, and HTTP/HTTPS rejection.
// Edge cases: null input, absolute file path, data:image/png base64 URI, HTTPS URL.

using SkiaSharp;
using ReceiptToolkit.Core.Rendering.Assets;

namespace ReceiptToolkit.Core.Tests.Rendering;

public sealed class LogoResolverTests
{
    // T3.1 — Null source returns null (no image to resolve).
    [Fact]
    public void LogoResolver_Null_ReturnsNull()
    {
        SKImage? result = LogoResolver.Resolve(null);

        Assert.Null(result);
    }

    // T3.2 — Absolute file path to a valid PNG returns a non-null SKImage with positive dimensions.
    //         Arrange: write a 1x1 PNG to a temp file; Act: Resolve; Assert: non-null + Width>0 + Height>0.
    [Fact]
    public void LogoResolver_FilePath_ReturnsImage()
    {
        // Arrange — create a 1x1 PNG on disk.
        string tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        using var bitmap = new SKBitmap(1, 1);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(tempPath, data.ToArray());

        SKImage? result = null;
        try
        {
            // Act
            result = LogoResolver.Resolve(tempPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Width > 0);
            Assert.True(result.Height > 0);
        }
        finally
        {
            result?.Dispose();
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // T3.3 — data:image/png;base64,... URI returns a non-null SKImage.
    //         Uses a known-good 1x1 transparent PNG encoded as base64.
    [Fact]
    public void LogoResolver_DataUriBase64_ReturnsImage()
    {
        // 1x1 transparent PNG, base64-encoded.
        const string base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
        string dataUri = "data:image/png;base64," + base64;

        using SKImage? result = LogoResolver.Resolve(dataUri);

        Assert.NotNull(result);
    }

    // T3.4 — HTTPS URL throws NotSupportedException with a message containing "HTTP" (case-insensitive).
    //         HTTP fetching is explicitly prohibited in the renderer (hard rule: file path + data: URI only).
    [Fact]
    public void LogoResolver_HttpsUrl_Throws()
    {
        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => LogoResolver.Resolve("https://example.com/logo.png"));

        Assert.Contains("HTTP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
