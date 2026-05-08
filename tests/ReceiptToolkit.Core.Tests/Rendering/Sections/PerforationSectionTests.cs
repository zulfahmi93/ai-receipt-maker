// Purpose: RED-phase tests for Phase 3b/D — PerforationSection renderer (T3b.23).
// Categories: Unit — section rendering, geometric height assertion via Measure for
//             layout-toggle-driven visibility, pixel-mode assertion for scallop arc
//             stroke painted with theme.dividerColor.
// Edge cases: ShowPerforatedBottom=false collapses Measure to 0f (geometric half);
//             ShowPerforatedBottom=true paints scallop arc strokes in dividerColor
//             at a deterministic coord on the band midline (pixel half).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class PerforationSectionTests
{
    private const float Width = 360f;

    // T3b.23 — PerforationSection is drawn iff ShowPerforatedBottom is true.
    //
    //   Geometric half:
    //     Measure(showPerforatedBottom=true)  > 0f
    //     Measure(showPerforatedBottom=false) == 0f
    //
    //   Pixel half: render the showPerforatedBottom=true variant onto a white bitmap
    //   and sample a pixel that deterministically lands on a scallop arc stroke.
    //
    //   Coord rationale (document for GREEN constraint):
    //     x = 4  — well inside scallop #1's left arc arm. Scallop #1 starts at x=0
    //              and its right edge is at x=scallopDiameter. If scallopDiameter >= 8f
    //              (which the GREEN spec requires), x=4 is inside the left half of the
    //              first scallop. The arc stroke at x=4 passes through the column.
    //     y = (int)(measure / 2)  — the band mid-height. The perforation band height
    //              equals scallopDiameter (band == one full scallop row). The mid-height
    //              of the band is scallopRadius. That is exactly the apex of every
    //              scallop's arc — i.e. the point farthest from the chord baseline.
    //              The stroke must cross this row for any valid scallop implementation.
    //
    //   Constraint for GREEN implementation:
    //     scallopDiameter MUST be >= 8f so that x=4 is inside the first scallop.
    //     If scallopDiameter < 8f, x=4 may miss; update both coord and this comment.
    //
    //   The pixel is asserted to equal SKColor.Parse(data.Theme!.DividerColor!) which
    //   is "#B8C8C1" from the sample fixture. PerforationSection must use dividerColor
    //   for its scallop stroke — the GREEN spec must honour this.
    //   IsAntialias=false is required on the paint object to avoid sub-pixel blending
    //   producing a near-but-not-exact color at the sampled coord.
    [Fact]
    public void PerforationSection_DrawnIff_ShowPerforatedBottomIsTrue()
    {
        ReceiptData dataWith = SectionTestBase.LoadSampleData();
        ReceiptData dataWithout = dataWith with
        {
            Layout = (dataWith.Layout ?? new ReceiptLayout()) with { ShowPerforatedBottom = false },
        };

        var section = new PerforationSection();

        // ---- Geometric half ----
        (SKBitmap measureBitmap, RenderContext measureCtx, FontProvider measureFonts) =
            SectionTestBase.CreateBitmapContext(dataWith);
        using (measureBitmap)
        using (measureCtx)
        using (measureFonts)
        {
            float measureWith = section.Measure(Width, dataWith, measureCtx);
            float measureWithout = section.Measure(Width, dataWithout, measureCtx);

            Assert.True(
                measureWith > 0f,
                "Expected Measure(showPerforatedBottom=true) > 0f — perforation band must claim height when enabled");

            Assert.Equal(0f, measureWithout);
        }

        // ---- Pixel half ----
        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) =
            SectionTestBase.CreateBitmapContext(dataWith);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            float measure = section.Measure(Width, dataWith, ctx);

            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            section.Draw(canvas, new SKPoint(0f, 0f), Width, dataWith, ctx);

            // Sample coord on the scallop arc stroke at the band midline.
            // x=4 is inside scallop #1's left arm (requires scallopDiameter >= 8f).
            // y=(int)(measure/2) is the apex row of every scallop — the arc stroke
            // must cross this row by definition of a half-circle scallop pattern.
            int x = 4;
            int y = (int)(measure / 2);

            SKColor actualPixel = bitmap.GetPixel(x, y);

            Assert.NotNull(dataWith.Theme);
            Assert.NotNull(dataWith.Theme.DividerColor);

            SKColor expectedColor = SKColor.Parse(dataWith.Theme!.DividerColor!);
            Assert.Equal(expectedColor, actualPixel);
        }
    }
}
