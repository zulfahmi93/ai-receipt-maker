// Purpose: Phase 4 sub-cluster C-B — generate command (T4.4, T4.5, T4.6).
// Categories: Process tests — spawn the built CLI binary and assert the on-disk
//             effect (file existence + non-empty + magic-byte sniff). Generate is
//             the expensive command (full PDF/PNG render via SkiaSharp) so each
//             test scopes to one assertion to keep wall time predictable.
// Edge cases:
//   T4.4 — both --pdf and --png written when supplied; assert each file ≥ 1 byte
//          and starts with the format's magic header.
//   T4.5 — when output paths point into a missing nested directory the CLI auto-
//          creates the parent dir (delegated to ReceiptGenerator.Save{Pdf,Png}Async).
//   T4.6 — overwrite without --force exits 3 and leaves the existing file untouched;
//          retry with --force succeeds and overwrites the bytes.

namespace ReceiptToolkit.Cli.Tests;

public sealed class GenerateCommandTests
{
    // T4.4 — Happy path: --pdf and --png both supplied, both files written.
    //        Magic-byte sniff is the cheapest robust integrity check at the CLI
    //        boundary; full PDF/PNG correctness is owned by Phase 3d/3e tests.
    [Fact]
    public void Generate_WithPdfAndPngOutputs_WritesBothFiles()
    {
        using var temp = new TempDirectory();
        string pdfPath = temp.CombinePath("out.pdf");
        string pngPath = temp.CombinePath("out.png");

        CliResult result = CliRunner.Run(
            "generate",
            "--input", FixtureFiles.SampleJsonPath,
            "--pdf", pdfPath,
            "--png", pngPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(pdfPath), $"Expected PDF at {pdfPath}. Stderr: {result.StdErr}");
        Assert.True(File.Exists(pngPath), $"Expected PNG at {pngPath}. Stderr: {result.StdErr}");

        byte[] pdfBytes = File.ReadAllBytes(pdfPath);
        byte[] pngBytes = File.ReadAllBytes(pngPath);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, pdfBytes.AsSpan(0, 4).ToArray()); // "%PDF"
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, pngBytes.AsSpan(0, 4).ToArray()); // PNG magic
    }

    // T4.5 — Output paths under a not-yet-existing nested directory: the CLI must
    //        auto-create the parent. The behaviour itself lives in ReceiptGenerator.
    //        Save{Pdf,Png}Async, but the CLI must not pre-check or short-circuit
    //        in a way that shadows that contract.
    [Fact]
    public void Generate_WithMissingOutputDirectory_AutoCreatesParent()
    {
        using var temp = new TempDirectory();
        string nestedDir = temp.CombinePath("nested", "deeper");
        string pdfPath = Path.Combine(nestedDir, "receipt.pdf");
        string pngPath = Path.Combine(nestedDir, "receipt.png");
        Assert.False(Directory.Exists(nestedDir), "Pre-condition: nested dir must not pre-exist.");

        CliResult result = CliRunner.Run(
            "generate",
            "--input", FixtureFiles.SampleJsonPath,
            "--pdf", pdfPath,
            "--png", pngPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(pdfPath));
        Assert.True(File.Exists(pngPath));
    }

    // T4.6 — Overwrite gate. First call succeeds (writes pdf+png). Second call without
    //        --force exits 3 and leaves the bytes alone. Third call with --force
    //        succeeds. The "leaves alone" check is a byte-equality assertion against
    //        the bytes captured after the first run — proves we did not partially
    //        write before bailing out.
    [Fact]
    public void Generate_RefusesOverwriteWithoutForce_AndAllowsWithForce()
    {
        using var temp = new TempDirectory();
        string pdfPath = temp.CombinePath("out.pdf");
        string pngPath = temp.CombinePath("out.png");

        CliResult first = CliRunner.Run(
            "generate",
            "--input", FixtureFiles.SampleJsonPath,
            "--pdf", pdfPath,
            "--png", pngPath);
        Assert.Equal(0, first.ExitCode);

        byte[] originalPdf = File.ReadAllBytes(pdfPath);
        byte[] originalPng = File.ReadAllBytes(pngPath);
        // Mutate one of the existing files so byte-equality after refusal is a
        // strong signal (default deterministic-rendering would leave bytes equal).
        File.WriteAllBytes(pdfPath, [0xFF, 0xFE]);
        File.WriteAllBytes(pngPath, [0xFF, 0xFE]);

        CliResult refused = CliRunner.Run(
            "generate",
            "--input", FixtureFiles.SampleJsonPath,
            "--pdf", pdfPath,
            "--png", pngPath);
        Assert.Equal(3, refused.ExitCode);
        Assert.Equal(new byte[] { 0xFF, 0xFE }, File.ReadAllBytes(pdfPath));
        Assert.Equal(new byte[] { 0xFF, 0xFE }, File.ReadAllBytes(pngPath));

        CliResult forced = CliRunner.Run(
            "generate",
            "--input", FixtureFiles.SampleJsonPath,
            "--pdf", pdfPath,
            "--png", pngPath,
            "--force");
        Assert.Equal(0, forced.ExitCode);
        byte[] forcedPdf = File.ReadAllBytes(pdfPath);
        byte[] forcedPng = File.ReadAllBytes(pngPath);
        Assert.True(forcedPdf.Length > 2, "After --force, the PDF must have been re-written.");
        Assert.True(forcedPng.Length > 2, "After --force, the PNG must have been re-written.");
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, forcedPdf.AsSpan(0, 4).ToArray());
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, forcedPng.AsSpan(0, 4).ToArray());

        // Sanity: original first-write bytes parsed (so the test fixture is OK).
        Assert.True(originalPdf.Length > 2);
        Assert.True(originalPng.Length > 2);
    }
}
