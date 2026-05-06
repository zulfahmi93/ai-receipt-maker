using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every line item has a non-negative discount.</summary>
public sealed class ItemDiscountRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Items == null)
            yield break;

        for (int i = 0; i < data.Items.Count; i++)
        {
            if (data.Items[i].Discount < 0)
                yield return new ValidationError($"items[{i}].discount", "Item discount must be zero or greater.");
        }
    }
}
