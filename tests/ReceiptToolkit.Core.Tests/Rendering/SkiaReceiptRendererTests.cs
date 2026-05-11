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
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
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

    // T3c.3 — theme.highlightColor flows through SkiaReceiptRenderer.Render to the
    // TOTAL bar fill in the composed bitmap. Codebase truth uses HighlightColor
    // (plan T3c.3 wording said accentColor — divergence #20). AccentColor is left
    // at the sample default so QrSection is unaffected. BorderRadius/ShowShadow
    // are forced off so the bar mid-pixel sample lands cleanly inside the fill.
    [Fact]
    public void T3c_3_HighlightColor_ChangesTotalBarPixel()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { HighlightColor = "#FF0000" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        // Independent re-derivation of TOTAL bar position. Renderer composition:
        //   y = padding; foreach visible section: if !first y += sectionGap; draw; y += h
        // So Totals's draw-y = padding + Σ(visible pre-Totals heights) + sectionGap *
        // (count of visible pre-Totals). Pre-Totals sections in mockup order are
        // indices 0..4 in SkiaReceiptRenderer's array. Visibility is computed (>0f
        // Measure) — we do not hardcode "all 5 visible".
        IReceiptSection[] preTotals =
        [
            new HeaderSection(),
            new TitleSection(),
            new MetaSection(),
            new CustomerCashierSection(),
            new ItemTableSection(),
        ];

        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float yTotalsTop = padding;
        int visibleBefore = 0;
        for (int i = 0; i < preTotals.Length; i++)
        {
            float h = preTotals[i].Measure(contentWidth, data, ctxForExpected);
            if (h > 0f)
            {
                if (visibleBefore > 0)
                {
                    yTotalsTop += sectionGap;
                }

                yTotalsTop += h;
                visibleBefore++;
            }
        }

        // Add the gap before Totals if any pre-Totals section is visible.
        if (visibleBefore > 0)
        {
            yTotalsTop += sectionGap;
        }

        float totalsHeight = new TotalsSection().Measure(contentWidth, data, ctxForExpected);
        float yTotalsBottom = yTotalsTop + totalsHeight;

        // TotalBarHeight mirrors TotalsSection.TotalBarHeight (private const). The bar
        // is the LAST 22f within the totals section's measured height, so its mid-Y
        // is yTotalsBottom - TotalBarHeight/2.
        const float TotalBarHeight = 22f;
        float barMidY = yTotalsBottom - (TotalBarHeight / 2f);
        int sampleY = (int)Math.Round(barMidY);
        int sampleX = (int)Math.Round(padding + (contentWidth / 2f));

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor pixel = bitmap.GetPixel(sampleX, sampleY);

        Assert.Equal((byte)255, pixel.Red);
        Assert.Equal((byte)0, pixel.Green);
        Assert.Equal((byte)0, pixel.Blue);
        Assert.Equal((byte)255, pixel.Alpha);
    }

    // T3c.4 — Dual-purpose:
    //   (a) Re-confirm paper paint on a different colour from T3c.2's magenta to
    //       prove no stale-cache passthrough.
    //   (b) Prove layering: TOTAL bar pixel must NOT equal the paper colour, i.e.
    //       the highlight rect is drawn AFTER the paper rect, not erased by it.
    // HighlightColor and AccentColor are left at sample defaults so the bar tint
    // is the sample value (#E8F0EC), distinctly != blue paper.
    [Fact]
    public void T3c_4_PaperColor_BackgroundAndLayering()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#0000FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        // Same re-derivation as T3c.3 — TOTAL bar mid-Y from independent geometry.
        IReceiptSection[] preTotals =
        [
            new HeaderSection(),
            new TitleSection(),
            new MetaSection(),
            new CustomerCashierSection(),
            new ItemTableSection(),
        ];

        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float yTotalsTop = padding;
        int visibleBefore = 0;
        for (int i = 0; i < preTotals.Length; i++)
        {
            float h = preTotals[i].Measure(contentWidth, data, ctxForExpected);
            if (h > 0f)
            {
                if (visibleBefore > 0)
                {
                    yTotalsTop += sectionGap;
                }

                yTotalsTop += h;
                visibleBefore++;
            }
        }

        if (visibleBefore > 0)
        {
            yTotalsTop += sectionGap;
        }

        float totalsHeight = new TotalsSection().Measure(contentWidth, data, ctxForExpected);
        float yTotalsBottom = yTotalsTop + totalsHeight;

        const float TotalBarHeight = 22f;
        float barMidY = yTotalsBottom - (TotalBarHeight / 2f);
        int sampleY = (int)Math.Round(barMidY);
        int sampleX = (int)Math.Round(padding + (contentWidth / 2f));

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        // (a) Paper paint — corner must be the new blue paper colour.
        SKColor topLeft = bitmap.GetPixel(0, 0);
        Assert.Equal((byte)0, topLeft.Red);
        Assert.Equal((byte)0, topLeft.Green);
        Assert.Equal((byte)255, topLeft.Blue);
        Assert.Equal((byte)255, topLeft.Alpha);

        // (b) Layering — bar mid-pixel must not equal paper. The highlight fill is
        // the sample default (#E8F0EC), whose Blue byte is 0xEC != 0xFF. Asserting
        // pixel.Blue != 255 is the cleanest single-byte guard that this pixel was
        // overwritten by the highlight rect after the paper rect was drawn.
        SKColor barPixel = bitmap.GetPixel(sampleX, sampleY);
        Assert.NotEqual((byte)255, barPixel.Blue);
    }

    // T3c.5 — layout.receiptWidth flows through SkiaReceiptRenderer.Measure as canvas
    // width and through the paper-paint DrawRect as fill width. Mutate sample fixture
    // 360 → 480, force borderRadius=0 + showShadow=false so the (W-1, 0) top-right
    // corner is inside the paper-fill rect. Magenta #FF00FF paper colour mirrors
    // T3c.2's distinct-sentinel pattern — distinct from any real theme default so a
    // hardcoded 360 in the DrawRect call would leave the right edge untouched and
    // fail the corner assertion.
    [Fact]
    public void T3c_5_ReceiptWidth_DrivesCanvasWidth()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#FF00FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                ReceiptWidth = 480,
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        var renderer = new SkiaReceiptRenderer();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);
        Assert.Equal(480, (int)size.Width);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor expected = SKColor.Parse("#FF00FF");
        SKColor topRight = bitmap.GetPixel(bitmapWidth - 1, 0);
        Assert.Equal(expected.Red, topRight.Red);
        Assert.Equal(expected.Green, topRight.Green);
        Assert.Equal(expected.Blue, topRight.Blue);
        Assert.Equal((byte)255, topRight.Alpha);
    }

    // T3c.6 — layout.dividerStyle drives the inter-section divider stroke pattern at
    // the midpoint of the gap preceding TotalsSection. Three styles produce three
    // pixel signatures along that horizontal line:
    //   "solid"  → every sampled X equals dividerColor (no paper gaps along the line).
    //   "dashed" → mix of dividerColor + paperColor pixels; max contiguous stroke run ≥ 5
    //             (proves it's the [6,4] dash period, not the [2,3] dot period).
    //   "dotted" → mix; max contiguous stroke run ≤ 3 (proves [2,3] dot period).
    // Antialiasing is forced off in GREEN so divider pixels are exact integer colour
    // matches; classification here uses byte-equal RGB comparison.
    //
    // Divider Y is re-derived independently from the composer's visible-section
    // accumulator (same pattern as T3c.3 / T3c.4) so the test stays robust if a future
    // sub-cluster reorders sections.
    [Theory]
    [InlineData("solid")]
    [InlineData("dashed")]
    [InlineData("dotted")]
    public void T3c_6_DividerStyle_ProducesDistinctStrokePattern(string style)
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                DividerStyle = style,
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        // Independent re-derivation of pre-Totals stack — mirrors T3c.3 / T3c.4
        // visible-only accumulator + leading-gap-before-Totals.
        IReceiptSection[] preTotals =
        [
            new HeaderSection(),
            new TitleSection(),
            new MetaSection(),
            new CustomerCashierSection(),
            new ItemTableSection(),
        ];

        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float yTotalsTop = padding;
        int visibleBefore = 0;
        for (int i = 0; i < preTotals.Length; i++)
        {
            float h = preTotals[i].Measure(contentWidth, data, ctxForExpected);
            if (h > 0f)
            {
                if (visibleBefore > 0)
                {
                    yTotalsTop += sectionGap;
                }

                yTotalsTop += h;
                visibleBefore++;
            }
        }

        if (visibleBefore > 0)
        {
            yTotalsTop += sectionGap;
        }

        int dividerY = (int)Math.Round(yTotalsTop - (sectionGap / 2f));

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor stroke = SKColor.Parse(data.Theme!.DividerColor!);
        SKColor paper = SKColor.Parse(data.Theme!.PaperColor!);

        int xStart = (int)Math.Round(padding);
        int xEnd = (int)Math.Round(layout.ReceiptWidth - padding);

        int strokePixels = 0;
        int paperPixels = 0;
        int maxStrokeRun = 0;
        int currentStrokeRun = 0;

        for (int x = xStart; x < xEnd; x++)
        {
            SKColor px = bitmap.GetPixel(x, dividerY);
            bool isStroke = px.Red == stroke.Red && px.Green == stroke.Green && px.Blue == stroke.Blue;
            bool isPaper = px.Red == paper.Red && px.Green == paper.Green && px.Blue == paper.Blue;

            if (isStroke)
            {
                strokePixels++;
                currentStrokeRun++;
                if (currentStrokeRun > maxStrokeRun)
                {
                    maxStrokeRun = currentStrokeRun;
                }
            }
            else
            {
                currentStrokeRun = 0;
                if (isPaper)
                {
                    paperPixels++;
                }
            }
        }

        int totalSampled = xEnd - xStart;

        switch (style)
        {
            case "solid":
                // Every sampled X is stroke; no paper pixels along the line.
                Assert.Equal(totalSampled, strokePixels);
                Assert.Equal(0, paperPixels);
                break;

            case "dashed":
                // Mix; dash period [6,4] → max contiguous stroke run ≥ 5 (allow 1px boundary slack).
                Assert.True(strokePixels > 0, "dashed: expected at least one stroke pixel");
                Assert.True(paperPixels > 0, "dashed: expected at least one paper pixel");
                Assert.True(
                    maxStrokeRun >= 5,
                    $"dashed: expected max stroke run ≥ 5 (got {maxStrokeRun}) — proves [6,4] dash period");
                break;

            case "dotted":
                // Mix; dot period [2,3] → max contiguous stroke run ≤ 3 (allow 1px boundary slack).
                Assert.True(strokePixels > 0, "dotted: expected at least one stroke pixel");
                Assert.True(paperPixels > 0, "dotted: expected at least one paper pixel");
                Assert.True(
                    maxStrokeRun <= 3,
                    $"dotted: expected max stroke run ≤ 3 (got {maxStrokeRun}) — proves [2,3] dot period");
                break;

            default:
                Assert.Fail($"unhandled style: {style}");
                break;
        }
    }

    // T3c.6 negative — null / empty / whitespace / unknown layout.dividerStyle MUST
    // suppress the leading-divider draw entirely. Pins the early-return branch in
    // SkiaReceiptRenderer.DrawLeadingDivider so a future refactor that reorders the
    // style-recognition check cannot silently leak a default-style draw.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("dotty")]
    public void T3c_6_DividerStyle_NullOrUnknown_DrawsNoDivider(string? style)
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                DividerStyle = style,
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        IReceiptSection[] preTotals =
        [
            new HeaderSection(),
            new TitleSection(),
            new MetaSection(),
            new CustomerCashierSection(),
            new ItemTableSection(),
        ];

        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float yTotalsTop = padding;
        int visibleBefore = 0;
        for (int i = 0; i < preTotals.Length; i++)
        {
            float h = preTotals[i].Measure(contentWidth, data, ctxForExpected);
            if (h > 0f)
            {
                if (visibleBefore > 0)
                {
                    yTotalsTop += sectionGap;
                }

                yTotalsTop += h;
                visibleBefore++;
            }
        }

        if (visibleBefore > 0)
        {
            yTotalsTop += sectionGap;
        }

        int dividerY = (int)Math.Round(yTotalsTop - (sectionGap / 2f));

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor stroke = SKColor.Parse(data.Theme!.DividerColor!);

        int xStart = (int)Math.Round(padding);
        int xEnd = (int)Math.Round((float)layout.ReceiptWidth - padding);

        int strokePixels = 0;
        for (int x = xStart; x < xEnd; x++)
        {
            SKColor px = bitmap.GetPixel(x, dividerY);
            if (px.Red == stroke.Red && px.Green == stroke.Green && px.Blue == stroke.Blue)
            {
                strokePixels++;
            }
        }

        Assert.Equal(0, strokePixels);
    }

    // T3c.6 negative — sections whose RequiresLeadingDivider is false MUST NOT
    // produce a divider at their preceding gap, even with layout.DividerStyle="solid".
    // Samples the gap midpoint between Header (idx 0) and Title (idx 1, inherits
    // default-false). A copy-paste of `=> true` onto the wrong section would fail
    // this test; current state expects 0 stroke pixels along that row.
    [Fact]
    public void T3c_6_NonRequiringSection_HasNoLeadingDivider()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                DividerStyle = "solid",
                BorderRadius = 0,
                ShowShadow = false,
            },
        };

        ReceiptLayout layout = data.Layout!;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        // Geometry — gap between Header (idx 0) and Title (idx 1). yTitleTop =
        // padding + Header.Measure + sectionGap; dividerY = (yTitleTop - sectionGap)
        // + sectionGap/2 = padding + Header.Measure + sectionGap/2. We re-derive
        // exactly how the composer would land that row.
        using var fontsForExpected = new FontProvider();
        using var ctxForExpected = new RenderContext(fontsForExpected, resolvedLogo: null);

        float headerHeight = new HeaderSection().Measure(contentWidth, data, ctxForExpected);
        Assert.True(headerHeight > 0f, "fixture invariant: HeaderSection visible at sample defaults");

        int dividerY = (int)Math.Round(padding + headerHeight + (sectionGap / 2f));

        var renderer = new SkiaReceiptRenderer();

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        SKSize size = renderer.Measure(data, ctx);

        int bitmapWidth = (int)size.Width;
        int bitmapHeight = (int)Math.Ceiling(size.Height);
        using var bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, data, ctx);

        SKColor stroke = SKColor.Parse(data.Theme!.DividerColor!);

        int xStart = (int)Math.Round(padding);
        int xEnd = (int)Math.Round((float)layout.ReceiptWidth - padding);

        int strokePixels = 0;
        for (int x = xStart; x < xEnd; x++)
        {
            SKColor px = bitmap.GetPixel(x, dividerY);
            if (px.Red == stroke.Red && px.Green == stroke.Green && px.Blue == stroke.Blue)
            {
                strokePixels++;
            }
        }

        Assert.Equal(0, strokePixels);
    }

    // T3c.7 — layout.borderRadius clips the paper-paint to a rounded rectangle.
    // Pixels OUTSIDE the rounded curve must remain at the bitmap's default state
    // (transparent black, alpha=0) because no paint operation touches them.
    // Mid-edge pixels along the straight portions of each side must equal paperColor.
    // Interior center pixels must equal paperColor.
    //
    // Sentinel paper colour #FF00FF mirrors T3c.2 / T3c.5 — distinct from any real
    // theme default so a missing clip would leave the corners painted magenta and
    // fail the alpha=0 assertion. ShowShadow forced off so no shadow rect bleeds
    // into the corner samples.
    //
    // borderRadius=20 chosen so (0,0) is clearly outside the quadrant: distance
    // from the curve's centre (20,20) to (0,0) is sqrt(800) ≈ 28.3, well beyond
    // the 20-pixel radius. Same arithmetic holds at every corner: top-right corner
    // arc centre = (W-20, 20), pixel (W-1, 0) is sqrt(19² + 20²) = sqrt(761) ≈ 27.6
    // away — outside; bottom-left = (20, H-20), pixel (0, H-1) is sqrt(20² + 19²) =
    // sqrt(761) ≈ 27.6 away — outside; bottom-right by symmetry. All four corners
    // safely outside the 20-pixel curve, all four mid-edges safely on straight
    // portions far from any curve.
    [Fact]
    public void T3c_7_BorderRadius_ClipsCornersToRoundRect()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#FF00FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 20,
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

        SKColor paper = SKColor.Parse("#FF00FF");

        // Four corners — must be clipped (alpha=0). The bitmap default is fully
        // transparent black; if Render paints a square paper rect, all four would
        // be magenta with alpha=255 instead.
        SKColor topLeft = bitmap.GetPixel(0, 0);
        SKColor topRight = bitmap.GetPixel(bitmapWidth - 1, 0);
        SKColor bottomLeft = bitmap.GetPixel(0, bitmapHeight - 1);
        SKColor bottomRight = bitmap.GetPixel(bitmapWidth - 1, bitmapHeight - 1);

        Assert.Equal((byte)0, topLeft.Alpha);
        Assert.Equal((byte)0, topRight.Alpha);
        Assert.Equal((byte)0, bottomLeft.Alpha);
        Assert.Equal((byte)0, bottomRight.Alpha);

        // Top-edge midpoint — straight portion of the rect between corner curves.
        // Must be paperColor (inside the rounded rect at y=0).
        SKColor topEdgeMid = bitmap.GetPixel(bitmapWidth / 2, 0);
        Assert.Equal(paper.Red, topEdgeMid.Red);
        Assert.Equal(paper.Green, topEdgeMid.Green);
        Assert.Equal(paper.Blue, topEdgeMid.Blue);
        Assert.Equal((byte)255, topEdgeMid.Alpha);

        // Bottom-edge midpoint — same property, opposite side.
        SKColor bottomEdgeMid = bitmap.GetPixel(bitmapWidth / 2, bitmapHeight - 1);
        Assert.Equal(paper.Red, bottomEdgeMid.Red);
        Assert.Equal(paper.Green, bottomEdgeMid.Green);
        Assert.Equal(paper.Blue, bottomEdgeMid.Blue);
        Assert.Equal((byte)255, bottomEdgeMid.Alpha);

        // Left-edge midpoint at vertical centre — far from any corner curve.
        SKColor leftEdgeMid = bitmap.GetPixel(0, bitmapHeight / 2);
        Assert.Equal(paper.Red, leftEdgeMid.Red);
        Assert.Equal(paper.Green, leftEdgeMid.Green);
        Assert.Equal(paper.Blue, leftEdgeMid.Blue);
        Assert.Equal((byte)255, leftEdgeMid.Alpha);

        // Right-edge midpoint at vertical centre — symmetric to left.
        SKColor rightEdgeMid = bitmap.GetPixel(bitmapWidth - 1, bitmapHeight / 2);
        Assert.Equal(paper.Red, rightEdgeMid.Red);
        Assert.Equal(paper.Green, rightEdgeMid.Green);
        Assert.Equal(paper.Blue, rightEdgeMid.Blue);
        Assert.Equal((byte)255, rightEdgeMid.Alpha);
    }

    // T3c.7 negative — non-positive layout.BorderRadius (zero or negative) MUST
    // fall through to the flat-DrawRect path: every corner pixel is paperColor
    // with alpha=255, no clipping. Pins the `if (BorderRadius > 0)` branch in
    // SkiaReceiptRenderer so a future refactor that changes the predicate (e.g.
    // `>= 0` would treat 0 as rounded with rx=0) cannot regress the flat path.
    // T3c.2 already covers BorderRadius=0 implicitly with PaperColor=#FF00FF;
    // this Theory adds explicit pinning for 0 + negative values together.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void T3c_7_BorderRadius_NonPositive_DrawsFlatRect(int borderRadius)
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#FF00FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = borderRadius,
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

        SKColor paper = SKColor.Parse("#FF00FF");

        // All four corners must be opaque paper — no clipping at any non-positive radius.
        SKColor topLeft = bitmap.GetPixel(0, 0);
        SKColor topRight = bitmap.GetPixel(bitmapWidth - 1, 0);
        SKColor bottomLeft = bitmap.GetPixel(0, bitmapHeight - 1);
        SKColor bottomRight = bitmap.GetPixel(bitmapWidth - 1, bitmapHeight - 1);

        foreach (SKColor corner in new[] { topLeft, topRight, bottomLeft, bottomRight })
        {
            Assert.Equal(paper.Red, corner.Red);
            Assert.Equal(paper.Green, corner.Green);
            Assert.Equal(paper.Blue, corner.Blue);
            Assert.Equal((byte)255, corner.Alpha);
        }
    }

    // T3c.7 boundary — borderRadius at half the smaller dimension. Skia clamps
    // very large radii silently to a pill / ellipse shape rather than throwing.
    // borderRadius=180 with receiptWidth=360 forces rx = W/2; the resulting paper
    // shape is an ellipse / pill bounded by the canvas rect. (0,0) is still
    // clearly outside (distance from arc centre (180,180) to (0,0) is 180·sqrt(2)
    // ≈ 254.6, well outside the 180-px curve). The contract pinned here is
    // narrow: corners stay transparent + Render does not throw. Interior pixel
    // sampling is intentionally avoided — section content paints on top of the
    // paper, so any specific interior coordinate could land on text / divider /
    // table chrome rather than raw paper. Mid-edge pixels at the ellipse vertex
    // are also skipped — rasteriser rounding at the tangent point is
    // implementation-defined.
    [Fact]
    public void T3c_7_BorderRadius_AtHalfWidth_ClipsCorners_NoCrash()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Theme = (baseData.Theme ?? new ReceiptTheme()) with { PaperColor = "#FF00FF" },
            Layout = (baseData.Layout ?? new ReceiptLayout()) with
            {
                BorderRadius = 180,
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

        SKColor paper = SKColor.Parse("#FF00FF");

        // Corners — clearly outside the ellipse / pill at any height >= 360.
        SKColor topLeft = bitmap.GetPixel(0, 0);
        SKColor topRight = bitmap.GetPixel(bitmapWidth - 1, 0);
        SKColor bottomLeft = bitmap.GetPixel(0, bitmapHeight - 1);
        SKColor bottomRight = bitmap.GetPixel(bitmapWidth - 1, bitmapHeight - 1);

        Assert.Equal((byte)0, topLeft.Alpha);
        Assert.Equal((byte)0, topRight.Alpha);
        Assert.Equal((byte)0, bottomLeft.Alpha);
        Assert.Equal((byte)0, bottomRight.Alpha);

        // Existence guard — at least one pixel in the bitmap must equal opaque
        // paperColor. Proves the paper-paint pass ran (i.e. the clamp path didn't
        // skip the draw). A linear scan is overkill for a pinned-test perf budget;
        // we sample a horizontal strip 1/3 down the bitmap which is well inside
        // the ellipse but typically lands above the bulk of section content.
        int scanY = bitmapHeight / 3;
        bool foundPaperPixel = false;
        for (int x = 0; x < bitmapWidth; x++)
        {
            SKColor px = bitmap.GetPixel(x, scanY);
            if (px.Red == paper.Red && px.Green == paper.Green && px.Blue == paper.Blue && px.Alpha == 255)
            {
                foundPaperPixel = true;
                break;
            }
        }

        Assert.True(foundPaperPixel,
            $"expected at least one opaque paper pixel along scan row y={scanY} — paper-paint pass missing?");
    }
}
