// Purpose: RED-phase test for Phase 3b/B — CustomerCashierSection renderer (T3b.6).
// Categories: Unit — section rendering, PDF text extraction via PdfPig, two-column
//             null-aware field rendering for customer and cashier bands.
// Edge cases: null customer/cashier fields are hidden without leaving an empty row gap,
//             verified via Measure shrinking when fields are set to null; non-null fields
//             (customerName="Walk-in Customer", cashierName="Alex Johnson",
//             cashierId="EMP-001") must appear in the PDF text.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class CustomerCashierSectionTests
{
    // T3b.6 — CustomerCashierSection two-column null-aware: cashier and customer fields
    //          are shown only when non-null, and null fields are hidden without leaving an
    //          empty row gap (verified by Measure(full) > Measure(all-null)). The sample
    //          fixture has customerName="Walk-in Customer" (other customer fields null),
    //          cashierName="Alex Johnson", cashierId="EMP-001". All three non-null strings
    //          must appear in the full-data PDF text. When every customer + cashier field is
    //          set to null, none of those strings appear and Measure returns 0 (or strictly
    //          less than Measure(full)), confirming no empty gap row is reserved.
    [Fact]
    public void CustomerCashierSection_ShowsNonNullFields_HidesNullFields_WithoutGapRow()
    {
        ReceiptData full = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new CustomerCashierSection();

        string textFull = SectionTestBase.RenderSectionToPdfText(section, full, fonts);

        Assert.Contains("Walk-in Customer", textFull, StringComparison.Ordinal);
        Assert.Contains("Alex Johnson", textFull, StringComparison.Ordinal);
        Assert.Contains("EMP-001", textFull, StringComparison.Ordinal);

        // Null out the entire customer and cashier objects — the section must produce no
        // output. Customer and Cashier are nullable on ReceiptData so a null parent is the
        // canonical "absent" form; the section must also tolerate this without throwing.
        ReceiptData allNull = full with
        {
            Customer = null,
            Cashier = null,
        };

        string textAllNull = SectionTestBase.RenderSectionToPdfText(section, allNull, fonts);

        Assert.DoesNotContain("Walk-in Customer", textAllNull, StringComparison.Ordinal);
        Assert.DoesNotContain("Alex Johnson", textAllNull, StringComparison.Ordinal);
        Assert.DoesNotContain("EMP-001", textAllNull, StringComparison.Ordinal);

        const float Width = 360f;
        float heightFull;
        float heightAllNull;

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightFull = section.Measure(Width, full, ctx);
        }

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightAllNull = section.Measure(Width, allNull, ctx);
        }

        Assert.True(
            heightFull > heightAllNull,
            $"Expected Measure(full) > Measure(all-null); got {heightFull} vs {heightAllNull} — null fields must not leave a gap row");
    }
}
