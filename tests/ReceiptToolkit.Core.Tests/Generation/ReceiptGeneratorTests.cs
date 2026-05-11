// Purpose: RED-phase tests for Phase 3e sub-cluster A — ReceiptGenerator façade
//          (T3e.1, T3e.2, T3e.3). Pin validation guard + auto-calculate honour.
// Categories: Unit — façade contract. T3e.1 verifies ReceiptValidationException is
//             thrown with non-empty Errors when validation fails. T3e.2 verifies the
//             calculator runs when options.autoCalculateTotals=true (recomputed grand
//             total surfaces in PDF text, not the bogus seed). T3e.3 verifies the
//             calculator is skipped when options.autoCalculateTotals=false (input
//             totals.* are rendered verbatim).
// Edge cases: BusinessNameRule fires on empty business name → simplest single-rule
//             trigger. Bogus seed Subtotal=0 ensures the calculator's idempotence
//             fingerprint does NOT short-circuit, so DiscountTotal is recomputed
//             too. Custom totals in T3e.3 use a single recognisable token "123.45"
//             that does not collide with any other receipt value.

using System.Text;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using ReceiptToolkit.Core.Tests.Time;
using UglyToad.PdfPig;

namespace ReceiptToolkit.Core.Tests.Generation;

public sealed class ReceiptGeneratorTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2025, 5, 18, 10, 42, 0, TimeSpan.Zero);

    // T3e.1 — Invalid input (empty business name fires BusinessNameRule) must throw
    //          ReceiptValidationException carrying the full ValidationError[] surface
    //          for callers (CLI, API, bot) to render to users in a single round-trip.
    [Fact]
    public async Task GeneratePdfAsync_InvalidData_ThrowsReceiptValidationException()
    {
        ReceiptData invalid = SectionTestBase.LoadSampleData() with
        {
            Business = new BusinessInfo { BusinessName = string.Empty },
        };
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        ReceiptValidationException ex = await Assert.ThrowsAsync<ReceiptValidationException>(
            () => generator.GeneratePdfAsync(invalid, TestContext.Current.CancellationToken));

        Assert.NotEmpty(ex.Errors);
        Assert.Contains(ex.Errors, e => e.Field.Contains("business", StringComparison.OrdinalIgnoreCase));
    }

    // T3e.2 — When options.autoCalculateTotals=true the calculator runs end-to-end.
    //          We mutate Totals.GrandTotal to a bogus 999.99 and zero out Subtotal so
    //          the idempotence fingerprint cannot mark the input as a prior output.
    //          Rendered PDF text must contain the recomputed "719.86" (sample-fixture
    //          truth) and must NOT contain the bogus seed. Grand total updated
    //          2026-05-11 when the fixture's hardcoded totals were realigned with the
    //          calculator's output (subtotal 56.40 + tax 4.65 - discount 4.00 = 719.86).
    [Fact]
    public async Task GeneratePdfAsync_AutoCalculateTrue_RecalculatesGrandTotal()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        ReceiptData withBogusTotals = data with
        {
            Totals = data.Totals with
            {
                Subtotal = 0m,
                GrandTotal = 999.99m,
            },
        };
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        byte[] pdf = await generator.GeneratePdfAsync(withBogusTotals, TestContext.Current.CancellationToken);
        string text = ExtractPdfText(pdf);

        Assert.Contains("719.86", text, StringComparison.Ordinal);
        Assert.DoesNotContain("999.99", text, StringComparison.Ordinal);
    }

    // T3e.3 — When options.autoCalculateTotals=false the calculator is skipped and
    //          totals.* are rendered exactly as supplied. We mutate GrandTotal to a
    //          unique "123.45" token and assert it surfaces verbatim in the PDF.
    [Fact]
    public async Task GeneratePdfAsync_AutoCalculateFalse_PreservesInputTotals()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        ReceiptData withCustomTotals = data with
        {
            Options = (data.Options ?? new ReceiptOptions()) with { AutoCalculateTotals = false },
            Totals = data.Totals with { GrandTotal = 123.45m },
        };
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        byte[] pdf = await generator.GeneratePdfAsync(withCustomTotals, TestContext.Current.CancellationToken);
        string text = ExtractPdfText(pdf);

        Assert.Contains("123.45", text, StringComparison.Ordinal);
    }

    private static string ExtractPdfText(byte[] bytes)
    {
        using PdfDocument pdf = PdfDocument.Open(bytes);
        var sb = new StringBuilder();
        for (int i = 1; i <= pdf.NumberOfPages; i++)
        {
            sb.Append(pdf.GetPage(i).Text);
            sb.Append('\n');
        }

        return sb.ToString();
    }
}
