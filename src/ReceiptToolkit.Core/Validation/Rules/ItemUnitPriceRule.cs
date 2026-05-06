using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every line item has a non-negative unit price.</summary>
public sealed class ItemUnitPriceRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Items == null)
            yield break;

        for (int i = 0; i < data.Items.Count; i++)
        {
            if (data.Items[i].UnitPrice < 0)
                yield return new ValidationError($"items[{i}].unitPrice", "Item unit price must be zero or greater.");
        }
    }
}
