namespace ReceiptToolkit.Core.Currency;

/// <summary>Describes a supported currency with its ISO 4217 code and decimal precision.</summary>
/// <param name="Code">The ISO 4217 three-letter currency code (e.g. <c>USD</c>).</param>
/// <param name="DecimalPlaces">Number of minor-unit decimal places used by this currency (e.g. 2 for USD, 0 for JPY).</param>
public sealed record CurrencyInfo(string Code, int DecimalPlaces);

/// <summary>Static lookup table of supported ISO 4217 currency codes.</summary>
public static class CurrencyTable
{
    /// <summary>All supported currencies keyed by their ISO 4217 code (case-insensitive).</summary>
    public static IReadOnlyDictionary<string, CurrencyInfo> All { get; } =
        new Dictionary<string, CurrencyInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = new CurrencyInfo("USD", 2),
            ["EUR"] = new CurrencyInfo("EUR", 2),
            ["GBP"] = new CurrencyInfo("GBP", 2),
            ["JPY"] = new CurrencyInfo("JPY", 0),
            ["IDR"] = new CurrencyInfo("IDR", 0),
            ["MYR"] = new CurrencyInfo("MYR", 2),
            ["SGD"] = new CurrencyInfo("SGD", 2),
            ["AUD"] = new CurrencyInfo("AUD", 2),
            ["CAD"] = new CurrencyInfo("CAD", 2),
            ["CNY"] = new CurrencyInfo("CNY", 2),
            ["HKD"] = new CurrencyInfo("HKD", 2),
            ["INR"] = new CurrencyInfo("INR", 2),
            ["KRW"] = new CurrencyInfo("KRW", 0),
            ["THB"] = new CurrencyInfo("THB", 2),
            ["VND"] = new CurrencyInfo("VND", 0),
            ["NZD"] = new CurrencyInfo("NZD", 2),
            ["CHF"] = new CurrencyInfo("CHF", 2),
        };

    /// <summary>Attempts to retrieve currency information for the given ISO 4217 code.</summary>
    /// <param name="code">ISO 4217 currency code to look up. Lookup is case-insensitive.</param>
    /// <param name="info">When this method returns <see langword="true"/>, contains the matching <see cref="CurrencyInfo"/>; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a supported currency; otherwise <see langword="false"/>.</returns>
    public static bool TryGet(string code, out CurrencyInfo? info)
    {
        if (All.TryGetValue(code, out var found))
        {
            info = found;
            return true;
        }

        info = null;
        return false;
    }
}
