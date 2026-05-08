// Purpose: Fixture-file helpers for CLI process tests. Writes the bundled sample JSON
//          (or a mutated invalid copy) into a per-test temp directory so each Process
//          spawn sees an isolated working directory.
// Categories: Test infrastructure — paths only. CLI process tests own the Cli boundary;
//             they must not call the in-process generator façade.
// Edge cases: invalid-fixture mutation strips the businessName field from JSON via a
//             simple regex on the raw text rather than re-serialising via STJ — keeps
//             the test fixture format identical to what users hand-write.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReceiptToolkit.Cli.Tests;

internal static class FixtureFiles
{
    /// <summary>
    ///   Locates the bundled sample receipt fixture. The CLI csproj copies
    ///   <c>examples/sample_receipt_data.json</c> next to the CLI assembly, and the
    ///   CLI Tests project's project-reference inherits the same content into its own
    ///   output directory.
    /// </summary>
    public static string SampleJsonPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "sample_receipt_data.json");

    /// <summary>Reads the bundled sample JSON as raw UTF-8 text.</summary>
    public static string ReadSampleJson() => File.ReadAllText(SampleJsonPath);

    /// <summary>
    ///   Returns a copy of the sample JSON with <c>business.businessName</c> set to an
    ///   empty string so <c>BusinessNameRule</c> fires. Used to drive the validate-fail
    ///   exit-code-2 path without inventing a brand-new fixture.
    /// </summary>
    public static string BuildInvalidJson()
    {
        string raw = ReadSampleJson();
        JsonNode root = JsonNode.Parse(raw)
            ?? throw new InvalidOperationException("Sample fixture failed to parse as JSON.");
        JsonNode business = root["business"]
            ?? throw new InvalidOperationException("Sample fixture missing 'business' object.");
        business["businessName"] = string.Empty;
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    ///   Allocates a unique temp directory under the OS temp root for the current test
    ///   run, returning the absolute path. Caller is responsible for deletion (use
    ///   <see cref="TempDirectory"/> for RAII-style cleanup).
    /// </summary>
    public static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "receipt-toolkit-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = FixtureFiles.CreateTempDir();
    }

    public string CombinePath(params string[] segments)
    {
        string[] all = new string[segments.Length + 1];
        all[0] = Path;
        Array.Copy(segments, 0, all, 1, segments.Length);
        return System.IO.Path.Combine(all);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; CI temp roots get reaped between runs.
        }
        catch (UnauthorizedAccessException)
        {
            // Same — suppress so test teardown does not mask real assertions.
        }
    }
}
