// Purpose: RED-phase tests for Phase 3c-polish B — MetaSection label casing (T3cP.3).
// Verifies: label literals uppercase ("RECEIPT NO.", "DATE & TIME"), label paint uses
//           DefaultMutedLabelColor (mid-grey #8A8A8A), value paint unchanged.
// Pixel-mode probe pattern: render section to SKBitmap, sample a known coordinate
//   inside a label-glyph bounding box, compare against DefaultMutedLabelColor ±5 RGB.
//   See divergence #17 (CustomerCashierSection gutter test) for the established pattern.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class MetaSectionLabelCasingTests
{
    private const float Width = 360f;
    private const int WidthPx = 360;

    // T3cP.3a — MetaSection must render "RECEIPT NO." (all-caps) and must NOT render
    //            the old mixed-case form "Receipt No." in the PDF text extraction.
    [Fact]
    public void MetaSection_ReceiptNoLabelIsUppercase()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new MetaSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        // Must contain new uppercase form.
        Assert.Contains("RECEIPT NO.", text, StringComparison.Ordinal);

        // Must NOT contain old mixed-case form (regression guard).
        Assert.DoesNotContain("Receipt No.", text, StringComparison.Ordinal);
    }

    // T3cP.3b — MetaSection must render "DATE & TIME" (all-caps) for the combined row.
    [Fact]
    public void MetaSection_DateAndTimeLabelIsUppercase()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new MetaSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        // Must contain new uppercase form.
        Assert.Contains("DATE & TIME", text, StringComparison.Ordinal);

        // Must NOT contain old mixed-case form.
        Assert.DoesNotContain("Date & Time", text, StringComparison.Ordinal);
    }

    // T3cP.3c — Label text must render with a mid-grey muted colour matching
    //            ThemeColors.DefaultMutedLabelColor (#8A8A8A) within ±5 RGB tolerance.
    //            Probe position: label column of the first row (RECEIPT NO.), ~(4px, labelFontSize-2px).
    //            The section renders left-aligned label starting at originX=0; within the first
    //            RowHeight (16px), a glyph pixel should exist in the left region at approximately
    //            (4, 9) (label baseline = fontSize=11, so around y=11, scan row y=9 for ascender).
    [Fact]
    public void MetaSection_LabelTextRendersMuted()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new MetaSection();
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

        // Sample a horizontal band in the label column (x=0..45) at y rows 3..13 (label glyph zone).
        // Find at least one pixel that is the muted colour within tolerance.
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
            $"Expected at least one pixel in the label column (x=0..45, y=3..13) with colour ≈{expectedMuted} (±{Tolerance} RGB); " +
            $"no such pixel found. Label paint may not be wired to DefaultMutedLabelColor.");
    }

    private static bool IsWithinTolerance(SKColor actual, SKColor expected, int tolerance)
    {
        return Math.Abs(actual.Red - expected.Red) <= tolerance
            && Math.Abs(actual.Green - expected.Green) <= tolerance
            && Math.Abs(actual.Blue - expected.Blue) <= tolerance;
    }
}
