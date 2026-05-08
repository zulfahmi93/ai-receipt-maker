using System.CommandLine;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;

namespace ReceiptToolkit.Cli.Commands;

/// <summary>
///   <c>sample --output dir/</c> — renders the bundled sample receipt fixture to
///   <c>sample.pdf</c> and <c>sample.png</c> under the supplied directory. The
///   bundled fixture (<c>examples/sample_receipt_data.json</c>) is copied next to
///   the CLI assembly via a csproj <c>Content Include</c>, so the path resolves the
///   same way after <c>dotnet publish</c>.
/// </summary>
internal static class SampleCommand
{
    private const string SampleFixtureName = "sample_receipt_data.json";
    private const string SamplePdfName = "sample.pdf";
    private const string SamplePngName = "sample.png";

    /// <summary>Constructs the sample sub-command and wires its action.</summary>
    public static Command Build()
    {
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output directory for sample.pdf and sample.png.",
            Required = true,
        };

        var command = new Command("sample", "Generate the bundled sample receipt as PDF and PNG.");
        command.Add(outputOption);
        command.SetAction(parseResult =>
        {
            string outputDir = parseResult.GetRequiredValue(outputOption);
            return Execute(outputDir);
        });

        return command;
    }

    /// <summary>
    ///   Resolves the bundled sample fixture, renders it, and writes the PDF + PNG
    ///   into <paramref name="outputDir"/>. Auto-creates the directory when missing
    ///   (delegated to <see cref="ReceiptGenerator"/>'s save helpers).
    /// </summary>
    public static int Execute(string outputDir)
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, SampleFixtureName);
        if (!File.Exists(fixturePath))
        {
            Console.Error.WriteLine($"Bundled sample fixture missing: {fixturePath}");
            return ExitCodes.InputError;
        }

        if (!CommandHelpers.TryReadReceiptJson(fixturePath, out ReceiptData data))
        {
            return ExitCodes.InputError;
        }

        string pdfPath = Path.Combine(outputDir, SamplePdfName);
        string pngPath = Path.Combine(outputDir, SamplePngName);

        try
        {
            using var generator = new ReceiptGenerator();
            generator.SavePdfAsync(data, pdfPath).GetAwaiter().GetResult();
            generator.SavePngAsync(data, pngPath).GetAwaiter().GetResult();
        }
        catch (ReceiptValidationException ex)
        {
            // Should never fire in production — the bundled fixture is the canonical
            // golden — but if it does, surface it like the other commands so debugging
            // is uniform.
            CommandHelpers.WriteValidationErrors(ex.Errors);
            return ExitCodes.ValidationFailed;
        }

        return ExitCodes.Success;
    }
}
