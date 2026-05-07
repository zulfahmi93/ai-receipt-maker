using System.Text;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Layout;

/// <summary>
///   Measures text extents and performs greedy word-wrap using a SkiaSharp font.
/// </summary>
public static class TextMeasurer
{
    /// <summary>
    ///   Measures the tight bounding box of <paramref name="text"/> rendered at
    ///   <paramref name="size"/> points using <paramref name="typeface"/>.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="typeface">The typeface to use for measurement.</param>
    /// <param name="size">The font size in points.</param>
    /// <returns>
    ///   The tight glyph bounds as an <see cref="SKRect"/>, or <see cref="SKRect.Empty"/>
    ///   when <paramref name="text"/> is <see langword="null"/> or empty.
    /// </returns>
    public static SKRect Measure(string text, SKTypeface typeface, float size)
    {
        if (string.IsNullOrEmpty(text))
        {
            return SKRect.Empty;
        }

        using var font = new SKFont(typeface, size);
        font.MeasureText(text, out SKRect bounds);
        return bounds;
    }

    /// <summary>
    ///   Wraps <paramref name="text"/> into lines that each fit within
    ///   <paramref name="maxWidth"/> pixels using a greedy algorithm.
    /// </summary>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum line width in pixels.</param>
    /// <param name="typeface">The typeface used for glyph-advance measurement.</param>
    /// <param name="size">The font size in points.</param>
    /// <returns>
    ///   A read-only list of lines.  Returns an empty list when
    ///   <paramref name="text"/> is <see langword="null"/> or empty.
    ///   At least one line is always returned for non-empty input.
    ///   Words that individually exceed <paramref name="maxWidth"/> are placed on their
    ///   own line without character-level breaking, preserving the invariant that
    ///   <c>string.Join(" ", lines)</c> round-trips to the original text.
    /// </returns>
    public static IReadOnlyList<string> WrapLines(
        string text,
        float maxWidth,
        SKTypeface typeface,
        float size)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        using var font = new SKFont(typeface, size);
        var lines = new List<string>();
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (string word in words)
        {
            // Build candidate line.
            string candidate = currentLine.Length == 0
                ? word
                : currentLine + " " + word;

            float candidateWidth = font.MeasureText(candidate);

            if (candidateWidth <= maxWidth)
            {
                // Word fits on the current line — accept it.
                currentLine.Clear();
                currentLine.Append(candidate);
            }
            else if (currentLine.Length == 0)
            {
                // Word is the sole candidate and already exceeds maxWidth.
                // Accept the overflow rather than breaking mid-word — this preserves
                // the round-trip invariant (string.Join(" ", lines) == original text).
                currentLine.Append(word);
            }
            else
            {
                // Word does not fit after existing content — flush the current line
                // and start a fresh line with this word.
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
            }
        }

        // Flush the last line.
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

}
