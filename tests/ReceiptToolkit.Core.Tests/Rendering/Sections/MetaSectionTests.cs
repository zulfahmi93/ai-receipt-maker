// Purpose: RED-phase test for Phase 3b/A — MetaSection renderer (T3b.5).
// Categories: Unit — section rendering, PDF text extraction via PdfPig, two-column
//             null-aware field rendering.
// Edge cases: every non-null meta field appears in the PDF; date formatting routes
//             through DateTimeFormatter (locale ms-MY + format dd MMM yyyy ⇒ "18 Mei 2025");
//             nulls hide cleanly without leaving a gap row (verified by Measure shrinking).

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Formatting;
using ReceiptToolkit.Core.Rendering;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Sections;

public sealed class MetaSectionTests
{
    // T3b.5 — MetaSection renders all non-null meta fields in a two-column block:
    //          receiptNumber, branchName, terminalId, orderNumber, referenceNumber, and a
    //          locale-formatted date. Null fields are hidden without leaving an empty row,
    //          which is verified by Measure(reduced) being strictly less than Measure(full)
    //          after nulling two fields. Receipt number + branch name remain visible across
    //          both renders to prove the section didn't collapse entirely.
    [Fact]
    public void MetaSection_DrawsAllNonNullFields_HidesNulls()
    {
        ReceiptData full = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new MetaSection();

        string textFull = SectionTestBase.RenderSectionToPdfText(section, full, fonts);

        // Sample fixture has dateTime="2025-05-18T10:42:00", dateFormat="dd MMM yyyy",
        // locale="ms-MY"; DateTimeFormatter produces "18 Mei 2025" (Mei = Malay May).
        string expectedDate = DateTimeFormatter.FormatDate(full.Receipt.DateTime!, full.Options!);

        Assert.Contains("INV-2025-06789", textFull, StringComparison.Ordinal);
        Assert.Contains("Main Branch", textFull, StringComparison.Ordinal);
        Assert.Contains("POS-03", textFull, StringComparison.Ordinal);
        Assert.Contains("ORD-1024", textFull, StringComparison.Ordinal);
        Assert.Contains("REF-778812", textFull, StringComparison.Ordinal);
        Assert.Contains(expectedDate, textFull, StringComparison.Ordinal);

        // Now drop terminalId + orderNumber and re-render: those values must vanish from
        // the PDF text, but receiptNumber + branchName must still appear, and the
        // measured height must shrink (proving null rows don't leave a gap).
        ReceiptData reduced = full with
        {
            Receipt = full.Receipt with
            {
                TerminalId = null,
                OrderNumber = null,
            },
        };

        string textReduced = SectionTestBase.RenderSectionToPdfText(section, reduced, fonts);

        Assert.DoesNotContain("POS-03", textReduced, StringComparison.Ordinal);
        Assert.DoesNotContain("ORD-1024", textReduced, StringComparison.Ordinal);
        Assert.Contains("INV-2025-06789", textReduced, StringComparison.Ordinal);
        Assert.Contains("Main Branch", textReduced, StringComparison.Ordinal);

        const float Width = 360f;
        float heightFull;
        float heightReduced;

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightFull = section.Measure(Width, full, ctx);
        }

        using (var ctx = new RenderContext(fonts, resolvedLogo: null))
        {
            heightReduced = section.Measure(Width, reduced, ctx);
        }

        Assert.True(
            heightReduced < heightFull,
            $"Expected Measure(reduced) < Measure(full); got {heightReduced} vs {heightFull}");
    }

    // T3b.5a — Visual review V9.3 blocker B3: when both DateFormat and TimeFormat are
    //           configured and the receipt timestamp is parseable, MetaSection must emit
    //           a single combined "Date & Time" row rather than two separate Date and
    //           Time rows. The mockup labels this row "DATE & TIME" and joins the two
    //           values with a middle dot. The combined row reduces vertical noise and
    //           matches the design's information density.
    [Fact]
    public void MetaSection_EmitsCombinedDateAndTimeRow_WhenBothFormatsPresent()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var section = new MetaSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);
        string formattedDate = DateTimeFormatter.FormatDate(data.Receipt.DateTime!, data.Options!);
        string formattedTime = DateTimeFormatter.FormatTime(data.Receipt.DateTime!, data.Options!);

        Assert.Contains("DATE & TIME", text, StringComparison.Ordinal);
        Assert.Contains($"{formattedDate} · {formattedTime}", text, StringComparison.Ordinal);

        const float Width = 360f;
        using var ctx = new RenderContext(fonts, resolvedLogo: null);
        float combinedHeight = section.Measure(Width, data, ctx);

        ReceiptData dateOnly = data with
        {
            Options = (data.Options ?? new ReceiptOptions()) with { TimeFormat = null },
        };
        float dateOnlyHeight = section.Measure(Width, dateOnly, ctx);

        Assert.Equal(combinedHeight, dateOnlyHeight);
    }

    // T3b.5b — When only DateFormat is present (TimeFormat null/blank), MetaSection emits
    //           a single Date row labelled "Date" — the combined "Date & Time" form only
    //           applies when both pieces are rendered.
    [Fact]
    public void MetaSection_EmitsSeparateDateRow_WhenOnlyDateFormatPresent()
    {
        ReceiptData baseData = SectionTestBase.LoadSampleData();
        ReceiptData data = baseData with
        {
            Options = (baseData.Options ?? new ReceiptOptions()) with { TimeFormat = null },
        };

        using var fonts = new FontProvider();
        var section = new MetaSection();

        string text = SectionTestBase.RenderSectionToPdfText(section, data, fonts);

        Assert.DoesNotContain("DATE & TIME", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TIME", text, StringComparison.Ordinal);
        Assert.Contains("DATE", text, StringComparison.Ordinal);
    }
}
