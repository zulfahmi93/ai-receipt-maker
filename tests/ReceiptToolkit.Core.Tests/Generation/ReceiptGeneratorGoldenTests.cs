// Purpose: RED-phase tests for Phase 3e sub-cluster D — golden-file byte-equality
//          (T3e.8, T3e.9). Pin sample_receipt_data.json PDF/PNG output to the
//          committed examples/golden/sample_receipt_data.golden.{pdf,png} bytes.
// Categories: Snapshot — Skia rasterisation differs across platforms (font hinting,
//             subpixel positioning, compression library version), so the gate runs
//             on Linux CI only per the CLAUDE.md hard rule. macOS / Windows runs
//             skip cleanly via Assert.Skip rather than failing.
// Edge cases: When a golden file is missing on a Linux run, Assert.Fail surfaces a
//             clear regeneration instruction instead of returning empty bytes
//             that would make the byte-equality assertion fail with a confusing
//             zero-length diff.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using ReceiptToolkit.Core.Tests.Time;

namespace ReceiptToolkit.Core.Tests.Generation;

public sealed class ReceiptGeneratorGoldenTests
{
    // The golden clock instant is part of the PDF /CreationDate metadata; any
    // change to this value invalidates the committed sample_receipt_data.golden.pdf
    // and requires regeneration. Choose a memorable, fixed instant.
    private static readonly DateTimeOffset GoldenClock =
        new(2025, 5, 18, 10, 42, 0, TimeSpan.Zero);

    private const string GoldenPdfFile = "sample_receipt_data.golden.pdf";
    private const string GoldenPngFile = "sample_receipt_data.golden.png";

    // T3e.8 — Sample-fixture PDF byte-equal to the committed golden PDF on Linux.
    [Fact]
    public async Task GeneratePdfAsync_MatchesGoldenBytes_OnLinux()
    {
        SkipIfNotLinux();

        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(GoldenClock), fonts);

        byte[] actual = await generator.GeneratePdfAsync(data, TestContext.Current.CancellationToken);
        byte[] expected = LoadGoldenOrFail(GoldenPdfFile);

        Assert.Equal(expected, actual);
    }

    // T3e.9 — Sample-fixture PNG byte-equal to the committed golden PNG on Linux.
    [Fact]
    public async Task GeneratePngAsync_MatchesGoldenBytes_OnLinux()
    {
        SkipIfNotLinux();

        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(GoldenClock), fonts);

        byte[] actual = await generator.GeneratePngAsync(data, TestContext.Current.CancellationToken);
        byte[] expected = LoadGoldenOrFail(GoldenPngFile);

        Assert.Equal(expected, actual);
    }

    private static void SkipIfNotLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip(
                "Golden tests are gated to Linux CI per the CLAUDE.md hard rule. " +
                "SkiaSharp rasterisation differs across platforms (font hinting, " +
                "subpixel positioning), so byte-equality is enforced on Linux only.");
        }
    }

    private static byte[] LoadGoldenOrFail(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden", fileName);
        if (!File.Exists(path))
        {
            Assert.Fail(
                $"Golden file missing: {path}. " +
                $"Run the generator on a Linux host to produce examples/golden/{fileName} " +
                $"and commit the bytes. Phase 3e divergence #23 documents this gap.");
        }

        return File.ReadAllBytes(path);
    }
}
