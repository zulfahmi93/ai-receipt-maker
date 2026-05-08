// Purpose: RED-phase tests for Phase 3e sub-cluster B — SavePdfAsync / SavePngAsync
//          (T3e.4, T3e.5). Pin file write + auto-create-output-dir behaviour.
// Categories: Integration — façade writes a real file to disk and the parent dir
//             is created when missing. Each test uses a per-test temp directory
//             scoped via try/finally to keep the suite hermetic.
// Edge cases: T3e.4 path lives under a never-existed `out/` subdirectory inside
//             a fresh temp dir to prove auto-create. T3e.5 mirrors the contract
//             for PNG with the matching magic-byte assertion.

using System.Text;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Tests.Rendering.Sections;
using ReceiptToolkit.Core.Tests.Time;

namespace ReceiptToolkit.Core.Tests.Generation;

public sealed class ReceiptGeneratorSaveTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2025, 5, 18, 10, 42, 0, TimeSpan.Zero);

    // T3e.4 — SavePdfAsync writes the bytes to disk and auto-creates the parent dir.
    //          The path's `out/` subdirectory does not exist before the call; the
    //          assertion proves the directory was created and the file holds a real
    //          PDF byte stream.
    [Fact]
    public async Task SavePdfAsync_WritesFile_AndAutoCreatesDirectory()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        string root = NewTempDir();
        string path = Path.Combine(root, "out", "r.pdf");
        try
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(path)!),
                "Pre-condition: the `out/` directory must not exist before SavePdfAsync.");

            await generator.SavePdfAsync(data, path, TestContext.Current.CancellationToken);

            Assert.True(File.Exists(path), $"Expected SavePdfAsync to write {path}.");
            byte[] bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.True(bytes.Length >= 5, $"Expected a non-trivial PDF file; got {bytes.Length} bytes.");
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }
        finally
        {
            CleanupTempDir(root);
        }
    }

    // T3e.5 — SavePngAsync mirrors the SavePdfAsync contract: writes a PNG file and
    //          auto-creates the parent dir. We assert the 4-byte PNG magic at the
    //          head of the persisted bytes.
    [Fact]
    public async Task SavePngAsync_WritesFile_AndAutoCreatesDirectory()
    {
        ReceiptData data = SectionTestBase.LoadSampleData();
        using var fonts = new FontProvider();
        using var generator = new ReceiptGenerator(new FixedClock(FixedNow), fonts);

        string root = NewTempDir();
        string path = Path.Combine(root, "out", "r.png");
        try
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(path)!),
                "Pre-condition: the `out/` directory must not exist before SavePngAsync.");

            await generator.SavePngAsync(data, path, TestContext.Current.CancellationToken);

            Assert.True(File.Exists(path), $"Expected SavePngAsync to write {path}.");
            byte[] bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.True(bytes.Length >= 4, $"Expected a non-trivial PNG file; got {bytes.Length} bytes.");
            Assert.Equal((byte)0x89, bytes[0]);
            Assert.Equal((byte)0x50, bytes[1]);
            Assert.Equal((byte)0x4E, bytes[2]);
            Assert.Equal((byte)0x47, bytes[3]);
        }
        finally
        {
            CleanupTempDir(root);
        }
    }

    private static string NewTempDir()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "receipt-toolkit-tests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        return root;
    }

    private static void CleanupTempDir(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; the per-run randomised directory keeps tests hermetic
            // even when a stray handle prevents deletion.
        }
    }
}
