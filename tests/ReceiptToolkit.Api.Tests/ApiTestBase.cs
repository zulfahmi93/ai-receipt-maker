// Purpose: Shared helpers for Phase 5 API tests — fixture loading, JSON serialization
//          options aligned with the API's camelCase contract, and a reusable
//          WebApplicationFactory<Program> per test class.
// Categories: Test infrastructure — wraps Microsoft.AspNetCore.Mvc.Testing boilerplate
//             so each endpoint test stays focused on the route under test.

using System.Text.Json;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Api.Tests;

internal static class ApiTestBase
{
    /// <summary>JSON options matching the API surface (camelCase, lenient on unknown).</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///   Loads <c>Fixtures/sample_receipt_data.json</c> raw JSON text from the test output
    ///   directory (copied via the csproj <c>Content Include</c>).
    /// </summary>
    public static string LoadSampleJson()
    {
        string fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "sample_receipt_data.json");
        return File.ReadAllText(fixturePath);
    }

    /// <summary>Loads the sample fixture as a <see cref="ReceiptData"/> instance.</summary>
    public static ReceiptData LoadSampleData() => ReceiptData.FromJson(LoadSampleJson());
}
