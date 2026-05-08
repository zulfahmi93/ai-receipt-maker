// Purpose: Phase 4 sub-cluster C-C — sample command (T4.7).
// Categories: Process tests — spawn the built CLI binary and assert sample.pdf +
//             sample.png land under the supplied output directory. The sample
//             fixture is bundled next to the CLI binary via the csproj Content
//             Include in ReceiptToolkit.Cli.csproj — survives publish.
// Edge cases:
//   T4.7 — fresh empty output directory, both files written with the canonical
//          names "sample.pdf" / "sample.png". Magic-byte sniff confirms format.
//          Auto-create-parent behaviour is already covered by GenerateCommandTests
//          T4.5 (same Save{Pdf,Png}Async path) so this test focuses on the sample
//          fixture-discovery contract.

namespace ReceiptToolkit.Cli.Tests;

public sealed class SampleCommandTests
{
    // T4.7 — `sample --output dir/` writes sample.pdf + sample.png. The CLI must
    //        locate the bundled sample fixture without any --input flag — that is
    //        the whole point of the sample command.
    [Fact]
    public void Sample_WithOutputDirectory_WritesSamplePdfAndPng()
    {
        using var temp = new TempDirectory();
        string outputDir = temp.CombinePath("sample-out");

        CliResult result = CliRunner.Run("sample", "--output", outputDir);

        Assert.Equal(0, result.ExitCode);
        string pdfPath = Path.Combine(outputDir, "sample.pdf");
        string pngPath = Path.Combine(outputDir, "sample.png");
        Assert.True(File.Exists(pdfPath), $"Expected sample.pdf at {pdfPath}. Stderr: {result.StdErr}");
        Assert.True(File.Exists(pngPath), $"Expected sample.png at {pngPath}. Stderr: {result.StdErr}");

        byte[] pdfBytes = File.ReadAllBytes(pdfPath);
        byte[] pngBytes = File.ReadAllBytes(pngPath);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, pdfBytes.AsSpan(0, 4).ToArray());
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, pngBytes.AsSpan(0, 4).ToArray());
    }
}
