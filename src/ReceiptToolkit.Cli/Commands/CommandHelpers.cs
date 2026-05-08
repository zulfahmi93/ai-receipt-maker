using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Cli.Commands;

/// <summary>
///   Shared helpers used by validate, generate, and sample commands. Centralises the
///   "read + parse + report" plumbing so each command stays focused on its own
///   business logic and exit-code mapping.
/// </summary>
internal static class CommandHelpers
{
    /// <summary>
    ///   Attempts to read <paramref name="path"/> and parse it as <see cref="ReceiptData"/>.
    ///   On failure writes a structured stderr message and returns <see langword="false"/>;
    ///   the caller maps that to <see cref="ExitCodes.InputError"/>.
    /// </summary>
    /// <param name="path">Absolute or relative path to the JSON file.</param>
    /// <param name="data">Receives the parsed <see cref="ReceiptData"/> on success.</param>
    public static bool TryReadReceiptJson(string path, out ReceiptData data)
    {
        try
        {
            string json = File.ReadAllText(path);
            data = ReceiptData.FromJson(json);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Failed to read '{path}': {ex.Message}");
            data = null!;
            return false;
        }
    }

    /// <summary>
    ///   Writes a uniform "Validation failed with N error(s)" header to stderr followed
    ///   by one indented line per <see cref="ValidationError"/>. Used by both the
    ///   validate command (after running the rule set directly) and the generate /
    ///   sample commands (after catching <see cref="ReceiptValidationException"/>).
    /// </summary>
    public static void WriteValidationErrors(IReadOnlyList<ValidationError> errors)
    {
        Console.Error.WriteLine($"Validation failed with {errors.Count} error(s):");
        foreach (ValidationError error in errors)
        {
            Console.Error.WriteLine($"  - {error.Field}: {error.Message}");
        }
    }
}
