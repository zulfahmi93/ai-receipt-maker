using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReceiptToolkit.Contracts.Json;

/// <summary>Converts <see cref="decimal"/> to and from a JSON string token to preserve monetary scale.</summary>
public sealed class DecimalStringJsonConverter : JsonConverter<decimal>
{
    /// <summary>Reads a <see cref="decimal"/> from either a JSON string or number token.</summary>
    /// <exception cref="JsonException">
    ///   Thrown when the string token is empty, not a valid decimal, or the token type is unsupported.
    /// </exception>
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ParseStringToken(reader.GetString()),
            JsonTokenType.Number => reader.GetDecimal(),
            _ => throw new JsonException(
                $"Cannot convert token type {reader.TokenType} to decimal."),
        };
    }

    private static decimal ParseStringToken(string? raw)
    {
        // Use TryParse so that empty strings and malformed values surface as JsonException
        // (the correct serialization exception) rather than FormatException.
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return value;

        throw new JsonException(
            $"The value '{raw}' is not a valid decimal string.");
    }

    /// <summary>Writes a <see cref="decimal"/> as a JSON string token, preserving scale.</summary>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}
