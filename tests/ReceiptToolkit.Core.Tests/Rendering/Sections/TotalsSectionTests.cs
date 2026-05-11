// Purpose: RED-phase tests for Phase 3b/C — TotalsSection renderer (T3b.11–T3b.15).
// Categories: Unit — section rendering, geometric height assertions via Measure for
//             toggle-driven row visibility, pixel-mode assertion for themed TOTAL bar.
// Edge cases: DiscountTotal=0 hides the discount row (T3b.11); ServiceCharge=0 hides
//             the service-charge row (T3b.12); RoundingAdjustment=0 hides the rounding
//             row (T3b.13); ShowTaxBreakdown=false hides the tax row via nullable-parent
//             guard (T3b.14); TOTAL bar background color matches theme.highlightColor
//             sampled at the bottom-right interior of the bar (T3b.15).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class TotalsSectionTests
{
    private const float Width = 360f;

    // T3b.11 — TotalsSection hides the discount row when DiscountTotal is zero.
    //           Sample now has DiscountTotal=0 (workshop demo — no discount).
    //           Mutating it to 50.00m adds the discount row; Measure(with-discount)
    //           must be strictly greater than Measure(without-discount, i.e. the
    //           original sample), proving no gap row is reserved when discount is zero.
    [Fact]
    public void TotalsSection_HidesDiscountRow_WhenDiscountTotalIsZero()
    {
        ReceiptData withoutDiscountData = SectionTestBase.LoadSampleData();
        ReceiptData withDiscountData = withoutDiscountData with
        {
            Totals = withoutDiscountData.Totals with { DiscountTotal = 50.00m },
        };

        var section = new TotalsSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.True(
            section.Measure(Width, withDiscountData, ctx) > section.Measure(Width, withoutDiscountData, ctx),
            "Expected Measure(DiscountTotal=50.00) > Measure(DiscountTotal=0) — discount row must be hidden when zero");
    }

    // T3b.12 — TotalsSection hides the service-charge row when ServiceCharge is zero.
    //           Sample has ServiceCharge=0 (without-service baseline). Mutating it to 5.00m
    //           adds the service-charge row; Measure(with-service) must be strictly greater
    //           than Measure(without-service, i.e. the original sample).
    [Fact]
    public void TotalsSection_HidesServiceChargeRow_WhenServiceChargeIsZero()
    {
        ReceiptData withoutServiceData = SectionTestBase.LoadSampleData();
        ReceiptData withServiceData = withoutServiceData with
        {
            Totals = withoutServiceData.Totals with { ServiceCharge = 5.00m },
        };

        var section = new TotalsSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.True(
            section.Measure(Width, withServiceData, ctx) > section.Measure(Width, withoutServiceData, ctx),
            "Expected Measure(ServiceCharge=5.00) > Measure(ServiceCharge=0) — service-charge row must be hidden when zero");
    }

    // T3b.13 — TotalsSection hides the rounding row when RoundingAdjustment is zero.
    //           Sample has RoundingAdjustment=0 (without-rounding baseline). Mutating it to
    //           0.05m adds the rounding row; Measure(with-rounding) must be strictly greater
    //           than Measure(without-rounding, i.e. the original sample).
    [Fact]
    public void TotalsSection_HidesRoundingRow_WhenRoundingAdjustmentIsZero()
    {
        ReceiptData withoutRoundingData = SectionTestBase.LoadSampleData();
        ReceiptData withRoundingData = withoutRoundingData with
        {
            Totals = withoutRoundingData.Totals with { RoundingAdjustment = 0.05m },
        };

        var section = new TotalsSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.True(
            section.Measure(Width, withRoundingData, ctx) > section.Measure(Width, withoutRoundingData, ctx),
            "Expected Measure(RoundingAdjustment=0.05) > Measure(RoundingAdjustment=0) — rounding row must be hidden when zero");
    }

    // T3b.14 — TotalsSection hides the tax row when ShowTaxBreakdown is false.
    //           Sample has ShowTaxBreakdown=true (with-tax baseline). The nullable-parent
    //           guard pattern is required because Options is ReceiptOptions? — using a
    //           bare `data.Options with { … }` would emit CS8602 on a null receiver.
    //           Measure(with-tax) must be strictly greater than Measure(without-tax).
    [Fact]
    public void TotalsSection_HidesTaxRow_WhenShowTaxBreakdownIsFalse()
    {
        ReceiptData withTaxData = SectionTestBase.LoadSampleData();
        ReceiptData withoutTaxData = withTaxData with
        {
            Options = (withTaxData.Options ?? new ReceiptOptions()) with { ShowTaxBreakdown = false },
        };

        var section = new TotalsSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.True(
            section.Measure(Width, withTaxData, ctx) > section.Measure(Width, withoutTaxData, ctx),
            "Expected Measure(ShowTaxBreakdown=true) > Measure(ShowTaxBreakdown=false) — tax row must be hidden when breakdown is disabled");
    }

    // T3b.15 — TotalsSection draws the TOTAL bar with theme.highlightColor as its
    //           background fill. The sample fixture has theme.highlightColor="#E8F0EC".
    //           A pixel sampled at the bottom-right interior of the TOTAL bar (the last
    //           row of TotalsSection) must match SKColor.Parse(theme.highlightColor).
    //           ThemeColors.ResolveOrDefault is internal; we compare directly against
    //           SKColor.Parse(data.Theme!.HighlightColor!) — equivalent when the hex is
    //           valid and non-null (the precondition assertions verify this).
    [Fact]
    public void TotalsSection_DrawsTotalBar_WithHighlightColor()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();

        Assert.NotNull(data.Theme);
        Assert.NotNull(data.Theme.HighlightColor);

        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) = SectionTestBase.CreateBitmapContext(data);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var section = new TotalsSection();
            float height = section.Measure(Width, data, ctx);
            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);

            // Sample point: bottom-right interior of the TOTAL bar — last row in TotalsSection.
            int x = (int)(Width - 10);
            int y = (int)(height - 8);
            SKColor actualPixel = bitmap.GetPixel(x, y);

            SKColor expectedColor = SKColor.Parse(data.Theme!.HighlightColor!);
            Assert.Equal(expectedColor, actualPixel);
        }
    }
}
