// Purpose: RED-phase tests for Phase 2c (T2c.1–T2c.3) — MoneyFormatter.Format.
// Categories: Unit — pure in-process formatting; tests currency symbol resolution,
//             decimal place rounding (MidpointRounding.AwayFromZero), and built-in vs
//             custom symbol precedence via CurrencyTable.
// Edge cases: explicit symbol override, zero decimal places (JPY), rounding midpoint
//             (12.5 → 13 in JPY), and null CurrencySymbol fallback to built-in table.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;

namespace ReceiptToolkit.Core.Tests.Formatting;

public sealed class MoneyFormatterTests
{
    // T2c.1 — Format USD with explicit symbol prefix, 2 decimal places.
    //          amount=12.5m, options.Currency="USD", options.CurrencySymbol="$"
    //          → "$12.50"
    [Fact]
    public void T2c_01_Format_UsdWithExplicitSymbol_RoundsTo2DpAndPrefixesSymbol()
    {
        var options = new ReceiptOptions { Currency = "USD", CurrencySymbol = "$" };

        var result = MoneyFormatter.Format(12.5m, options);

        Assert.Equal("$12.50", result);
    }

    // T2c.2 — Format JPY with built-in symbol (no custom CurrencySymbol), 0 decimal places.
    //          amount=12.5m, options.Currency="JPY", options.CurrencySymbol=null
    //          Rounding: MidpointRounding.AwayFromZero → 12.5 rounds to 13.
    //          → "¥13"
    [Fact]
    public void T2c_02_Format_JpyWithoutCustomSymbol_UsesBuiltInSymbolAndZeroDp()
    {
        var options = new ReceiptOptions { Currency = "JPY" };

        var result = MoneyFormatter.Format(12.5m, options);

        Assert.Equal("¥13", result);
    }

    // T2c.3 — Custom CurrencySymbol overrides built-in table entry.
    //          amount=12.5m, options.Currency="USD", options.CurrencySymbol="USD$"
    //          → "USD$12.50"
    [Fact]
    public void T2c_03_Format_CustomSymbolOverridesBuiltInTable()
    {
        var options = new ReceiptOptions { Currency = "USD", CurrencySymbol = "USD$" };

        var result = MoneyFormatter.Format(12.5m, options);

        Assert.Equal("USD$12.50", result);
    }
}
