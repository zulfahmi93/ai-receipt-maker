namespace ReceiptToolkit.Contracts;

/// <summary>QR code and supplemental URL information printed on the receipt.</summary>
public sealed record QrInfo
{
    /// <summary>Data encoded into the QR code (typically a URL).</summary>
    public string? QrCodeValue { get; init; }

    /// <summary>Instructional label displayed below the QR code.</summary>
    public string? QrCodeLabel { get; init; }

    /// <summary>URL for customers to leave a review.</summary>
    public string? ReviewUrl { get; init; }

    /// <summary>URL where the customer can view the digital receipt.</summary>
    public string? DigitalReceiptUrl { get; init; }

    /// <summary>URL for customer support.</summary>
    public string? SupportUrl { get; init; }
}
