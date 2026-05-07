using QRCoder;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering;

/// <summary>
///   Paints a QR code directly onto an <see cref="SKCanvas"/> as filled module rectangles.
/// </summary>
/// <remarks>
///   <para>
///     Rendering is performed by iterating the QR module matrix and drawing each set
///     module as a filled <see cref="SKRect"/> — no PNG round-trip is used.
///   </para>
///   <para>
///     Anti-aliasing is disabled so module boundaries remain sharp and pixel-accurate.
///     Quiet-zone padding is not added; the caller is responsible for inset sizing of
///     the <c>rect</c> argument passed to <see cref="Paint"/>.
///   </para>
/// </remarks>
public static class QrPainter
{
    /// <summary>
    ///   Paints a QR code for <paramref name="value"/> into <paramref name="rect"/>
    ///   on <paramref name="canvas"/> using <paramref name="moduleColor"/> for set modules.
    /// </summary>
    /// <param name="canvas">The target canvas to draw onto.</param>
    /// <param name="value">
    ///   The string value to encode as a QR code.  Must not be <see langword="null"/>,
    ///   empty, or whitespace.
    /// </param>
    /// <param name="rect">
    ///   The bounding rectangle for the QR code.  The rect is assumed to be square;
    ///   non-square rects will produce non-square modules.
    /// </param>
    /// <param name="moduleColor">
    ///   The fill colour for set (dark) modules.  Unset (light) modules are not painted,
    ///   so the canvas background shows through.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   Thrown when <paramref name="value"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public static void Paint(SKCanvas canvas, string value, SKRect rect, SKColor moduleColor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.L);

        int matrixDim = qrData.ModuleMatrix.Count;
        float cellSize = rect.Width / matrixDim;

        using var paint = new SKPaint
        {
            Color = moduleColor,
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        };

        for (int r = 0; r < matrixDim; r++)
        {
            for (int c = 0; c < matrixDim; c++)
            {
                if (qrData.ModuleMatrix[r][c])
                {
                    canvas.DrawRect(
                        SKRect.Create(
                            rect.Left + c * cellSize,
                            rect.Top + r * cellSize,
                            cellSize,
                            cellSize),
                        paint);
                }
            }
        }
    }
}
