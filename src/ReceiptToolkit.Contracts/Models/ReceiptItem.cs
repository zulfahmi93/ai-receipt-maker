using System.Text.Json.Serialization;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts;

/// <summary>A single line item on the receipt.</summary>
public sealed record ReceiptItem
{
    /// <summary>Stock-keeping unit identifier for the item.</summary>
    public string? Sku { get; init; }

    /// <summary>Display name of the item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional short description of the item.</summary>
    public string? Description { get; init; }

    /// <summary>Number of units purchased.</summary>
    public int Quantity { get; init; }

    /// <summary>Unit of measure label (e.g. pcs, kg).</summary>
    public string? Unit { get; init; }

    /// <summary>Price per unit, serialized as a JSON string to preserve monetary scale.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal UnitPrice { get; init; }

    /// <summary>Discount applied to this line item, serialized as a JSON string.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal Discount { get; init; }

    /// <summary>Applicable tax rate as a fractional value (e.g. 0.0825 for 8.25%).</summary>
    public double TaxRate { get; init; }

    /// <summary>Line total after discount, serialized as a JSON string to preserve monetary scale.</summary>
    [JsonConverter(typeof(DecimalStringJsonConverter))]
    public decimal Total { get; init; }
}
