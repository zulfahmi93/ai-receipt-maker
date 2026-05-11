// Purpose: RED-phase tests for Phase 3c-polish E (T3cP.9) — FooterSection vertical
//          rhythm tightening: LineSpacingFactor 1.15 → 1.1 and LegalNote uses a
//          smaller LegalNoteFontSize (9f) vs other body lines (BodyFontSize 11f).
// Categories: Unit — section geometry assertions via Measure.
// Strategy:
//   T3cP.9a (TightLineSpacing): Three body lines at both narrow and normal width.
//              Measure must be strictly less after the spacing-factor reduction.
//              Baseline captured against the CURRENT implementation (LineHeight=14f,
//              LineGap=2f); after GREEN the test should pass, so we assert the new
//              expected value.
//   T3cP.9b (LegalNoteSmaller): legal-note-only fixture vs body-note-only fixture.
//              Measure(legalOnly) <= Measure(bodyOnly) at same width, because the
//              legal note line-height contribution is smaller. At a width wide enough
//              that neither wraps, the difference in Measure will reflect the smaller
//              per-line slot for the legal note.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class FooterRhythmTests
{
    private const float Width = 360f;

    // T3cP.9a — Tight line spacing: after the 1.15→1.1 factor change the Measure
    // result for a 3-body-line footer must decrease vs the legacy 1.15 formula.
    // The legacy formula (LineHeight=14, LineGap=2) gave:
    //   3 lines * 14 + 2 lines * 2 = 46
    // The new formula uses LineSpacingFactor: each line slot = fontSize * LineSpacingFactor.
    // With BodyFontSize=11 and factor=1.1: slot = 12.1; spacing = slot - fontSize = 1.1.
    // 3 lines * 12.1 + 2 * 1.1 = 36.3 + 2.2 = 38.5
    // So Measure(3 body lines, no contact) must be < 46 after GREEN.
    // We assert < 46f as the proxy for "spacing got tighter" without pinning to exact float.
    [Fact]
    public void FooterSection_TightLineSpacing_MeasureSmallerThanLegacy()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();

        // Three body lines, no contact, no thank-you (which uses different font size).
        ReceiptData threeLines = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                ThankYouMessage = null,
                FooterNote = "Line one for spacing test.",
                ReturnPolicy = "Line two for spacing test.",
                LegalNote = null,
                CustomFooterLines = ["Line three for spacing test."],
            },
            Options = (baseData.Options ?? new ReceiptOptions()) with { ShowFooterContact = false },
        };

        var section = new FooterSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float measured = section.Measure(Width, threeLines, ctx);

        // Legacy LineHeight=14, LineGap=2 → 3*14 + 2*2 = 46.
        // New formula with factor 1.1 on 11pt body → well below 46.
        Assert.True(
            measured < 46f,
            $"Expected Measure(3 body lines) < 46 (legacy value) after spacing tighten; got {measured}");
    }

    // T3cP.9b — LegalNote smaller font: legal-note-only footer measures less than
    // a body-note-only footer at a width where neither wraps (Width=360).
    // BodyFontSize=11f uses slot = 11 * 1.1 = 12.1
    // LegalNoteFontSize=9f uses slot = 9 * 1.1 = 9.9
    // So Measure(legalOnly) < Measure(bodyNoteOnly).
    [Fact]
    public void FooterSection_LegalNoteUsesSmaller_FontSize_ThanBodyNote()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();

        // Single legal-note line only.
        ReceiptData legalOnly = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                ThankYouMessage = null,
                FooterNote = null,
                ReturnPolicy = null,
                LegalNote = "Short legal.",
                CustomFooterLines = [],
            },
            Options = (baseData.Options ?? new ReceiptOptions()) with { ShowFooterContact = false },
        };

        // Single body-note line (same text length, same wrap behaviour).
        ReceiptData bodyNoteOnly = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                ThankYouMessage = null,
                FooterNote = "Short legal.",
                ReturnPolicy = null,
                LegalNote = null,
                CustomFooterLines = [],
            },
            Options = (baseData.Options ?? new ReceiptOptions()) with { ShowFooterContact = false },
        };

        var section = new FooterSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        float legalHeight = section.Measure(Width, legalOnly, ctx);
        float bodyHeight = section.Measure(Width, bodyNoteOnly, ctx);

        Assert.True(
            legalHeight < bodyHeight,
            $"Expected Measure(legalOnly)={legalHeight} < Measure(bodyNoteOnly)={bodyHeight} — legal note uses smaller font → smaller slot");
    }
}
