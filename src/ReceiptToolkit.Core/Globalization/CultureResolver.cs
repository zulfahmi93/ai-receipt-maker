using System.Globalization;

namespace ReceiptToolkit.Core.Globalization;

/// <summary>
///   Shared helper that resolves a <see cref="CultureInfo"/> from an IETF locale tag,
///   falling back to <see cref="CultureInfo.InvariantCulture"/> on failure.
/// </summary>
/// <remarks>
///   Used by <c>MoneyFormatter</c> and <c>DateTimeFormatter</c> so that locale-resolution
///   logic lives in exactly one place.
/// </remarks>
internal static class CultureResolver
{
    /// <summary>
    ///   Returns the <see cref="CultureInfo"/> for <paramref name="locale"/> when it is a
    ///   recognised IETF tag, or <see cref="CultureInfo.InvariantCulture"/> when
    ///   <paramref name="locale"/> is <see langword="null"/>, empty, or unrecognised.
    /// </summary>
    /// <param name="locale">
    ///   An IETF language tag such as <c>"en-US"</c>, <c>"ja-JP"</c>, or <c>"ms-MY"</c>;
    ///   <see langword="null"/> and empty strings are silently treated as invariant.
    /// </param>
    /// <returns>
    ///   A <see cref="CultureInfo"/> instance — never <see langword="null"/>.
    /// </returns>
    internal static CultureInfo Resolve(string? locale)
    {
        if (string.IsNullOrEmpty(locale))
        {
            return CultureInfo.InvariantCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
