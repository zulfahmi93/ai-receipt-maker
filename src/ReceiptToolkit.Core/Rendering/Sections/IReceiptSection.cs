using ReceiptToolkit.Contracts;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   A single visual band of a rendered receipt (header, item table, totals, footer, ...).
/// </summary>
/// <remarks>
///   <para>
///     Sections follow a two-pass layout protocol:
///     <list type="number">
///       <item><description>
///         <see cref="Measure"/> reports the height the section will occupy at the given
///         width without drawing anything.  The renderer uses these heights to compute
///         page break points and origin positions.
///       </description></item>
///       <item><description>
///         <see cref="Draw"/> paints the section onto the canvas at the chosen origin.
///         The painted output must not exceed the height returned by the matching
///         <see cref="Measure"/> call for the same <c>width</c> / <see cref="ReceiptData"/>
///         / <see cref="RenderContext"/> inputs.
///       </description></item>
///     </list>
///   </para>
///   <para>
///     Conditional sections (e.g. logo when <c>options.showLogo=false</c>, QR when
///     <c>options.showQrCode=false</c>, perforation when
///     <c>layout.showPerforatedBottom=false</c>) signal omission by returning <c>0f</c>
///     from <see cref="Measure"/> and performing no canvas draw operations in
///     <see cref="Draw"/>.  Callers rely on this exact contract — a <c>0f</c> measure
///     means the section contributes no vertical space and should not be visited.
///   </para>
///   <para>
///     Sections are stateless beyond their own configuration.  All per-render inputs are
///     supplied via the <c>data</c> and <c>ctx</c> parameters; no JSON values, theme
///     colours, or layout dimensions may be hard-coded inside an implementation.
///   </para>
/// </remarks>
public interface IReceiptSection
{
    /// <summary>
    ///   Reports the vertical space (in pixels) the section will occupy when drawn at the
    ///   given <paramref name="width"/>.
    /// </summary>
    /// <param name="width">The available content width in pixels.</param>
    /// <param name="data">The full receipt model; the section reads its inputs from here.</param>
    /// <param name="ctx">Per-render resources (fonts, resolved logo, ...).</param>
    /// <returns>
    ///   The required height in pixels, or <c>0f</c> when the section is omitted for
    ///   the given inputs (e.g. a disabled toggle, a missing optional field).
    /// </returns>
    float Measure(float width, ReceiptData data, RenderContext ctx);

    /// <summary>
    ///   Paints the section onto <paramref name="canvas"/> starting at
    ///   <paramref name="origin"/>.
    /// </summary>
    /// <param name="canvas">The target canvas.</param>
    /// <param name="origin">The top-left corner at which to begin drawing.</param>
    /// <param name="width">The available content width in pixels.</param>
    /// <param name="data">The full receipt model; the section reads its inputs from here.</param>
    /// <param name="ctx">Per-render resources (fonts, resolved logo, ...).</param>
    /// <remarks>
    ///   When <see cref="Measure"/> would return <c>0f</c> for the same inputs, this method
    ///   must perform no draw operations.
    /// </remarks>
    void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx);
}
