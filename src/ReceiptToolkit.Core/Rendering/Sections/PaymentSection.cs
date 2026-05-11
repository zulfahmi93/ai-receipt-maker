using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the payment tender band as a fixed 2x2 grid (T3cP.8 — Phase 3c-polish D).
/// </summary>
/// <remarks>
///   <para>
///     Grid layout (first payment entry):
///     <code>
///       Row 1: [PAYMENT METHOD  value]   [AMOUNT PAID  value]
///       Row 2: [CARD ENDING     value]   [AUTH CODE    value]
///     </code>
///     Cell labels are uppercase muted (via <see cref="ThemeColors.DefaultMutedLabelColor"/>);
///     values use Normal body weight at body text color.
///   </para>
///   <para>
///     Grid height is fixed at exactly two cell-rows regardless of value caption length.
///     Long values wrap within the cell via <see cref="TextMeasurer.WrapLines"/> but the
///     Measure return does not grow — grid height depends only on row count.
///   </para>
///   <para>
///     Icon column: <c>PaymentInfo</c> has no <c>Icon</c> field in the current contract.
///     A small left-pad rectangle placeholder is reserved for a future icon column.
///     <!-- icon-column-deferred -->
///   </para>
///   <para>
///     An empty <see cref="ReceiptData.Payments"/> collection causes <see cref="Measure"/>
///     to return <c>0f</c> and <see cref="Draw"/> to perform no operations, per the
///     omission contract.
///   </para>
///   <para>
///     Money strings are produced by <see cref="MoneyFormatter.Format"/>. Section code
///     never rounds or parses amounts.
///   </para>
/// </remarks>
public sealed class PaymentSection : IReceiptSection
{
    // Grid cell metrics.
    private const float CellRowHeight = 32f;
    private const float LabelFontSize = 9f;
    private const float ValueFontSize = 11f;
    private const float CellPadRight = 8f;
    private const float LabelValueGap = 2f;
    private const float RowGap = 6f;

    // Icon placeholder reserved on far left; icon-column-deferred.
    private const float IconPlaceholderWidth = 20f;
    private const float IconPlaceholderHeight = 14f;

    private const string FontFamily = "Inter";

    // Grid cell labels (uppercase muted).
    private const string LabelPaymentMethod = "PAYMENT METHOD";
    private const string LabelAmountPaid = "AMOUNT PAID";
    private const string LabelCardEnding = "CARD ENDING";
    private const string LabelAuthCode = "AUTH CODE";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Payments.Count == 0)
        {
            return 0f;
        }

        // Fixed 2-row grid height regardless of caption length.
        return (CellRowHeight * 2f) + RowGap;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Payments.Count == 0)
        {
            return;
        }

        PaymentInfo payment = data.Payments[0];
        ReceiptOptions options = data.Options ?? new ReceiptOptions();

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);

        SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        // Draw icon placeholder (reserved for future icon column).
        float iconTop = origin.Y + ((CellRowHeight - IconPlaceholderHeight) / 2f);
        using (var iconPaint = new SKPaint
        {
            Color = ThemeColors.ResolveOrDefault(
                data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor).WithAlpha(60),
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        })
        {
            canvas.DrawRect(
                new SKRect(
                    origin.X,
                    iconTop,
                    origin.X + IconPlaceholderWidth - 4f,
                    iconTop + IconPlaceholderHeight),
                iconPaint);
        }

        float contentLeft = origin.X + IconPlaceholderWidth;
        float contentWidth = width - IconPlaceholderWidth;
        float cellWidth = contentWidth / 2f;

        float row1Top = origin.Y;
        float row2Top = origin.Y + CellRowHeight + RowGap;

        string methodValue = payment.Method ?? string.Empty;
        string amountValue = MoneyFormatter.Format(payment.Amount, options);
        string cardEndingValue = payment.CardLastFour ?? string.Empty;
        string authCodeValue = payment.AuthCode ?? string.Empty;

        DrawCell(canvas, contentLeft, row1Top, cellWidth,
            LabelPaymentMethod, methodValue, normalFace,
            ThemeColors.DefaultMutedLabelColor, textColor);

        DrawCell(canvas, contentLeft + cellWidth, row1Top, cellWidth,
            LabelAmountPaid, amountValue, normalFace,
            ThemeColors.DefaultMutedLabelColor, textColor);

        DrawCell(canvas, contentLeft, row2Top, cellWidth,
            LabelCardEnding, cardEndingValue, normalFace,
            ThemeColors.DefaultMutedLabelColor, textColor);

        DrawCell(canvas, contentLeft + cellWidth, row2Top, cellWidth,
            LabelAuthCode, authCodeValue, normalFace,
            ThemeColors.DefaultMutedLabelColor, textColor);
    }

    private static void DrawCell(
        SKCanvas canvas,
        float cellLeft,
        float cellTop,
        float cellWidth,
        string label,
        string value,
        SKTypeface face,
        SKColor labelColor,
        SKColor valueColor)
    {
        float availableWidth = cellWidth - CellPadRight;

        using var labelFont = new SKFont(face, LabelFontSize);
        using var valueFont = new SKFont(face, ValueFontSize);
        using var labelPaint = new SKPaint { Color = labelColor, IsAntialias = true };
        using var valuePaint = new SKPaint { Color = valueColor, IsAntialias = true };

        // Label line — uppercase muted.
        float labelBaselineY = cellTop + LabelFontSize;
        canvas.DrawText(label, cellLeft, labelBaselineY, labelFont, labelPaint);

        // Value lines — wrap within cell width.
        IReadOnlyList<string> valueLines = TextMeasurer.WrapLines(
            value, availableWidth, face, ValueFontSize);

        float remainingHeight = CellRowHeight - LabelFontSize - LabelValueGap;
        float lineY = labelBaselineY + LabelValueGap + ValueFontSize;

        foreach (string line in valueLines)
        {
            float usedHeight = lineY - labelBaselineY - LabelValueGap;
            if (usedHeight > remainingHeight)
            {
                break;
            }

            canvas.DrawText(line, cellLeft, lineY, valueFont, valuePaint);
            lineY += ValueFontSize + LabelValueGap;
        }
    }
}
