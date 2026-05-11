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
    //             footerNote       = "We appreciate your trust."
    //             returnPolicy     = "Workmanship guaranteed 30 days."
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
        Assert.Contains("We appreciate your trust.", fullText, StringComparison.Ordinal);
        Assert.Contains("Workmanship guaranteed 30 days.", fullText, StringComparison.Ordinal);
        // Legal note: per-word presence check (not verbatim) per divergence #18.
        // Linux FreeType produces slightly wider glyph metrics than macOS, so the
        // long legal note wraps at width=360 on Linux; PdfPig concatenates wrap-row
        // boundaries by stripping whitespace, breaking the verbatim substring.
        // The four tokens below collectively appear on only one line of the sample
        // fixture (legalNote), so per-word presence still uniquely identifies it.
        Assert.Contains("computer", fullText, StringComparison.Ordinal);
        Assert.Contains("generated", fullText, StringComparison.Ordinal);
        Assert.Contains("signature", fullText, StringComparison.Ordinal);

        // Null thankYouMessage — only "Thank you!" must vanish.
        ReceiptData noThanks = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { ThankYouMessage = null },
        };
        Assert.DoesNotContain(
            "Thank you!",
            SectionTestBase.RenderSectionToPdfText(section, noThanks, fonts),
            StringComparison.Ordinal);

        // Null footerNote — only "We appreciate your trust." must vanish.
        ReceiptData noNote = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { FooterNote = null },
        };
        Assert.DoesNotContain(
            "We appreciate your trust.",
            SectionTestBase.RenderSectionToPdfText(section, noNote, fonts),
            StringComparison.Ordinal);

        // Null returnPolicy — only "Workmanship guaranteed 30 days." must vanish.
        ReceiptData noPolicy = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { ReturnPolicy = null },
        };
        Assert.DoesNotContain(
            "Workmanship guaranteed 30 days.",
            SectionTestBase.RenderSectionToPdfText(section, noPolicy, fonts),
            StringComparison.Ordinal);

        // Null legalNote — only the legal disclaimer must vanish. Use per-word
        // absence (matches the per-word presence relaxation above): "signature"
        // is unique to the legal note in the sample fixture, so its absence
        // proves the line was suppressed even when wrap-aware text extraction
        // mangles the surrounding tokens.
        ReceiptData noLegal = data with
        {
            Footer = (data.Footer ?? new FooterInfo()) with { LegalNote = null },
        };
        Assert.DoesNotContain(
            "signature",
            SectionTestBase.RenderSectionToPdfText(section, noLegal, fonts),
            StringComparison.Ordinal);
    }

    // T3b.21 — FooterSection renders both customFooterLines verbatim.
    //           Sample fixture customFooterLines: ["Follow us @keraniauto", "Visit kerani.example"].
    //           Both must appear in the extracted PDF text with ordinal comparison.
    [Fact]
    public void FooterSection_RendersCustomFooterLines_Verbatim()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new FooterSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Follow us @keraniauto", text, StringComparison.Ordinal);
        Assert.Contains("Visit kerani.example", text, StringComparison.Ordinal);
    }

    // T3b.21a — FooterSection skips blank/whitespace customFooterLines entries instead of
    //            reserving height for them. Defensive parity with the four optional message
    //            fields, which already short-circuit on `string.IsNullOrWhiteSpace`.
    //            Geometric assertion: Measure(["A","","B"]) == Measure(["A","B"]).
    [Fact]
    public void FooterSection_SkipsBlankCustomFooterLines_InMeasure()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();

        ReceiptData twoLines = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                CustomFooterLines = ["Follow us @keraniauto", "Visit kerani.example"],
            },
        };

        ReceiptData twoLinesWithBlanks = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                CustomFooterLines = ["Follow us @keraniauto", "", "   ", "Visit kerani.example"],
            },
        };

        var section = new FooterSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        Assert.Equal(
            section.Measure(Width, twoLines, ctx),
            section.Measure(Width, twoLinesWithBlanks, ctx));
    }

    // T3b.22a — Visual review V9.3 blocker B2: footer body lines that exceed the available
    //            content width must wrap rather than overflow. Currently a single long line
    //            (e.g. the legal note) renders past the canvas right edge. The fix routes
    //            every body line through TextMeasurer.WrapLines, summing wrapped line counts
    //            in Measure and drawing the wrapped lines in order in Draw. The geometric
    //            assertion: a long legal note at the same width must produce a strictly
    //            larger Measure than a short one, and the long line's text must be present
    //            in the rendered PDF (whitespace-normalized to tolerate cross-line breaks).
    [Fact]
    public void FooterSection_WrapsLongBodyLines_AtAvailableWidth()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();

        const string LongLine = "This is a deliberately long legal note used to verify that footer body text wraps at the configured content width when the line cannot fit on a single row.";
        const string ShortLine = "Short.";

        ReceiptData longData = baseData with
        {
            Footer = (baseData.Footer ?? new FooterInfo()) with
            {
                ThankYouMessage = null,
                FooterNote = null,
                ReturnPolicy = null,
                LegalNote = LongLine,
                CustomFooterLines = [],
            },
            Options = (baseData.Options ?? new ReceiptOptions()) with { ShowFooterContact = false },
        };

        ReceiptData shortData = longData with
        {
            Footer = (longData.Footer ?? new FooterInfo()) with { LegalNote = ShortLine },
        };

        var section = new FooterSection();
        using var fonts = new FontProvider();
        using var ctx = new RenderContext(fonts, resolvedLogo: null);

        const float NarrowWidth = 200f;
        float longHeight = section.Measure(NarrowWidth, longData, ctx);
        float shortHeight = section.Measure(NarrowWidth, shortData, ctx);

        Assert.True(
            longHeight > shortHeight,
            $"Expected wrap to make Measure(long) > Measure(short); got {longHeight} vs {shortHeight}");

        // PdfPig drops whitespace at line-wrap boundaries, so a verbatim substring
        // assertion on the whole long line is unsafe. Instead assert every word from
        // the source line is present in the extracted text — wrapping must preserve
        // every word, even if PdfPig joins them across lines.
        string text = SectionTestBase.RenderSectionToPdfText(section, longData, fonts, width: NarrowWidth);
        foreach (string word in LongLine.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Contains(word, text, StringComparison.Ordinal);
        }
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
