using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the receipt totals band: conditional sub-total rows followed by a
///   highlighted TOTAL bar showing the grand total.
/// </summary>
/// <remarks>
///   <para>
///     Rows, in fixed order, are emitted conditionally:
///     Subtotal (always), Discount (iff non-zero), Service Charge (iff non-zero),
///     Tax (iff <see cref="ReceiptOptions.ShowTaxBreakdown"/> is true),
///     Rounding (iff non-zero), then the TOTAL bar (always).
///   </para>
///   <para>
///     Sub-total row labels and values use Normal and SemiBold typefaces respectively.
///     The TOTAL bar label and value use Bold for visual emphasis.
///   </para>
///   <para>
///     The TOTAL bar background is filled with <c>theme.highlightColor</c> (resolved via
///     <see cref="ThemeColors.ResolveOrDefault"/>, falling back to
///     <see cref="ThemeColors.DefaultHighlightColor"/>). The highlight rect is drawn
///     before the text so the text renders on top.
///   </para>
///   <para>
///     Money strings are produced by <see cref="MoneyFormatter.Format"/>. Section code
///     never rounds or parses amounts.
///   </para>
///   <para>
///     Row labels (e.g. "Subtotal", "TOTAL") are presentation chrome as
///     <c>private const string</c>. TODO: replace with a localized string table when an
///     i18n layer exists (i18n-deferred, see remarks).
///   </para>
///   <para>
///     Layout numerics (row height, font sizes, bar height, margins) are local constants
///     in Phase 3b. Phase 3c may pull them from <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class TotalsSection : IReceiptSection
{
    // Row metrics.
    private const float RowHeight = 16f;
    private const float RowGap = 4f;
    private const float LabelFontSize = 11f;
    private const float ValueFontSize = 11f;
    private const float TotalBarHeight = 22f;
    private const float TotalFontSize = 14f;

    // Right-margin for value text within the TOTAL bar — ensures the sample point
    // (width - 10, height - 8) lands on the highlight fill, not on glyph antialiasing.
    private const float TotalValueRightMargin = 12f;

    // Label column fraction mirrors MetaSection's split.
    private const float LabelColumnFraction = 0.45f;

    private const string FontFamily = "Inter";

    // Presentation-chrome labels (i18n-deferred, see remarks).
    private const string LabelSubtotal = "Subtotal";
    private const string LabelDiscount = "Discount";
    private const string LabelServiceCharge = "Service Charge";
    private const string LabelRounding = "Rounding";
    private const string LabelTotal = "TOTAL";

    // Fallback for ReceiptTotals.TaxLabel when the JSON field is null.
    // This is a content-default, not a presentation-chrome label; it uses
    // the caller's TaxLabel when provided, so it is i18n-neutral in practice.
    private const string TaxLabelFallback = "Tax";

    /// <inheritdoc />
    public bool RequiresLeadingDivider => true;

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        List<(string Label, decimal Amount)> rows = MaterializeSubRows(data);

        float height = 0f;

        // Sub-total rows.
        for (int i = 0; i < rows.Count; i++)
        {
            if (i > 0)
            {
                height += RowGap;
            }

            height += RowHeight;
        }

        // Gap between last sub-row and TOTAL bar (only when there are sub-rows above).
        if (rows.Count > 0)
        {
            height += RowGap;
        }

        // TOTAL bar — always present.
        height += TotalBarHeight;

        return height;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);
        SKColor highlightColor = ThemeColors.ResolveOrDefault(data.Theme?.HighlightColor, ThemeColors.DefaultHighlightColor);

        SKTypeface labelFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);
        SKTypeface valueFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.SemiBold);
        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Bold);

        ReceiptOptions options = data.Options ?? new ReceiptOptions();

        List<(string Label, decimal Amount)> rows = MaterializeSubRows(data);

        float y = origin.Y;

        // Draw sub-total rows.
        for (int i = 0; i < rows.Count; i++)
        {
            if (i > 0)
            {
                y += RowGap;
            }

            (string label, decimal amount) = rows[i];
            string valueStr = MoneyFormatter.Format(amount, options);
            DrawSubRow(canvas, origin.X, y, width, label, valueStr, labelFace, valueFace, mutedColor, textColor);
            y += RowHeight;
        }

        // Gap before TOTAL bar.
        if (rows.Count > 0)
        {
            y += RowGap;
        }

        // TOTAL bar: fill highlight rect first, then draw text on top.
        float totalBarTopY = y;
        using (var highlightPaint = new SKPaint
        {
            Color = highlightColor,
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        })
        {
            canvas.DrawRect(new SKRect(origin.X, totalBarTopY, origin.X + width, totalBarTopY + TotalBarHeight), highlightPaint);
        }

        // Vertical center of text within the bar.
        float totalBaselineY = totalBarTopY + (TotalBarHeight / 2f) + (TotalFontSize / 2f);

        string grandTotalStr = MoneyFormatter.Format(data.Totals.GrandTotal, options);

        using var boldFont = new SKFont(boldFace, TotalFontSize);
        using var totalTextPaint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
        };

        // "TOTAL" label — left-aligned inside bar.
        canvas.DrawText(LabelTotal, origin.X + 4f, totalBaselineY, boldFont, totalTextPaint);

        // Grand total value — right-aligned with margin so (width - 10, height - 8)
        // lands on highlight fill, not on glyph antialiasing.
        SKRect valueBounds = TextMeasurer.Measure(grandTotalStr, boldFace, TotalFontSize);
        float valueX = origin.X + width - TotalValueRightMargin - valueBounds.Width - valueBounds.Left;
        canvas.DrawText(grandTotalStr, valueX, totalBaselineY, boldFont, totalTextPaint);
    }

    // Builds the ordered list of sub-total rows (everything except the TOTAL bar).
    // Return type is List<T> to satisfy CA1859 — do not widen to IReadOnlyList<T>.
    private static List<(string Label, decimal Amount)> MaterializeSubRows(ReceiptData data)
    {
        var rows = new List<(string Label, decimal Amount)>(capacity: 5);

        // Subtotal — always.
        rows.Add((LabelSubtotal, data.Totals.Subtotal));

        // Discount — iff non-zero.
        if (data.Totals.DiscountTotal != 0m)
        {
            rows.Add((LabelDiscount, data.Totals.DiscountTotal));
        }

        // Service Charge — iff non-zero.
        if (data.Totals.ServiceCharge != 0m)
        {
            rows.Add((LabelServiceCharge, data.Totals.ServiceCharge));
        }

        // Tax — iff ShowTaxBreakdown is true.
        if (data.Options?.ShowTaxBreakdown == true)
        {
            string taxLabel = data.Totals.TaxLabel ?? TaxLabelFallback;
            rows.Add((taxLabel, data.Totals.TaxTotal));
        }

        // Rounding — iff non-zero.
        if (data.Totals.RoundingAdjustment != 0m)
        {
            rows.Add((LabelRounding, data.Totals.RoundingAdjustment));
        }

        return rows;
    }

    private static void DrawSubRow(
        SKCanvas canvas,
        float originX,
        float topY,
        float width,
        string label,
        string value,
        SKTypeface labelFace,
        SKTypeface valueFace,
        SKColor labelColor,
        SKColor valueColor)
    {
        float labelColumnWidth = width * LabelColumnFraction;
        float valueColumnLeft = originX + labelColumnWidth;
        float valueColumnWidth = width - labelColumnWidth;
        float baselineY = topY + LabelFontSize;

        using var labelFont = new SKFont(labelFace, LabelFontSize);
        using var valueFont = new SKFont(valueFace, ValueFontSize);
        using var labelPaint = new SKPaint { Color = labelColor, IsAntialias = true };
        using var valuePaint = new SKPaint { Color = valueColor, IsAntialias = true };

        canvas.DrawText(label, originX, baselineY, labelFont, labelPaint);

        SKRect valueBounds = TextMeasurer.Measure(value, valueFace, ValueFontSize);
        float valueX = valueColumnLeft + valueColumnWidth - valueBounds.Width - valueBounds.Left;
        canvas.DrawText(value, valueX, baselineY, valueFont, valuePaint);
    }
}
