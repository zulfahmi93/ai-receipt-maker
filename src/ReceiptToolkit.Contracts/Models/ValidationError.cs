namespace ReceiptToolkit.Contracts;

/// <summary>Represents a single validation failure, identifying the field and describing the problem.</summary>
/// <param name="Field">Dot-notation path to the field that failed validation (e.g. <c>items[0].quantity</c>).</param>
/// <param name="Message">Human-readable description of why the field failed validation.</param>
public sealed record ValidationError(string Field, string Message);
