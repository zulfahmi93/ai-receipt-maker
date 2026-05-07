// Purpose: RED-phase tests for Phase 2c (T2c.4–T2c.5) — DateTimeFormatter.FormatDate & FormatTime.
// Categories: Unit — pure in-process localization; tests ICU locale resolution,
//             invariant culture fallback, and custom DateFormat/TimeFormat patterns.
// Edge cases: locale-specific month names (ms-MY → "Mei" for May), AM/PM designator
//             with invariant culture (tt → "AM"), ISO 8601 datetime parsing and re-formatting.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;

namespace ReceiptToolkit.Core.Tests.Formatting;

public sealed class DateTimeFormatterTests
{
    // T2c.4 — FormatDate with ms-MY locale produces Malay month name.
    //          isoDateTime="2025-05-18T10:42:00", Locale="ms-MY", DateFormat="dd MMM yyyy"
    //          ICU data on .NET 10 (macOS): May → "Mei"
    //          → "18 Mei 2025"
    [Fact]
    public void T2c_04_FormatDate_MalayLocaleProducesLocalisedMonth()
    {
        var options = new ReceiptOptions { Locale = "ms-MY", DateFormat = "dd MMM yyyy" };

        var result = DateTimeFormatter.FormatDate("2025-05-18T10:42:00", options);

        Assert.Equal("18 Mei 2025", result);
    }

    // T2c.5 — FormatTime with null Locale (invariant culture), tt specifier for AM/PM.
    //          isoDateTime="2025-05-18T10:42:00", Locale=null, TimeFormat="hh:mm tt"
    //          Invariant culture AM designator: "AM"
    //          → "10:42 AM"
    [Fact]
    public void T2c_05_FormatTime_InvariantLocaleProducesAmPmDesignator()
    {
        var options = new ReceiptOptions { Locale = null, TimeFormat = "hh:mm tt" };

        var result = DateTimeFormatter.FormatTime("2025-05-18T10:42:00", options);

        Assert.Equal("10:42 AM", result);
    }
}
