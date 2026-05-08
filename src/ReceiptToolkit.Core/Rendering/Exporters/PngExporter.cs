using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Assets;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Exporters;

/// <summary>
///   Exports a <see cref="ReceiptData"/> instance to a single PNG byte stream.
/// </summary>
/// <remarks>
///   <para>
///     The receipt is composed once via <see cref="SkiaReceiptRenderer"/>, rasterised
///     into an <c>SKBitmap</c> sized at <c>canvasSize × Scale</c> for crisp text, and
///     encoded to PNG. <see cref="Scale"/> defaults to <c>2</c> so the rendered
///     output is hi-DPI by default.
///   </para>
///   <para>
///     Drop-shadow emission defers to the renderer composer via
///     <see cref="RenderContext.EmitShadow"/>. The exporter sets the flag to
///     <see cref="EmitShadow"/> at the start of every call so PDF/SVG paths remain
///     shadow-free even when the same <see cref="RenderContext"/> instance is reused.
///   </para>
/// </remarks>
public sealed class PngExporter
{
    /// <summary>Default raster scale factor (hi-DPI).</summary>
    public const int DefaultScale = 2;

    private readonly FontProvider _fonts;

    /// <summary>
    ///   Initialises a new <see cref="PngExporter"/>.
    /// </summary>
    /// <param name="fonts">Caller-owned font provider; not disposed by this class.</param>
    /// <param name="scale">Raster scale factor. Defaults to <see cref="DefaultScale"/>.</param>
    /// <param name="emitShadow">
    ///   When <see langword="true"/> (default), the receipt is rendered with a drop-shadow
    ///   border. Tests that pin exact bitmap dimensions to <c>receiptWidth × scale</c>
    ///   must opt out by passing <see langword="false"/>.
    /// </param>
    public PngExporter(FontProvider fonts, int scale = DefaultScale, bool emitShadow = true)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be positive.");
        }

        _fonts = fonts;
        Scale = scale;
        EmitShadow = emitShadow;
    }

    /// <summary>The raster scale factor applied to the bitmap dimensions.</summary>
    public int Scale { get; }

    /// <summary>
    ///   When <see langword="true"/>, the exporter requests a drop-shadow border from
    ///   the composer; the bitmap is sized to include the shadow margin.
    /// </summary>
    public bool EmitShadow { get; }

    /// <summary>
    ///   Renders <paramref name="data"/> to a PNG byte stream with no logo image.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <returns>The PNG bytes (starting with the <c>89 50 4E 47</c> magic).</returns>
    public byte[] Export(ReceiptData data) => Export(data, resolvedLogo: null);

    /// <summary>
    ///   Renders <paramref name="data"/> to a PNG byte stream using the supplied
    ///   pre-resolved logo image.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="resolvedLogo">
    ///   Logo image already resolved from <c>business.businessLogoUrl</c>, or
    ///   <see langword="null"/> for a logo-less render. The caller retains ownership
    ///   of the handle; this method does not dispose it.
    /// </param>
    /// <returns>The PNG bytes (starting with the <c>89 50 4E 47</c> magic).</returns>
    public byte[] Export(ReceiptData data, SKImage? resolvedLogo)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var ctx = new RenderContext(_fonts, resolvedLogo) { EmitShadow = EmitShadow };
        var renderer = new SkiaReceiptRenderer();
        SKSize canvasSize = renderer.Measure(data, ctx);

        // Apply the scale factor at bitmap-allocation time and compose via
        // canvas.Scale(...) so renderer math stays in logical pixels — each logical
        // pixel maps to Scale × Scale device pixels for hi-DPI crispness.
        int width = (int)Math.Ceiling(canvasSize.Width * Scale);
        int height = (int)Math.Ceiling(canvasSize.Height * Scale);

        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Scale(Scale, Scale);
            renderer.Render(canvas, data, ctx);
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded.ToArray();
    }
}
