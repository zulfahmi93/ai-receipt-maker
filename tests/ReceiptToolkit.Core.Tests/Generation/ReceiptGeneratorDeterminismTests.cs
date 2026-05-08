// Purpose: RED-phase tests for Phase 3e sub-cluster C — determinism (T3e.6, T3e.7).
//          Two consecutive GeneratePdfAsync / GeneratePngAsync calls with a fixed
//          IClock and the same FontProvider must produce byte-equal output.
// Categories: Unit — façade determinism. Pins the contract that golden tests rely on:
//             same input + same clock + same font lifetime ⇒ identical bytes.
// Edge cases: tests share one FontProvider + one ReceiptGenerator across both calls
//             so font subset state, IClock value, and exporter wiring are all stable.
//             A single-instance generator is the realistic production usage; if a
//             future change introduces hidden mutable state across calls, this test
//             fails and surfaces the regression immediately.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using ReceiptToolkit.Core.Tests.Time;

namespace ReceiptToolkit.Core.Tests.Generation;

public sealed class ReceiptGeneratorDeterminismTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2025, 5, 18, 10, 42, 0, TimeSpan.Zero);

    // T3e.6 — Two consecutive GeneratePdfAsync calls with fixed IClock + shared
    //          FontProvider produce byte-equal output. PDF determinism depends on
    //          a stable /CreationDate (from the clock), stable /ID (Skia derives
    //          this from metadata + Creation), and stable font subset embedding
    //          (single FontProvider + single generator avoids re-init reordering).
    [Fact]
    public async Task GeneratePdfAsync_TwoConsecutiveCalls_ProduceByteEqualOutput()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        byte[] first = await generator.GeneratePdfAsync(data, TestContext.Current.CancellationToken);
        byte[] second = await generator.GeneratePdfAsync(data, TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
    }

    // T3e.7 — Two consecutive GeneratePngAsync calls with the same fixtures produce
    //          byte-equal output. PNG has no embedded timestamp and no /ID; bytes are
    //          fully determined by the rasterised bitmap, so the test mostly pins
    //          renderer + section determinism (no hidden RNG, no per-call state leak).
    [Fact]
    public async Task GeneratePngAsync_TwoConsecutiveCalls_ProduceByteEqualOutput()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        byte[] first = await generator.GeneratePngAsync(data, TestContext.Current.CancellationToken);
        byte[] second = await generator.GeneratePngAsync(data, TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
    }
}
