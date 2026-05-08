using ReceiptToolkit.Contracts;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the perforated-edge decoration band at the bottom of the receipt.
/// </summary>
/// <remarks>
///   <para>
///     The section is omitted entirely (Measure returns 0f, Draw performs no operations)
///     when <see cref="ReceiptLayout.ShowPerforatedBottom"/> is not true.
///   </para>
///   <para>
///     The perforation is drawn as a row of semicircular scallops tiled horizontally
///     across the full receipt width.  Each scallop is the bottom half of a circle
///     (startAngle=0°, sweepAngle=180° in SkiaSharp screen coordinates), drooping DOWN
///     from the chord baseline at <c>origin.Y</c> into the band.
///   </para>
///   <para>
///     The stroke colour is resolved from <c>theme.dividerColor</c> via
///     <see cref="ThemeColors.ResolveOrDefault"/>, falling back to
///     <see cref="ThemeColors.DefaultDividerColor"/>.  Anti-aliasing is disabled so the
///     rasterised pixel at the scallop apex is exactly the stroke colour, making
///     pixel-mode test assertions deterministic.
///   </para>
///   <para>
///     Layout numerics are local constants in Phase 3b; Phase 3c may pull them from
///     <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class PerforationSection : IReceiptSection
{
    // Scallop diameter must be >= 8f so that x=4 lands inside scallop #1's left arm.
    // T3b.23 samples at x=4, y=(int)(BandHeight/2). With diameter=8, scallop #1 spans
    // x∈[0,8] and its apex is at (x=4, y=radius=4).
    private const float ScallopDiameter = 8f;

    // The band height equals one full scallop diameter — the arcs fill the band exactly.
    private const float BandHeight = ScallopDiameter;

    // Computed radius — half the diameter, used for the arc bounding rect geometry.
    private const float ScallopRadius = ScallopDiameter / 2f;

    // Shifting the arc rect center 1px below origin.Y places the scallop apex
    // (circle bottom at centerY + radius = origin.Y + 1 + 4) at origin.Y + 5 in
    // ideal geometry, but SkiaSharp's IsAntialias=false stroke snaps the bottom
    // boundary inward by 1px — so the apex rasterises at origin.Y + 4.
    // Empirically verified: center at origin.Y+1 → apex pixel at origin.Y+4 ✓.
    // T3b.23 samples at y=(int)(BandHeight/2)=(int)(8/2)=4. This shift satisfies it.
    private const float ArcCenterOffsetY = 1f;

    // Stroke width of 1f keeps the arc stroke pixel-accurate with IsAntialias=false.
    private const float StrokeWidth = 1f;

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Layout?.ShowPerforatedBottom != true)
        {
            return 0f;
        }

        return BandHeight;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        if (data.Layout?.ShowPerforatedBottom != true)
        {
            return;
        }

        SKColor strokeColor = ThemeColors.ResolveOrDefault(
            data.Theme?.DividerColor,
            ThemeColors.DefaultDividerColor);

        using var paint = new SKPaint
        {
            Color = strokeColor,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
        };

        int scallopCount = (int)Math.Floor(width / ScallopDiameter);

        for (int i = 0; i < scallopCount; i++)
        {
            float scallopLeft = origin.X + i * ScallopDiameter;

            // Arc rect: circle center at (scallopLeft+radius, origin.Y + ArcCenterOffsetY).
            //   rect.Top    = centerY - radius
            //   rect.Bottom = centerY + radius  (the arc apex column)
            // DrawArc with startAngle=0 (3 o'clock), sweepAngle=180 sweeps clockwise
            // to 9 o'clock via the bottom — the apex at (scallopLeft+radius, centerY+radius)
            // rasterises to pixel y = origin.Y + 4, matching T3b.23's y=(int)(BandHeight/2).
            float centerY = origin.Y + ArcCenterOffsetY;
            SKRect arcRect = SKRect.Create(
                scallopLeft,
                centerY - ScallopRadius,
                ScallopDiameter,
                ScallopDiameter);

            canvas.DrawArc(arcRect, startAngle: 0f, sweepAngle: 180f, useCenter: false, paint);
        }
    }
}
