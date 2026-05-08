// Purpose: RED-phase test for Phase 3c sub-cluster C absorbed into 3d/B — pins the
//          SkiaReceiptRenderer EmitShadow contract end-to-end (T3c.8, divergence #21).
// Categories: Unit — full-receipt composition. Asserts EmitShadow=true grows both
//              Measure dimensions and paints a non-zero-alpha pixel inside the shadow
//              margin region beyond the paper rect.
// Edge cases: T3c.8 lives at the renderer level rather than the PngExporter level
//              because the shadow contract is a property of the composer (Measure +
//              Render); raster-only exporters merely opt in via the RenderContext flag.
//              Keeping the assertion at this level lets PDF/SVG exporters (which leave
//              EmitShadow=false) reuse the same composer with no shadow path.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering;

/// <summary>
///   RED-phase coverage for the <c>EmitShadow</c> contract on
///   <see cref="SkiaReceiptRenderer"/>. The current scaffold ignores the flag — Measure
///   returns identical sizes regardless of <see cref="RenderContext.EmitShadow"/> and
///   no shadow paint is emitted — so all three sub-assertions fail for the right reason.
/// </summary>
public sealed class SkiaReceiptRendererEmitShadowTests
{
    // T3c.8 — EmitShadow=true grows the canvas on the right and bottom edges and
    // paints a shadow pixel beyond the paper rect. Two invariants in one fact:
    //   1. Measure(ctx{EmitShadow=true}) > Measure(ctx{EmitShadow=false}) on both axes.
    //   2. After Render-ing into a bitmap sized for the with-shadow canvas, a pixel in
    //      the shadow margin (just past the paper's right edge, at vertical mid-height)
    //      has non-zero alpha.
    [Fact]
    public void EmitShadow_True_GrowsCanvasAndPaintsShadowPixel()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();

        using var ctxWithout = new RenderContext(fonts, resolvedLogo: null);
        using var ctxWith = new RenderContext(fonts, resolvedLogo: null) { EmitShadow = true };

        var renderer = new SkiaReceiptRenderer();
        SKSize sizeWithout = renderer.Measure(data, ctxWithout);
        SKSize sizeWith = renderer.Measure(data, ctxWith);

        Assert.True(
            sizeWith.Width > sizeWithout.Width,
            $"Expected shadow to grow width; without={sizeWithout.Width}, with={sizeWith.Width}");
        Assert.True(
            sizeWith.Height > sizeWithout.Height,
            $"Expected shadow to grow height; without={sizeWithout.Height}, with={sizeWith.Height}");

        // Render into a bitmap sized for the with-shadow canvas.
        int w = (int)Math.Ceiling(sizeWith.Width);
        int h = (int)Math.Ceiling(sizeWith.Height);
        using var bmp = new SKBitmap(w, h);
        using (var canvas = new SKCanvas(bmp))
        {
            renderer.Render(canvas, data, ctxWith);
        }

        // Sample a pixel in the bottom-right shadow margin (just inside the canvas,
        // beyond the paper rect). Paper width ≈ sizeWithout.Width; sampling at
        // sizeWithout.Width + 1 lands inside the shadow zone on the right side.
        int sampleX = (int)sizeWithout.Width + 1;
        int sampleY = (int)sizeWithout.Height / 2;
        SKColor pixel = bmp.GetPixel(sampleX, sampleY);
        Assert.True(
            pixel.Alpha > 0,
            $"Expected shadow pixel at ({sampleX},{sampleY}) to have non-zero alpha; got {pixel}.");
    }
}
