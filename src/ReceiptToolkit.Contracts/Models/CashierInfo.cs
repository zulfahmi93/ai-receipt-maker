namespace ReceiptToolkit.Contracts;

/// <summary>Information about the cashier or operator who processed the transaction.</summary>
public sealed record CashierInfo
{
    /// <summary>Full name of the cashier.</summary>
    public string? CashierName { get; init; }

    /// <summary>Employee or staff identifier for the cashier.</summary>
    public string? CashierId { get; init; }
}
