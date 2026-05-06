using System.Text.Json.Serialization;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts;

/// <summary>Payment tender details for one payment method used on this receipt.</summary>
public sealed record PaymentInfo
{
    /// <summary>Human-readable name of the payment method (e.g. "Visa Credit Card").</summary>
    public string? Method { get; init; }

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
