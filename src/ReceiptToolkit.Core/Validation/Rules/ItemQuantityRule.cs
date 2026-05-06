using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every line item has a quantity greater than zero.</summary>
public sealed class ItemQuantityRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Items == null)
            yield break;

        for (int i = 0; i < data.Items.Count; i++)
        {
            if (data.Items[i].Quantity <= 0)
                yield return new ValidationError($"items[{i}].quantity", "Item quantity must be greater than zero.");
        }
    }
}
