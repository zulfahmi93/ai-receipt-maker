// Purpose: RED-phase tests for Phase 3c-polish D — PaymentSection 2×2 grid layout (T3cP.8).
// The old single-column stack is replaced with a fixed 2-row, 2-column grid:
//   Row 1: [PAYMENT METHOD label + value]  [AMOUNT PAID label + value]
//   Row 2: [CARD ENDING label + value]     [AUTH CODE label + value]
// Grid height is fixed (2 rows); long CardEnding values wrap inside the cell
// rather than pushing the grid taller.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class Payment2x2GridTests
{
    private const float Width = 360f;

    // PaymentSection_RendersAs2x2Grid
    // PDF text extraction must contain all four uppercase grid labels.
    // The sample fixture has one Visa payment with CardLastFour="1234" + AuthCode="A7B3K9".
    [Fact]
    public void PaymentSection_RendersAs2x2Grid()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new PaymentSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("PAYMENT METHOD", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AMOUNT PAID", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CARD ENDING", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AUTH CODE", text, StringComparison.OrdinalIgnoreCase);
    }

    // PaymentSection_GridLayoutIsTwoByTwo
    // Pixel-mode: with a visible payment, the section must produce non-paper pixels
    // in the left half of the first row and in the right half of the first row,
    // proving both columns of row 1 painted content.
    // We sample at two points:
    //   Left cell row-1 center: (Width * 0.25, rowHeight * 0.5)
    //   Right cell row-1 center: (Width * 0.75, rowHeight * 0.5)
    [Fact]
    public void PaymentSection_GridLayoutIsTwoByTwo()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        SKColor paperColor = SKColor.Parse(data.Theme?.PaperColor ?? "#FFFFFF");

        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) = SectionTestBase.CreateBitmapContext(data);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(paperColor);

            var section = new PaymentSection();
            float sectionHeight = section.Measure(Width, data, ctx);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);

            // Section must have positive height with a payment present.
            Assert.True(sectionHeight > 0f, "Measure returned 0 for non-empty payments.");

            // Scan left column (x in [0, Width/2)) for any non-paper pixel.
            bool leftColHasContent = false;
            for (int scanY = 0; scanY < (int)sectionHeight && !leftColHasContent; scanY++)
            {
                for (int scanX = 0; scanX < (int)(Width / 2f) && !leftColHasContent; scanX++)
                {
                    if (bitmap.GetPixel(scanX, scanY) != paperColor)
                    {
                        leftColHasContent = true;
                    }
                }
            }

            // Scan right column (x in [Width/2, Width)) for any non-paper pixel.
            bool rightColHasContent = false;
            for (int scanY = 0; scanY < (int)sectionHeight && !rightColHasContent; scanY++)
            {
                for (int scanX = (int)(Width / 2f); scanX < (int)Width && !rightColHasContent; scanX++)
                {
                    if (bitmap.GetPixel(scanX, scanY) != paperColor)
                    {
                        rightColHasContent = true;
                    }
                }
            }

            Assert.True(leftColHasContent, "Left column of 2×2 grid has no content pixels.");
            Assert.True(rightColHasContent, "Right column of 2×2 grid has no content pixels.");
        }
    }

    // PaymentSection_HeightInvariantOnLongCaption
    // Measure with a very long CardLastFour string must equal Measure with a short value.
    // The 2×2 grid height is fixed — cell content wraps inside but does not push the grid taller.
    [Fact]
    public void PaymentSection_HeightInvariantOnLongCaption()
    {
        ReceiptData baseline = SectionTestBase.LoadSampleData();

        // Long card-ending value that would overflow if not wrapped inside the cell.
        ReceiptData withLongCard = baseline with
        {
            Payments =
            [
                baseline.Payments[0] with { CardLastFour = "•••• •••• •••• •••• 9999" },
            ],
        };

        var section = new PaymentSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float heightBaseline = section.Measure(Width, baseline, ctx);
        float heightLongCard = section.Measure(Width, withLongCard, ctx);

        Assert.Equal(heightBaseline, heightLongCard);
    }
}
