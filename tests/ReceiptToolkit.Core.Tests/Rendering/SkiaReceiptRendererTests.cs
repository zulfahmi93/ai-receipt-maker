// Purpose: RED-phase tests for Phase 3c sub-cluster A — SkiaReceiptRenderer (T3c.1, T3c.2).
// Categories: Unit — full-receipt composition. T3c.1 verifies Measure aggregates section
//             heights + sectionGap (visible-only) + 2*padding and Render does not throw;
//             T3c.2 verifies the renderer paints a paper-colour background rectangle at
//             (0,0,W,H) before sections draw (Option B background-paint scope).
// Edge cases: borderRadius and showShadow are forced off in both tests so corner (0,0)
//             is inside the paper-fill rect (T3c.7/T3c.8 own non-zero radius and shadow).
//             T3c.1 sums heights of visible sections (>0f Measure) only and adds gap
//             between them, matching the composition algorithm in Phase3bVisualPreview.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering;

/// <summary>
///   RED-phase coverage for <c>SkiaReceiptRenderer</c> (T3c.1, T3c.2). The production
///   class does not exist yet — these tests pin the Measure/Render contract and the
///   Option B paper-paint scope before the GREEN phase implements the type.
/// </summary>
public sealed class SkiaReceiptRendererTests
{
    // T3c.1 — Renderer.Measure aggregates section heights + sectionGap (between visible
    // sections only) + 2*padding outer margin, and Render composes onto a canvas without
    // throwing. We sum heights independently using the same 10 sections in mockup order
    // and compare floats with a small precision tolerance (single-decimal precision is
    // sufficient — Measure values are ints/scaled-floats, no chained-arithmetic drift).
    [Fact]
    public void T3c_1_RendersSectionsInMockupOrder_WithSectionGap()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        int receiptWidth = layout.ReceiptWidth;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap;
        float contentWidth = receiptWidth - (2 * padding);

        // Mockup order — must match SkiaReceiptRenderer's internal sequence exactly.
        IReceiptSection[] sections =
        [
            new HeaderSection(),
            new TitleSection(),
            new MetaSection(),
            new CustomerCashierSection(),
            new ItemTableSection(),
            new TotalsSection(),
            new PaymentSection(),
            new QrSection(),
            new FooterSection(),
            new PerforationSection(),
        ];

        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float sumOfVisibleHeights = 0f;
        int visibleCount = 0;
        for (int i = 0; i < sections.Length; i++)
        {
            float h = sections[i].Measure(contentWidth, data, ctxForExpected);
            if (h > 0f)
            {
                sumOfVisibleHeights += h;
                visibleCount++;
            }
        }

        float expectedHeight = sumOfVisibleHeights
            + (sectionGap * Math.Max(0, visibleCount - 1))
            + (2 * padding);

        // SUT — does not exist yet. Compile-fails until GREEN lands.
        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        Assert.Equal(receiptWidth, (int)size.Width);
        Assert.Equal(expectedHeight, size.Height, precision: 1);

        // Render onto a bitmap sized exactly to Measure. No exception => composition path runs.
        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        // Crude pixel-presence guard — paired with T3c.2's paper-colour corner assertion.
        // Sample roughly mid-receipt at an x just inside the padded content column.
        // The two tests catch different broken-renderer states:
        //   T3c.2 catches a totally no-op Render — corner stays transparent black,
        //         which fails the magenta corner assertion.
        //   T3c.1 catches a Render that paints paper but skips Draw on sections —
        //         midPixel would equal paper, which fails this NotEqual assertion.
        // (Total no-op leaves midPixel transparent black, which differs from paper too,
        //  so this assertion passes on its own — that path is owned by T3c.2.)
        // If this guard ever flakes (mid-Y lands between item rows on a future fixture)
        // the orchestrator may drop it without losing T3c.2's corner coverage.
        SKColor paper = SKColor.Parse(data.Theme!.PaperColor!);
        int sampleX = (int)padding + 4;
        int sampleY = (int)(size.Height / 2);
        SKColor midPixel = bitmap.GetPixel(sampleX, sampleY);
        Assert.NotEqual(paper, midPixel);
    }

    // T3c.2 — Renderer paints theme.paperColor as the background fill at (0,0,W,H) before
    // sections draw (Option B). Force borderRadius=0 and showShadow=false so the (0,0)
    // corner is inside the fill rect. Magenta (#FF00FF) is chosen to be visually distinct
    // from any real theme default in the codebase, so a missing background paint can't
    // accidentally match.
    [Fact]
    public void T3c_2_PaintsPaperColor_BackgroundCorner()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#FF00FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor expected = SKColor.Parse("#FF00FF");

        // Top-left corner — must be inside the paper-fill rect when borderRadius=0.
        SKColor topLeft = bitmap.GetPixel(0, 0);
        Assert.Equal(expected.Red, topLeft.Red);
        Assert.Equal(expected.Green, topLeft.Green);
        Assert.Equal(expected.Blue, topLeft.Blue);
        Assert.Equal((byte)255, topLeft.Alpha);

        // Two extra cheap edge samples at the vertical mid-line — left and right paper edges.
        int midY = (int)(size.Height / 2);
        SKColor leftEdge = bitmap.GetPixel(0, midY);
        SKColor rightEdge = bitmap.GetPixel(bitmapWidth - 1, midY);

        Assert.Equal(expected.Red, leftEdge.Red);
        Assert.Equal(expected.Green, leftEdge.Green);
        Assert.Equal(expected.Blue, leftEdge.Blue);

        Assert.Equal(expected.Red, rightEdge.Red);
        Assert.Equal(expected.Green, rightEdge.Green);
        Assert.Equal(expected.Blue, rightEdge.Blue);
    }
}
