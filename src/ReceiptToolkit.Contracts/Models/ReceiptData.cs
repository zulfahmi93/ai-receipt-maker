using System.Text.Json;
using System.Text.Json.Nodes;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts;

/// <summary>Root model representing a complete receipt document.</summary>
/// <remarks>
///   <para>
///     C# record equality is value-based for scalar properties but reference-based for
///     collection properties (<see cref="Items"/>, <see cref="Payments"/>).  Do not rely on
///     <c>==</c> or <see cref="object.Equals(object?)"/> for structural comparison of two
///     <see cref="ReceiptData"/> instances that contain collections; use JSON round-trip
///     comparison via <see cref="System.Text.Json.Nodes.JsonNode.DeepEquals"/> instead
///     (see T1.7).
///   </para>
///   <!-- TODO (Phase 2+): if deep value equality over collections becomes a hard requirement,
///        override Equals/GetHashCode or introduce a dedicated structural comparer rather than
///        extending the JSON round-trip workaround. -->
/// </remarks>
public sealed record ReceiptData
{
    /// <summary>Schema version of the receipt document format. Defaults to <c>1</c>.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Business information shown in the receipt header.</summary>
    public BusinessInfo Business { get; init; } = new();

    /// <summary>Transaction metadata such as receipt number, date, and branch.</summary>
    public ReceiptMetadata Receipt { get; init; } = new();

    /// <summary>Optional customer information associated with the transaction.</summary>
    public CustomerInfo? Customer { get; init; }

    /// <summary>Optional cashier or operator information.</summary>
    public CashierInfo? Cashier { get; init; }

    /// <summary>Ordered list of line items purchased in this transaction.</summary>
    public IReadOnlyList<ReceiptItem> Items { get; init; } = [];

    /// <summary>Aggregated monetary totals for the receipt.</summary>
    public ReceiptTotals Totals { get; init; } = new();

    /// <summary>One or more payment tenders used to settle this transaction.</summary>
    public IReadOnlyList<PaymentInfo> Payments { get; init; } = [];

    /// <summary>Optional QR code and supplemental URL information.</summary>
    public QrInfo? Qr { get; init; }

    /// <summary>Optional footer content printed at the bottom of the receipt.</summary>
    public FooterInfo? Footer { get; init; }

    /// <summary>Optional color theme tokens for receipt rendering.</summary>
    public ReceiptTheme? Theme { get; init; }

    /// <summary>Optional layout and spacing configuration.</summary>
    public ReceiptLayout? Layout { get; init; }

    /// <summary>Optional display and behavior options.</summary>
    public ReceiptOptions? Options { get; init; }

    /// <summary>Deserializes a <see cref="ReceiptData"/> instance from a JSON string.</summary>
    /// <remarks>
    ///   When <c>schemaVersion</c> is absent from the JSON, the property defaults to <c>1</c>.
    ///   An explicit <c>"schemaVersion": 0</c> is preserved as-is; only a truly absent key
    ///   triggers the default.  Detection uses a <see cref="JsonNode"/> pre-pass so the
    ///   sentinel value (CLR default <c>0</c>) is never confused with a deliberate zero.
    /// </remarks>
    public static ReceiptData FromJson(string json)
    {
        var data = JsonSerializer.Deserialize(json, ReceiptJsonContext.Default.ReceiptData)
            ?? throw new JsonException("Receipt JSON deserialized to null.");

        // The source-gen deserializer writes the CLR default (0) when schemaVersion is absent
        // from the JSON, ignoring the C# property initializer default (1).  Apply the default
        // only when the key was genuinely absent — not when the caller explicitly wrote 0.
        // TODO: if a legitimate schemaVersion 0 ever exists, replace this with a proper
        //       versioned migration strategy rather than extending the sentinel pattern.
        if (data.SchemaVersion == 0)
        {
            var schemaVersionPresent =
                JsonNode.Parse(json)?["schemaVersion"] is not null;

            if (!schemaVersionPresent)
                return data with { SchemaVersion = 1 };
        }

        return data;
    }

    /// <summary>Serializes this instance to a compact camelCase JSON string.</summary>
    public string ToJson() =>
        JsonSerializer.Serialize(this, ReceiptJsonContext.Default.ReceiptData);
}
