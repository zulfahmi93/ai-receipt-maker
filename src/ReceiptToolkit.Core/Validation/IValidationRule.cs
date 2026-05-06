using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation;

/// <summary>Defines a single validation rule that inspects a <see cref="ReceiptData"/> instance and returns any violations found.</summary>
public interface IValidationRule
{
    /// <summary>Validates the supplied receipt data and returns zero or more errors.</summary>
    /// <param name="data">The receipt data to validate.</param>
    /// <returns>A sequence of <see cref="ValidationError"/> instances, one per violation. Returns an empty sequence when the data satisfies this rule.</returns>
    IEnumerable<ValidationError> Validate(ReceiptData data);
}
