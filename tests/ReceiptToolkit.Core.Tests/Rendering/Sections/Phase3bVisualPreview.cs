// One-shot harness for Phase 3b visual review (V9.3 sign-off vs mockups/receipt.png).
// Gated on env var RECEIPT_VISUAL_PREVIEW=1 so it stays out of the regular test cycle.
// Stitches the 11 IReceiptSection implementations onto a single SKBitmap using the
// sample fixture, paints the paper background from the theme, and writes the PNG to
// the path indicated by RECEIPT_VISUAL_PREVIEW_OUT (default /tmp/receipt-3b-preview.png).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class Phase3bVisualPreview
{
    [Fact]
    public void RenderFullReceiptPreview()
    {
        if (Environment.GetEnvironmentVariable("RECEIPT_VISUAL_PREVIEW") != "1")
        {
            return;
        }

        string outPath = Environment.GetEnvironmentVariable("RECEIPT_VISUAL_PREVIEW_OUT")
            ?? "/tmp/receipt-3b-preview.png";

        ReceiptData data = SectionTestBase.LoadSampleData();
        ReceiptLayout layout = data.Layout ?? new ReceiptLayout();
        int width = layout.ReceiptWidth;
        float padding = layout.Padding;
        float sectionGap = layout.SectionGap ?? SkiaReceiptRenderer.DefaultSectionGap;
        float contentWidth = width - (2 * padding);

        IReceiptSection[] sections =
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

        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float[] heights = new float[sections.Length];
        float total = 0f;
        int visible = 0;
        for (int i = 0; i < sections.Length; i++)
        {
            heights[i] = sections[i].Measure(contentWidth, data, ctx);
            if (heights[i] > 0f)
            {
                if (visible > 0)
                {
                    total += sectionGap;
                }

                total += heights[i];
                visible++;
            }
        }

        int height = (int)Math.Ceiling((2 * padding) + total);

        SKColor paper = SKColor.TryParse(data.Theme?.PaperColor, out SKColor c) ? c : SKColors.White;

        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(paper);

            float y = padding;
            bool first = true;
            for (int i = 0; i < sections.Length; i++)
            {
                if (heights[i] <= 0f)
                {
                    continue;
                }

                if (!first)
                {
                    y += sectionGap;
                }

                sections[i].Draw(canvas, new SKPoint(padding, y), contentWidth, data, ctx);
                y += heights[i];
                first = false;
            }
        }

        using SKImage img = SKImage.FromBitmap(bitmap);
        using SKData encoded = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(outPath, encoded.ToArray());

        Assert.True(File.Exists(outPath), $"PNG not written to {outPath}");
    }
}
