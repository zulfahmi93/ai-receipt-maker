using System.Collections.Frozen;
using System.Globalization;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Currency;
using ReceiptToolkit.Core.Globalization;

namespace ReceiptToolkit.Core.Formatting;

/// <summary>
///   Pure-function formatter that converts a <see cref="decimal"/> amount into a
///   localized money string for display on a receipt.
/// </summary>
/// <remarks>
///   <para>
///     Rounding uses <see cref="MidpointRounding.AwayFromZero"/> (round-half-up), which
///     matches the contract established by <c>ReceiptCalculator.CalculateTotals</c> and
///     the consumer-receipt norm (e.g. Japanese 四捨五入). Exact <c>.x5</c> midpoints
///     always round away from zero (e.g. 12.5 with 0 decimal places → 13).
///   </para>
///   <para>
///     Symbol resolution order: <see cref="ReceiptOptions.CurrencySymbol"/> when non-null
///     and non-empty; otherwise the built-in static table keyed on
///     <see cref="ReceiptOptions.Currency"/> (case-insensitive); otherwise an empty string.
///   </para>
///   <para>
///     Culture resolution: <see cref="CultureInfo.GetCultureInfo(string)"/> is attempted
///     when <see cref="ReceiptOptions.Locale"/> is non-null and non-empty; any
///     <see cref="CultureNotFoundException"/> or a null/empty locale falls back to
///     <see cref="CultureInfo.InvariantCulture"/>.
///   </para>
/// </remarks>
public static class MoneyFormatter
{
    private static readonly FrozenDictionary<string, string> BuiltInSymbols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = "$",
            ["EUR"] = "€",
            ["GBP"] = "£",
            ["JPY"] = "¥",
            ["CNY"] = "¥",
            ["MYR"] = "RM",
            ["SGD"] = "S$",
            ["IDR"] = "Rp",
            ["KRW"] = "₩",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///   Formats <paramref name="amount"/> as a money string using the display settings
    ///   from <paramref name="options"/>.
    /// </summary>
    /// <param name="amount">The monetary value to format. Kept as <see cref="decimal"/> throughout; no <see cref="double"/> conversion occurs.</param>
    /// <param name="options">Receipt display options supplying currency code, symbol override, and locale. Must not be <see langword="null"/>.</param>
    /// <returns>
    ///   A string of the form <c>{symbol}{number}</c> where <c>symbol</c> is resolved from
    ///   <paramref name="options"/> and <c>number</c> is formatted with the "N" specifier
    ///   plus the currency's decimal-place count, giving locale-aware digit grouping and
    ///   decimal separators.
    /// </returns>
    /// <remarks>
    ///   Decimal places come from <see cref="CurrencyTable.TryGet(string, out CurrencyInfo?)"/>
    ///   for <see cref="ReceiptOptions.Currency"/>, defaulting to <c>2</c> for unknown or
    ///   empty codes. Rounding is <see cref="MidpointRounding.AwayFromZero"/> — see class
    ///   remarks for rationale.
    /// </remarks>
    public static string Format(decimal amount, ReceiptOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var currencyCode = options.Currency ?? string.Empty;
        var decimalPlaces = CurrencyTable.TryGet(currencyCode, out var info)
            ? info!.DecimalPlaces
            : 2;

        var rounded = Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);
        var culture = CultureResolver.Resolve(options.Locale);
        var formatted = rounded.ToString("N" + decimalPlaces, culture);

        var symbol = ResolveSymbol(options, currencyCode);

        return symbol + formatted;
    }

    private static string ResolveSymbol(ReceiptOptions options, string currencyCode)
    {
        if (!string.IsNullOrEmpty(options.CurrencySymbol))
        {
            return options.CurrencySymbol;
        }

        return BuiltInSymbols.TryGetValue(currencyCode, out var builtIn)
            ? builtIn
            : string.Empty;
    }
}
