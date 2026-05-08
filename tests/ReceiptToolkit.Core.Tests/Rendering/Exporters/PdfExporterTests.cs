// Purpose: RED-phase tests for Phase 3d sub-cluster A — PdfExporter (T3d.1–T3d.4).
// Categories: Unit — PDF magic header bytes, /CreationDate metadata threaded from
//             injected IClock, text extraction across pages, multi-page pagination
//             driven by content height vs configured PageHeight.
// Edge cases: short receipt fits one page; long-items fixture (50 lines) spans 2+
//             pages at DefaultPageHeight=1200; CreationDate honours fixed clock and
//             not the system clock (PdfPig surfaces it as raw string D:YYYYMMDD...).

using System.Text;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Contracts.Time;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Exporters;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReceiptToolkit.Core.Tests.Rendering.Exporters;

public sealed class PdfExporterTests
{
    // T3d.1 — Export must return a non-empty byte array whose first 5 bytes are the
    //          ASCII magic header "%PDF-". This pins the output as a real PDF stream
    //          rather than raw bitmap or arbitrary bytes.
    [Fact]
    public void Export_ReturnsBytesStartingWithPdfMagic()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var clock = new FixedClock(new DateTimeOffset(2025, 5, 18, 10, 42, 0, TimeSpan.Zero));
        var exporter = new PdfExporter(clock, fonts);

        byte[] bytes = exporter.Export(data);

        Assert.True(bytes.Length >= 5, $"Expected at least 5 bytes; got {bytes.Length}.");
        string magic = Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", magic);
    }

    // T3d.2 — The PDF /CreationDate metadata must be sourced from the injected IClock,
    //          not DateTime.Now. Pi day (2024-03-14) is used as a value distinct from
    //          today and from any other test clock, so the assertion fails decisively
    //          if the system clock leaks through. PdfPig exposes CreationDate as a raw
    //          string (e.g. "D:20240314..."); we assert on the YYYYMMDD substring to
    //          tolerate the raw form regardless of timezone serialisation.
    [Fact]
    public void Export_PdfMetadata_CreationDate_FromInjectedClock()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var clock = new FixedClock(new DateTimeOffset(2024, 3, 14, 9, 26, 0, TimeSpan.Zero));
        var exporter = new PdfExporter(clock, fonts);

        byte[] bytes = exporter.Export(data);

        using PdfDocument pdf = PdfDocument.Open(bytes);
        string? creationDate = pdf.Information.CreationDate;

        Assert.NotNull(creationDate);
        Assert.Contains("20240314", creationDate, StringComparison.Ordinal);
    }

    // T3d.3 — A round-tripped PDF must contain the business name and a recognisable
    //          slice of the grand total when text is extracted across all pages.
    //          Using "56.73" as a single token avoids PdfPig's tendency to fuse
    //          adjacent text draws across whitespace (divergence #17/#18) — multi-word
    //          phrases like "Elevate Studio Sdn Bhd" would be brittle. Business name
    //          is two tokens with a deliberate space; "Elevate Studio" is the literal
    //          single-line string in the sample fixture.
    [Fact]
    public void Export_PdfText_ContainsBusinessNameAndGrandTotal()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var clock = new FixedClock(new DateTimeOffset(2025, 5, 18, 10, 42, 0, TimeSpan.Zero));
        var exporter = new PdfExporter(clock, fonts);

        byte[] bytes = exporter.Export(data);

        using PdfDocument pdf = PdfDocument.Open(bytes);
        var sb = new StringBuilder();
        for (int i = 1; i <= pdf.NumberOfPages; i++)
        {
            Page page = pdf.GetPage(i);
            sb.Append(page.Text);
            sb.Append('\n');
        }

        string text = sb.ToString();

        Assert.Contains("Elevate Studio", text, StringComparison.Ordinal);
        Assert.Contains("56.73", text, StringComparison.Ordinal);
    }

    // T3d.4 — Page count is derived from total composed receipt height vs configured
    //          PageHeight. The sample fixture (~1040px composed) must fit in one page
    //          at DefaultPageHeight=1200; the long-items fixture (50 line items,
    //          ~2010px composed) must span two or more pages at the same PageHeight.
    //          Both halves use the same exporter configuration to isolate pagination
    //          as the cause.
    [Fact]
    public void Export_PageCount_IsOneForSampleAndAtLeastTwoForLongItems()
    {
        using var fonts = new FontProvider();
        var clock = new FixedClock(new DateTimeOffset(2025, 5, 18, 10, 42, 0, TimeSpan.Zero));
        var exporter = new PdfExporter(clock, fonts, PdfExporter.DefaultPageHeight);

        ReceiptData sample = SectionTestBase.LoadSampleData();
        byte[] sampleBytes = exporter.Export(sample);
        using (PdfDocument samplePdf = PdfDocument.Open(sampleBytes))
        {
            Assert.Equal(1, samplePdf.NumberOfPages);
        }

        ReceiptData longData = LoadLongItemsData();
        byte[] longBytes = exporter.Export(longData);
        using (PdfDocument longPdf = PdfDocument.Open(longBytes))
        {
            Assert.True(
                longPdf.NumberOfPages >= 2,
                $"Expected long-items PDF to span >= 2 pages; got {longPdf.NumberOfPages}.");
        }
    }

    private static ReceiptData LoadLongItemsData()
    {
        string fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "sample_receipt_long_items.json");

        string json = File.ReadAllText(fixturePath);
        return ReceiptData.FromJson(json);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
