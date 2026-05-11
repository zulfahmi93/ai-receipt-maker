using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the customer / cashier band as two columns: customer fields (Name, ID,
///   Phone, Email) in the left column, cashier fields (Name, ID) in the right column.
///   Only non-null, non-blank fields are emitted; absent fields leave no gap row.
/// </summary>
/// <remarks>
///   <para>
///     Driven by <see cref="ReceiptData.Customer"/> (<see cref="CustomerInfo"/>) and
///     <see cref="ReceiptData.Cashier"/> (<see cref="CashierInfo"/>). When both are
///     <see langword="null"/> — or both exist but every field is <see langword="null"/> or
///     blank — the section is omitted: <see cref="Measure"/> returns <c>0f</c> and
///     <see cref="Draw"/> performs no canvas operations.
///   </para>
///   <para>
///     The two columns split the available width equally minus a fixed
///     <see cref="ColumnGutter"/> separating them. Within each column, label text is
///     muted and value text uses the primary text colour, mirroring the MetaSection
///     two-column pattern.
///   </para>
///   <para>
///     Layout numerics (font sizes, row height, gap) are local constants in Phase 3b.
///     Phase 3c may pull them from <see cref="ReceiptLayout"/>.
///   </para>
///   <para>
///     Labels ("Customer", "ID", "Phone", "Email", "Cashier") are presentation chrome
///     kept as <c>private const string</c> here — they are not user-visible JSON content.
///     TODO: replace with a localized string table when an i18n layer exists.
///   </para>
/// </remarks>
public sealed class CustomerCashierSection : IReceiptSection
{
    private const float RowHeight = 16f;
    private const float RowGap = 4f;
    private const float LabelFontSize = 11f;
    private const float ValueFontSize = 11f;
    private const float ColumnGutter = 16f;
    private const float LabelColumnFraction = 0.35f;
    private const string FontFamily = "Inter";

    // Presentation-chrome labels — not user-visible content (i18n-deferred, see remarks).
    private const string LabelCustomer = "Customer";
    private const string LabelId = "ID";
    private const string LabelPhone = "Phone";
    private const string LabelEmail = "Email";
    private const string LabelCashier = "Cashier";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        // Measure mirrors Draw's column order — see Draw's flipped order comment.
        int leftRows = MaterializeCashierRows(data).Count;
        int rightRows = MaterializeCustomerRows(data).Count;
        int rows = Math.Max(leftRows, rightRows);

        if (rows == 0)
        {
            return 0f;
        }

        return (rows * RowHeight) + ((rows - 1) * RowGap);
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        // Column order mirrors mockups/receipt.png: Cashier left, Customer right.
        // Earlier Phase 3b layout placed Customer on the left; flipped 2026-05-11 at
        // Phase 3c-polish follow-up.
        List<(string Label, string Value)> leftRows = MaterializeCashierRows(data);
        List<(string Label, string Value)> rightRows = MaterializeCustomerRows(data);

        int totalRows = Math.Max(leftRows.Count, rightRows.Count);
        if (totalRows == 0)
        {
            return;
        }

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);
        SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        float columnWidth = (width - ColumnGutter) / 2f;
        float rightColumnLeft = origin.X + columnWidth + ColumnGutter;

        for (int i = 0; i < totalRows; i++)
        {
            float rowY = origin.Y + (i * (RowHeight + RowGap));

            if (i < leftRows.Count)
            {
                (string label, string value) = leftRows[i];
                DrawRow(canvas, origin.X, rowY, columnWidth, label, value, face, mutedColor, textColor);
            }

            if (i < rightRows.Count)
            {
                (string label, string value) = rightRows[i];
                DrawRow(canvas, rightColumnLeft, rowY, columnWidth, label, value, face, mutedColor, textColor);
            }
        }
    }

    // Collects non-blank customer fields in display order: Name, ID, Phone, Email.
    // Returns List<T> (not IReadOnlyList<T>) to satisfy CA1859 with TreatWarningsAsErrors=true.
    private static List<(string Label, string Value)> MaterializeCustomerRows(ReceiptData data)
    {
        CustomerInfo? c = data.Customer;
        var rows = new List<(string Label, string Value)>(capacity: 4);

        if (c is null)
        {
            return rows;
        }

        if (!string.IsNullOrWhiteSpace(c.CustomerName))
        {
            rows.Add((LabelCustomer, c.CustomerName));
        }

        if (!string.IsNullOrWhiteSpace(c.CustomerId))
        {
            rows.Add((LabelId, c.CustomerId));
        }

        if (!string.IsNullOrWhiteSpace(c.CustomerPhone))
        {
            rows.Add((LabelPhone, c.CustomerPhone));
        }

        if (!string.IsNullOrWhiteSpace(c.CustomerEmail))
        {
            rows.Add((LabelEmail, c.CustomerEmail));
        }

        return rows;
    }

    // Collects non-blank cashier fields in display order: Name, ID.
    // Returns List<T> (not IReadOnlyList<T>) to satisfy CA1859 with TreatWarningsAsErrors=true.
    private static List<(string Label, string Value)> MaterializeCashierRows(ReceiptData data)
    {
        CashierInfo? c = data.Cashier;
        var rows = new List<(string Label, string Value)>(capacity: 2);

        if (c is null)
        {
            return rows;
        }

        if (!string.IsNullOrWhiteSpace(c.CashierName))
        {
            rows.Add((LabelCashier, c.CashierName));
        }

        if (!string.IsNullOrWhiteSpace(c.CashierId))
        {
            rows.Add((LabelId, c.CashierId));
        }

        return rows;
    }

    private static void DrawRow(
        SKCanvas canvas,
        float originX,
        float topY,
        float columnWidth,
        string label,
        string value,
        SKTypeface face,
        SKColor labelColor,
        SKColor valueColor)
    {
        // Split label / value at a fixed 35 % / 65 % within the column — same fraction as MetaSection.
        float labelColWidth = columnWidth * LabelColumnFraction;
        float valueColLeft = originX + labelColWidth;
        float valueColWidth = columnWidth - labelColWidth;
        float baselineY = topY + LabelFontSize;

        using var font = new SKFont(face, LabelFontSize);
        using var labelPaint = new SKPaint
        {
            Color = labelColor,
            IsAntialias = true,
        };
        using var valuePaint = new SKPaint
        {
            Color = valueColor,
            IsAntialias = true,
        };

        // Label: left-aligned within the label sub-column.
        canvas.DrawText(label, originX, baselineY, font, labelPaint);

        // Value: right-aligned within the value sub-column, mirroring MetaSection.
        SKRect valueBounds = TextMeasurer.Measure(value, face, ValueFontSize);
        float valueX = valueColLeft + valueColWidth - valueBounds.Width - valueBounds.Left;
        canvas.DrawText(value, valueX, baselineY, font, valuePaint);
    }
}
