// Purpose: RED-phase tests for Phase 3c-polish D — TotalsSection divider rule above Subtotal
//          row (T3cP.7).  Three invariants:
//          1. A horizontal rule pixel appears in the top padding zone (y < 4) above any text,
//             proving the rule stroke was painted (without rule, only paper color is there).
//          2. The TOTAL band fill is non-zero alpha and not pure paper white.
//          3. Measure with the rule present grows beyond the no-rule baseline by at least
//             (ruleStrokeWidth + ruleTopPadding) = 5px; asserting Measure > pre-rule-expected.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class TotalsSectionRuleTests
{
    private const float Width = 360f;

    // TotalsSection_DrawsRuleAboveSubtotal
    // Pixel-mode: render section at origin (0,0). The Subtotal row text baseline is at
    // y ≈ RuleTopPadding + ruleStrokeWidth + LabelFontSize ≈ 4+1+11 = 16 with the rule.
    // Without a rule, the first text baseline is at y = LabelFontSize = 11 (current code),
    // so the zone y in [0, 3] is pure paper color. With the rule, that zone should contain
    // a non-paper stroke pixel.
    // We scan y in [0, 3], x across full width — all must be paper without rule (currently).
    [Fact]
    public void TotalsSection_DrawsRuleAboveSubtotal()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        SKColor paperColor = SKColor.Parse(data.Theme?.PaperColor ?? "#FAF8F3");

        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) = SectionTestBase.CreateBitmapContext(data);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(paperColor);

            var section = new TotalsSection();
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);

            // Scan y = [0, 5] — the rule stroke lands at y = RuleTopPadding = 4.
            // With rule present, at least one pixel at y=4 must differ from paper color.
            bool rulePixelFound = false;
            for (int scanY = 0; scanY <= 5 && !rulePixelFound; scanY++)
            {
                for (int scanX = 0; scanX < (int)Width && !rulePixelFound; scanX++)
                {
                    SKColor pixel = bitmap.GetPixel(scanX, scanY);
                    if (pixel != paperColor)
                    {
                        rulePixelFound = true;
                    }
                }
            }

            Assert.True(rulePixelFound,
                "Expected a non-paper pixel in y=[0,5] zone — rule above Subtotal not drawn. " +
                "Rule lands at y=RuleTopPadding=4; a non-paper pixel must appear there.");
        }
    }

    // TotalsSection_TotalBandRenders
    // Pixel-mode: inside TOTAL bar must have alpha > 0 and not be pure paper color.
    // Sample fixture highlightColor=#E8F0EC is a visible tint — band center must show it.
    [Fact]
    public void TotalsSection_TotalBandRenders()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        SKColor paperColor = SKColor.Parse(data.Theme?.PaperColor ?? "#FAF8F3");

        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) = SectionTestBase.CreateBitmapContext(data);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(paperColor);

            var section = new TotalsSection();
            float height = section.Measure(Width, data, ctx);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);

            // Sample inside TOTAL bar: bottom-right interior (mirrors T3b.15).
            int x = (int)(Width - 10);
            int y = (int)(height - 8);
            SKColor pixel = bitmap.GetPixel(x, y);

            Assert.True(pixel.Alpha > 0, "TOTAL band pixel has zero alpha — band not drawn.");
            Assert.NotEqual(paperColor, pixel);
        }
    }

    // TotalsSection_MeasureGrowsByRulePadding
    // With the rule, Measure must exceed the no-rule sum by at least 5px
    // (ruleTopPadding=4 + ruleStrokeWidth=1 minimum).
    // No-rule baseline for the workshop sample fixture:
    //   Sub-rows: Subtotal (always) + Tax (ShowTaxBreakdown=true) = 2 rows
    //             (DiscountTotal=0 and ServiceCharge=0 are both hidden.)
    //   No-rule Measure = rows*RowHeight + (rows-1)*RowGap + RowGap + TotalBarHeight
    //                   = 2*16 + 1*4 + 4 + 22 = 32 + 4 + 4 + 22 = 62f
    // With rule: must be ≥ 62 + 5 = 67f. Updated 2026-05-11 alongside the
    // Elevate Studio → Kerani Auto Workshop fixture swap, which zeroed
    // DiscountTotal and reduced the visible sub-row count from 3 to 2.
    [Fact]
    public void TotalsSection_MeasureGrowsByRulePadding()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();

        var section = new TotalsSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float measured = section.Measure(Width, data, ctx);

        const float NoRuleBaseline = 62f;
        const float MinRuleOverhead = 5f;

        Assert.True(measured >= NoRuleBaseline + MinRuleOverhead,
            $"Expected Measure >= {NoRuleBaseline + MinRuleOverhead} (no-rule baseline {NoRuleBaseline} + rule overhead {MinRuleOverhead}); got {measured}.");
    }
}
