using System.Text.Json.Serialization;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts;

/// <summary>Payment tender details for one payment method used on this receipt.</summary>
public sealed record PaymentInfo
{
    /// <summary>Human-readable name of the payment method (e.g. "Cash", "Visa Credit Card").</summary>
    public string? Method { get; init; }

    /// <summary>
    ///   Optional payment-method icon source. Same shape as
    ///   <c>business.businessLogoUrl</c>: a local file path or a <c>data:</c> URI.
    ///   HTTP/HTTPS sources are rejected by <c>LogoResolver</c>. When non-null and
    ///   resolvable, the icon renders inside the PaymentSection 2×2 grid's icon
    ///   column. When null, a paper-coloured placeholder slot is rendered instead.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>Amount paid with this tender, serialized as a JSON string to preserve monetary scale.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal Amount { get; init; }

    /// <summary>Last four digits of the card used, if applicable.</summary>
    public string? CardLastFour { get; init; }

    /// <summary>Authorization code returned by the payment processor.</summary>
    public string? AuthCode { get; init; }

    /// <summary>Unique transaction identifier from the payment processor.</summary>
    public string? TransactionId { get; init; }
}
