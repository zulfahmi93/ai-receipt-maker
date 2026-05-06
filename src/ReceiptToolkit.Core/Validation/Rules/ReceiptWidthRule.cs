using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that <see cref="ReceiptLayout.ReceiptWidth"/>, when <see cref="ReceiptData.Layout"/> is non-null, is within the accepted range [200, 1200].</summary>
public sealed class ReceiptWidthRule : IValidationRule
{
    private const int MinWidth = 200;
    private const int MaxWidth = 1200;

    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        ReceiptLayout? layout = data.Layout;

        if (layout is null)
            yield break;

        if (layout.ReceiptWidth < MinWidth || layout.ReceiptWidth > MaxWidth)
            yield return new ValidationError(
                "layout.receiptWidth",
                $"Receipt width {layout.ReceiptWidth} is out of the valid range [{MinWidth}, {MaxWidth}].");
    }
}
