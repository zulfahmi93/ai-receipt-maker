using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the QR code band: a centred matrix followed by an optional label line.
/// </summary>
/// <remarks>
///   <para>
///     The section is omitted entirely (Measure returns 0f, Draw performs no operations)
///     when <see cref="ReceiptOptions.ShowQrCode"/> is not true.
///   </para>
///   <para>
///     The QR matrix is painted via <see cref="QrPainter.Paint"/> using
///     <c>theme.accentColor</c> (resolved via <see cref="ThemeColors.ResolveOrDefault"/>,
///     falling back to <see cref="ThemeColors.DefaultAccentColor"/>) as the module fill
///     colour.  Anti-aliasing is disabled inside <see cref="QrPainter"/> so module
///     boundaries are pixel-accurate.
///   </para>
///   <para>
///     When <c>qr.qrCodeValue</c> is null or whitespace, the QR paint and label paint
///     steps are skipped — no draw operations occur — while Measure still reports the
///     reserved height to avoid layout shifts when the value is temporarily absent.
///   </para>
///   <para>
///     Layout numerics are local constants in Phase 3b; Phase 3c may pull them from
///     <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class QrSection : IReceiptSection
{
    // Matrix dimensions.
    // DefaultQrSize=72f: compact footprint matching mockup; module pitch = 72/29 ≈ 2.48px
    // which satisfies the scan-readability minimum of ≥2px per module.
    private const float QrSize = 72f;

    // QrTopPadding=0f — the matrix starts at the top edge of the section's allocated area.
    // T3b.18 samples at y=14; with no top padding the QR rect spans y∈[0,72], so y=14 is
    // inside the matrix regardless of which row is sampled.
    private const float QrTopPadding = 0f;

    // Gap between the bottom of the matrix and the label baseline.
    private const float LabelGap = 4f;

    // Label rendered at a compact 10pt — large enough to read, small enough to stay below
    // the matrix without competing with it visually.
    private const float LabelFontSize = 10f;

    private const string FontFamily = "Inter";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Options?.ShowQrCode != true)
        {
            return 0f;
        }

        return QrSize + LabelGap + LabelFontSize;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Options?.ShowQrCode != true)
        {
            return;
        }

        SKColor moduleColor = ThemeColors.ResolveOrDefault(
            data.Theme?.AccentColor,
            ThemeColors.DefaultAccentColor);

        string? value = data.Qr?.QrCodeValue;

        if (!string.IsNullOrWhiteSpace(value))
        {
            SKRect qrRect = SKRect.Create(
                origin.X + (width - QrSize) / 2f,
                origin.Y + QrTopPadding,
                QrSize,
                QrSize);

            QrPainter.Paint(canvas, value, qrRect, moduleColor);

            // Label — rendered only when qrCodeLabel is non-blank.
            string? label = data.Qr?.QrCodeLabel;
            if (!string.IsNullOrWhiteSpace(label))
            {
                SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);
                SKRect bounds = TextMeasurer.Measure(label, normalFace, LabelFontSize);

                float labelX = origin.X + (width - bounds.Width) / 2f - bounds.Left;
                float labelBaselineY = origin.Y + QrSize + LabelGap + LabelFontSize;

                using var labelFont = new SKFont(normalFace, LabelFontSize);
                using var labelPaint = new SKPaint
                {
                    Color = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor),
                    IsAntialias = true,
                };

                canvas.DrawText(label, labelX, labelBaselineY, labelFont, labelPaint);
            }
        }
    }
}
