// Purpose: RED-phase tests for Phase 2 (T2.1–T2.14) — ReceiptValidator rule set.
// Categories: Unit — pure in-process, fixture file read for T2.14.
// Edge cases: empty fields, non-positive quantities, negative money, out-of-range
//             tax/width, malformed ISO date, malformed hex color, unknown currency,
//             aggregate-not-first-fail, valid fixture round-trip.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Validation;

namespace ReceiptToolkit.Core.Tests.Validation;

public sealed class ReceiptValidatorTests
{
    // T2.1 — Empty business name fails validation
    [Fact]
    public void T2_01_EmptyBusinessName_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Business = new BusinessInfo { BusinessName = "" }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "business.businessName");
    }

    // T2.2 — Empty receipt number fails validation
    [Fact]
    public void T2_02_EmptyReceiptNumber_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Receipt = new ReceiptMetadata { ReceiptNumber = "" }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "receipt.receiptNumber");
    }

    // T2.3 — Empty items list fails validation
    [Fact]
    public void T2_03_EmptyItems_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Items = []
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "items");
    }

    // T2.4 — Non-positive (zero) quantity fails validation
    [Fact]
    public void T2_04_NonPositiveQuantity_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Items =
            [
                new ReceiptItem
                {
                    Name = "Widget",
                    Quantity = 0,
                    UnitPrice = 10m,
                    Discount = 0m,
                    TaxRate = 0.08,
                    Total = 10m,
                }
            ]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "items[0].quantity");
    }

    // T2.5 — Negative unit price fails validation
    [Fact]
    public void T2_05_NegativeUnitPrice_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Items =
            [
                new ReceiptItem
                {
                    Name = "Widget",
                    Quantity = 1,
                    UnitPrice = -10m,
                    Discount = 0m,
                    TaxRate = 0.08,
                    Total = 10m,
                }
            ]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "items[0].unitPrice");
    }

    // T2.6 — Negative discount fails validation
    [Fact]
    public void T2_06_NegativeDiscount_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Items =
            [
                new ReceiptItem
                {
                    Name = "Widget",
                    Quantity = 1,
                    UnitPrice = 10m,
                    Discount = -5m,
                    TaxRate = 0.08,
                    Total = 10m,
                }
            ]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "items[0].discount");
    }

    // T2.7 — Tax rate out of range [0, 1] fails validation (uses Theory for boundary test)
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void T2_07_TaxRateOutOfRange_FailsValidation(double taxRate)
    {
        var data = ValidBaseline() with
        {
            Items =
            [
                new ReceiptItem
                {
                    Name = "Widget",
                    Quantity = 1,
                    UnitPrice = 10m,
                    Discount = 0m,
                    TaxRate = taxRate,
                    Total = 10m,
                }
            ]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "items[0].taxRate");
    }

    // T2.8 — Negative payment amount fails validation
    [Fact]
    public void T2_08_NegativePaymentAmount_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Payments = [new PaymentInfo { Amount = -10m }]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "payments[0].amount");
    }

    // T2.9 — Invalid ISO 8601 datetime fails validation
    [Fact]
    public void T2_09_InvalidIsoDateTime_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Receipt = new ReceiptMetadata
            {
                ReceiptNumber = "R-1",
                DateTime = "not-a-date"
            }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "receipt.dateTime");
    }

    // T2.10 — Invalid hex color in theme fails validation
    [Fact]
    public void T2_10_InvalidHexColorInTheme_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Theme = new ReceiptTheme { AccentColor = "not-hex" }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "theme.accentColor");
    }

    // T2.11 — Unknown ISO 4217 currency code fails validation
    [Fact]
    public void T2_11_UnknownCurrencyCode_FailsValidation()
    {
        var data = ValidBaseline() with
        {
            Options = new ReceiptOptions { Currency = "ZZZ" }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "options.currency");
    }

    // T2.12 — Receipt width out of valid range fails validation (uses Theory for boundary test)
    [Theory]
    [InlineData(100)]
    [InlineData(1500)]
    public void T2_12_ReceiptWidthOutOfRange_FailsValidation(int width)
    {
        var data = ValidBaseline() with
        {
            Layout = new ReceiptLayout { ReceiptWidth = width }
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.Contains(errors, e => e.Field == "layout.receiptWidth");
    }

    // T2.13 — Aggregates all errors when multiple rules violated
    [Fact]
    public void T2_13_AggregatesAllErrors_NotFirstFail()
    {
        var data = new ReceiptData
        {
            Business = new BusinessInfo { BusinessName = "" },
            Receipt = new ReceiptMetadata { ReceiptNumber = "" },
            Items = [],
            Payments = [new PaymentInfo { Amount = -10m }]
        };

        var errors = new ReceiptValidator().Validate(data);

        Assert.True(errors.Count >= 3, "Expected at least 3 errors for multi-violation receipt");
        Assert.Contains(errors, e => e.Field == "business.businessName");
        Assert.Contains(errors, e => e.Field == "receipt.receiptNumber");
        Assert.Contains(errors, e => e.Field == "items");
    }

    // T2.14 — Valid sample fixture parses and validates with no errors
    [Fact]
    public void T2_14_ValidSampleFixture_ReturnsEmpty()
    {
        var json = File.ReadAllText(FixturePath);
        var data = ReceiptData.FromJson(json);

        var errors = new ReceiptValidator().Validate(data);

        Assert.Empty(errors);
    }

    // Helper: fixture path
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_receipt_data.json");

    // Helper: minimal valid baseline for mutation tests
    private static ReceiptData ValidBaseline() => new()
    {
        Business = new BusinessInfo { BusinessName = "Acme" },
        Receipt = new ReceiptMetadata { ReceiptNumber = "R-1", DateTime = "2025-05-18T10:42:00" },
        Items =
        [
            new ReceiptItem
            {
                Name = "Widget",
                Quantity = 1,
                UnitPrice = 10m,
                Discount = 0m,
                TaxRate = 0.08,
                Total = 10m,
            }
        ],
        Payments = [new PaymentInfo { Amount = 10m }],
    };
}
