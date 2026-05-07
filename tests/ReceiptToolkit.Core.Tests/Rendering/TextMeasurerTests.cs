// Purpose: RED-phase tests for Phase 3 (T3.7–T3.8) — TextMeasurer bounds and word-wrap.
// Categories: Unit — in-process SkiaSharp text measurement; tests that Measure returns
//             non-zero bounds for non-empty text, returns SKRect.Empty for empty input,
//             and that WrapLines performs greedy word-wrap with no character loss.
// Edge cases: empty string → empty bounds / empty list; long sentence → ≥2 lines;
//             word-join round-trip equality (no characters dropped by wrap algorithm).

using SkiaSharp;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Layout;

namespace ReceiptToolkit.Core.Tests.Rendering;

public sealed class TextMeasurerTests
{
    // T3.7 — Measure("hello", ...) returns bounds with positive Width and Height.
    //         Measure("", ...) returns SKRect.Empty (no text → no bounds).
    [Fact]
    public void TextMeasurer_NonEmpty_ReturnsBounds()
    {
        using var fp = new FontProvider();
        SKTypeface inter = fp.GetTypeface("Inter", SKFontStyleWeight.Normal);

        SKRect nonEmpty = TextMeasurer.Measure("hello", inter, 12f);
        SKRect empty = TextMeasurer.Measure("", inter, 12f);

        Assert.True(nonEmpty.Width > 0, "Expected Width > 0 for non-empty text");
        Assert.True(nonEmpty.Height > 0, "Expected Height > 0 for non-empty text");
        Assert.Equal(SKRect.Empty, empty);
    }

    // T3.8 — WrapLines produces at least 2 lines for a sentence that exceeds 40px at 12pt,
    //         and the words can be re-joined (space-separated) to recover the original text
    //         (no characters are lost or duplicated by the greedy wrap algorithm).
    [Fact]
    public void TextMeasurer_WrapLines_BreaksLongInput()
    {
        using var fp = new FontProvider();
        SKTypeface inter = fp.GetTypeface("Inter", SKFontStyleWeight.Normal);
        const string input = "a long sentence that must wrap at least once";

        System.Collections.Generic.IReadOnlyList<string> lines =
            TextMeasurer.WrapLines(input, 40f, inter, 12f);

        Assert.True(lines.Count >= 2, $"Expected at least 2 lines, got {lines.Count}");
        Assert.Equal(input.Trim(), string.Join(" ", lines).Trim());
    }
}
