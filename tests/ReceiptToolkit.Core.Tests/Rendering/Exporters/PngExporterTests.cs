// Purpose: RED-phase tests for Phase 3d sub-cluster B — PngExporter byte output and
//          dimensions (T3d.5, T3d.6, T3d.7). The exporter currently throws
//          NotImplementedException; these tests pin its public contract before GREEN.
// Categories: Unit — exporter byte stream contract. T3d.5 pins the PNG magic header,
//              T3d.6 pins shadow-disabled bitmap width = receiptWidth * scale, T3d.7
//              round-trips the bytes through SKBitmap.Decode with shadow enabled.
// Edge cases: T3d.6 forces emitShadow=false so width is exact; T3d.7 keeps the default
//              emitShadow=true and only asserts width >= receiptWidth * scale because
//              the shadow margin pushes the bitmap slightly wider.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Exporters;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Exporters;

/// <summary>
///   RED-phase coverage for <c>PngExporter</c> (T3d.5–T3d.7). The production class's
///   <c>Export</c> method currently throws <see cref="NotImplementedException"/> — every
///   fact below fails for the right reason until GREEN lands the rasterise + encode path.
/// </summary>
public sealed class PngExporterTests
{
    // T3d.5 — Export returns a non-empty byte array whose first four bytes are the PNG
    // signature (0x89 0x50 0x4E 0x47 = "\x89PNG"). Defaults are kept (scale=2,
    // emitShadow=true) because the magic bytes are independent of size or shadow.
    [Fact]
    public void Export_ReturnsBytesStartingWithPngMagic()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var exporter = new PngExporter(fonts);

        byte[] bytes = exporter.Export(data);

        Assert.True(
            bytes.Length >= 4,
            $"Expected at least 4 bytes for PNG magic header; got {bytes.Length}.");
        Assert.Equal((byte)0x89, bytes[0]);
        Assert.Equal((byte)0x50, bytes[1]);
        Assert.Equal((byte)0x4E, bytes[2]);
        Assert.Equal((byte)0x47, bytes[3]);
    }

    // T3d.6 — With emitShadow=false the bitmap width is exactly receiptWidth * scale.
    // The sample fixture has layout.receiptWidth = 360 and we pass scale=2 → 720px wide.
    // Height is data-dependent (sum of visible section heights × scale); we only assert
    // it is positive so test stays robust against future fixture edits.
    [Fact]
    public void Export_DimensionsMatchReceiptWidthTimesScale_WhenShadowDisabled()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var exporter = new PngExporter(fonts, scale: 2, emitShadow: false);

        byte[] bytes = exporter.Export(data);
        using SKBitmap? bitmap = SKBitmap.Decode(bytes);

        Assert.NotNull(bitmap);
        Assert.Equal(720, bitmap!.Width);
        Assert.True(bitmap.Height > 0, $"Expected positive bitmap height; got {bitmap.Height}.");
    }

    // T3d.7 — Default-emit-shadow bytes round-trip through SKBitmap.Decode. The shadow
    // margin pushes the bitmap slightly wider than receiptWidth * scale, so we assert
    // width >= 720 (= 360 × 2) rather than equality. Decoding success itself proves the
    // bytes form a valid PNG payload end-to-end (signature, IHDR, IDAT, IEND).
    [Fact]
    public void Export_PngRoundTrip_DecodesViaSKBitmap()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var exporter = new PngExporter(fonts);

        byte[] bytes = exporter.Export(data);
        using SKBitmap? bitmap = SKBitmap.Decode(bytes);

        Assert.NotNull(bitmap);
        Assert.True(
            bitmap!.Width >= 720,
            $"Expected width >= 720 (receiptWidth × scale, plus shadow margin); got {bitmap.Width}.");
        Assert.True(bitmap.Height > 0, $"Expected positive bitmap height; got {bitmap.Height}.");
    }
}
