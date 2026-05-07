using System.Globalization;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Globalization;

namespace ReceiptToolkit.Core.Formatting;

/// <summary>
///   Pure-function formatter that parses an ISO-8601 datetime string and re-formats
///   the date and time components using the locale and format patterns from
///   <see cref="ReceiptOptions"/>.
/// </summary>
/// <remarks>
///   Input strings are parsed with <see cref="DateTimeStyles.RoundtripKind"/>
///   against <see cref="CultureInfo.InvariantCulture"/>, so UTC-suffixed (<c>Z</c>)
///   and offset-qualified (<c>+HH:mm</c>) strings preserve their <see cref="DateTimeKind"/>,
///   while bare local strings (no offset, e.g. <c>"2025-05-18T10:42:00"</c>) are treated
///   as <see cref="DateTimeKind.Unspecified"/>, which is appropriate for receipt display.
///   Culture resolution mirrors <c>MoneyFormatter</c>: non-null, non-empty
///   <see cref="ReceiptOptions.Locale"/> is passed to
///   <see cref="CultureInfo.GetCultureInfo(string)"/>; any <see cref="CultureNotFoundException"/>
///   or null/empty locale falls back to <see cref="CultureInfo.InvariantCulture"/>.
/// </remarks>
public static class DateTimeFormatter
{
    /// <summary>
    ///   Parses <paramref name="isoDateTime"/> and returns the date portion formatted
    ///   with <see cref="ReceiptOptions.DateFormat"/> and the locale derived from
    ///   <see cref="ReceiptOptions.Locale"/>.
    /// </summary>
    /// <param name="isoDateTime">
    ///   An ISO-8601 datetime string (e.g. <c>"2025-05-18T10:42:00"</c>,
    ///   <c>"2025-05-18T02:42:00Z"</c>). Must not be <see langword="null"/>.
    /// </param>
    /// <param name="options">
    ///   Receipt display options supplying <see cref="ReceiptOptions.DateFormat"/> and
    ///   <see cref="ReceiptOptions.Locale"/>. Must not be <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   The date formatted by <see cref="DateTime.ToString(string?, IFormatProvider)"/>
    ///   using <see cref="ReceiptOptions.DateFormat"/> as the format string and the
    ///   resolved culture as the format provider.
    /// </returns>
    public static string FormatDate(string isoDateTime, ReceiptOptions options)
    {
        ArgumentNullException.ThrowIfNull(isoDateTime);
        ArgumentNullException.ThrowIfNull(options);

        // RoundtripKind without AssumeLocal: bare strings (no offset) become DateTimeKind.Unspecified,
        // which is correct for display formatting. AssumeLocal would conflict with RoundtripKind and throw.
        var parsed = DateTime.Parse(
            isoDateTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        var culture = CultureResolver.Resolve(options.Locale);
        return parsed.ToString(options.DateFormat, culture);
    }

    /// <summary>
    ///   Parses <paramref name="isoDateTime"/> and returns the time portion formatted
    ///   with <see cref="ReceiptOptions.TimeFormat"/> and the locale derived from
    ///   <see cref="ReceiptOptions.Locale"/>.
    /// </summary>
    /// <param name="isoDateTime">
    ///   An ISO-8601 datetime string (e.g. <c>"2025-05-18T10:42:00"</c>,
    ///   <c>"2025-05-18T02:42:00Z"</c>). Must not be <see langword="null"/>.
    /// </param>
    /// <param name="options">
    ///   Receipt display options supplying <see cref="ReceiptOptions.TimeFormat"/> and
    ///   <see cref="ReceiptOptions.Locale"/>. Must not be <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   The time formatted by <see cref="DateTime.ToString(string?, IFormatProvider)"/>
    ///   using <see cref="ReceiptOptions.TimeFormat"/> as the format string and the
    ///   resolved culture as the format provider.
    /// </returns>
    public static string FormatTime(string isoDateTime, ReceiptOptions options)
    {
        ArgumentNullException.ThrowIfNull(isoDateTime);
        ArgumentNullException.ThrowIfNull(options);

        // Same parse style as FormatDate — see comment there for RoundtripKind rationale.
        var parsed = DateTime.Parse(
            isoDateTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        var culture = CultureResolver.Resolve(options.Locale);
        return parsed.ToString(options.TimeFormat, culture);
    }
}
