// Purpose: RED-phase tests for Phase 3c-polish B — ItemTableSection header casing (T3cP.4).
// Verifies: column header literals uppercase ("ITEM", "QTY", "PRICE", "TOTAL") and
//           header paint uses ThemeColors.DefaultMutedLabelColor (#8A8A8A).
// Body rows unchanged (item names still mixed-case).
// Pixel-mode probe: render to SKBitmap, sample header row y zone for muted colour.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class ItemTableHeaderCasingTests
{
    private const float Width = 360f;
    private const int WidthPx = 360;

    // T3cP.4a — ItemTableSection column headers must render in all-caps:
    //            "ITEM", "QTY", "PRICE", "TOTAL" in the extracted PDF text.
    [Fact]
    public void ItemTableSection_ColumnHeadersAreUppercase()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("ITEM", text, StringComparison.Ordinal);
        Assert.Contains("QTY", text, StringComparison.Ordinal);
        Assert.Contains("PRICE", text, StringComparison.Ordinal);
        Assert.Contains("TOTAL", text, StringComparison.Ordinal);

        // Old mixed-case forms must not appear as standalone headers.
        // Note: "Item" may appear as part of item names, so we check the header-specific
        // lowercase "Qty", "Price", "Total" which only existed as headers.
        Assert.DoesNotContain("Qty", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Price", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Total", text, StringComparison.Ordinal);
    }

    // T3cP.4b — Column header row must render with muted label colour
    //            (ThemeColors.DefaultMutedLabelColor, #8A8A8A) within ±5 RGB.
    //            Probe: header row occupies y=0..RowHeight(16px).
    //            Sample x=0..45, y=3..13 (label glyph zone of ITEM header).
    [Fact]
    public void ItemTableSection_ColumnHeadersRenderMuted()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new ItemTableSection();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float measured = section.Measure(Width, data, ctx);
        int height = Math.Max(1, (int)Math.Ceiling(measured));

        using var bmp = new SKBitmap(WidthPx, height);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);
        }

        // Expected muted label colour: #8A8A8A (same as ThemeColors.DefaultMutedLabelColor defined in 3c-polish B).
        var expectedMuted = new SKColor(0x8A, 0x8A, 0x8A);
        const int Tolerance = 5;

        // Scan the ITEM header glyph zone: x=0..45, y=3..13.
        bool foundMuted = false;
        for (int x = 0; x <= 45 && !foundMuted; x++)
        {
            for (int y = 3; y <= 13 && !foundMuted; y++)
            {
                SKColor px = bmp.GetPixel(x, y);
                if (IsWithinTolerance(px, expectedMuted, Tolerance))
                {
                    foundMuted = true;
                }
            }
        }

        Assert.True(
            foundMuted,
            $"Expected at least one pixel in the ITEM header glyph zone (x=0..45, y=3..13) with colour ≈{expectedMuted} (±{Tolerance} RGB); " +
            $"no such pixel found. Header paint may not be wired to DefaultMutedLabelColor.");
    }

    // T3cP.4c — Body item rows must remain unchanged: item names still present in
    //            mixed-case form, confirming only the header row was changed.
    [Fact]
    public void ItemTableSection_BodyItemRows_RemainsUnchanged()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        // Body row item names — these must still appear (unchanged).
        Assert.Contains("Brake Disc (Front, Pair)", text, StringComparison.Ordinal);
        Assert.Contains("Brake Pad (Front Set)", text, StringComparison.Ordinal);
    }

    private static bool IsWithinTolerance(SKColor actual, SKColor expected, int tolerance)
    {
        return Math.Abs(actual.Red - expected.Red) <= tolerance
            && Math.Abs(actual.Green - expected.Green) <= tolerance
            && Math.Abs(actual.Blue - expected.Blue) <= tolerance;
    }
}
