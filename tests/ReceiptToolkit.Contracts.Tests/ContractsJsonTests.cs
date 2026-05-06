// Purpose: RED-phase tests for Phase 1 (T1.1–T1.7) — Contracts + JSON parsing.
// Categories: Unit — pure in-process, no I/O beyond fixture file read.
// Edge cases: minimal JSON, full fixture, decimal-as-string, schema version default,
//             source-gen context wiring, unknown field tolerance, round-trip equality.
// All 7 tests are expected to FAIL in RED phase (NotImplementedException from stubs).

using System.Text.Json;
using ReceiptToolkit.Contracts.Json;

namespace ReceiptToolkit.Contracts.Tests;

public sealed class ContractsJsonTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_receipt_data.json");

    // -------------------------------------------------------------------------
    // T1.1 — Parse minimal JSON that matches the real schema nesting:
    //        business.businessName, receipt.receiptNumber, items[one item].
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_1_ParsesMinimalJson()
    {
        const string json = """
            {
              "business": { "businessName": "X" },
              "receipt": { "receiptNumber": "R-1" },
              "items": [
                {
                  "name": "Widget",
                  "quantity": 1,
                  "unitPrice": "9.99",
                  "discount": "0",
                  "total": "9.99"
                }
              ]
            }
            """;

        var data = ReceiptData.FromJson(json);

        Assert.NotNull(data);
        Assert.Equal("X", data.Business.BusinessName);
        Assert.Equal("R-1", data.Receipt.ReceiptNumber);
        Assert.Single(data.Items);
        Assert.Equal("Widget", data.Items[0].Name);
    }

    // -------------------------------------------------------------------------
    // T1.2 — Parse the full sample fixture: every top-level node resolved,
    //         spot-check load-bearing values.
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_2_ParsesFullSampleFixture()
    {
        var json = File.ReadAllText(FixturePath);

        var data = ReceiptData.FromJson(json);

        Assert.NotNull(data);
        // business
        Assert.Equal("Elevate Studio", data.Business.BusinessName);
        // items
        Assert.Equal(5, data.Items.Count);
        // payments
        Assert.Single(data.Payments);
        Assert.Equal(56.73m, data.Payments[0].Amount);
        // totals
        Assert.Equal(56.73m, data.Totals.GrandTotal);
        // optional top-level nodes all resolved
        Assert.NotNull(data.Customer);
        Assert.NotNull(data.Cashier);
        Assert.NotNull(data.Qr);
        Assert.NotNull(data.Footer);
        Assert.NotNull(data.Theme);
        Assert.NotNull(data.Layout);
        Assert.NotNull(data.Options);
    }

    // -------------------------------------------------------------------------
    // T1.3 — Decimal money fields serialize as JSON **string**, not number.
    //         Round-trip: produce JSON, assert string token, re-parse, check value.
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_3_DecimalMoneyFieldsSerializeAsJsonString()
    {
        // Build a minimal ReceiptData with known decimal values.
        var data = new ReceiptData
        {
            Business = new BusinessInfo { BusinessName = "Test" },
            Receipt = new ReceiptMetadata { ReceiptNumber = "R-99" },
            Items =
            [
                new ReceiptItem
                {
                    Name = "Item A",
                    Quantity = 1,
                    UnitPrice = 12.50m,
                    Discount = 0m,
                    Total = 12.50m,
                }
            ],
            Totals = new ReceiptTotals { GrandTotal = 56.73m },
            Payments = [new PaymentInfo { Amount = 56.73m }],
        };

        var json = data.ToJson();

        // Assert the decimal is emitted as a JSON string token (has surrounding quotes).
        Assert.Contains("\"unitPrice\":\"12.50\"", json);

        // Re-parse and verify decimal value survives round-trip.
        var reparsed = ReceiptData.FromJson(json);
        Assert.Equal(12.50m, reparsed.Items[0].UnitPrice);
        Assert.Equal(56.73m, reparsed.Totals.GrandTotal);
        Assert.Equal(56.73m, reparsed.Payments[0].Amount);

        // Confirm via JsonDocument that the node kind is String, not Number.
        using var doc = JsonDocument.Parse(json);
        var unitPriceKind = doc.RootElement
            .GetProperty("items")[0]
            .GetProperty("unitPrice")
            .ValueKind;
        Assert.Equal(JsonValueKind.String, unitPriceKind);
    }

    // -------------------------------------------------------------------------
    // T1.4 — schemaVersion defaults to 1 when absent; serializes when present.
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_4_SchemaVersionDefaultsToOneAndSerializesBack()
    {
        // Parse JSON with no schemaVersion → default 1.
        const string jsonNoVersion = """
            {
              "business": { "businessName": "Y" },
              "receipt": { "receiptNumber": "R-2" },
              "items": []
            }
            """;

        var parsed = ReceiptData.FromJson(jsonNoVersion);
        Assert.Equal(1, parsed.SchemaVersion);

        // Serialize a default-constructed ReceiptData → JSON contains schemaVersion:1.
        var defaultData = new ReceiptData();
        var serialized = defaultData.ToJson();
        Assert.Contains("\"schemaVersion\":1", serialized);
    }

    // -------------------------------------------------------------------------
    // T1.5 — [JsonSerializable] source-gen context is wired and used.
    //         ReceiptJsonContext.Default.ReceiptData must be non-null.
    //         JsonSerializer.Serialize via the type-info must equal ToJson().
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_5_SourceGenContextIsWiredAndUsed()
    {
        var typeInfo = ReceiptJsonContext.Default.ReceiptData;
        Assert.NotNull(typeInfo);

        var data = new ReceiptData
        {
            Business = new BusinessInfo { BusinessName = "AOT" },
            Receipt = new ReceiptMetadata { ReceiptNumber = "R-3" },
        };

        var viaContext = JsonSerializer.Serialize(data, typeInfo);
        var viaToJson = data.ToJson();

        // Both paths must produce equivalent JSON.
        Assert.Equal(viaToJson, viaContext);
    }

    // -------------------------------------------------------------------------
    // T1.6 — Unknown / extra JSON fields are ignored without throwing.
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_6_UnknownFieldsIgnoredWithoutError()
    {
        // Inject unknown fields at root and inside business.
        var baseJson = File.ReadAllText(FixturePath);

        // Insert unknown field at root level and inside business object.
        var augmented = baseJson
            .Replace(
                "\"schemaVersion\": 1,",
                "\"schemaVersion\": 1, \"someUnknownField\": \"ignored\",")
            .Replace(
                "\"businessName\": \"Elevate Studio\",",
                "\"businessName\": \"Elevate Studio\", \"unknownNested\": true,");

        var data = ReceiptData.FromJson(augmented);

        Assert.NotNull(data);
        Assert.Equal("Elevate Studio", data.Business.BusinessName);
    }

    // -------------------------------------------------------------------------
    // T1.7 — Round-trip equality: FromJson → ToJson → FromJson produces equal data.
    //         Uses normalized JSON comparison (JsonNode.DeepEquals) because
    //         IReadOnlyList<T> inside a record does not auto-deep-equal by value.
    // -------------------------------------------------------------------------
    [Fact]
    public void T1_7_RoundTripProducesEqualData()
    {
        var fixtureJson = File.ReadAllText(FixturePath);

        var a = ReceiptData.FromJson(fixtureJson);
        var b = ReceiptData.FromJson(a.ToJson());

        // Normalize both back to JSON and use JsonNode.DeepEquals to compare structure,
        // avoiding C# record equality limitations on collection properties.
        var nodeA = System.Text.Json.Nodes.JsonNode.Parse(a.ToJson());
        var nodeB = System.Text.Json.Nodes.JsonNode.Parse(b.ToJson());

        Assert.True(
            System.Text.Json.Nodes.JsonNode.DeepEquals(nodeA, nodeB),
            "Round-tripped JSON must be structurally identical to the first serialization.");
    }
}
