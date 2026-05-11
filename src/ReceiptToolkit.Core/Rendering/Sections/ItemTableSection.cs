using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the line-item table band: one row per <see cref="ReceiptItem"/> in
///   <see cref="ReceiptData.Items"/>. Each row shows the item name (wrapped when needed),
///   quantity, unit price, and line total. Optional sub-lines for SKU and description
///   are controlled by <see cref="ReceiptOptions.ShowSku"/> and
///   <see cref="ReceiptOptions.ShowItemDescription"/>.
/// </summary>
/// <remarks>
///   <para>
///     Column budget (for a 360 px receipt): name column = full width minus the three
///     right-side columns (Qty 40 px, UnitPrice 60 px, Total 70 px = 170 px reserved).
///     The remaining ~190 px is the name column, wide enough for typical item names but
///     intentionally narrow enough to exercise <see cref="TextMeasurer.WrapLines"/> on
///     longer descriptions. This split is a Phase 3b constant; Phase 3c may pull column
///     widths from <see cref="ReceiptLayout"/>.
///   </para>
///   <para>
///     Long item names are wrapped via <see cref="TextMeasurer.WrapLines"/>; per
///     divergence #13 a single word that exceeds the column width is placed on its own
///     line without glyph-level breaking. The <see cref="Measure"/> pass accounts for
///     all wrapped lines so the Draw pass never overflows.
///   </para>
///   <para>
///     SKU sub-line appears directly under the item name iff
///     <see cref="ReceiptOptions.ShowSku"/> is <see langword="true"/> and the item's
///     <see cref="ReceiptItem.Sku"/> is non-blank.
///   </para>
///   <para>
///     Description sub-line appears iff <see cref="ReceiptOptions.ShowItemDescription"/>
///     is <see langword="true"/> and the item's <see cref="ReceiptItem.Description"/> is
///     non-blank.
///   </para>
///   <para>
///     Money strings are produced by <see cref="MoneyFormatter.Format"/>. Section code
///     never rounds or parses amounts.
///   </para>
///   <para>
///     Column and header labels ("Item", "Qty", "Price", "Total") are presentation chrome
///     as <c>private const string</c>. TODO: replace with a localized string table when an
///     i18n layer exists.
///   </para>
/// </remarks>
public sealed class ItemTableSection : IReceiptSection
{
    // Row metrics.
    private const float RowHeight = 16f;      // Height of a single text line within a row.
    private const float RowGap = 6f;          // Vertical gap between item rows.
    private const float SubLineGap = 2f;      // Gap between the name line(s) and a sub-line.
    private const float HeaderBottomGap = 4f; // Gap between the header row and the first item row.

    // Font sizes.
    private const float HeaderFontSize = 11f;
    private const float NameFontSize = 11f;
    private const float SubLineFontSize = 10f;
    private const float RightColFontSize = 11f;

    // Column widths.
    // Reserved right-side columns: Qty(40) + UnitPrice(60) + Total(70) = 170 px.
    // Name column = total width − 170 px.  Documented for Phase 3c to promote to ReceiptLayout.
    private const float QtyColWidth = 40f;
    private const float PriceColWidth = 60f;
    private const float TotalColWidth = 70f;
    private const float ReservedRightWidth = QtyColWidth + PriceColWidth + TotalColWidth;

    private const string FontFamily = "Inter";

    // Presentation-chrome column headers (i18n-deferred, see remarks). All-caps per 3c-polish B.
    private const string HeaderItem = "ITEM";
    private const string HeaderQty = "QTY";
    private const string HeaderPrice = "PRICE";
    private const string HeaderTotal = "TOTAL";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Items.Count == 0)
        {
            return 0f;
        }

        float nameColWidth = Math.Max(width - ReservedRightWidth, 1f);
        SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        // Header row.
        float height = RowHeight + HeaderBottomGap;

        bool showSku = data.Options?.ShowSku == true;
        bool showDesc = data.Options?.ShowItemDescription == true;

        for (int i = 0; i < data.Items.Count; i++)
        {
            ReceiptItem item = data.Items[i];

            if (i > 0)
            {
                height += RowGap;
            }

            // Name lines (wrapped).
            IReadOnlyList<string> nameLines = TextMeasurer.WrapLines(item.Name, nameColWidth, face, NameFontSize);
            int lineCount = Math.Max(nameLines.Count, 1);
            height += lineCount * RowHeight;
            if (lineCount > 1)
            {
                height += (lineCount - 1) * SubLineGap;
            }

            // SKU sub-line.
            if (showSku && !string.IsNullOrWhiteSpace(item.Sku))
            {
                height += SubLineGap + RowHeight;
            }

            // Description sub-line.
            if (showDesc && !string.IsNullOrWhiteSpace(item.Description))
            {
                height += SubLineGap + RowHeight;
            }
        }

        return height;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Items.Count == 0)
        {
            return;
        }

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);

        SKTypeface boldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.SemiBold);
        SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        float nameColWidth = Math.Max(width - ReservedRightWidth, 1f);
        float qtyColLeft = origin.X + nameColWidth;
        float priceColLeft = qtyColLeft + QtyColWidth;
        float totalColLeft = priceColLeft + PriceColWidth;

        float y = origin.Y;

        // --- Header row --- labels use muted label colour per 3c-polish B.
        DrawHeaderRow(canvas, origin.X, y, nameColWidth, qtyColLeft, priceColLeft, totalColLeft, boldFace, ThemeColors.DefaultMutedLabelColor);
        y += RowHeight + HeaderBottomGap;

        // --- Item rows ---
        bool showSku = data.Options?.ShowSku == true;
        bool showDesc = data.Options?.ShowItemDescription == true;
        ReceiptOptions options = data.Options ?? new ReceiptOptions();

        for (int i = 0; i < data.Items.Count; i++)
        {
            ReceiptItem item = data.Items[i];

            if (i > 0)
            {
                y += RowGap;
            }

            // Name lines (wrapped). WrapLines returns empty when Name is "" (the record default);
            // the fallback ensures lineY advances by RowHeight to match Measure's Math.Max(…, 1) floor.
            IReadOnlyList<string> nameLines = TextMeasurer.WrapLines(item.Name, nameColWidth, normalFace, NameFontSize);
            if (nameLines.Count == 0)
            {
                nameLines = [item.Name];
            }

            // Right-column values align with the first (top) name line.
            float firstLineBaseline = y + NameFontSize;

            // Draw wrapped name lines.
            float lineY = y;
            for (int li = 0; li < nameLines.Count; li++)
            {
                if (li > 0)
                {
                    lineY += SubLineGap;
                }

                DrawLeftText(canvas, origin.X, lineY + NameFontSize, nameLines[li], normalFace, NameFontSize, textColor);
                lineY += RowHeight;
            }

            // Right columns: Qty (centered), UnitPrice (right-aligned), Total (right-aligned).
            string qtyStr = item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DrawCenteredText(canvas, qtyColLeft, firstLineBaseline, QtyColWidth, qtyStr, normalFace, RightColFontSize, textColor);

            string priceStr = MoneyFormatter.Format(item.UnitPrice, options);
            DrawRightText(canvas, priceColLeft, firstLineBaseline, PriceColWidth, priceStr, normalFace, RightColFontSize, textColor);

            string totalStr = MoneyFormatter.Format(item.Total, options);
            DrawRightText(canvas, totalColLeft, firstLineBaseline, TotalColWidth, totalStr, normalFace, RightColFontSize, textColor);

            // SKU sub-line.
            if (showSku && !string.IsNullOrWhiteSpace(item.Sku))
            {
                DrawLeftText(canvas, origin.X, lineY + SubLineGap + SubLineFontSize, item.Sku!, normalFace, SubLineFontSize, mutedColor);
                lineY += SubLineGap + RowHeight;
            }

            // Description sub-line.
            if (showDesc && !string.IsNullOrWhiteSpace(item.Description))
            {
                DrawLeftText(canvas, origin.X, lineY + SubLineGap + SubLineFontSize, item.Description!, normalFace, SubLineFontSize, mutedColor);
                lineY += SubLineGap + RowHeight;
            }

            y = lineY;
        }
    }

    private static void DrawHeaderRow(
        SKCanvas canvas,
        float originX,
        float y,
        float nameColWidth,
        float qtyColLeft,
        float priceColLeft,
        float totalColLeft,
        SKTypeface face,
        SKColor color)
    {
        float baselineY = y + HeaderFontSize;

        DrawLeftText(canvas, originX, baselineY, HeaderItem, face, HeaderFontSize, color);
        DrawCenteredText(canvas, qtyColLeft, baselineY, QtyColWidth, HeaderQty, face, HeaderFontSize, color);
        DrawRightText(canvas, priceColLeft, baselineY, PriceColWidth, HeaderPrice, face, HeaderFontSize, color);
        DrawRightText(canvas, totalColLeft, baselineY, TotalColWidth, HeaderTotal, face, HeaderFontSize, color);
    }

    private static void DrawLeftText(
        SKCanvas canvas,
        float x,
        float baselineY,
        string text,
        SKTypeface face,
        float size,
        SKColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        using var font = new SKFont(face, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, baselineY, font, paint);
    }

    private static void DrawCenteredText(
        SKCanvas canvas,
        float colLeft,
        float baselineY,
        float colWidth,
        string text,
        SKTypeface face,
        float size,
        SKColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SKRect bounds = TextMeasurer.Measure(text, face, size);
        float x = colLeft + (colWidth / 2f) - (bounds.Width / 2f) - bounds.Left;

        using var font = new SKFont(face, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, baselineY, font, paint);
    }

    private static void DrawRightText(
        SKCanvas canvas,
        float colLeft,
        float baselineY,
        float colWidth,
        string text,
        SKTypeface face,
        float size,
        SKColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SKRect bounds = TextMeasurer.Measure(text, face, size);
        float x = colLeft + colWidth - bounds.Width - bounds.Left;

        using var font = new SKFont(face, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, baselineY, font, paint);
    }
}
