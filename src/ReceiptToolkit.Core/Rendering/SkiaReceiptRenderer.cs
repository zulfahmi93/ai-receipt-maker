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
///     The paper background is painted before any section draws (Option B scope) over the
///     entire <c>(0, 0, width, height)</c> region. Geometry switches on
///     <c>layout.BorderRadius</c>: positive values draw a rounded rectangle via
///     <see cref="SKCanvas.DrawRoundRect(SKRect, float, float, SKPaint)"/> so pixels outside
///     the corner curves remain at the bitmap's default state (transparent black, alpha=0);
///     zero or negative values fall back to <see cref="SKCanvas.DrawRect(SKRect, SKPaint)"/>
///     covering the full rectangle. Antialiasing is disabled for the paper fill so the
///     rectangle aligns exactly to integer pixel boundaries — corner samples are reliable.
///   </para>
///   <para>
///     Caller contract for the corner-clip behaviour: the destination canvas's backing
///     surface (typically <see cref="SKBitmap"/>) MUST be in its default fully-transparent
///     state (alpha=0 across all pixels) before <see cref="Render"/> runs. If the caller
///     pre-clears or pre-fills the bitmap with an opaque colour, pixels outside the
///     rounded corner curves will retain that pre-fill rather than reading alpha=0, and
///     the implicit clipping contract no longer holds. Out-of-range
///     <c>layout.BorderRadius</c> values are not validated by the renderer: very large
///     radii (greater than half the smaller dimension) are clamped silently by Skia's
///     rasteriser to produce a pill or ellipse-like shape rather than throwing. Callers
///     that need a hard ceiling should enforce it at the validation layer.
///   </para>
///   <para>
///     Layout numerics (<c>ReceiptWidth</c>, <c>Padding</c>, <c>SectionGap</c>) and
///     theme colours come from <see cref="ReceiptData"/> only; nothing is hard-coded
///     in this composer. Theme strings are resolved via
///     <see cref="ThemeColors.ResolveOrDefault"/>.
///   </para>
///   <para>
///     Sections may opt into a leading divider via <see cref="IReceiptSection.RequiresLeadingDivider"/>;
///     the composer paints the stroke at the gap midpoint when the next visible section requires one.
///     Style is read from <c>layout.DividerStyle</c> (<c>"solid"</c>/<c>"dashed"</c>/<c>"dotted"</c>);
///     null/empty/unknown values suppress the draw.
///   </para>
/// </remarks>
public sealed class SkiaReceiptRenderer
{
    private static readonly float[] DashIntervals = [6f, 4f];
    private static readonly float[] DotIntervals = [2f, 3f];

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
            SKRect paperRect = new(0, 0, layout.ReceiptWidth, totalHeight);
            if (layout.BorderRadius > 0)
            {
                canvas.DrawRoundRect(paperRect, layout.BorderRadius, layout.BorderRadius, paperPaint);
            }
            else
            {
                canvas.DrawRect(paperRect, paperPaint);
            }
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
                float gapStartY = y;
                y += sectionGap;
                if (_sections[i].RequiresLeadingDivider)
                {
                    float dividerY = MathF.Round(gapStartY + (sectionGap / 2f));
                    DrawLeadingDivider(canvas, dividerY, padding, layout.ReceiptWidth - padding, data);
                }
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

    /// <summary>
    ///   Paints a single horizontal divider stroke at <paramref name="dividerY"/> spanning
    ///   <paramref name="xStart"/> to <paramref name="xEnd"/>. The divider Y is the
    ///   midpoint of the inter-section gap that precedes a section whose
    ///   <see cref="IReceiptSection.RequiresLeadingDivider"/> is <see langword="true"/>.
    ///   Stroke colour is resolved from <c>theme.dividerColor</c> with
    ///   <see cref="ThemeColors.DefaultDividerColor"/> as fallback. Style branch on
    ///   <c>layout.DividerStyle</c>: <c>"solid"</c> draws a hairline with no
    ///   <see cref="SKPathEffect"/>; <c>"dashed"</c> applies a [6, 4] dash; <c>"dotted"</c>
    ///   applies a [2, 3] dot. Any other value of <c>layout.DividerStyle</c> — including
    ///   <see langword="null"/>, the empty string, whitespace, or an unrecognised token —
    ///   causes an early return before any <see cref="SKPaint"/> is allocated, so no
    ///   stroke is drawn at all. (Note: a <see langword="null"/> path effect on the
    ///   <c>"solid"</c> branch is the correct value for a plain hairline; that null is
    ///   distinct from the early-return null-style suppression.) StrokeWidth is 1px and
    ///   antialiasing is disabled so exact-pixel sampling is reliable; callers must pass
    ///   an integer-valued <paramref name="dividerY"/> (snap with <see cref="MathF.Round(float)"/>
    ///   when computed from a half-integer gap midpoint) to avoid platform-dependent
    ///   rasteriser snapping at half-pixel rows.
    /// </summary>
    private static void DrawLeadingDivider(
        SKCanvas canvas,
        float dividerY,
        float xStart,
        float xEnd,
        ReceiptData data)
    {
        string? style = data.Layout?.DividerStyle;
        if (style is not ("solid" or "dashed" or "dotted"))
        {
            return;
        }

        using SKPathEffect? effect = style switch
        {
            "dashed" => SKPathEffect.CreateDash(DashIntervals, 0f),
            "dotted" => SKPathEffect.CreateDash(DotIntervals, 0f),
            _ => null,
        };

        SKColor stroke = ThemeColors.ResolveOrDefault(
            data.Theme?.DividerColor,
            ThemeColors.DefaultDividerColor);

        using var paint = new SKPaint
        {
            Color = stroke,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false,
            PathEffect = effect,
        };

        canvas.DrawLine(xStart, dividerY, xEnd, dividerY, paint);
    }
}
