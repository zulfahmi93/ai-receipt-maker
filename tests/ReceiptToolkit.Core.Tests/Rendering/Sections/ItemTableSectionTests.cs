// Purpose: RED-phase tests for Phase 3b/B — ItemTableSection renderer (T3b.7–T3b.10).
// Categories: Unit — section rendering, PDF text extraction via PdfPig, geometric height
//             assertions via Measure, toggle-driven column/sub-line visibility.
// Edge cases: 5-item fixture — all item names must appear (T3b.7); multi-word long name
//             wraps to ≥2 lines causing strictly greater height than narrow baseline (T3b.8);
//             ShowSku toggle controls SKU column appearance + text presence (T3b.9);
//             ShowItemDescription toggle controls description sub-line + height (T3b.10).
//             Per divergence #13, WrapLines never breaks a single oversize word at glyph
//             boundary — long name must be multi-word to guarantee ≥2 line wrap.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Layout;
using ReceiptToolkit.Core.Rendering.Sections;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class ItemTableSectionTests
{
    private const float Width = 360f;

    // T3b.7 — ItemTableSection renders one row per item: all 5 item names from the sample
    //          fixture must appear in the extracted PDF text. Confirms the section iterates
    //          the full items collection without dropping rows.
    [Fact]
    public void ItemTableSection_RendersOneRowPerItem_AllNamesPresent()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        string pdfText = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.Contains("Premium Notebook", pdfText, StringComparison.Ordinal);
        Assert.Contains("Ballpoint Pen Set", pdfText, StringComparison.Ordinal);
        Assert.Contains("Ceramic Coffee Mug", pdfText, StringComparison.Ordinal);
        Assert.Contains("Canvas Tote Bag", pdfText, StringComparison.Ordinal);
        Assert.Contains("Thank You Card", pdfText, StringComparison.Ordinal);
    }

    // T3b.8 — ItemTableSection wraps long multi-word item names to ≥2 lines without column
    //          overflow. A wide-name fixture (first item name mutated to a long multi-word
    //          string guaranteed to exceed 360 px at any reasonable font size) must produce
    //          strictly greater Measure height than the narrow-name baseline, confirming the
    //          section reserves extra vertical space for wrapped lines. Per divergence #13,
    //          WrapLines never breaks a single word at glyph boundary, so the long name is
    //          composed of multiple words. Both first and last tokens of the long name must
    //          appear in the extracted PDF text.
    [Fact]
    public void ItemTableSection_WrapsLongItemName_HeightExceedsNarrowBaseline()
    {
        ReceiptData narrow = SectionTestBase.LoadSampleData();
        const string LongName = "Premium Italian Leather Pocket Notebook with Foiled Edges and Gold Ribbon";

        ReceiptData wide = narrow with
        {
            Items = [.. narrow.Items.Select((item, i) =>
                i == 0 ? item with { Name = LongName } : item)],
        };

        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        // Verify the long name wraps: WrapLines on the name column width should produce ≥2 lines.
        // Use Inter Normal at a representative item-name font size (12pt) against the
        // name-column width (full width minus any SKU/qty/price columns — at minimum half).
        // We do not hard-code the exact column split; we confirm ≥2 lines conservatively
        // using the full receipt width, which a truly long name must still wrap within.
        SKTypeface face = fonts.GetTypeface("Inter", SKFontStyleWeight.Normal);
        IReadOnlyList<string> lines = TextMeasurer.WrapLines(LongName, Width, face, 12f);
        Assert.True(
            lines.Count >= 2,
            $"Expected WrapLines to produce ≥2 lines for the long name at width {Width}; got {lines.Count} line(s)");

        // Geometric assertion: wide-name fixture must produce strictly greater Measure height.
        float heightNarrow;
        float heightWide;

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightNarrow = section.Measure(Width, narrow, ctx);
        }

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightWide = section.Measure(Width, wide, ctx);
        }

        Assert.True(
            heightWide > heightNarrow,
            $"Expected Measure(wide-name) > Measure(narrow-name); got {heightWide} vs {heightNarrow}");

        // Text presence: both boundary tokens of the long name must appear in the PDF.
        string pdfText = SectionTestBase.RenderSectionToPdfText(section, wide, fonts);
        Assert.Contains("Premium Italian", pdfText, StringComparison.Ordinal);
        Assert.Contains("Gold Ribbon", pdfText, StringComparison.Ordinal);
    }

    // T3b.9 — SKU column appears iff options.ShowSku == true. The sample fixture default is
    //          ShowSku=false. A record-with mutation flips it to true. When false, SKU strings
    //          ("SKU-001" through "SKU-005") must NOT appear in the PDF text. When true, at
    //          least "SKU-001" must appear. Text-presence is the primary assertion; a height
    //          assertion is omitted because a fixed-column layout may not increase row height
    //          when a new column is added at fixed width.
    [Fact]
    public void ItemTableSection_SkuColumnAppearsOnlyWhenShowSkuTrue()
    {
        ReceiptData skuOff = SectionTestBase.LoadSampleData();
        // Sample fixture already has ShowSku=false — assert the default holds.
        Assert.False(skuOff.Options!.ShowSku, "Fixture precondition: ShowSku must default to false");

        ReceiptData skuOn = skuOff with
        {
            Options = skuOff.Options with { ShowSku = true },
        };

        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        string textSkuOff = SectionTestBase.RenderSectionToPdfText(section, skuOff, fonts);
        string textSkuOn = SectionTestBase.RenderSectionToPdfText(section, skuOn, fonts);

        // SKU strings must not appear when toggle is off.
        Assert.DoesNotContain("SKU-001", textSkuOff, StringComparison.Ordinal);
        Assert.DoesNotContain("SKU-002", textSkuOff, StringComparison.Ordinal);

        // SKU strings must appear when toggle is on.
        Assert.Contains("SKU-001", textSkuOn, StringComparison.Ordinal);
        Assert.Contains("SKU-002", textSkuOn, StringComparison.Ordinal);
    }

    // T3b.10 — Description sub-line appears iff options.ShowItemDescription == true. The
    //           sample fixture default is ShowItemDescription=false. Only the first item
    //           has a non-null description ("A5 hardcover"). When false, that string must
    //           NOT appear in the PDF text. When true, it must appear. The extra sub-line
    //           per item with a description adds height, so Measure(true) > Measure(false).
    [Fact]
    public void ItemTableSection_DescriptionSubLineAppearsOnlyWhenShowItemDescriptionTrue()
    {
        ReceiptData descOff = SectionTestBase.LoadSampleData();
        // Sample fixture already has ShowItemDescription=false — assert the default holds.
        Assert.False(descOff.Options!.ShowItemDescription, "Fixture precondition: ShowItemDescription must default to false");

        ReceiptData descOn = descOff with
        {
            Options = descOff.Options with { ShowItemDescription = true },
        };

        using var fonts = new FontProvider();
        var section = new ItemTableSection();

        string textDescOff = SectionTestBase.RenderSectionToPdfText(section, descOff, fonts);
        string textDescOn = SectionTestBase.RenderSectionToPdfText(section, descOn, fonts);

        // Description must not appear when toggle is off.
        Assert.DoesNotContain("A5 hardcover", textDescOff, StringComparison.Ordinal);

        // Description must appear when toggle is on.
        Assert.Contains("A5 hardcover", textDescOn, StringComparison.Ordinal);

        // Geometric assertion: description sub-line must add height.
        float heightDescOff;
        float heightDescOn;

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightDescOff = section.Measure(Width, descOff, ctx);
        }

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightDescOn = section.Measure(Width, descOn, ctx);
        }

        Assert.True(
            heightDescOn > heightDescOff,
            $"Expected Measure(showItemDescription=true) > Measure(showItemDescription=false); got {heightDescOn} vs {heightDescOff}");
    }
}
