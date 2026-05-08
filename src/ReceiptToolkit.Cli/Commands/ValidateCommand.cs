using System.CommandLine;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Validation;

namespace ReceiptToolkit.Cli.Commands;

/// <summary>
///   <c>validate --input file.json</c> — parses a receipt JSON file and runs the
///   <see cref="ReceiptValidator"/> rule set against it. Exits with
///   <see cref="ExitCodes.Success"/> when valid, <see cref="ExitCodes.ValidationFailed"/>
///   when one or more rules fail, or <see cref="ExitCodes.InputError"/> when the
///   input file cannot be read.
/// </summary>
internal static class ValidateCommand
{
    /// <summary>Constructs the validate sub-command and wires its action.</summary>
    public static Command Build()
    {
        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "Path to the receipt JSON file.",
            Required = true,
        };

        var command = new Command("validate", "Validate a receipt JSON file against the schema and business rules.");
        command.Add(inputOption);
        command.SetAction(parseResult =>
        {
            string inputPath = parseResult.GetRequiredValue(inputOption);
            return Execute(inputPath);
        });

        return command;
    }

    /// <summary>
    ///   Reads <paramref name="inputPath"/>, runs validation, prints any errors to
    ///   stderr, and returns the matching <see cref="ExitCodes"/> value.
    /// </summary>
    public static int Execute(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return ExitCodes.InputError;
        }

        if (!CommandHelpers.TryReadReceiptJson(inputPath, out ReceiptData data))
        {
            return ExitCodes.InputError;
        }

        var validator = new ReceiptValidator();
        IReadOnlyList<ValidationError> errors = validator.Validate(data);
        if (errors.Count == 0)
        {
            return ExitCodes.Success;
        }

        CommandHelpers.WriteValidationErrors(errors);
        return ExitCodes.ValidationFailed;
    }
}
