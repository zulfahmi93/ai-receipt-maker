// Purpose: Shared helpers for Phase 3b section-renderer tests — fixture loading,
//          single-section PDF rendering with PdfPig text extraction, and bitmap
//          context construction for pixel-mode sub-clusters.
// Categories: Test infrastructure — wraps SkiaSharp + PdfPig boilerplate so each
//             section test stays focused on the IReceiptSection contract.
// Edge cases: handles 0f Measure (forces height = 1f to keep PDF page valid);
//             RenderContext lifetime is owned here (caller never sees it directly).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

/// <summary>
///   Shared helpers for Phase 3b section-renderer tests.
/// </summary>
internal static class SectionTestBase
{
    /// <summary>
    ///   Loads <c>Fixtures/sample_receipt_data.json</c> from the test output directory
    ///   (copied via the csproj <c>Content Include</c>) and deserialises it via
    ///   <see cref="ReceiptData.FromJson"/>.
    /// </summary>
    public static ReceiptData LoadSampleData()
    {
        string fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "sample_receipt_data.json");

        string json = File.ReadAllText(fixturePath);
        return ReceiptData.FromJson(json);
    }

    /// <summary>
    ///   Renders <paramref name="section"/> alone onto a single-page PDF, opens the PDF
    ///   via PdfPig, and returns the extracted text from page 1.
    /// </summary>
    /// <param name="section">The section under test.</param>
    /// <param name="data">The receipt model fed to the section.</param>
    /// <param name="fonts">Caller-owned font provider.</param>
    /// <param name="resolvedLogo">
    ///   Optional resolved logo image. Ownership transfers to the
    ///   <see cref="RenderContext"/> created here and is disposed when the helper exits.
    /// </param>
    /// <param name="width">Content width passed to Measure/Draw. Default 360.</param>
    public static string RenderSectionToPdfText(
        IReceiptSection section,
        ReceiptData data,
        FontProvider fonts,
        SKImage? resolvedLogo = null,
        float width = 360f)
    {
        using var ctx = new RenderContext(fonts, resolvedLogo);

        // PDF pages need positive dimensions even when the section is omitted (Measure → 0f).
        float measured = section.Measure(width, data, ctx);
        float pageHeight = Math.Max(measured, 1f);

        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            using SKCanvas canvas = document.BeginPage(width, pageHeight);
            canvas.Clear(SKColors.White);
            section.Draw(canvas, new SKPoint(0f, 0f), width, data, ctx);
            document.EndPage();
            document.Close();
        }

        byte[] pdfBytes = stream.ToArray();
        using PdfDocument pdf = PdfDocument.Open(pdfBytes);
        Page page = pdf.GetPage(1);
        return page.Text;
    }

    /// <summary>
    ///   Allocates an <see cref="SKBitmap"/> sized for full-receipt rendering plus the
    ///   <see cref="RenderContext"/> and <see cref="FontProvider"/> used by future
    ///   pixel-mode sub-clusters (C/D). The caller wraps the bitmap in an
    ///   <see cref="SKCanvas"/> externally and is responsible for disposing all
    ///   returned values.
    /// </summary>
    public static (SKBitmap Bitmap, RenderContext Context, FontProvider Fonts) CreateBitmapContext(
        ReceiptData data,
        int width = 360,
        int height = 1200)
    {
        _ = data;

        var fonts = new FontProvider();
        var bmp = new SKBitmap(width, height);
        var ctx = new RenderContext(fonts, resolvedLogo: null);
        return (bmp, ctx, fonts);
    }
}
