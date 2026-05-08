using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the payment tender band: one compact block per <see cref="PaymentInfo"/>
///   in <see cref="ReceiptData.Payments"/>.
/// </summary>
/// <remarks>
///   <para>
///     Each payment block contains:
///     <list type="bullet">
///       <item><description>
///         A header line with the payment method name (Bold, left) and the amount
///         (Bold, right-aligned).
///       </description></item>
///       <item><description>
///         Up to three optional sub-lines printed verbatim (muted, smaller font):
///         <see cref="PaymentInfo.CardLastFour"/>, <see cref="PaymentInfo.AuthCode"/>,
///         and <see cref="PaymentInfo.TransactionId"/>, each on a separate sub-line when
///         non-blank.  No decorative prefixes are added — the raw values are printed
///         as-is so test assertions can match them without prefix knowledge.
///       </description></item>
///     </list>
///   </para>
///   <para>
///     An empty <see cref="ReceiptData.Payments"/> collection causes <see cref="Measure"/>
///     to return <c>0f</c> and <see cref="Draw"/> to perform no operations, per the
///     <see cref="IReceiptSection"/> omission contract.
///   </para>
///   <para>
///     Money strings are produced by <see cref="MoneyFormatter.Format"/>. Section code
///     never rounds or parses amounts.
///   </para>
///   <para>
///     Layout numerics (row height, font sizes, block gap) are local constants in Phase 3b.
///     Phase 3c may pull them from <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class PaymentSection : IReceiptSection
{
    // Row metrics.
    private const float RowHeight = 16f;
    private const float SubLineGap = 2f;
    private const float HeaderFontSize = 12f;
    private const float SubLineFontSize = 10f;
    private const float BlockGap = 8f;   // Vertical gap between multiple payment blocks.

    private const string FontFamily = "Inter";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Payments.Count == 0)
        {
            return 0f;
        }

        float height = 0f;

        for (int i = 0; i < data.Payments.Count; i++)
        {
            if (i > 0)
            {
                height += BlockGap;
            }

            height += MeasureBlock(data.Payments[i]);
        }

        return height;
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

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);

        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);
        SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        ReceiptOptions options = data.Options ?? new ReceiptOptions();

        float y = origin.Y;

        for (int i = 0; i < data.Payments.Count; i++)
        {
            if (i > 0)
            {
                y += BlockGap;
            }

            DrawBlock(canvas, origin.X, y, width, data.Payments[i], options, boldFace, normalFace, textColor, mutedColor);
            y += MeasureBlock(data.Payments[i]);
        }
    }

    // Returns the height consumed by a single payment block.
    private static float MeasureBlock(PaymentInfo payment)
    {
        float height = RowHeight; // Header line (method + amount).

        // One sub-line per non-blank metadata field.
        List<string> subLines = MaterializeSubLines(payment);
        foreach (string _ in subLines)
        {
            height += SubLineGap + RowHeight;
        }

        return height;
    }

    // Builds the list of sub-line strings for a single payment.
    // Return type is List<string> to satisfy CA1859 — do not widen to IReadOnlyList<string>.
    private static List<string> MaterializeSubLines(PaymentInfo payment)
    {
        var lines = new List<string>(capacity: 3);

        if (!string.IsNullOrWhiteSpace(payment.CardLastFour))
        {
            lines.Add(payment.CardLastFour!);
        }

        if (!string.IsNullOrWhiteSpace(payment.AuthCode))
        {
            lines.Add(payment.AuthCode!);
        }

        if (!string.IsNullOrWhiteSpace(payment.TransactionId))
        {
            lines.Add(payment.TransactionId!);
        }

        return lines;
    }

    private static void DrawBlock(
        SKCanvas canvas,
        float originX,
        float topY,
        float width,
        PaymentInfo payment,
        ReceiptOptions options,
        SKTypeface boldFace,
        SKTypeface normalFace,
        SKColor textColor,
        SKColor mutedColor)
    {
        float headerBaselineY = topY + HeaderFontSize;

        using var boldFont = new SKFont(boldFace, HeaderFontSize);
        using var subFont = new SKFont(normalFace, SubLineFontSize);
        using var textPaint = new SKPaint { Color = textColor, IsAntialias = true };
        using var mutedPaint = new SKPaint { Color = mutedColor, IsAntialias = true };

        // Header line — method name (left) + amount (right).
        string methodText = payment.Method ?? string.Empty;
        if (!string.IsNullOrEmpty(methodText))
        {
            canvas.DrawText(methodText, originX, headerBaselineY, boldFont, textPaint);
        }

        string amountStr = MoneyFormatter.Format(payment.Amount, options);
        SKRect amountBounds = TextMeasurer.Measure(amountStr, boldFace, HeaderFontSize);
        float amountX = originX + width - amountBounds.Width - amountBounds.Left;
        canvas.DrawText(amountStr, amountX, headerBaselineY, boldFont, textPaint);

        // Sub-lines — one per non-blank metadata field, drawn in muted color.
        List<string> subLines = MaterializeSubLines(payment);
        float y = topY + RowHeight;

        foreach (string line in subLines)
        {
            float subBaselineY = y + SubLineGap + SubLineFontSize;
            canvas.DrawText(line, originX, subBaselineY, subFont, mutedPaint);
            y += SubLineGap + RowHeight;
        }
    }
}
