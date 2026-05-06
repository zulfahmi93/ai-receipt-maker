using System.Text.Json.Serialization;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts;

/// <summary>Aggregated monetary totals for the receipt.</summary>
public sealed record ReceiptTotals
{
    /// <summary>Sum of all line item totals before discounts and charges.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal Subtotal { get; init; }

    /// <summary>Total discount deducted from the subtotal.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal DiscountTotal { get; init; }

    /// <summary>Service charge applied to the transaction.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal ServiceCharge { get; init; }

    /// <summary>Display label for the tax line (e.g. "Tax (8.25%)").</summary>
    public string? TaxLabel { get; init; }

    /// <summary>Total tax amount charged.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal TaxTotal { get; init; }

    /// <summary>Rounding adjustment applied to reach the final amount.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal RoundingAdjustment { get; init; }

    /// <summary>Final amount due after all adjustments.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal GrandTotal { get; init; }
}
