using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Validation.Rules;

namespace ReceiptToolkit.Core.Validation;

/// <summary>Runs all registered <see cref="IValidationRule"/> instances against a <see cref="ReceiptData"/> document and aggregates the results.</summary>
public sealed class ReceiptValidator
{
    private readonly IReadOnlyList<IValidationRule> _rules;

    /// <summary>Initializes a new <see cref="ReceiptValidator"/> with the built-in rule set.</summary>
    public ReceiptValidator() : this(DefaultRules()) { }

    /// <summary>Initializes a new <see cref="ReceiptValidator"/> with a custom rule composition, suitable for dependency injection.</summary>
    /// <param name="rules">The rules to execute during validation. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules"/> is <see langword="null"/>.</exception>
    public ReceiptValidator(IEnumerable<IValidationRule> rules) =>
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));

    /// <summary>Validates the supplied <see cref="ReceiptData"/> against all registered rules and returns every violation found.</summary>
    /// <param name="data">The receipt data to validate. Must not be <see langword="null"/>.</param>
    /// <returns>A read-only list containing one <see cref="ValidationError"/> per violation. Returns an empty list when the data is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<ValidationError> Validate(ReceiptData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var errors = new List<ValidationError>();
        foreach (IValidationRule rule in _rules)
            errors.AddRange(rule.Validate(data));
        return errors;
    }

    private static IReadOnlyList<IValidationRule> DefaultRules() =>
    [
        new BusinessNameRule(),
        new ReceiptNumberRule(),
        new ItemsNotEmptyRule(),
        new ItemQuantityRule(),
        new ItemUnitPriceRule(),
        new ItemDiscountRule(),
        new ItemTaxRateRule(),
        new PaymentAmountRule(),
        new ReceiptDateTimeRule(),
        new ThemeColorsRule(),
        new CurrencyCodeRule(),
        new ReceiptWidthRule(),
    ];
}
