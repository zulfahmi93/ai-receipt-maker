namespace ReceiptToolkit.Contracts;

/// <summary>Metadata describing the receipt transaction itself.</summary>
public sealed record ReceiptMetadata
{
    /// <summary>Title printed at the top of the receipt (e.g. "RECEIPT").</summary>
    public string? ReceiptTitle { get; init; }

    /// <summary>Unique receipt or invoice number.</summary>
    public string ReceiptNumber { get; init; } = string.Empty;

    /// <summary>Date and time of the transaction in ISO 8601 format.</summary>
    public string? DateTime { get; init; }

    /// <summary>Name of the branch or location where the transaction occurred.</summary>
    public string? BranchName { get; init; }

    /// <summary>Identifier of the POS terminal that processed the transaction.</summary>
    public string? TerminalId { get; init; }

    /// <summary>Order number associated with the transaction.</summary>
    public string? OrderNumber { get; init; }

    /// <summary>External reference number for the transaction.</summary>
    public string? ReferenceNumber { get; init; }
}
