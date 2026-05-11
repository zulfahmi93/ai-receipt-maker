using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the receipt title band: a single centered line driven by
///   <see cref="ReceiptMetadata.ReceiptTitle"/>, framed by horizontal rule lines above
///   and below and rendered with per-glyph letter-spacing (tracking).
/// </summary>
/// <remarks>
///   <para>
///     When <see cref="ReceiptMetadata.ReceiptTitle"/> is <see langword="null"/> or
///     whitespace, this section is omitted: <see cref="Measure"/> returns <c>0f</c> and
///     <see cref="Draw"/> performs no canvas operations (per
///     <see cref="IReceiptSection"/> omitted-section contract).
///   </para>
///   <para>
///     Layout (top to bottom):
///     <c>RulePadding</c> + rule stroke + <c>RulePadding</c> + FontSize text row +
///     <c>RulePadding</c> + rule stroke + <c>RulePadding</c>.
///     Total height = 2 × (2 × RulePadding + RuleStrokeWidth) + FontSize = 42f.
///   </para>
///   <para>
///     Letter-spacing is implemented glyph-by-glyph because SkiaSharp's
///     <see cref="SKFont"/> has no built-in tracking property.  Each character is drawn
///     individually at a cumulative x-advance of <c>glyphWidth + Tracking</c>, where
///     <c>Tracking = <see cref="TrackingEm"/> × <see cref="FontSize"/></c>
///     (currently 0.15f × 16f = 2.4 px per inter-glyph gap, matching the mockup
///     wordmark spacing).  The full tracked run is centered by pre-computing total width.
///   </para>
///   <para>
///     Rule stroke colour is resolved from <c>theme.dividerColor</c> via
///     <see cref="ThemeColors.ResolveOrDefault"/> with
///     <see cref="ThemeColors.DefaultDividerColor"/> as fallback.
///   </para>
/// </remarks>
public sealed class TitleSection : IReceiptSection
{
    private const float FontSize = 16f;
    private const string FontFamily = "Inter";

    /// <summary>
    ///   Tracking per inter-glyph gap expressed as an em fraction.
    ///   0.15 em at 16 pt = 2.4 px, producing the expanded wordmark spacing visible
    ///   in <c>mockups/receipt.png</c>.
    /// </summary>
    private const float TrackingEm = 0.15f;

    /// <summary>Absolute tracking in pixels at <see cref="FontSize"/>.</summary>
    private const float Tracking = TrackingEm * FontSize; // 2.4 px per gap

    /// <summary>Padding above the upper rule and below the lower rule, in pixels.</summary>
    private const float RulePadding = 6f;

    /// <summary>Stroke width of the horizontal rule lines, in pixels.</summary>
    private const float RuleStrokeWidth = 1f;

    /// <summary>
    ///   Pre-computed total section height when the title is present.
    ///   Formula: top-padding + rule + inner-padding + FontSize + inner-padding + rule + bottom-padding.
    ///   = 2 × (2 × RulePadding + RuleStrokeWidth) + FontSize = 42f.
    /// </summary>
    private const float FullHeight =
        RulePadding + RuleStrokeWidth + RulePadding + FontSize + RulePadding + RuleStrokeWidth + RulePadding;

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        return string.IsNullOrWhiteSpace(data.Receipt.ReceiptTitle) ? 0f : FullHeight;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        string? title = data.Receipt.ReceiptTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor dividerColor = ThemeColors.ResolveOrDefault(data.Theme?.DividerColor, ThemeColors.DefaultDividerColor);
        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);

        using var rulePaint = new SKPaint
        {
            Color = dividerColor,
            StrokeWidth = RuleStrokeWidth,
            IsStroke = true,
            IsAntialias = false, // crisp 1 px lines
        };

        // --- Rule above title ---
        float ruleAboveY = origin.Y + RulePadding;
        canvas.DrawLine(origin.X, ruleAboveY, origin.X + width, ruleAboveY, rulePaint);

        // --- Title text (glyph-by-glyph tracking) ---
        float baselineY = origin.Y + RulePadding + RuleStrokeWidth + RulePadding + FontSize;
        DrawTrackedCenteredLine(canvas, title, boldFace, FontSize, textColor, origin.X, width, baselineY);

        // --- Rule below title ---
        float ruleBelowY = origin.Y + RulePadding + RuleStrokeWidth + RulePadding + FontSize + RulePadding;
        canvas.DrawLine(origin.X, ruleBelowY, origin.X + width, ruleBelowY, rulePaint);
    }

    /// <summary>
    ///   Draws <paramref name="text"/> centered within the section width using per-glyph
    ///   letter-spacing (<see cref="Tracking"/> px per inter-glyph gap).
    /// </summary>
    /// <remarks>
    ///   Each glyph is measured individually via <see cref="TextMeasurer.Measure"/> and
    ///   drawn at a cumulative x-advance offset.  The total tracked width is computed in a
    ///   first pass so the run can be accurately centered.  This avoids calling
    ///   <c>SKFont.MeasureText</c> on the full string (which does not account for tracking)
    ///   and avoids canvas-level <c>Scale</c> hacks that would distort glyph shapes.
    /// </remarks>
    private static void DrawTrackedCenteredLine(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float size,
        SKColor color,
        float originX,
        float sectionWidth,
        float baselineY)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
        };

        // First pass: compute total tracked width.
        // Each glyph contributes its measured width; inter-glyph gaps add Tracking px.
        // The last character has no trailing gap.
        float totalWidth = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            SKRect glyphBounds = TextMeasurer.Measure(text[i].ToString(), typeface, size);
            totalWidth += glyphBounds.Width;
            if (i < text.Length - 1)
            {
                totalWidth += Tracking;
            }
        }

        // Center the run within [originX .. originX + sectionWidth].
        float startX = originX + ((sectionWidth - totalWidth) / 2f);
        float curX = startX;

        // Second pass: draw each glyph.
        for (int i = 0; i < text.Length; i++)
        {
            string glyph = text[i].ToString();
            SKRect glyphBounds = TextMeasurer.Measure(glyph, typeface, size);

            // DrawText baseline x is adjusted for bounds.Left (leftmost ink offset).
            float drawX = curX - glyphBounds.Left;
            canvas.DrawText(glyph, drawX, baselineY, font, paint);

            curX += glyphBounds.Width;
            if (i < text.Length - 1)
            {
                curX += Tracking;
            }
        }
    }
}
