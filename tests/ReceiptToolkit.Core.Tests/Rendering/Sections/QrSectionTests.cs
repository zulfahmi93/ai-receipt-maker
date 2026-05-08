// Purpose: RED-phase tests for Phase 3b/D — QrSection renderer (T3b.18–T3b.19).
// Categories: Unit — section rendering, geometric height assertion via Measure for
//             toggle-driven QR visibility, pixel-mode assertion for QR matrix paint,
//             PDF text extraction for QR label text presence.
// Edge cases: ShowQrCode=false collapses Measure to 0f (T3b.18 geometric half);
//             ShowQrCode=true renders a QR matrix with at least one non-white pixel
//             in the finder-pattern region (T3b.18 pixel half);
//             qrCodeLabel "Scan to view receipt" appears verbatim in rendered PDF
//             text when ShowQrCode=true (T3b.19).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class QrSectionTests
{
    private const float Width = 360f;

    // T3b.18 — QrSection renders QR matrix only when ShowQrCode is true.
    //           Geometric half: Measure(ShowQrCode=true) > 0f; Measure(ShowQrCode=false) == 0f.
    //           Pixel half: render the ShowQrCode=true variant onto a white bitmap; sample
    //           a coord inside the top-left finder-pattern region of the QR matrix.
    //           QR finder patterns are always dark (black) modules at the three corners of
    //           the matrix; sampling near the center-top of the bitmap at approximately
    //           (width/2 - 4, 4) should land on the QR matrix regardless of vertical offset.
    //           Any non-white SKColor at that coord confirms a QR module was painted — we
    //           do not pin the exact color because QrSection chooses its own module color.
    //
    //           Coord rationale:
    //             x = (int)(Width / 2) - 4  — horizontal center-left, well inside the matrix
    //             y = 4                      — four pixels below origin; QR section draws the
    //                                          matrix starting near the top of the section's
    //                                          allocated area, so row 4 is inside the matrix.
    //           If the GREEN implementation adds top padding > 4px, the coord should be
    //           adjusted; document here so the .NET Expert sees the constraint.
    [Fact]
    public void QrSection_RendersQrMatrix_WhenShowQrCodeIsTrue()
    {
        ReceiptData dataWithQr = SectionTestBase.LoadSampleData();
        ReceiptData dataWithoutQr = dataWithQr with
        {
            Options = (dataWithQr.Options ?? new ReceiptOptions()) with { ShowQrCode = false },
        };

        var section = new QrSection();

        // ---- Geometric half ----
        (SKBitmap measureBitmap, RenderContext measureCtx, FontProvider measureFonts) =
            SectionTestBase.CreateBitmapContext(dataWithQr);
        using (measureBitmap)
        using (measureCtx)
        using (measureFonts)
        {
            Assert.True(
                section.Measure(Width, dataWithQr, measureCtx) > 0f,
                "Expected Measure(ShowQrCode=true) > 0f — QR section must claim height when enabled");

            Assert.Equal(
                0f,
                section.Measure(Width, dataWithoutQr, measureCtx));
        }

        // ---- Pixel half ----
        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) =
            SectionTestBase.CreateBitmapContext(dataWithQr);
        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            section.Draw(canvas, new SKPoint(0f, 0f), Width, dataWithQr, ctx);

            // Sample coord inside the QR top-left finder-pattern region.
            // The QR matrix is centred within the Width=360 receipt: qrLeft = (360-120)/2 = 120.
            // QrSection uses QrTopPadding=0 so the matrix starts at origin.Y=0.
            //
            // Finder-pattern geometry for QRCoder ECCLevel.L encoding
            // "https://example.com/receipt/INV-2025-06789":
            //   The QR version is deterministic for this value (~v3, 29 modules).
            //   cellSize = 120 / 29 ≈ 4.14 px.
            //   The top-left 7x7 finder pattern occupies modules [0..6, 0..6].
            //   Its centre 3x3 fill (always dark) spans modules [2..4, 2..4].
            //   Module (3, 3) centre pixel ≈ (qrLeft + 3.5*cellSize, 0 + 3.5*cellSize)
            //                              ≈ (120 + 14.5, 14.5) → pixel (134, 14).
            //   Modules (row=3, col=3) are GUARANTEED dark in any valid QR code.
            //   Original coord (176, 4) landed on a light module for this specific
            //   value; updated to the finder-pattern centre which is always dark.
            int x = 134;
            int y = 14;

            SKColor actualPixel = bitmap.GetPixel(x, y);

            Assert.NotEqual(SKColors.White, actualPixel);
        }
    }

    // T3b.19 — QrSection renders the qrCodeLabel verbatim below the QR matrix.
    //           Sample fixture has qrCodeLabel = "Scan to view receipt". The extracted
    //           PDF text must contain this string with an ordinal comparison; any
    //           normalisation (trimming, lowercasing) would cause test to fail, which
    //           is intentional — the label must be rendered as-is.
    [Fact]
    public void QrSection_RendersLabel_WhenShowQrCodeIsTrue()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new QrSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Scan to view receipt", text, StringComparison.Ordinal);
    }
}
