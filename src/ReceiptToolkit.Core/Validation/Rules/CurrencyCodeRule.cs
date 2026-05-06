using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Currency;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that <see cref="ReceiptOptions.Currency"/>, when present, is a recognized ISO 4217 currency code.</summary>
public sealed class CurrencyCodeRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        string? code = data.Options?.Currency;

        if (code is null)
            yield break;

        if (!CurrencyTable.TryGet(code, out _))
            yield return new ValidationError("options.currency", $"'{code}' is not a recognized ISO 4217 currency code.");
    }
}
