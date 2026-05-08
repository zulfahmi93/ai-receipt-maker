// Purpose: RED-phase tests for Phase 3b/D — FooterSection renderer (T3b.20–T3b.22).
// Categories: Unit — section rendering, PDF text extraction via PdfPig for footer
//             message text presence and selective null-suppression, geometric height
//             assertion for contact-block toggle.
// Edge cases: all four footer messages render verbatim from sample fixture (T3b.20);
//             nulling each message field individually removes only that string from
//             the rendered PDF text (T3b.20 null-one-at-a-time);
//             customFooterLines renders both lines verbatim (T3b.21);
//             ShowFooterContact=false collapses the contact block so Measure decreases
//             (T3b.22) — the contact block must not reserve height when hidden.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class FooterSectionTests
{
    private const float Width = 360f;

    // T3b.20 — FooterSection renders all four optional messages verbatim, and each
    //           message disappears independently when its source field is nulled.
    //           The fixture provides:
    //             thankYouMessage  = "Thank you!"
    //             footerNote       = "We appreciate your support."
    //             returnPolicy     = "Returns accepted within 7 days with receipt."
    //             legalNote        = "This receipt is computer generated and does not require a signature."
    //           The nullable-parent guard `(data.Footer ?? new FooterInfo()) with { … }` is
    //           required because ReceiptData.Footer is FooterInfo? — a bare `data.Footer with`
    //           would emit CS8602 under TreatWarningsAsErrors.
    [Fact]
    public void FooterSection_RendersOptionalMessages_VerbatimNullableOneAtATime()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new FooterSection();

        // Full render — all four messages must be present.
        string fullText = SectionTestBase.RenderSectionToPdfText(section, data, fonts);
        Assert.Contains("Thank you!", fullText, StringComparison.Ordinal);
        Assert.Contains("We appreciate your support.", fullText, StringComparison.Ordinal);
        Assert.Contains("Returns accepted within 7 days with receipt.", fullText, StringComparison.Ordinal);
        Assert.Contains("This receipt is computer generated and does not require a signature.", fullText, StringComparison.Ordinal);

        // Null thankYouMessage — only "Thank you!" must vanish.
        ReceiptData noThanks = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { ThankYouMessage = null },
        };
        Assert.DoesNotContain(
            "Thank you!",
            SectionTestBase.RenderSectionToPdfText(section, noThanks, fonts),
            StringComparison.Ordinal);

        // Null footerNote — only "We appreciate your support." must vanish.
        ReceiptData noNote = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { FooterNote = null },
        };
        Assert.DoesNotContain(
            "We appreciate your support.",
            SectionTestBase.RenderSectionToPdfText(section, noNote, fonts),
            StringComparison.Ordinal);

        // Null returnPolicy — only "Returns accepted within 7 days with receipt." must vanish.
        ReceiptData noPolicy = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { ReturnPolicy = null },
        };
        Assert.DoesNotContain(
            "Returns accepted within 7 days with receipt.",
            SectionTestBase.RenderSectionToPdfText(section, noPolicy, fonts),
            StringComparison.Ordinal);

        // Null legalNote — only the legal disclaimer must vanish.
        ReceiptData noLegal = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { LegalNote = null },
        };
        Assert.DoesNotContain(
            "This receipt is computer generated and does not require a signature.",
            SectionTestBase.RenderSectionToPdfText(section, noLegal, fonts),
            StringComparison.Ordinal);
    }

    // T3b.21 — FooterSection renders both customFooterLines verbatim.
    //           Sample fixture customFooterLines: ["Follow us @elevatestudio", "Visit elevatestudio.com"].
    //           Both must appear in the extracted PDF text with ordinal comparison.
    [Fact]
    public void FooterSection_RendersCustomFooterLines_Verbatim()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new FooterSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Follow us @elevatestudio", text, StringComparison.Ordinal);
        Assert.Contains("Visit elevatestudio.com", text, StringComparison.Ordinal);
    }

    // T3b.22 — FooterSection collapses the contact block when ShowFooterContact is false.
    //           The sample has ShowFooterContact=true; mutating it to false via the nullable-
    //           parent guard must produce a strictly smaller Measure height, proving no height
    //           is reserved for the contact block when the toggle is off.
    [Fact]
    public void FooterSection_HidesContactBlock_WhenShowFooterContactIsFalse()
    {
        ReceiptData withContact = SectionTestBase.LoadSampleData();
        ReceiptData withoutContact = withContact with
        {
            Options = (withContact.Options ?? new ReceiptOptions()) with { ShowFooterContact = false },
        };

        var section = new FooterSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.True(
            section.Measure(Width, withContact, ctx) > section.Measure(Width, withoutContact, ctx),
            "Expected Measure(showFooterContact=true) > Measure(showFooterContact=false) — contact block must collapse when toggle is off");
    }
}
