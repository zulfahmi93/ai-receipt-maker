namespace ReceiptToolkit.Contracts;

/// <summary>Footer content printed at the bottom of the receipt.</summary>
public sealed record FooterInfo
{
    /// <summary>Primary thank-you message shown to the customer.</summary>
    public string? ThankYouMessage { get; init; }

    /// <summary>Additional note displayed in the footer area.</summary>
    public string? FooterNote { get; init; }

    /// <summary>Return or refund policy statement.</summary>
    public string? ReturnPolicy { get; init; }

    /// <summary>Legal disclaimer or compliance note.</summary>
    public string? LegalNote { get; init; }

    /// <summary>Ordered list of additional free-form footer lines.</summary>
    public IReadOnlyList<string> CustomFooterLines { get; init; } = [];
}
