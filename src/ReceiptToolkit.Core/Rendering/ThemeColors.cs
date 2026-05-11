using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering;

/// <summary>
///   Internal helpers for resolving <see cref="ReceiptToolkit.Contracts.ReceiptTheme"/>
///   hex tokens into SkiaSharp colours, with documented neutral fallbacks.
/// </summary>
/// <remarks>
///   <para>
///     The fallback is taken <em>only</em> when the JSON value is <see langword="null"/>,
///     whitespace, or fails <see cref="SKColor.TryParse(string, out SKColor)"/>. It is
///     never layered over a successfully-parsed theme value — that would be a
///     "looked nicer in my IDE" choice and is forbidden by the rendering contract.
///   </para>
///   <para>
///     Per-role neutral defaults are exposed as named constants so every section uses
///     the same defensible neutral: <see cref="DefaultTextColor"/> (black) for primary
///     text and <see cref="DefaultMutedTextColor"/> (gray) for muted/secondary text.
///     A black fallback for muted text would collapse the visual distinction between
///     primary and secondary copy whenever <c>theme.mutedTextColor</c> is omitted.
///   </para>
/// </remarks>
internal static class ThemeColors
{
    /// <summary>Neutral fallback for primary body text when the theme value is missing or unparseable.</summary>
    public static readonly SKColor DefaultTextColor = SKColors.Black;

    /// <summary>Neutral fallback for muted/secondary text when the theme value is missing or unparseable.</summary>
    public static readonly SKColor DefaultMutedTextColor = SKColors.Gray;

    /// <summary>
    ///   Neutral fallback for the TOTAL bar / highlight band when the theme value is missing
    ///   or unparseable. <see cref="SKColors.LightGray"/> is chosen as a generic mid-tone that
    ///   stays distinct from typical light paper backgrounds without locking the renderer to
    ///   a specific theme accent.
    /// </summary>
    public static readonly SKColor DefaultHighlightColor = SKColors.LightGray;

    /// <summary>
    ///   Neutral fallback for accent-colored elements (QR modules, headings) when the theme
    ///   value is missing or unparseable. <see cref="SKColors.DarkSlateGray"/> provides a
    ///   mid-tone that reads clearly against both light paper backgrounds and white bitmap
    ///   surfaces without imposing a brand color.
    /// </summary>
    public static readonly SKColor DefaultAccentColor = SKColors.DarkSlateGray;

    /// <summary>
    ///   Neutral fallback for divider lines and perforation strokes when the theme value is
    ///   missing or unparseable. <see cref="SKColors.LightGray"/> is a light neutral that
    ///   renders a visible but non-intrusive separator on light paper backgrounds.
    /// </summary>
    public static readonly SKColor DefaultDividerColor = SKColors.LightGray;

    /// <summary>
    ///   Neutral fallback for the paper background when the theme value is missing or
    ///   unparseable. <see cref="SKColors.White"/> matches a plain thermal-receipt surface
    ///   and keeps black/gray text legible without imposing a brand tint.
    /// </summary>
    public static readonly SKColor DefaultPaperColor = SKColors.White;

    /// <summary>
    ///   Default color for uppercase muted cell labels (e.g. PaymentSection 2x2 grid).
    ///   Hex #8A8A8A is a mid-gray that reads against light paper backgrounds while
    ///   remaining visually subordinate to body-weight values. // 3c-polish D
    /// </summary>
    public static readonly SKColor DefaultMutedLabelColor = new SKColor(0x8A, 0x8A, 0x8A);

    /// <summary>
    ///   Returns the parsed <see cref="SKColor"/> from <paramref name="hex"/>, or
    ///   <paramref name="fallback"/> when <paramref name="hex"/> is null, whitespace, or
    ///   does not satisfy <see cref="SKColor.TryParse(string, out SKColor)"/>.
    /// </summary>
    public static SKColor ResolveOrDefault(string? hex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        return SKColor.TryParse(hex, out SKColor parsed) ? parsed : fallback;
    }
}
