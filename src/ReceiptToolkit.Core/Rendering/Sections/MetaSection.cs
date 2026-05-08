using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the receipt metadata band as a two-column block: label (muted) on the
///   left, value (text colour) on the right. Hidden rows leave no gap.
/// </summary>
/// <remarks>
///   <para>
///     Rows, in fixed order, are emitted iff their underlying field is non-blank:
///     Receipt #, Date, Time, Branch, Terminal, Order, Reference. Date and Time route
///     through <see cref="DateTimeFormatter"/> using <see cref="ReceiptOptions"/>.
///   </para>
///   <para>
///     Time is rendered iff <see cref="ReceiptMetadata.DateTime"/> is non-blank
///     <em>and</em> <see cref="ReceiptOptions.TimeFormat"/> is non-blank. The format
///     spec itself is the formatter's contract — the section does not validate it.
///   </para>
///   <para>
///     Labels (e.g. "Receipt #", "Branch") are presentation chrome rather than JSON
///     content; they live as constants here for Phase 3b. A future i18n layer may pull
///     them from a localized string table — TODO when that layer exists.
///   </para>
/// </remarks>
public sealed class MetaSection : IReceiptSection
{
    private const float RowHeight = 16f;
    private const float RowGap = 4f;
    private const float LabelFontSize = 11f;
    private const float ValueFontSize = 11f;
    private const float LabelColumnFraction = 0.35f;
    private const string FontFamily = "Inter";

    private const string LabelReceipt = "Receipt #";
    private const string LabelDate = "Date";
    private const string LabelTime = "Time";
    private const string LabelDateAndTime = "Date & Time";
    private const string DateTimeSeparator = " · ";
    private const string LabelBranch = "Branch";
    private const string LabelTerminal = "Terminal";
    private const string LabelOrder = "Order";
    private const string LabelReference = "Ref";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        int rows = MaterializeRows(data).Count;
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

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);
        SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        float y = origin.Y;
        bool first = true;
        foreach ((string label, string value) in MaterializeRows(data))
        {
            if (!first)
            {
                y += RowGap;
            }

            DrawRow(canvas, origin.X, y, width, label, value, face, mutedColor, textColor);
            y += RowHeight;
            first = false;
        }
    }

    // Builds the row list once per Measure / Draw call. The DateTimeFormatter calls
    // are not free (DateTime.Parse + culture lookup), so we avoid re-running them
    // by collecting the rows eagerly rather than re-iterating an IEnumerable twice.
    private static List<(string Label, string Value)> MaterializeRows(ReceiptData data)
    {
        ReceiptMetadata r = data.Receipt;
        ReceiptOptions? options = data.Options;
        var rows = new List<(string Label, string Value)>(capacity: 7);

        if (!string.IsNullOrWhiteSpace(r.ReceiptNumber))
        {
            rows.Add((LabelReceipt, r.ReceiptNumber));
        }

        string? dateValue = null;
        if (!string.IsNullOrWhiteSpace(r.DateTime) && options is not null
            && !string.IsNullOrWhiteSpace(options.DateFormat))
        {
            string formatted = DateTimeFormatter.FormatDate(r.DateTime, options);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                dateValue = formatted;
            }
        }

        string? timeValue = null;
        if (!string.IsNullOrWhiteSpace(r.DateTime) && options is not null
            && !string.IsNullOrWhiteSpace(options.TimeFormat))
        {
            string formatted = DateTimeFormatter.FormatTime(r.DateTime, options);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                timeValue = formatted;
            }
        }

        if (dateValue is not null && timeValue is not null)
        {
            rows.Add((LabelDateAndTime, dateValue + DateTimeSeparator + timeValue));
        }
        else if (dateValue is not null)
        {
            rows.Add((LabelDate, dateValue));
        }
        else if (timeValue is not null)
        {
            rows.Add((LabelTime, timeValue));
        }

        if (!string.IsNullOrWhiteSpace(r.BranchName))
        {
            rows.Add((LabelBranch, r.BranchName));
        }

        if (!string.IsNullOrWhiteSpace(r.TerminalId))
        {
            rows.Add((LabelTerminal, r.TerminalId));
        }

        if (!string.IsNullOrWhiteSpace(r.OrderNumber))
        {
            rows.Add((LabelOrder, r.OrderNumber));
        }

        if (!string.IsNullOrWhiteSpace(r.ReferenceNumber))
        {
            rows.Add((LabelReference, r.ReferenceNumber));
        }

        return rows;
    }

    private static void DrawRow(
        SKCanvas canvas,
        float originX,
        float topY,
        float width,
        string label,
        string value,
        SKTypeface face,
        SKColor labelColor,
        SKColor valueColor)
    {
        float labelColumnWidth = width * LabelColumnFraction;
        float valueColumnLeft = originX + labelColumnWidth;
        float valueColumnWidth = width - labelColumnWidth;
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

        // Label: left-aligned within the label column.
        canvas.DrawText(label, originX, baselineY, font, labelPaint);

        // Value: right-aligned within the value column.
        SKRect valueBounds = TextMeasurer.Measure(value, face, ValueFontSize);
        float valueX = valueColumnLeft + valueColumnWidth - valueBounds.Width - valueBounds.Left;
        canvas.DrawText(value, valueX, baselineY, font, valuePaint);
    }
}
