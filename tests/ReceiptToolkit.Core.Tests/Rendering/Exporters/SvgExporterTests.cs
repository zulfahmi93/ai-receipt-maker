// Purpose: RED-phase tests for Phase 3d sub-cluster C — SvgExporter byte output and
//          textual content (T3d.8, T3d.9). The exporter currently throws
//          NotImplementedException; these tests pin its public contract before GREEN.
// Categories: Unit — exporter byte stream contract. T3d.8 pins the SVG root tag in the
//              UTF-8 payload; T3d.9 pins the business name surfacing as a textual
//              substring of the rendered SVG.
// Edge cases: SkiaSharp's SVG backend may prefix output with an XML declaration
//              (<?xml version="1.0" encoding="utf-8"?>) and/or a UTF-8 BOM, so T3d.8
//              uses Assert.Contains("<svg", ...) rather than StartsWith. T3d.9 assumes
//              SkiaSharp emits one <text> element per draw call (contiguous business
//              name); if a future Skia release fragments the run across <tspan>
//              boundaries, the assertion may need tightening.

using System.Text;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Exporters;
using ReceiptToolkit.Core.Tests.Rendering.Sections;

namespace ReceiptToolkit.Core.Tests.Rendering.Exporters;

/// <summary>
///   RED-phase coverage for <c>SvgExporter</c> (T3d.8–T3d.9). The production class's
///   <c>Export</c> method currently throws <see cref="NotImplementedException"/> — every
///   fact below fails for the right reason until GREEN lands the SkSvgCanvas pipeline.
/// </summary>
public sealed class SvgExporterTests
{
    // T3d.8 — Export returns a non-empty UTF-8 byte stream whose decoded contents
    // contain the SVG root tag "<svg". Assert.Contains (instead of StartsWith) tolerates
    // an optional XML declaration prefix and any UTF-8 BOM Skia might emit.
    [Fact]
    public void Export_ReturnsBytesStartingWithSvgTag()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var exporter = new SvgExporter(fonts);

        byte[] bytes = exporter.Export(data);
        string svg = Encoding.UTF8.GetString(bytes);

        Assert.NotEmpty(bytes);
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
    }

    // T3d.9 — The sample fixture's business name "Elevate Studio" surfaces in the SVG
    // payload as a contiguous substring. SkiaSharp's SVG backend serialises text via
    // <text> elements containing the literal characters; one draw call per <text>
    // element keeps the run contiguous. If a future Skia release splits the run across
    // <tspan> boundaries, this assertion will need to widen (e.g. strip tags first).
    [Fact]
    public void Export_SvgContainsBusinessNameAsTextNode()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        var exporter = new SvgExporter(fonts);

        byte[] bytes = exporter.Export(data);
        string svg = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Elevate Studio", svg, StringComparison.Ordinal);
    }
}
