using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that <see cref="ReceiptMetadata.ReceiptNumber"/> is non-empty.</summary>
public sealed class ReceiptNumberRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (string.IsNullOrWhiteSpace(data.Receipt?.ReceiptNumber))
            yield return new ValidationError("receipt.receiptNumber", "Receipt number is required.");
    }
}
