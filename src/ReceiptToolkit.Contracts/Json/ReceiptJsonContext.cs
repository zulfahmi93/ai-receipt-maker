using System.Text.Json.Serialization;

namespace ReceiptToolkit.Contracts.Json;

/// <summary>Source-generation context that registers <see cref="ReceiptToolkit.Contracts.ReceiptData"/> for AOT-safe JSON serialization.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(ReceiptToolkit.Contracts.ReceiptData))]
public sealed partial class ReceiptJsonContext : JsonSerializerContext;
