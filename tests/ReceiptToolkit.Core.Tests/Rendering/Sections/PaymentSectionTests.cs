// Purpose: RED-phase tests for Phase 3b/C — PaymentSection renderer (T3b.16–T3b.17).
// Categories: Unit — section rendering, PDF text extraction via PdfPig for payment
//             method name, card last-four, and authorization code text presence.
// Edge cases: single Visa payment renders all three fields verbatim (T3b.16);
//             two-payment fixture renders both method names and the second method
//             is absent from single-payment render (T3b.17 sanity); Measure(multi)
//             strictly exceeds Measure(single) proving a row is added per payment.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class PaymentSectionTests
{
    private const float Width = 360f;

    // T3b.16 — PaymentSection renders a compact block for the single Visa payment in the
    //           sample fixture. The extracted PDF text must contain the payment method name
    //           ("Visa Credit Card"), the card last-four digits ("1234"), and the auth code
    //           ("A7B3K9"). All three fields must be present; none are optional presentation.
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

    // T3b.17 — PaymentSection renders one row per payment when multiple payments are
    //           present. A two-payment fixture (original Visa + a Cash payment) must produce
    //           PDF text containing both method names. The single-payment render must NOT
    //           contain "Cash" (sanity: the second payment is not phantom-rendered).
    //           Measure(multi) must be strictly greater than Measure(single), proving the
    //           section reserves an additional row for each extra payment.
    [Fact]
    public void PaymentSection_MultiplePayments_RendersRows()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();

        var p1 = data.Payments[0];
        var p2 = new PaymentInfo { Method = "Cash", Amount = 10.00m };
        ReceiptData multi = data with { Payments = [p1, p2] };

        using var fonts = new FontProvider();
        var section = new PaymentSection();

        string singleText = SectionTestBase.RenderSectionToPdfText(section, data, fonts);
        string multiText = SectionTestBase.RenderSectionToPdfText(section, multi, fonts);

        // Both payment methods must appear in the multi-payment render.
        Assert.Contains("Visa Credit Card", multiText, StringComparison.Ordinal);
        Assert.Contains("Cash", multiText, StringComparison.Ordinal);

        // Sanity: the Cash payment is absent from the single-payment render.
        Assert.DoesNotContain("Cash", singleText, StringComparison.Ordinal);

        // Geometric assertion: multi-payment must produce strictly greater Measure height.
        using var measureCtx = new RenderContext(fonts, resolvedLogo: null);

        float heightSingle = section.Measure(Width, data, measureCtx);
        float heightMulti = section.Measure(Width, multi, measureCtx);

        Assert.True(
            heightMulti > heightSingle,
            $"Expected Measure(2 payments) > Measure(1 payment); got {heightMulti} vs {heightSingle}");
    }
}
