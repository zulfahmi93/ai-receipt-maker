// Purpose: RED-phase tests for Phase 3c-polish C (T3cP.6) — TitleSection horizontal
//          rule lines above and below title, and glyph-by-glyph tracking for "RECEIPT".
//          Pixel-mode probes per Phase 3b divergence #17 pattern.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Layout;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class TitleSectionRulesAndTrackingTests
{
    private const float Width = 360f;
    private const float TitleFontSize = 16f;
    private const string FontFamily = "Inter";

    // T3cP.6a — TitleSection draws a horizontal rule above the title.
    //
    // Probe: After adding rules + padding, Measure returns
    //   RulePaddingAbove + RuleStrokeWidth + RulePaddingBelow + FontSize
    //                     + RulePaddingAbove + RuleStrokeWidth + RulePaddingBelow.
    // Concretely: 6 + 1 + 6 + 16 + 6 + 1 + 6 = 42f.
    // Render on bitmap; probe row = RulePaddingAbove (6px from top).
    //
    // Critical: we probe at x=10 (far left of the canvas), well outside the centered
    // text glyph run (centered text starts at ~(360-titleWidth)/2 ≈ 140px for "RECEIPT").
    // A full-width horizontal rule spans x=0..Width, so it WILL land at x=10.
    // Centered text without tracking will NOT land at x=10.
    // This distinguishes "rule drawn" from "text glyph near probe row".
    [Fact]
    public void TitleSection_DrawsRuleAboveTitle()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        Assert.False(string.IsNullOrWhiteSpace(data.Receipt.ReceiptTitle), "Fixture needs receiptTitle");

        using var fonts = new FontProvider();
        var section = new TitleSection();

        using var ctx = new RenderContext(fonts, resolvedLogo: null);
        float height = section.Measure(Width, data, ctx);

        // Measure must exceed FontSize — rules + padding are present.
        Assert.True(
            height > TitleFontSize,
            $"Measure {height:F1} must be > FontSize {TitleFontSize} once rules are added (pre-GREEN: expect failure here)");

        using var bitmap = new SKBitmap((int)Width, (int)height + 2);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);
        }

        // Rule above should be at y ≈ RulePaddingAbove (6px from top).
        // Probe at x=10 — far left, no centered text glyph here, but a full-width rule is.
        const int AboveRuleRow = 6;
        const int LeftProbeX = 10;
        bool ruleFound = HasNonPaperPixelInColumn(bitmap, LeftProbeX, AboveRuleRow - 1, AboveRuleRow + 1, SKColors.White);

        Assert.True(
            ruleFound,
            $"Expected rule stroke pixel at x={LeftProbeX}, y~{AboveRuleRow} (above title, left edge). " +
            $"SectionHeight={height:F1}. Full-width rule must land at x=10; centered text does not.");
    }

    // T3cP.6b — TitleSection draws a horizontal rule below the title.
    //
    // Probe row = RulePaddingAbove + RuleStrokeWidth + RulePaddingBelow + FontSize
    //             + RulePaddingAbove + RuleStrokeWidth/2  (centre of bottom rule)
    //           ≈ 6 + 1 + 6 + 16 + 6 + 0 = 35, stroke centred at ~35.
    [Fact]
    public void TitleSection_DrawsRuleBelowTitle()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        Assert.False(string.IsNullOrWhiteSpace(data.Receipt.ReceiptTitle), "Fixture needs receiptTitle");

        using var fonts = new FontProvider();
        var section = new TitleSection();

        using var ctx = new RenderContext(fonts, resolvedLogo: null);
        float height = section.Measure(Width, data, ctx);

        using var bitmap = new SKBitmap((int)Width, (int)height + 2);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);
        }

        // Bottom rule ≈ y = 6+1+6+16+6 = 35. Probe ±1 row.
        const int BelowRuleRow = 35;
        bool ruleFound = HasNonPaperPixelInRow(bitmap, BelowRuleRow - 1, BelowRuleRow + 2, SKColors.White);

        Assert.True(
            ruleFound,
            $"Expected rule stroke pixel near y={BelowRuleRow} (below title). " +
            $"SectionHeight={height:F1}. Bottom rule must be drawn using dividerColor.");
    }

    // T3cP.6c — TitleSection renders "RECEIPT" with glyph-by-glyph tracking.
    //
    // Tracked width = sum(per-glyph widths) + tracking * (glyphCount - 1).
    // Baseline ref = TextMeasurer.Measure(fullString) width (no tracking).
    // Tracked render MUST produce a wider pixel span than the baseline.
    //
    // Proxy: section Measure() returns greater height than plain FontSize (proves rules
    // landed), AND the actual rendered glyph row pixel span is strictly wider than
    // TextMeasurer.Measure("RECEIPT", boldFace, 16f).Width.
    [Fact]
    public void TitleSection_WordmarkUsesTracking()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        Assert.False(string.IsNullOrWhiteSpace(data.Receipt.ReceiptTitle), "Fixture needs receiptTitle");

        using var fonts = new FontProvider();
        var section = new TitleSection();

        using var ctx = new RenderContext(fonts, resolvedLogo: null);
        float height = section.Measure(Width, data, ctx);

        // Measure return must exceed bare FontSize (rules + padding added).
        Assert.True(
            height > TitleFontSize,
            $"Measure must be > {TitleFontSize} (rules add padding); got {height:F1}");

        using var bitmap = new SKBitmap((int)Width, (int)height + 2);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);
        }

        // Baseline reference: untracked width of the full title string.
        SKTypeface boldFace = fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);
        float baselineWidth = TextMeasurer.Measure(data.Receipt.ReceiptTitle!, boldFace, TitleFontSize).Width;

        // Pixel span of rendered title row: scan the text band for non-paper pixels.
        // Title baseline ≈ RulePaddingAbove + RuleStrokeWidth + RulePaddingBelow + FontSize
        //                = 6 + 1 + 6 + 16 = 29.
        const int TitleBaselineRow = 29;
        (int leftX, int rightX) = MeasurePixelSpan(bitmap, TitleBaselineRow - 4, TitleBaselineRow + 1, SKColors.White);

        float renderedSpan = leftX >= 0 && rightX >= leftX ? rightX - leftX : 0f;

        Assert.True(
            renderedSpan > baselineWidth,
            $"Tracked render span {renderedSpan:F1}px must exceed untracked baseline width {baselineWidth:F1}px. " +
            $"Tracking = 0.15 * {TitleFontSize} = {0.15f * TitleFontSize:F2}px per inter-glyph gap.");
    }

    private static bool HasNonPaperPixelInColumn(SKBitmap bitmap, int x, int yStart, int yEnd, SKColor paper)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        x = Math.Clamp(x, 0, w - 1);
        for (int y = Math.Max(0, yStart); y <= Math.Min(h - 1, yEnd); y++)
        {
            if (bitmap.GetPixel(x, y) != paper)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNonPaperPixelInRow(SKBitmap bitmap, int yStart, int yEnd, SKColor paper)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        for (int y = Math.Max(0, yStart); y <= Math.Min(h - 1, yEnd); y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (bitmap.GetPixel(x, y) != paper)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (int LeftX, int RightX) MeasurePixelSpan(SKBitmap bitmap, int yStart, int yEnd, SKColor paper)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        int leftX = -1;
        int rightX = -1;

        for (int y = Math.Max(0, yStart); y <= Math.Min(h - 1, yEnd); y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (bitmap.GetPixel(x, y) != paper)
                {
                    if (leftX < 0 || x < leftX)
                    {
                        leftX = x;
                    }

                    if (x > rightX)
                    {
                        rightX = x;
                    }
                }
            }
        }

        return (leftX, rightX);
    }
}
