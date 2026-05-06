namespace ReceiptToolkit.Contracts;

/// <summary>Customer information associated with the transaction.</summary>
public sealed record CustomerInfo
{
    /// <summary>Display name of the customer.</summary>
    public string? CustomerName { get; init; }

    /// <summary>Unique identifier for the customer in the loyalty or CRM system.</summary>
    public string? CustomerId { get; init; }

    /// <summary>Customer contact phone number.</summary>
    public string? CustomerPhone { get; init; }

    /// <summary>Customer contact email address.</summary>
    public string? CustomerEmail { get; init; }
}
