using System.Text.Json;
using System.Text.Json.Serialization;
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
/// </remarks>
public sealed record ReceiptData
{
    /// <summary>Constructs a <see cref="ReceiptData"/> with an explicit schema version.</summary>
    /// <param name="schemaVersion">
    ///   Document schema version. Defaults to <c>1</c> when the JSON omits the
    ///   <c>schemaVersion</c> field, because System.Text.Json honors the parameter default
    ///   instead of the property initializer when the constructor is annotated with
    ///   <see cref="JsonConstructorAttribute"/>.
    /// </param>
    [JsonConstructor]
    public ReceiptData(int schemaVersion = 1)
    {
        SchemaVersion = schemaVersion;
    }

    /// <summary>Schema version of the receipt document format. Defaults to <c>1</c>.</summary>
    public int SchemaVersion { get; init; }

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
    ///   When the JSON omits <c>schemaVersion</c>, the value defaults to <c>1</c> via the
    ///   <see cref="JsonConstructorAttribute"/>-annotated constructor's parameter default.
    ///   An explicit <c>"schemaVersion": 0</c> is preserved as-is.
    /// </remarks>
    public static ReceiptData FromJson(string json) =>
        JsonSerializer.Deserialize(json, ReceiptJsonContext.Default.ReceiptData)
            ?? throw new JsonException("Receipt JSON deserialized to null.");

    /// <summary>Serializes this instance to a compact camelCase JSON string.</summary>
    public string ToJson() =>
        JsonSerializer.Serialize(this, ReceiptJsonContext.Default.ReceiptData);
}
