using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every payment tender has a non-negative amount.</summary>
public sealed class PaymentAmountRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (data.Payments == null)
            yield break;

        for (int i = 0; i < data.Payments.Count; i++)
        {
            if (data.Payments[i].Amount < 0)
                yield return new ValidationError($"payments[{i}].amount", "Payment amount must be zero or greater.");
        }
    }
}
