using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Assets;

/// <summary>
///   Resolves a logo source string to an <see cref="SKImage"/>.
/// </summary>
/// <remarks>
///   <para>
///     Three source forms are recognised:
///     <list type="bullet">
///       <item><description>
///         <c>null</c> or whitespace — returns <see langword="null"/> (no image).
///       </description></item>
///       <item><description>
///         A <c>data:image/png;base64,…</c> or <c>data:image/jpeg;base64,…</c> data URI —
///         the base-64 payload is decoded and the image is created from the raw bytes.
///       </description></item>
///       <item><description>
///         Any other string — treated as a local file path and passed directly to
///         <see cref="SKImage.FromEncodedData(string)"/>.
///       </description></item>
///     </list>
///   </para>
///   <para>
///     HTTP/HTTPS URLs are explicitly <em>rejected</em> with a
///     <see cref="NotSupportedException"/> to keep rendering deterministic and SSRF-safe.
///   </para>
/// </remarks>
public static class LogoResolver
{
    /// <summary>
    ///   Resolves <paramref name="source"/> to an <see cref="SKImage"/>.
    /// </summary>
    /// <param name="source">
    ///   The logo source: <see langword="null"/>, a whitespace string, a
    ///   <c>data:image/…;base64,</c> data URI, or a local file path.
    ///   HTTP/HTTPS URLs are rejected.
    /// </param>
    /// <returns>
    ///   A new <see cref="SKImage"/> that the <em>caller</em> owns and must dispose,
    ///   or <see langword="null"/> when <paramref name="source"/> is <see langword="null"/>
    ///   or whitespace.
    /// </returns>
    /// <exception cref="NotSupportedException">
    ///   Thrown when <paramref name="source"/> starts with <c>http://</c> or
    ///   <c>https://</c> (case-insensitive).  HTTP/HTTPS sources are not permitted in
    ///   the renderer to preserve deterministic output and prevent SSRF.
    /// </exception>
    public static SKImage? Resolve(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        // HTTP/HTTPS is explicitly prohibited — deterministic + SSRF-safe rendering.
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "HTTP/HTTPS sources are not supported in the renderer (deterministic + no SSRF). " +
                "Use a local file path or data: URI instead.");
        }

        // data: URI — strip scheme prefix and decode base-64 payload.
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveDataUri(source);
        }

        // Treat as a local file path.
        return SKImage.FromEncodedData(source);
    }

    /// <summary>
    ///   Decodes a <c>data:image/png;base64,…</c> or <c>data:image/jpeg;base64,…</c>
    ///   URI into an <see cref="SKImage"/>.
    /// </summary>
    /// <param name="dataUri">The full data URI string.</param>
    /// <returns>A decoded <see cref="SKImage"/>.</returns>
    /// <exception cref="ArgumentException">
    ///   Thrown when the URI is not <c>image/png</c> or <c>image/jpeg</c>, or when the
    ///   comma separator is missing.
    /// </exception>
    private static SKImage ResolveDataUri(string dataUri)
    {
        // Reject non-image MIME types up front — only PNG/JPEG are supported sources.
        if (!dataUri.Contains("image/png", StringComparison.OrdinalIgnoreCase) &&
            !dataUri.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only data:image/png and data:image/jpeg data URIs are supported.",
                nameof(dataUri));
        }

        int commaIndex = dataUri.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0)
        {
            throw new ArgumentException("Invalid data URI: missing comma separator.", nameof(dataUri));
        }

        string payload = dataUri[(commaIndex + 1)..];
        byte[] bytes = Convert.FromBase64String(payload);
        return SKImage.FromEncodedData(bytes);
    }
}
