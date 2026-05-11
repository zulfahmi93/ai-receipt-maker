// Purpose: Tests for Phase 3b/C — PaymentSection renderer, updated for 3c-polish D 2x2 grid.
// T3b.16: single Visa payment renders; values present in PDF text.
// T3b.17: multi-payment fixture — now only first payment is rendered in the 2x2 grid.
//         Old assertion "multi height > single height" retired — 2x2 grid has fixed height.
//         New assertion: second-payment method NOT present in rendered output (grid shows
//         only the first payment), and Measure(empty) = 0 < Measure(single).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class PaymentSectionTests
{
    private const float Width = 360f;

    // T3b.16 — PaymentSection renders the first payment entry in the 2x2 grid.
    //           The extracted PDF text must contain the payment method name ("Visa Credit Card"),
    //           the card last-four digits ("1234"), and the auth code ("A7B3K9").
    [Fact]
    public void PaymentSection_SinglePayment_RendersCompactBlock()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new PaymentSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Visa Credit Card", text, StringComparison.Ordinal);
        Assert.Contains("1234", text, StringComparison.Ordinal);
        Assert.Contains("A7B3K9", text, StringComparison.Ordinal);
    }

    // T3b.17 — PaymentSection 2x2 grid shows only the first payment entry.
    //           A two-payment fixture (original Visa + Cash) must NOT contain "Cash"
    //           in the rendered output (only the first payment fills the grid).
    //           Measure(single) must be > Measure(empty) to prove the grid occupies space.
    [Fact]
    public void PaymentSection_MultiplePayments_RendersRows()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();

        var p1 = data.Payments[0];
        var p2 = new PaymentInfo { Method = "Cash", Amount = 10.00m };
        ReceiptData multi = data with { Payments = [p1, p2] };
        ReceiptData empty = data with { Payments = [] };

        using var fonts = new FontProvider();
        var section = new PaymentSection();

        // 2x2 grid renders only the first payment — "Cash" (second) must not appear.
        string multiText = SectionTestBase.RenderSectionToPdfText(section, multi, fonts);
        Assert.Contains("Visa Credit Card", multiText, StringComparison.Ordinal);
        Assert.DoesNotContain("Cash", multiText, StringComparison.Ordinal);

        // Geometric assertion: empty payments = 0; single payment = positive fixed height.
        using var measureCtx = new RenderContext(fonts, resolvedLogo: null);
        float heightEmpty = section.Measure(Width, empty, measureCtx);
        float heightSingle = section.Measure(Width, data, measureCtx);

        Assert.Equal(0f, heightEmpty);
        Assert.True(heightSingle > 0f,
            $"Expected Measure(1 payment) > 0; got {heightSingle}");
    }
}
