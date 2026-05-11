// Purpose: RED-phase tests for Phase 3b/A — HeaderSection renderer (T3b.1–T3b.3).
// Categories: Unit — section rendering, PDF text extraction via PdfPig, geometric
//             height assertions for logo presence/absence (no pixel sampling at this layer).
// Edge cases: optional tagline (present vs null), optional logo (enabled vs disabled
//             toggle); both must shrink Measure() output without breaking the
//             always-shown business name.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class HeaderSectionTests
{
    // T3b.1 — HeaderSection draws business.businessName onto the PDF; PdfPig's
    //          extracted text from page 1 contains "Kerani Auto Workshop" verbatim.
    [Fact]
    public void HeaderSection_DrawsBusinessName()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new HeaderSection();

        string pdfText = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Kerani Auto Workshop", pdfText, StringComparison.Ordinal);
    }

    // T3b.2 — HeaderSection renders the tagline when business.businessTagline is non-null
    //          and skips it when null. Three assertions: with-tagline PDF text contains the
    //          tagline; without-tagline PDF text does NOT contain it; and the with-tagline
    //          Measure height is strictly greater than the without-tagline Measure height
    //          (proves the section actually reserved space for the tagline rather than
    //          drawing it on top of other content).
    [Fact]
    public void HeaderSection_TaglineAppearsWhenPresent_HiddenWhenNull()
    {
        ReceiptData withTagline = SectionTestBase.LoadSampleData();
        ReceiptData withoutTagline = withTagline with
        {
            Business = withTagline.Business with { BusinessTagline = null },
        };

        using var fonts = new FontProvider();
        var section = new HeaderSection();

        string textWith = SectionTestBase.RenderSectionToPdfText(section, withTagline, fonts);
        string textWithout = SectionTestBase.RenderSectionToPdfText(section, withoutTagline, fonts);

        const float Width = 360f;
        float heightWith;
        float heightWithout;
        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightWith = section.Measure(Width, withTagline, ctx);
        }

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightWithout = section.Measure(Width, withoutTagline, ctx);
        }

        Assert.Contains("Honest work, honest price.", textWith, StringComparison.Ordinal);
        Assert.DoesNotContain("Honest work, honest price.", textWithout, StringComparison.Ordinal);
        Assert.True(
            heightWith > heightWithout,
            $"Expected Measure(with tagline) > Measure(without tagline); got {heightWith} vs {heightWithout}");
    }

    // T3b.3 — HeaderSection draws the logo when options.showLogo=true AND a non-null
    //          ResolvedLogo is supplied via RenderContext, and skips the logo when
    //          options.showLogo=false (regardless of the resolved image). Geometric
    //          assertion only: Measure(enabled) > Measure(disabled), and Measure(disabled)
    //          is still > 0 because the business name is unconditional. We don't assert on
    //          logo bytes — that belongs to a pixel-mode test in a future sub-cluster.
    [Fact]
    public void HeaderSection_LogoAppearsWhenEnabled_HiddenWhenDisabled()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        // The fixture has businessLogoUrl=null; flip it to a non-null sentinel so the
        // "logo configured" branch lands. The actual image bytes come from the resolved
        // SKImage (built below), not from the URL — LogoResolver isn't exercised here.
        ReceiptData enabled = baseData with
        {
            Business = baseData.Business with { BusinessLogoUrl = "anything" },
            Options = (baseData.Options ?? new ReceiptOptions()) with { ShowLogo = true },
        };
        ReceiptData disabled = enabled with
        {
            Options = enabled.Options! with { ShowLogo = false },
        };

        using var fonts = new FontProvider();
        var section = new HeaderSection();

        // Build a tiny in-memory SKImage to stand in for a resolved logo.
        using var bitmap = new SKBitmap(32, 32);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
        }

        const float Width = 360f;
        float heightEnabled;
        float heightDisabled;

        // RenderContext now treats ResolvedLogo as caller-owned (Phase 3e flip), so each
        // SKImage handle gets its own outer `using`. Without it the handle would leak
        // to the finalizer queue.
        using (SKImage logoEnabled = SKImage.FromBitmap(bitmap))
        using (var ctx = new RenderContext(fonts, logoEnabled))
        {
            heightEnabled = section.Measure(Width, enabled, ctx);
        }

        using (SKImage logoDisabled = SKImage.FromBitmap(bitmap))
        using (var ctx = new RenderContext(fonts, logoDisabled))
        {
            heightDisabled = section.Measure(Width, disabled, ctx);
        }

        Assert.True(
            heightEnabled > heightDisabled,
            $"Expected Measure(showLogo=true) > Measure(showLogo=false); got {heightEnabled} vs {heightDisabled}");
        Assert.True(
            heightDisabled > 0f,
            $"Expected Measure(showLogo=false) > 0 (business name is unconditional); got {heightDisabled}");
    }
}
