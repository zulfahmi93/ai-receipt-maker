// Purpose: RED-phase tests for Phase 3c-polish C (T3cP.5) — HeaderSection tagline
//          right-alignment. Tagline must be right-anchored under the wordmark, not centered.
//          Pixel-mode probe: glyph pixels present near right edge, absent at mid-X.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Layout;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class HeaderTaglineAlignmentTests
{
    // T3cP.5 — HeaderSection tagline must be right-anchored.
    //
    // Decision (mockup review 2026-05-11): tagline right edge aligns with section right
    // margin (origin.X + width - rightPadding). The mockup shows the tagline flush-right
    // with the section boundary, not with the wordmark's measured right edge (the wordmark
    // width varies with business name length, which would make right-anchoring fragile).
    // Right-margin padding matches the name's left start: both use ~0px outer padding (the
    // paper-edge padding is handled by the composer), so tagline draw x is:
    //   x = sectionWidth - taglineWidth (right flush, no extra margin)
    //
    // Probe strategy:
    //   1. Render HeaderSection (tagline present) onto an SKBitmap.
    //   2. Scan the tagline row for non-paper pixels.
    //   3. Assert: pixels exist near the right edge (at sectionWidth - taglineWidth/2).
    //   4. Assert: pixels do NOT exist at midX ± 4 (tagline is not centered).
    [Fact]
    public void HeaderSection_TaglineRightAligns()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        // Ensure tagline present.
        Assert.False(
            string.IsNullOrWhiteSpace(data.Business.BusinessTagline),
            "Sample fixture must have a BusinessTagline for this test");

        const float Width = 360f;
        const float TaglineFontSize = 12f;
        const string FontFamily = "Inter";

        using var fonts = new FontProvider();
        var section = new HeaderSection();

        using var ctx = new RenderContext(fonts, resolvedLogo: null);
        float sectionHeight = section.Measure(Width, data, ctx);
        Assert.True(sectionHeight > 0f, "Section must have positive height");

        using var bitmap = new SKBitmap((int)Width, (int)sectionHeight + 2);
        SKColor paperColor = SKColors.White;
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(paperColor);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);
        }

        // Measure tagline width to predict probe positions.
        SKTypeface normalFace = fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);
        SKRect taglineBounds = TextMeasurer.Measure(data.Business.BusinessTagline!, normalFace, TaglineFontSize);
        float taglineWidth = taglineBounds.Width;

        // Tagline row: after business name (22px) + name-tagline gap (8px) + tagline top.
        // Baseline is at origin.Y + NameFontSize + NameTaglineGap + TaglineFontSize.
        // Pixel row to probe = (int)(22 + 8 + 12) - 2 = 40 (cap line, not baseline descender).
        const int NameFontSize = 22;
        const int NameTaglineGap = 8;
        int taglineRow = NameFontSize + NameTaglineGap + (int)(TaglineFontSize * 0.7f); // near cap line

        // Right-anchor probe: expected tagline spans approximately [Width - taglineWidth .. Width].
        // Scan a column range in the right two-thirds of the section for non-paper pixels on taglineRow.
        // If right-anchored, the tagline starts at ~(Width - taglineWidth) and ends at Width.
        // Scan x from (Width - taglineWidth - 10) to (Width - 5) — comfortably inside the run.
        int rightScanStart = (int)(Width - taglineWidth - 10f);
        int rightScanEnd = (int)(Width - 5f);
        rightScanStart = Math.Clamp(rightScanStart, 0, bitmap.Width - 1);
        rightScanEnd = Math.Clamp(rightScanEnd, 0, bitmap.Width - 1);

        // Center probe: tagline must NOT have pixels at midX if right-anchored.
        // The left third of the section (x in [0 .. Width/3]) must be paper-colored.
        int midScanEnd = (int)(Width / 3f);

        bool foundAtRight = HasNonPaperPixelInRange(bitmap, rightScanStart, rightScanEnd, taglineRow - 2, taglineRow + 4, paperColor);
        bool foundAtLeft = HasNonPaperPixelInRange(bitmap, 0, midScanEnd, taglineRow - 2, taglineRow + 4, paperColor);

        Assert.True(
            foundAtRight,
            $"Expected tagline glyph pixel in x=[{rightScanStart}..{rightScanEnd}] (right-anchor zone), row~{taglineRow}. " +
            $"TaglineWidth={taglineWidth:F1}, Width={Width}. If tagline is centered, right scan zone will miss.");

        Assert.False(
            foundAtLeft,
            $"Tagline should NOT have pixels in x=[0..{midScanEnd}] (left-third, far from right anchor). " +
            $"Tagline is right-anchored; left third must be paper-colored.");
    }

    private static bool HasNonPaperPixelInRange(
        SKBitmap bitmap,
        int xStart,
        int xEnd,
        int yStart,
        int yEnd,
        SKColor paper)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        for (int y = Math.Max(0, yStart); y <= Math.Min(h - 1, yEnd); y++)
        {
            for (int x = Math.Max(0, xStart); x <= Math.Min(w - 1, xEnd); x++)
            {
                if (bitmap.GetPixel(x, y) != paper)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
