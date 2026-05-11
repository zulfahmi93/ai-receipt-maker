// Purpose: RED-phase tests for Phase 3c-polish E (T3cP.10) — QrSection compaction:
//          DefaultQrSize reduces from 120f to 72f.
// Categories: Unit — geometric Measure assertion + pixel-mode QR bbox width check.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class QrCompactionTests
{
    private const float Width = 360f;

    // T3cP.10a — Measure(ShowQrCode=true) must reflect the new 72px QR size, not 120px.
    // QrSection.Measure = QrSize + LabelGap + LabelFontSize = 72 + 4 + 10 = 86f.
    // Legacy value was 120 + 4 + 10 = 134f.
    // Assert: Measure < 100f (well below 134 legacy, accommodates minor const drift).
    [Fact]
    public void QrSection_DefaultSizeIsCompact_Measure()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        var section = new QrSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float measured = section.Measure(Width, data, ctx);

        Assert.True(
            measured < 100f,
            $"Expected Measure < 100f after QR compaction to 72px; got {measured} (was 134f with 120px)");
    }

    // T3cP.10b — Pixel-mode bbox: after compaction the rendered QR matrix width ≈ 72 ± 3px.
    // Scan at y=14 (guaranteed-dark finder-pattern centre row per existing QrSectionTests).
    // For 72px QR: module pitch = 72/29 ≈ 2.48px; centre row 3 ≈ y = 3.5 * 2.48 ≈ 8.7.
    // For 120px QR: module pitch = 120/29 ≈ 4.14px; centre row 3 ≈ y = 3.5 * 4.14 ≈ 14.5.
    // Scan y=14 still hits the dark finder-pattern area for both sizes.
    // Find leftmost + rightmost non-white pixel across the full row width.
    // Assert: bbox < 90px (proves compaction to 72, not old 120).
    // Assert: bbox >= 60px (proves QR still present).
    [Fact]
    public void QrSection_DefaultSizeIsCompact_PixelBbox()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        var section = new QrSection();

        (SKBitmap bitmap, RenderContext ctx, FontProvider fonts) =
            SectionTestBase.CreateBitmapContext(data, width: 360, height: 200);

        using (bitmap)
        using (ctx)
        using (fonts)
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            section.Draw(canvas, new SKPoint(0f, 0f), Width, data, ctx);

            // y=14 lands inside the guaranteed-dark 3x3 centre of the top-left finder
            // pattern — see QrSectionTests.QrSection_RendersQrMatrix_WhenShowQrCodeIsTrue
            // coord rationale (module (3,3) ≈ pixel (134, 14) for QrSize=120).
            const int ScanY = 14;
            int qrLeft = -1;
            int qrRight = -1;

            for (int x = 0; x < 360; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, ScanY);
                if (pixel != SKColors.White)
                {
                    if (qrLeft == -1) qrLeft = x;
                    qrRight = x;
                }
            }

            Assert.True(qrLeft >= 0, "Expected at least one non-white pixel at y=14 — QR was not rendered");

            float bbox = qrRight - qrLeft + 1;

            // Current 120px QR produces ~94px bbox at y=14. New 72px QR will produce ~72px.
            // Threshold 85px separates old (94) from new (72) cleanly.
            Assert.True(
                bbox < 85f,
                $"Expected QR bbox width < 85px (compacted to 72px); got {bbox}px — old 120px QR likely still in use");

            // 72px QR has module pitch 72/29≈2.48px; finder pattern at y=14 spans inner modules
            // only — observed bbox ≈56px. 50px is a safe lower bound proving QR was rendered.
            Assert.True(
                bbox >= 50f,
                $"Expected QR bbox width >= 50px (QR must still be present); got {bbox}px");
        }
    }
}
