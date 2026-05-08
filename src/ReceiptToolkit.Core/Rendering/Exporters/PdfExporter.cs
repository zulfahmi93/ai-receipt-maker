using ReceiptToolkit.Contracts;
using ReceiptToolkit.Contracts.Time;
using ReceiptToolkit.Core.Rendering.Assets;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Exporters;

/// <summary>
///   Exports a <see cref="ReceiptData"/> instance to a single-document PDF byte stream.
/// </summary>
/// <remarks>
///   <para>
///     The receipt is composed once via <see cref="SkiaReceiptRenderer"/> at the layout's
///     configured <c>ReceiptWidth</c>, then split across one or more PDF pages of fixed
///     <see cref="PageHeight"/>. Mid-section splits are accepted for MVP — pagination is
///     strip-based, not section-aware.
///   </para>
///   <para>
///     The PDF <c>/CreationDate</c> metadata is taken from the injected <see cref="IClock"/>
///     so deterministic tests can produce byte-equal output across runs.
///   </para>
/// </remarks>
public sealed class PdfExporter
{
    /// <summary>Default PDF page height in pixels. Sized so the bundled
    ///   <c>sample_receipt_data.json</c> (~1040px composed) fits in one page with
    ///   headroom; long-items fixtures spill across two or more pages.</summary>
    public const int DefaultPageHeight = 1200;

    private readonly IClock _clock;
    private readonly FontProvider _fonts;

    /// <summary>
    ///   Initialises a new <see cref="PdfExporter"/>.
    /// </summary>
    /// <param name="clock">Source of the document creation timestamp.</param>
    /// <param name="fonts">Caller-owned font provider; not disposed by this class.</param>
    /// <param name="pageHeight">Fixed page height in pixels. Defaults to <see cref="DefaultPageHeight"/>.</param>
    public PdfExporter(IClock clock, FontProvider fonts, int pageHeight = DefaultPageHeight)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(fonts);
        if (pageHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageHeight), pageHeight, "PageHeight must be positive.");
        }

        _clock = clock;
        _fonts = fonts;
        PageHeight = pageHeight;
    }

    /// <summary>The fixed PDF page height in pixels.</summary>
    public int PageHeight { get; }

    /// <summary>
    ///   Renders <paramref name="data"/> to a PDF byte stream.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <returns>The PDF bytes (starting with the <c>%PDF-</c> magic).</returns>
    public byte[] Export(ReceiptData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Capture the clock once so Creation and Modified metadata stay consistent
        // even if the underlying clock advances between reads.
        DateTime now = _clock.UtcNow.UtcDateTime;

        // TODO(T3e.x): resolve data.Business.businessLogoUrl into RenderContext via
        // LogoResolver. Phase 3d emits logo-less receipts even when the JSON declares
        // a logo URL — the ReceiptGenerator façade owns logo resolution.
        using var ctx = new RenderContext(_fonts, resolvedLogo: null);
        var renderer = new SkiaReceiptRenderer();
        SKSize size = renderer.Measure(data, ctx);

        int pageCount = Math.Max(1, (int)Math.Ceiling(size.Height / (double)PageHeight));

        using var stream = new MemoryStream();
        var metadata = new SKDocumentPdfMetadata
        {
            Creation = now,
            Modified = now,
        };

        using (var document = SKDocument.CreatePdf(stream, metadata))
        {
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                using SKCanvas canvas = document.BeginPage(size.Width, PageHeight);
                // Shift the entire composition up by pageIndex * PageHeight so each
                // page surfaces its own strip of the full receipt. Mid-row splits are
                // accepted for MVP (strip-based pagination, not section-aware).
                canvas.Translate(0, -pageIndex * (float)PageHeight);
                renderer.Render(canvas, data, ctx);
                document.EndPage();
            }

            document.Close();
        }

        return stream.ToArray();
    }
}
