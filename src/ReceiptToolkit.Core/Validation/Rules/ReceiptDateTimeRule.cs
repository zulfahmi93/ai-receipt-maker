using System.Globalization;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that <see cref="ReceiptMetadata.DateTime"/>, when present, is a parseable ISO 8601 date-time string.</summary>
public sealed class ReceiptDateTimeRule : IValidationRule
{
    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        string? value = data.Receipt?.DateTime;

        if (value is null)
            yield break;

        bool parsed = DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out _);

        if (!parsed)
            yield return new ValidationError("receipt.dateTime", "Receipt date/time must be a valid ISO 8601 string.");
    }
}
