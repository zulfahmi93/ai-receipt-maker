using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that the receipt contains at least one line item.</summary>
public sealed class ItemsNotEmptyRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Items == null || data.Items.Count < 1)
            yield return new ValidationError("items", "At least one line item is required.");
    }
}
