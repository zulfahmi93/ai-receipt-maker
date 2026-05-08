using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Assets;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Exporters;

/// <summary>
///   Exports a <see cref="ReceiptData"/> instance to an SVG byte stream.
/// </summary>
/// <remarks>
///   <para>
///     The receipt is composed once via <see cref="SkiaReceiptRenderer"/> against
///     an <see cref="SkiaSharp.SKSvgCanvas"/>, capturing every draw call as SVG
///     primitives (paths, text, image references). Output is UTF-8 XML starting with
///     the <c>&lt;svg</c> root tag.
///   </para>
///   <para>
///     Drop-shadow is intentionally suppressed for SVG: the
///     <see cref="RenderContext.EmitShadow"/> flag is left at its default
///     <see langword="false"/> so vector consumers (web embeds, design tools) receive
///     a flush-edged composition. Raster exporters (PNG) opt in to shadow separately.
///   </para>
/// </remarks>
public sealed class SvgExporter
{
    private readonly FontProvider _fonts;

    /// <summary>
    ///   Initialises a new <see cref="SvgExporter"/>.
    /// </summary>
    /// <param name="fonts">Caller-owned font provider; not disposed by this class.</param>
    public SvgExporter(FontProvider fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        _fonts = fonts;
    }

    /// <summary>
    ///   Renders <paramref name="data"/> to an SVG byte stream.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <returns>The SVG bytes (UTF-8 XML starting with <c>&lt;svg</c>).</returns>
    public byte[] Export(ReceiptData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // TODO(T3e.x): resolve data.Business.businessLogoUrl into RenderContext via
        // LogoResolver. Phase 3d emits logo-less SVGs even when the JSON declares a
        // logo URL — the ReceiptGenerator façade owns logo resolution.
        using var ctx = new RenderContext(_fonts, resolvedLogo: null);
        var renderer = new SkiaReceiptRenderer();
        SKSize canvasSize = renderer.Measure(data, ctx);

        using var stream = new MemoryStream();
        using (var managed = new SKManagedWStream(stream))
        {
            SKRect bounds = new(0, 0, canvasSize.Width, canvasSize.Height);
            using SKCanvas canvas = SKSvgCanvas.Create(bounds, managed);
            renderer.Render(canvas, data, ctx);
        } // disposes canvas first (flushes SVG), then disposes managed wrapper.

        return stream.ToArray();
    }
}
