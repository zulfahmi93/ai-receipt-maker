using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that <see cref="BusinessInfo.BusinessName"/> is non-empty.</summary>
public sealed class BusinessNameRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        if (string.IsNullOrWhiteSpace(data.Business?.BusinessName))
            yield return new ValidationError("business.businessName", "Business name is required.");
    }
}
