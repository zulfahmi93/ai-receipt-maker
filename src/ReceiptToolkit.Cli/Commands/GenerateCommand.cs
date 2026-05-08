using System.CommandLine;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;

namespace ReceiptToolkit.Cli.Commands;

/// <summary>
///   <c>generate --input X --pdf Y --png Z [--force]</c> — validates the input,
///   recalculates totals if configured, and writes PDF and/or PNG outputs through
///   <see cref="ReceiptGenerator"/>. By default refuses to overwrite an existing
///   output file (exits with <see cref="ExitCodes.OverwriteRefused"/>); pass
///   <c>--force</c> to opt in.
/// </summary>
internal static class GenerateCommand
{
    /// <summary>Constructs the generate sub-command and wires its action.</summary>
    public static Command Build()
    {
        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "Path to the receipt JSON file.",
            Required = true,
        };

        var pdfOption = new Option<string?>("--pdf")
        {
            Description = "Output path for the PDF file. Omit to skip PDF generation.",
        };

        var pngOption = new Option<string?>("--png")
        {
            Description = "Output path for the PNG file. Omit to skip PNG generation.",
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite output files when they already exist.",
        };

        var command = new Command("generate", "Generate receipt PDF and/or PNG from a JSON input.");
        command.Add(inputOption);
        command.Add(pdfOption);
        command.Add(pngOption);
        command.Add(forceOption);
        command.SetAction(parseResult =>
        {
            string inputPath = parseResult.GetRequiredValue(inputOption);
            string? pdfPath = parseResult.GetValue(pdfOption);
            string? pngPath = parseResult.GetValue(pngOption);
            bool force = parseResult.GetValue(forceOption);
            return Execute(inputPath, pdfPath, pngPath, force);
        });

        return command;
    }

    /// <summary>
    ///   Reads <paramref name="inputPath"/>, runs validation + generation, and writes
    ///   the requested artifacts. Returns the appropriate <see cref="ExitCodes"/>.
    /// </summary>
    public static int Execute(string inputPath, string? pdfPath, string? pngPath, bool force)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return ExitCodes.InputError;
        }

        if (string.IsNullOrEmpty(pdfPath) && string.IsNullOrEmpty(pngPath))
        {
            Console.Error.WriteLine("Specify at least one of --pdf or --png.");
            return ExitCodes.InputError;
        }

        if (!force)
        {
            if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
            {
                Console.Error.WriteLine($"Output file already exists (pass --force to overwrite): {pdfPath}");
                return ExitCodes.OverwriteRefused;
            }

            if (!string.IsNullOrEmpty(pngPath) && File.Exists(pngPath))
            {
                Console.Error.WriteLine($"Output file already exists (pass --force to overwrite): {pngPath}");
                return ExitCodes.OverwriteRefused;
            }
        }

        if (!CommandHelpers.TryReadReceiptJson(inputPath, out ReceiptData data))
        {
            return ExitCodes.InputError;
        }

        try
        {
            using var generator = new ReceiptGenerator();
            if (!string.IsNullOrEmpty(pdfPath))
            {
                generator.SavePdfAsync(data, pdfPath).GetAwaiter().GetResult();
            }

            if (!string.IsNullOrEmpty(pngPath))
            {
                generator.SavePngAsync(data, pngPath).GetAwaiter().GetResult();
            }
        }
        catch (ReceiptValidationException ex)
        {
            CommandHelpers.WriteValidationErrors(ex.Errors);
            return ExitCodes.ValidationFailed;
        }

        return ExitCodes.Success;
    }
}
