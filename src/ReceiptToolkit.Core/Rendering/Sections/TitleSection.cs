using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the receipt title band: a single centered line driven by
///   <see cref="ReceiptMetadata.ReceiptTitle"/>.
/// </summary>
/// <remarks>
///   When <see cref="ReceiptMetadata.ReceiptTitle"/> is <see langword="null"/> or
///   whitespace, this section is omitted: <see cref="Measure"/> returns <c>0f</c> and
///   <see cref="Draw"/> performs no canvas operations (per
///   <see cref="IReceiptSection"/> omitted-section contract).
/// </remarks>
public sealed class TitleSection : IReceiptSection
{
    private const float FontSize = 16f;
    private const string FontFamily = "Inter";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        return string.IsNullOrWhiteSpace(data.Receipt.ReceiptTitle) ? 0f : FontSize;
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
        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);
        SKRect bounds = TextMeasurer.Measure(title, boldFace, FontSize);

        using var font = new SKFont(boldFace, FontSize);
        using var paint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
        };

        float centerX = origin.X + (width / 2f);
        float x = centerX - (bounds.Width / 2f) - bounds.Left;
        float baselineY = origin.Y + FontSize;

        canvas.DrawText(title, x, baselineY, font, paint);
    }
}
