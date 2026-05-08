// Purpose: Test helper that spawns the built receipt-toolkit CLI as a child process
//          and captures stdout, stderr, and exit code. Phase 4 process tests exercise
//          the real shipped binary boundary — in-process Main(args) invocation would
//          bypass the System.CommandLine parser configuration we ship.
// Categories: Test infrastructure — wraps Process.Start boilerplate, locates the
//             CLI dll via the AssemblyMetadata key written by ReceiptToolkit.Cli.Tests.csproj.
// Edge cases: assembly-metadata key missing → throws InvalidOperationException with a
//             clear message (build wiring regression). Process timeout 30s — generous
//             for cold-start CLI plus PNG render of the sample fixture.

using System.Diagnostics;
using System.Reflection;

namespace ReceiptToolkit.Cli.Tests;

internal sealed record CliResult(int ExitCode, string StdOut, string StdErr);

internal static class CliRunner
{
    private const string AssemblyPathKey = "ReceiptToolkit.Cli.AssemblyPath";
    private const int DefaultTimeoutMs = 30_000;

    public static CliResult Run(params string[] args) => RunIn(workingDirectory: null, args);

    public static CliResult RunIn(string? workingDirectory, params string[] args)
    {
        string cliPath = ResolveCliPath();

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };
        psi.ArgumentList.Add("exec");
        psi.ArgumentList.Add(cliPath);
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Process.Start returned null for dotnet exec.");

        if (!process.WaitForExit(DefaultTimeoutMs))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Process already exited between the timeout check and Kill; nothing to do.
            }

            throw new TimeoutException(
                $"CLI did not exit within {DefaultTimeoutMs}ms. Args: {string.Join(' ', args)}");
        }

        string stdOut = process.StandardOutput.ReadToEnd();
        string stdErr = process.StandardError.ReadToEnd();
        return new CliResult(process.ExitCode, stdOut, stdErr);
    }

    private static string ResolveCliPath()
    {
        Assembly testAssembly = typeof(CliRunner).Assembly;
        AssemblyMetadataAttribute? meta = testAssembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, AssemblyPathKey, StringComparison.Ordinal));

        if (meta is null || string.IsNullOrEmpty(meta.Value))
        {
            throw new InvalidOperationException(
                $"Assembly metadata '{AssemblyPathKey}' missing — check ReceiptToolkit.Cli.Tests.csproj wiring.");
        }

        string normalized = Path.GetFullPath(meta.Value);
        if (!File.Exists(normalized))
        {
            throw new FileNotFoundException(
                $"CLI binary not found at '{normalized}'. Run `dotnet build` first.",
                normalized);
        }

        return normalized;
    }
}
