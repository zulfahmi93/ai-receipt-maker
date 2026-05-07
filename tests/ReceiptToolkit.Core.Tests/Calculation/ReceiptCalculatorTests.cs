// Purpose: RED-phase tests for Phase 2b (T2b.1–T2b.10) — ReceiptCalculator.CalculateTotals.
// Categories: Unit — pure in-process; one test reads the linked sample fixture.
// Edge cases: per-item tax accumulation, receipt-level vs item-level discounts,
//             rounding-adjustment pass-through, manual-totals respected when
//             AutoCalculateTotals=false, idempotence under repeat invocation,
//             multi-currency rounding (USD/MYR=2dp, JPY=0dp), and explicit
//             MidpointRounding.AwayFromZero (round-half-up) midpoint behaviour.

using System.Text.Json.Nodes;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Calculation;

namespace ReceiptToolkit.Core.Tests.Calculation;

public sealed class ReceiptCalculatorTests
{
    // T2b.1 — Subtotal sums per-item lineGross = qty*unitPrice - discount across all items.
    //          Items: (qty 2, unitPrice 10.00, disc 0)  -> 20.00
    //                 (qty 1, unitPrice  5.00, disc 1)  ->  4.00
    //          Expected subtotal = 24.00.
    [Fact]
    public void T2b_01_Subtotal_SumsLineGrossAcrossItems()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 2, UnitPrice = 10.00m, Discount = 0m, TaxRate = 0.0 },
                new ReceiptItem { Name = "B", Quantity = 1, UnitPrice = 5.00m,  Discount = 1.00m, TaxRate = 0.0 },
            ],
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(24.00m, result.Totals.Subtotal);
    }

    // T2b.2 — Per-item tax accumulates as a decimal running sum, rounded once at end.
    //          Item 1: lineGross 10, taxRate 0.10 -> 1.00
    //          Item 2: lineGross 20, taxRate 0.20 -> 4.00
    //          Expected taxTotal = 5.00.
    [Fact]
    public void T2b_02_PerItemTax_AccumulatesAcrossDifferingRates()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = 10.00m, Discount = 0m, TaxRate = 0.10 },
                new ReceiptItem { Name = "B", Quantity = 1, UnitPrice = 20.00m, Discount = 0m, TaxRate = 0.20 },
            ],
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(5.00m, result.Totals.TaxTotal);
    }

    // T2b.3 — discountTotal = inputTotals.DiscountTotal (receipt-level) + Σ items[i].Discount.
    //          Receipt-level discount = 5.00, items contribute 1.00 + 2.00 -> output 8.00.
    [Fact]
    public void T2b_03_DiscountTotal_AddsReceiptLevelAndItemLevelDiscounts()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = 10.00m, Discount = 1.00m, TaxRate = 0.0 },
                new ReceiptItem { Name = "B", Quantity = 1, UnitPrice = 10.00m, Discount = 2.00m, TaxRate = 0.0 },
            ],
            Totals = new ReceiptTotals { DiscountTotal = 5.00m },
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(8.00m, result.Totals.DiscountTotal);
    }

    // T2b.4 — serviceCharge passes through unchanged from input totals.
    [Fact]
    public void T2b_04_ServiceCharge_PassesThroughUnchanged()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = 10.00m, Discount = 0m, TaxRate = 0.0 },
            ],
            Totals = new ReceiptTotals { ServiceCharge = 2.50m },
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(2.50m, result.Totals.ServiceCharge);
    }

    // T2b.5 — roundingAdjustment passes through and is applied last in grandTotal.
    //          Single item lineGross=10, no tax/discount, service=0, rounding=0.07.
    //          Expected grandTotal = 10.00 - 0 + 0 + 0 + 0.07 = 10.07.
    [Fact]
    public void T2b_05_RoundingAdjustment_PassesThroughAndAppliedLastInGrandTotal()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = 10.00m, Discount = 0m, TaxRate = 0.0 },
            ],
            Totals = new ReceiptTotals { RoundingAdjustment = 0.07m },
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(0.07m, result.Totals.RoundingAdjustment);
        Assert.Equal(10.07m, result.Totals.GrandTotal);
    }

    // T2b.6 — grandTotal = subtotal − discountTotal + taxTotal + serviceCharge + roundingAdjustment.
    //          Crafted: subtotal=20, item discounts sum=0, receipt-level discount=2, tax=1.50,
    //                   service=1, rounding=0.05 -> grandTotal = 20 - 2 + 1.50 + 1 + 0.05 = 20.55.
    //          To get tax=1.50 exactly: lineGross=15.00 at taxRate=0.10.
    //          Use a second non-tax item to bring subtotal to 20.00 (lineGross=5.00, taxRate=0).
    [Fact]
    public void T2b_06_GrandTotal_FollowsFormula()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "Taxed",   Quantity = 1, UnitPrice = 15.00m, Discount = 0m, TaxRate = 0.10 },
                new ReceiptItem { Name = "Untaxed", Quantity = 1, UnitPrice = 5.00m,  Discount = 0m, TaxRate = 0.0 },
            ],
            Totals = new ReceiptTotals
            {
                DiscountTotal = 2.00m,
                ServiceCharge = 1.00m,
                RoundingAdjustment = 0.05m,
            },
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(20.00m, result.Totals.Subtotal);
        Assert.Equal(2.00m, result.Totals.DiscountTotal);
        Assert.Equal(1.50m, result.Totals.TaxTotal);
        Assert.Equal(1.00m, result.Totals.ServiceCharge);
        Assert.Equal(0.05m, result.Totals.RoundingAdjustment);
        Assert.Equal(20.55m, result.Totals.GrandTotal);
    }

    // T2b.7 — Manual totals are respected when Options.AutoCalculateTotals == false.
    //          Load fixture, force autoCalculateTotals=false, replace Totals with sentinel,
    //          assert all 7 fields come back unchanged.
    [Fact]
    public void T2b_07_ManualTotals_RespectedWhenAutoCalculateDisabled()
    {
        var json = File.ReadAllText(FixturePath);
        var loaded = ReceiptData.FromJson(json);

        var sentinel = new ReceiptTotals
        {
            Subtotal = 999.99m,
            DiscountTotal = 111.11m,
            ServiceCharge = 22.22m,
            TaxLabel = "Sentinel Tax",
            TaxTotal = 33.33m,
            RoundingAdjustment = 0.44m,
            GrandTotal = 12345.67m,
        };

        var data = loaded with
        {
            Options = (loaded.Options ?? new ReceiptOptions()) with { AutoCalculateTotals = false },
            Totals = sentinel,
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(sentinel.Subtotal, result.Totals.Subtotal);
        Assert.Equal(sentinel.DiscountTotal, result.Totals.DiscountTotal);
        Assert.Equal(sentinel.ServiceCharge, result.Totals.ServiceCharge);
        Assert.Equal(sentinel.TaxLabel, result.Totals.TaxLabel);
        Assert.Equal(sentinel.TaxTotal, result.Totals.TaxTotal);
        Assert.Equal(sentinel.RoundingAdjustment, result.Totals.RoundingAdjustment);
        Assert.Equal(sentinel.GrandTotal, result.Totals.GrandTotal);
    }

    // T2b.8 — Idempotence: Calc(Calc(d)) yields the same Totals as Calc(d).
    //          ReceiptData record equality is reference-based for collections (per its XML doc),
    //          so we round-trip both results through JSON and structurally compare via
    //          JsonNode.DeepEquals on the "totals" subtree (mirrors the Phase 1 T1.7 pattern).
    [Fact]
    public void T2b_08_Calculate_IsIdempotent()
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 2, UnitPrice = 12.34m, Discount = 0.50m, TaxRate = 0.0825 },
                new ReceiptItem { Name = "B", Quantity = 1, UnitPrice = 7.77m,  Discount = 0m,    TaxRate = 0.10 },
            ],
            Totals = new ReceiptTotals
            {
                DiscountTotal = 1.00m,
                ServiceCharge = 0.50m,
                RoundingAdjustment = 0.03m,
            },
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var once = ReceiptCalculator.CalculateTotals(data);
        var twice = ReceiptCalculator.CalculateTotals(once);

        Assert.Equal(once.Totals.Subtotal, twice.Totals.Subtotal);
        Assert.Equal(once.Totals.DiscountTotal, twice.Totals.DiscountTotal);
        Assert.Equal(once.Totals.ServiceCharge, twice.Totals.ServiceCharge);
        Assert.Equal(once.Totals.TaxLabel, twice.Totals.TaxLabel);
        Assert.Equal(once.Totals.TaxTotal, twice.Totals.TaxTotal);
        Assert.Equal(once.Totals.RoundingAdjustment, twice.Totals.RoundingAdjustment);
        Assert.Equal(once.Totals.GrandTotal, twice.Totals.GrandTotal);

        // Defence-in-depth: structural JSON equality of the totals subtree.
        var onceTotals = JsonNode.Parse(once.ToJson())!["totals"];
        var twiceTotals = JsonNode.Parse(twice.ToJson())!["totals"];
        Assert.True(JsonNode.DeepEquals(onceTotals, twiceTotals));
    }

    // T2b.9 — Multi-currency rounding honours CurrencyTable.DecimalPlaces.
    //          Single line: qty 1 × unitPrice 100.555 (no tax/discount/service/rounding).
    //          USD (2dp): 100.555 -> 100.56 (banker's: trailing 5 with preceding odd 5 -> up).
    //          MYR (2dp): same -> 100.56.
    //          JPY (0dp): 100.555 -> 101 (>.5 from 100, plain round-up; not a midpoint).
    //          Subtotal is also rounded to currency precision per the calculator contract.
    [Fact]
    public void T2b_09_MultiCurrencyRounding_HonoursCurrencyDecimalPlaces()
    {
        ReceiptData Build(string currency) => new()
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = 100.555m, Discount = 0m, TaxRate = 0.0 },
            ],
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = currency },
        };

        var usd = ReceiptCalculator.CalculateTotals(Build("USD"));
        var jpy = ReceiptCalculator.CalculateTotals(Build("JPY"));
        var myr = ReceiptCalculator.CalculateTotals(Build("MYR"));

        Assert.Equal(100.56m, usd.Totals.GrandTotal);
        Assert.Equal(100.56m, myr.Totals.GrandTotal);

        // JPY uses 0 decimal places — value must have no fractional part.
        Assert.Equal(101m, jpy.Totals.GrandTotal);
        Assert.Equal(0m, jpy.Totals.GrandTotal - decimal.Truncate(jpy.Totals.GrandTotal));
    }

    // T2b.10 — Round-half-up (MidpointRounding.AwayFromZero) at exact 2dp midpoints.
    //           Construct lineGross * (decimal)taxRate to land on an exact .xx5 midpoint:
    //             lineGross 12.45 × 0.10 = 1.245 -> round-half-up 2dp -> 1.25.
    //             lineGross 12.55 × 0.10 = 1.255 -> round-half-up 2dp -> 1.26.
    //           Note: the calculator's XML doc must declare MidpointRounding.AwayFromZero; that
    //           documentation check is enforced in the REFACTOR pass, not this test.
    [Theory]
    [InlineData(12.45, 1.25)]
    [InlineData(12.55, 1.26)]
    public void T2b_10_HalfUpRounding_AppliesToTaxTotalAtMidpoints(decimal unitPrice, decimal expectedTax)
    {
        var data = new ReceiptData
        {
            Items =
            [
                new ReceiptItem { Name = "A", Quantity = 1, UnitPrice = unitPrice, Discount = 0m, TaxRate = 0.10 },
            ],
            Options = new ReceiptOptions { AutoCalculateTotals = true, Currency = "USD" },
        };

        var result = ReceiptCalculator.CalculateTotals(data);

        Assert.Equal(expectedTax, result.Totals.TaxTotal);
    }

    // Helper: fixture path (linked into the test project's output as Fixtures/sample_receipt_data.json).
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_receipt_data.json");
}
