// Purpose: Phase 4 sub-cluster C-A — validate command exit-code contract (T4.1, T4.2, T4.3).
// Categories: Process tests — spawn the built CLI binary and assert exit code + stderr
//             content. The validate command is the first user-facing CLI surface;
//             machine-readable exit codes (0 valid, 1 missing-file, 2 validation-failed)
//             are the integration contract with shells, CI, and the API/bot.
// Edge cases:
//   T4.1 — happy path with bundled sample fixture; exit 0, no stderr noise.
//   T4.2 — sample fixture mutated to clear businessName so BusinessNameRule fires;
//          exit 2 + stderr lists at least one ValidationError pointing at "business".
//   T4.3 — argument points at a path that does not exist; exit 1 + stderr names the
//          missing path so users can fix the typo without re-reading help text.

namespace ReceiptToolkit.Cli.Tests;

public sealed class ValidateCommandTests
{
    // T4.1 — Valid input file exits 0. The bundled sample fixture is the canonical
    //        positive case; any future validation regression on it would also break
    //        Phase 3e ReceiptGeneratorTests, so this test pins the CLI surface only.
    [Fact]
    public void Validate_WithValidFixture_ExitsZero()
    {
        CliResult result = CliRunner.Run("validate", "--input", FixtureFiles.SampleJsonPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    // T4.2 — Invalid input exits 2 and lists ValidationErrors on stderr. We mutate
    //        the sample to clear businessName so BusinessNameRule fires deterministically;
    //        the assertion checks the field path substring rather than the exact rule
    //        wording so error-message polish does not flap the test.
    [Fact]
    public void Validate_WithInvalidFixture_ExitsTwoAndListsErrors()
    {
        using var temp = new TempDirectory();
        string invalidPath = temp.CombinePath("invalid.json");
        File.WriteAllText(invalidPath, FixtureFiles.BuildInvalidJson());

        CliResult result = CliRunner.Run("validate", "--input", invalidPath);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("business", result.StdErr, StringComparison.OrdinalIgnoreCase);
    }

    // T4.3 — Missing input file exits 1 with a helpful stderr that names the path.
    //        Distinct exit code from validation failure (2) because the failure mode
    //        is environmental, not data-driven — shell scripts may want to retry on 1
    //        (e.g. mount the volume) but never on 2.
    [Fact]
    public void Validate_WithMissingFile_ExitsOneAndNamesPath()
    {
        using var temp = new TempDirectory();
        string missingPath = temp.CombinePath("does-not-exist.json");

        CliResult result = CliRunner.Run("validate", "--input", missingPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("does-not-exist.json", result.StdErr, StringComparison.Ordinal);
    }
}
