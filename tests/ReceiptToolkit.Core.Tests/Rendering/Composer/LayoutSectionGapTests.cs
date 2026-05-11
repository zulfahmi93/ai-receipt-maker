// Purpose: RED-phase tests for Phase 3c-polish A T3cP.1 — ReceiptLayout.SectionGap
//          nullable (float?) with DefaultSectionGap fallback in SkiaReceiptRenderer.
// T3cP.1a: Measure with SectionGap=8f returns height < baseline (SectionGap=18 in sample).
// T3cP.1b: Measure with SectionGap=null equals baseline Measure with no mutation.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Composer;

/// <summary>
///   Pins the <c>SectionGap</c> nullable-float override contract added in Phase
///   3c-polish A (T3cP.1). When <c>layout.SectionGap</c> is provided, the composer
///   uses it; when null, it falls back to <see cref="SkiaReceiptRenderer.DefaultSectionGap"/>.
/// </summary>
public sealed class LayoutSectionGapTests
{
    // T3cP.1a — explicit SectionGap=8f produces a smaller total height than the
    // sample fixture default (sectionGap=18). Verifies that the composer reads the
    // nullable value rather than ignoring it.
    [Fact]
    public void LayoutSectionGap_AppliesToComposition()
    {
        ReceiptData sample = SectionTestBase.LoadSampleData();
        ReceiptData dataSmallGap = sample with
        {
            Layout = (sample.Layout ?? new ReceiptLayout()) with
            {
                SectionGap = 8f,
            },
        };

        var renderer = new SkiaReceiptRenderer();
        using var fonts = new FontProvider();
        using var ctxBaseline = new RenderContext(fonts, resolvedLogo: null);
        using var ctxSmall = new RenderContext(fonts, resolvedLogo: null);

        SKSize baseline = renderer.Measure(sample, ctxBaseline);
        SKSize small = renderer.Measure(dataSmallGap, ctxSmall);

        Assert.True(
            small.Height < baseline.Height,
            $"expected SectionGap=8f height ({small.Height}) < baseline ({baseline.Height})");
    }

    // T3cP.1b — SectionGap=null falls back to DefaultSectionGap, so Measure equals
    // the baseline measurement from the sample fixture (which already carries
    // sectionGap=18 matching DefaultSectionGap). Proves the null path is unchanged.
    [Fact]
    public void LayoutSectionGap_NullFallsBackToDefault()
    {
        ReceiptData sample = SectionTestBase.LoadSampleData();
        ReceiptData dataNullGap = sample with
        {
            Layout = (sample.Layout ?? new ReceiptLayout()) with
            {
                SectionGap = null,
            },
        };

        var renderer = new SkiaReceiptRenderer();
        using var fonts = new FontProvider();
        using var ctxBaseline = new RenderContext(fonts, resolvedLogo: null);
        using var ctxNull = new RenderContext(fonts, resolvedLogo: null);

        SKSize baseline = renderer.Measure(sample, ctxBaseline);
        SKSize withNull = renderer.Measure(dataNullGap, ctxNull);

        Assert.Equal(baseline.Height, withNull.Height, precision: 1);
    }
}
