namespace ReceiptToolkit.Contracts;

/// <summary>
///   Thrown by the <c>ReceiptGenerator</c> façade when a <see cref="ReceiptData"/>
///   instance fails one or more validation rules. The full error list is preserved on
///   <see cref="Errors"/> so callers can surface every violation in a single round-trip.
/// </summary>
public sealed class ReceiptValidationException : Exception
{
    /// <summary>The validation failures that caused this exception. Never <see langword="null"/>.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Initialises a new <see cref="ReceiptValidationException"/> with no errors.</summary>
    public ReceiptValidationException()
        : base(BuildMessage([]))
    {
        Errors = [];
    }

    /// <summary>Initialises a new <see cref="ReceiptValidationException"/> with a custom message.</summary>
    /// <param name="message">The exception message.</param>
    public ReceiptValidationException(string message)
        : base(message)
    {
        Errors = [];
    }

    /// <summary>Initialises a new <see cref="ReceiptValidationException"/> with a custom message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReceiptValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [];
    }

    /// <summary>Initialises a new <see cref="ReceiptValidationException"/> from a list of validation errors.</summary>
    /// <param name="errors">The validation failures. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is <see langword="null"/>.</exception>
    public ReceiptValidationException(IReadOnlyList<ValidationError> errors)
        : base(BuildMessage(errors))
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<ValidationError> errors) =>
        $"Receipt validation failed with {errors.Count} error(s).";
}
