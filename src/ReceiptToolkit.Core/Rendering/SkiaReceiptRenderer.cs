using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering;

/// <summary>
///   Composes the full receipt by walking the ten <see cref="IReceiptSection"/>
///   implementations in mockup order, summing their measured heights, and painting
///   the paper background plus each section onto an <see cref="SKCanvas"/>.
/// </summary>
/// <remarks>
///   <para>
///     Section order matches <c>mockups/receipt.png</c> top-to-bottom: header, title,
///     meta, customer/cashier, item table, totals, payment, QR, footer, perforation.
///     Sections that report a <c>0f</c> height from <see cref="IReceiptSection.Measure"/>
///     are treated as omitted: they contribute no vertical space, no
///     <see cref="IReceiptSection.Draw"/> call is issued, and the inter-section gap is
///     applied only between consecutive visible sections.
///   </para>
///   <para>
///     Total height is <c>2 * padding + Σ(visible heights) + sectionGap *
///     max(0, visibleCount - 1)</c>. The trailing <c>padding</c> below the last
///     visible section is supplied by the outer margin, not by an extra
///     <c>sectionGap</c>.
///   </para>
///   <para>
///     The paper background is painted as an explicit <see cref="SKCanvas.DrawRect(SKRect, SKPaint)"/>
///     covering <c>(0, 0, width, height)</c> before any section draws (Option B
///     scope). Antialiasing is disabled for that fill so the rectangle aligns
///     exactly to integer pixel boundaries — corner samples are reliable.
///   </para>
///   <para>
///     Layout numerics (<c>ReceiptWidth</c>, <c>Padding</c>, <c>SectionGap</c>) and
///     theme colours come from <see cref="ReceiptData"/> only; nothing is hard-coded
///     in this composer. Theme strings are resolved via
///     <see cref="ThemeColors.ResolveOrDefault"/>.
///   </para>
/// </remarks>
public sealed class SkiaReceiptRenderer
{
    private readonly IReceiptSection[] _sections =
    [
        new HeaderSection(),
        new TitleSection(),
        new MetaSection(),
        new CustomerCashierSection(),
        new ItemTableSection(),
        new TotalsSection(),
        new PaymentSection(),
        new QrSection(),
        new FooterSection(),
        new PerforationSection(),
    ];

    /// <summary>
    ///   Reports the overall receipt size for the given <paramref name="data"/> at the
    ///   layout's configured <c>ReceiptWidth</c>.
    /// </summary>
    /// <param name="data">The receipt model; layout, theme, and section inputs are read from here.</param>
    /// <param name="ctx">Per-render resources (fonts, resolved logo, ...).</param>
    /// <returns>
    ///   An <see cref="SKSize"/> whose <c>Width</c> equals <c>layout.ReceiptWidth</c> and
    ///   whose <c>Height</c> is the sum of visible section heights, inter-section gaps,
    ///   and the top/bottom padding.
    /// </returns>
    public SKSize Measure(ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        ReceiptLayout layout = data.Layout ?? new ReceiptLayout();
        float contentWidth = layout.ReceiptWidth - (2 * layout.Padding);

        List<float> heights = MeasureSections(contentWidth, data, ctx);
        float totalHeight = ComputeTotalHeight(heights, layout);

        return new SKSize(layout.ReceiptWidth, totalHeight);
    }

    /// <summary>
    ///   Paints the paper background and every visible section onto
    ///   <paramref name="canvas"/> at the canvas origin.
    /// </summary>
    /// <param name="canvas">The target canvas; assumed to be sized to <see cref="Measure"/>.</param>
    /// <param name="data">The receipt model; layout, theme, and section inputs are read from here.</param>
    /// <param name="ctx">Per-render resources (fonts, resolved logo, ...).</param>
    /// <remarks>
    ///   The paper rectangle is drawn first, then each section is drawn at
    ///   <c>(padding, currentY)</c> with the per-section content width. The cursor
    ///   advances by <c>height + sectionGap</c> only between consecutive visible
    ///   sections so the trailing gap does not pad the bottom edge.
    /// </remarks>
    public void Render(SKCanvas canvas, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        ReceiptLayout layout = data.Layout ?? new ReceiptLayout();
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap;
        float contentWidth = layout.ReceiptWidth - (2 * padding);

        List<float> heights = MeasureSections(contentWidth, data, ctx);
        float totalHeight = ComputeTotalHeight(heights, layout);

        SKColor paper = ThemeColors.ResolveOrDefault(
            data.Theme?.PaperColor,
            ThemeColors.DefaultPaperColor);

        using (var paperPaint = new SKPaint
        {
            Color = paper,
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
        })
        {
            canvas.DrawRect(0, 0, layout.ReceiptWidth, totalHeight, paperPaint);
        }

        float y = padding;
        bool first = true;
        for (int i = 0; i < _sections.Length; i++)
        {
            float h = heights[i];
            if (h <= 0f)
            {
                continue;
            }

            if (!first)
            {
                y += sectionGap;
            }

            _sections[i].Draw(canvas, new SKPoint(padding, y), contentWidth, data, ctx);
            y += h;
            first = false;
        }
    }

    private List<float> MeasureSections(float contentWidth, ReceiptData data, RenderContext ctx)
    {
        var heights = new List<float>(_sections.Length);
        for (int i = 0; i < _sections.Length; i++)
        {
            heights.Add(_sections[i].Measure(contentWidth, data, ctx));
        }

        return heights;
    }

    private static float ComputeTotalHeight(List<float> heights, ReceiptLayout layout)
    {
        float sum = 0f;
        int visibleCount = 0;
        for (int i = 0; i < heights.Count; i++)
        {
            if (heights[i] > 0f)
            {
                sum += heights[i];
                visibleCount++;
            }
        }

        return sum
            + (layout.SectionGap * Math.Max(0, visibleCount - 1))
            + (2 * layout.Padding);
    }
}
