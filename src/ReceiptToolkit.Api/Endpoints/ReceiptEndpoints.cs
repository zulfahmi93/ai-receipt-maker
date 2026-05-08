using Microsoft.AspNetCore.Mvc;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.Core.Validation;

namespace ReceiptToolkit.Api.Endpoints;

/// <summary>
///   Registers the receipt-toolkit HTTP routes against an
///   <see cref="IEndpointRouteBuilder"/>. Each endpoint is a thin shim over
///   <see cref="ReceiptGenerator"/> or <see cref="ReceiptValidator"/>.
/// </summary>
public static class ReceiptEndpoints
{
    private const string SampleResourceName = "ReceiptToolkit.Api.Resources.sample_receipt_data.json";

    /// <summary>
    ///   Maps the GET / health endpoint plus all <c>/api/receipts/*</c> routes.
    /// </summary>
    /// <param name="app">The endpoint builder to register against.</param>
    /// <returns>The supplied <paramref name="app"/> for fluent chaining.</returns>
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/", GetRoot);
        app.MapPost("/api/receipts/validate", PostValidate);
        app.MapPost("/api/receipts/pdf", PostPdf);
        app.MapPost("/api/receipts/png", PostPng);
        app.MapPost("/api/receipts/both", PostBoth);
        app.MapPost("/api/receipts/sample", PostSample);

        return app;
    }

    /// <summary>
    ///   Service identity probe. Returns <c>200 OK</c> with a small JSON document
    ///   containing the service name, the assembly informational version, and a
    ///   literal <c>"ok"</c> status string for health probes.
    /// </summary>
    private static IResult GetRoot()
    {
        var info = new ServiceInfo(
            Service: "receipt-toolkit-api",
            Version: typeof(ReceiptEndpoints).Assembly
                .GetName().Version?.ToString() ?? "0.0.0",
            Status: "ok");
        return Results.Ok(info);
    }

    /// <summary>
    ///   Validates a receipt document and reports every violation in a single
    ///   round-trip. Validation failures do <em>not</em> produce a 4xx response —
    ///   the endpoint always returns <c>200 OK</c> and reports the outcome via the
    ///   <c>valid</c> + <c>errors</c> fields. Structural JSON failures bubble up to
    ///   the global exception handler as a <c>400 ProblemDetails</c>.
    /// </summary>
    private static IResult PostValidate(
        [FromBody] ReceiptData data,
        [FromServices] ReceiptValidator validator)
    {
        IReadOnlyList<ValidationError> errors = validator.Validate(data);
        var response = new ValidateResponse(
            Valid: errors.Count == 0,
            Errors: errors.Count == 0
                ? []
                : [.. errors.Select(e => new ValidationErrorDto(e.Field, e.Message))]);
        return Results.Ok(response);
    }

    /// <summary>
    ///   Generates a PDF for the supplied receipt data. Validation failures bubble up
    ///   as <see cref="ReceiptValidationException"/> and are translated to RFC7807
    ///   <c>400 ProblemDetails</c> by the global exception handler. Generation
    ///   failures translate to <c>500 ProblemDetails</c>.
    /// </summary>
    private static async Task<IResult> PostPdf(
        [FromBody] ReceiptData data,
        [FromServices] ReceiptGenerator generator,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await generator.GeneratePdfAsync(data, cancellationToken).ConfigureAwait(false);
        return Results.File(bytes, contentType: "application/pdf");
    }

    /// <summary>
    ///   Generates a PNG raster (2× scale, with shadow) for the supplied receipt data.
    /// </summary>
    private static async Task<IResult> PostPng(
        [FromBody] ReceiptData data,
        [FromServices] ReceiptGenerator generator,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await generator.GeneratePngAsync(data, cancellationToken).ConfigureAwait(false);
        return Results.File(bytes, contentType: "image/png");
    }

    /// <summary>
    ///   Generates both PDF and PNG outputs in a single round-trip and returns them
    ///   as base64-encoded strings. Preferred over a multipart response for clients
    ///   that cannot stream a ZIP file (e.g. Flutter macOS demo).
    /// </summary>
    private static async Task<IResult> PostBoth(
        [FromBody] ReceiptData data,
        [FromServices] ReceiptGenerator generator,
        CancellationToken cancellationToken)
    {
        byte[] pdfBytes = await generator.GeneratePdfAsync(data, cancellationToken).ConfigureAwait(false);
        byte[] pngBytes = await generator.GeneratePngAsync(data, cancellationToken).ConfigureAwait(false);
        var response = new BothResponse(
            PdfBase64: Convert.ToBase64String(pdfBytes),
            PngBase64: Convert.ToBase64String(pngBytes));
        return Results.Ok(response);
    }

    /// <summary>
    ///   Generates a PDF from the bundled sample fixture. Useful for clients that want
    ///   to preview the rendering pipeline without crafting their own JSON payload.
    /// </summary>
    private static async Task<IResult> PostSample(
        [FromServices] ReceiptGenerator generator,
        CancellationToken cancellationToken)
    {
        ReceiptData sample = LoadEmbeddedSample();
        byte[] bytes = await generator.GeneratePdfAsync(sample, cancellationToken).ConfigureAwait(false);
        return Results.File(bytes, contentType: "application/pdf");
    }

    private static ReceiptData LoadEmbeddedSample()
    {
        using Stream? stream = typeof(ReceiptEndpoints).Assembly
            .GetManifestResourceStream(SampleResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded sample resource '{SampleResourceName}' not found.");
        using var reader = new StreamReader(stream);
        return ReceiptData.FromJson(reader.ReadToEnd());
    }
}

/// <summary>Service identity response body for <c>GET /</c>.</summary>
/// <param name="Service">Service name.</param>
/// <param name="Version">Assembly informational version.</param>
/// <param name="Status">Literal <c>"ok"</c>.</param>
public sealed record ServiceInfo(string Service, string Version, string Status);

/// <summary>Body returned by <c>POST /api/receipts/validate</c>.</summary>
/// <param name="Valid">True when no rules fired.</param>
/// <param name="Errors">Per-field violation list. Empty when <paramref name="Valid"/> is true.</param>
public sealed record ValidateResponse(bool Valid, IReadOnlyList<ValidationErrorDto> Errors);

/// <summary>Wire-format projection of <see cref="ValidationError"/>.</summary>
/// <param name="Field">Dot-notation field path.</param>
/// <param name="Message">Human-readable description.</param>
public sealed record ValidationErrorDto(string Field, string Message);

/// <summary>Body returned by <c>POST /api/receipts/both</c>.</summary>
/// <param name="PdfBase64">Base64-encoded PDF bytes.</param>
/// <param name="PngBase64">Base64-encoded PNG bytes.</param>
public sealed record BothResponse(string PdfBase64, string PngBase64);
