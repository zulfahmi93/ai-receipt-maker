namespace ReceiptToolkit.Contracts;

/// <summary>Display and behavior options for receipt generation.</summary>
public sealed record ReceiptOptions
{
    /// <summary>ISO 4217 currency code (e.g. "MYR").</summary>
    public string? Currency { get; init; }

    /// <summary>Currency symbol displayed on the receipt (e.g. "RM").</summary>
    public string? CurrencySymbol { get; init; }

    /// <summary>BCP 47 locale tag used for number and date formatting (e.g. "ms-MY").</summary>
    public string? Locale { get; init; }

    /// <summary>Date format pattern applied to the transaction date.</summary>
    public string? DateFormat { get; init; }

    /// <summary>Time format pattern applied to the transaction time.</summary>
    public string? TimeFormat { get; init; }

    /// <summary>Whether to display the item description line beneath each item name.</summary>
    public bool ShowItemDescription { get; init; }

    /// <summary>Whether to display the SKU code for each line item.</summary>
    public bool ShowSku { get; init; }

    /// <summary>Whether to display an itemized tax breakdown section.</summary>
    public bool ShowTaxBreakdown { get; init; }

    /// <summary>Whether to render the QR code block on the receipt.</summary>
    public bool ShowQrCode { get; init; }

    /// <summary>Whether to render the business logo on the receipt.</summary>
    public bool ShowLogo { get; init; }

    /// <summary>Whether to display contact information in the footer.</summary>
    public bool ShowFooterContact { get; init; }

    /// <summary>Whether totals should be automatically calculated from line items.</summary>
    public bool AutoCalculateTotals { get; init; }
}
