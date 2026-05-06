using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every line item has a tax rate within the inclusive range [0, 1].</summary>
public sealed class ItemTaxRateRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Items == null)
            yield break;

        for (int i = 0; i < data.Items.Count; i++)
        {
            double rate = data.Items[i].TaxRate;
            if (rate < 0.0 || rate > 1.0)
                yield return new ValidationError($"items[{i}].taxRate", "Item tax rate must be between 0 and 1 inclusive.");
        }
    }
}
