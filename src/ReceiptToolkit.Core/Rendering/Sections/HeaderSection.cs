using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the receipt header band: optional logo, business name, optional tagline.
/// </summary>
/// <remarks>
///   <para>
///     Layout (top to bottom): optional centered logo block, the business name (always
///     drawn, bold, centered), and an optional tagline (centered, muted weight).
///     A small inter-block gap separates each present block.
///   </para>
///   <para>
///     The logo is drawn iff <c>options.showLogo</c> is <see langword="true"/>,
///     <c>business.businessLogoUrl</c> is non-blank, and a non-null
///     <see cref="RenderContext.ResolvedLogo"/> is supplied. The section never resolves
///     URLs itself — that is <c>LogoResolver</c>'s responsibility upstream.
///   </para>
///   <para>
///     Layout numerics (logo size, font sizes, gap) are local to this section in
///     Phase 3b; Phase 3c may pull them from <see cref="ReceiptLayout"/>. User-visible
///     <em>strings</em> and <em>theme colours</em> always come from the model — the
///     "no hardcoded constants" rule applies to content and theme, not pure layout
///     spacing.
///   </para>
/// </remarks>
public sealed class HeaderSection : IReceiptSection
{
    private const float LogoSize = 56f;
    private const float LogoGap = 8f;
    private const float NameFontSize = 22f;
    private const float TaglineFontSize = 12f;
    private const float NameTaglineGap = 8f;
    private const string FontFamily = "Inter";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        float height = 0f;

        if (ShouldDrawLogo(data, ctx))
        {
            height += LogoSize + LogoGap;
        }

        // Business name is unconditional. Reserve a fixed line box derived from the
        // font size so absent glyph descenders don't squeeze the layout.
        height += NameFontSize;

        if (HasTagline(data))
        {
            height += NameTaglineGap + TaglineFontSize;
        }

        return height;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        float y = origin.Y;
        float centerX = origin.X + (width / 2f);

        if (ShouldDrawLogo(data, ctx))
        {
            SKImage logo = ctx.ResolvedLogo!;
            var dest = new SKRect(
                centerX - (LogoSize / 2f),
                y,
                centerX + (LogoSize / 2f),
                y + LogoSize);

            using var logoPaint = new SKPaint { IsAntialias = true };
            canvas.DrawImage(logo, dest, logoPaint);
            y += LogoSize + LogoGap;
        }

        // Business name — always drawn, centered.
        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);
        DrawCenteredLine(
            canvas,
            data.Business.BusinessName,
            boldFace,
            NameFontSize,
            textColor,
            centerX,
            y + NameFontSize);
        y += NameFontSize;

        if (HasTagline(data))
        {
            y += NameTaglineGap;
            SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);
            SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

            // Tagline right-anchors under the wordmark: right edge aligns with the
            // section right margin (origin.X + width). This matches the mockup where
            // the tagline reads flush-right regardless of business-name width.
            DrawRightAlignedLine(
                canvas,
                data.Business.BusinessTagline!,
                normalFace,
                TaglineFontSize,
                mutedColor,
                origin.X + width,
                y + TaglineFontSize);
        }
    }

    private static bool ShouldDrawLogo(ReceiptData data, RenderContext ctx) =>
        data.Options?.ShowLogo == true
        && !string.IsNullOrWhiteSpace(data.Business.BusinessLogoUrl)
        && ctx.ResolvedLogo is not null;

    private static bool HasTagline(ReceiptData data) =>
        !string.IsNullOrWhiteSpace(data.Business.BusinessTagline);

    private static void DrawCenteredLine(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float size,
        SKColor color,
        float centerX,
        float baselineY)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SKRect bounds = TextMeasurer.Measure(text, typeface, size);
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
        };

        float x = centerX - (bounds.Width / 2f) - bounds.Left;
        canvas.DrawText(text, x, baselineY, font, paint);
    }

    /// <summary>
    ///   Draws <paramref name="text"/> so its right ink edge aligns with
    ///   <paramref name="rightEdgeX"/> at the given baseline.
    /// </summary>
    private static void DrawRightAlignedLine(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float size,
        SKColor color,
        float rightEdgeX,
        float baselineY)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SKRect bounds = TextMeasurer.Measure(text, typeface, size);
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
        };

        // Right-align: position so that the glyph run ends exactly at rightEdgeX.
        // bounds.Left is the leftmost ink offset (usually negative or near-zero for LTR).
        float x = rightEdgeX - bounds.Width - bounds.Left;
        canvas.DrawText(text, x, baselineY, font, paint);
    }
}
