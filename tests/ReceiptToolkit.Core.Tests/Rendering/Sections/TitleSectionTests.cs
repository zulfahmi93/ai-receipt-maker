// Purpose: RED-phase test for Phase 3b/A — TitleSection renderer (T3b.4).
// Categories: Unit — section rendering, PDF text extraction via PdfPig.
// Edge cases: receipt.receiptTitle drives the rendered string; the section never
//             hard-codes "RECEIPT" so a JSON change to "INVOICE" or "ORDER" would
//             flow through unchanged. This test only covers the sample fixture
//             value; toggle coverage lands on the .NET Expert via additional cases.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class TitleSectionTests
{
    // T3b.4 — TitleSection draws the centered receipt.receiptTitle. PdfPig's text
    //          extraction loses centering geometry but preserves glyphs, so the
    //          assertion is value-based: extracted text contains "RECEIPT".
    [Fact]
    public void TitleSection_DrawsCenteredReceiptTitle()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new TitleSection();

        string pdfText = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("RECEIPT", pdfText, StringComparison.Ordinal);
    }
}
