namespace ReceiptToolkit.Contracts;

/// <summary>Business information displayed in the receipt header.</summary>
public sealed record BusinessInfo
{
    /// <summary>Trading name displayed at the top of the receipt.</summary>
    public string BusinessName { get; init; } = string.Empty;

    /// <summary>Optional tagline shown beneath the business name.</summary>
    public string? BusinessTagline { get; init; }

    /// <summary>URL of the business logo image.</summary>
    public string? BusinessLogoUrl { get; init; }

    /// <summary>Physical address of the business.</summary>
    public string? BusinessAddress { get; init; }

    /// <summary>Contact email address for the business.</summary>
    public string? BusinessEmail { get; init; }

    /// <summary>Contact phone number for the business.</summary>
    public string? BusinessPhone { get; init; }

    /// <summary>Business website URL.</summary>
    public string? BusinessWebsite { get; init; }

    /// <summary>Social media handle for the business.</summary>
    public string? SocialHandle { get; init; }

    /// <summary>Tax or VAT registration number.</summary>
    public string? TaxRegistrationNumber { get; init; }
}
