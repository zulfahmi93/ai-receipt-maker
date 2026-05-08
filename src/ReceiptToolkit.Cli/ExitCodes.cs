namespace ReceiptToolkit.Cli;

/// <summary>
///   Exit-code constants documented as part of the CLI's external contract. Shells
///   and CI pipelines branch on these — never repurpose a number once it ships.
/// </summary>
internal static class ExitCodes
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Environmental failure — input file missing, unreadable, or malformed JSON.</summary>
    public const int InputError = 1;

    /// <summary>Validation failed — the JSON parsed but violated one or more rules.</summary>
    public const int ValidationFailed = 2;

    /// <summary>Output file already exists and <c>--force</c> was not supplied.</summary>
    public const int OverwriteRefused = 3;
}
