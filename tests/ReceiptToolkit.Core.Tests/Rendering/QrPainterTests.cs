// Purpose: RED-phase tests for Phase 3 (T3.9) — QrPainter.Paint canvas output.
// Categories: Unit — in-process SkiaSharp canvas painting; tests that Paint draws
//             actual QR module cells into the canvas (non-zero black pixel count)
//             and that the drawn ratio is within a plausible range for a QR code
//             (not a blank canvas, not a solid fill).
// Edge cases: pixel ratio bounds (>10% black, <90% black) validates that the QR
//             matrix was rasterized as discrete cells rather than a trivial fill.

using SkiaSharp;
using ReceiptToolkit.Core.Rendering;

namespace ReceiptToolkit.Core.Tests.Rendering;

public sealed class QrPainterTests
{
    // T3.9 — Paint draws QR module rectangles into a 100x100 bitmap canvas.
    //         Verifies: at least one black pixel exists (Paint drew something),
    //         and that the black-pixel fraction is between 10% and 90% (sanity
    //         bounds that confirm a proper QR matrix, not a blank or solid canvas).
    [Fact]
    public void QrPainter_Paint_DrawsModulesIntoRect()
    {
        using var bmp = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        QrPainter.Paint(canvas, "https://example.com", new SKRect(0, 0, 100, 100), SKColors.Black);

        int total = 100 * 100;
        int black = 0;
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                if (bmp.GetPixel(x, y) == SKColors.Black)
                {
                    black++;
                }
            }
        }

        Assert.True(black > 0, "Expected at least one black pixel — Paint drew nothing");
        Assert.True(black > total * 0.10, $"Black pixel ratio {black}/{total} is below 10%; QR matrix was not rasterized");
        Assert.True(black < total * 0.90, $"Black pixel ratio {black}/{total} exceeds 90%; canvas appears solid-filled rather than QR-patterned");
    }
}
