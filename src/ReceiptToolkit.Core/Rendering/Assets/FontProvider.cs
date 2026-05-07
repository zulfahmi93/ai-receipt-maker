using System.Collections.Concurrent;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Assets;

/// <summary>
///   Loads and caches SkiaSharp typefaces from the embedded Inter variable font.
/// </summary>
/// <remarks>
///   <para>
///     Only the <c>"Inter"</c> family is supported.  System fonts are never consulted —
///     golden rendering tests depend on pixel-identical output from the embedded resource.
///   </para>
///   <para>
///     Typefaces are cached per <c>(family, weight)</c> key.  Callers <em>must not</em>
///     dispose the returned <see cref="SKTypeface"/> instances; they are owned by this
///     <see cref="FontProvider"/> and disposed when <see cref="Dispose"/> is called.
///   </para>
///   <para>
///     Weight selection via the <c>wght</c> axis is supported by the Inter variable font.
///     The current Phase 3 implementation loads the VF default instance for every weight
///     request; the <c>weight</c> parameter of <see cref="GetTypeface"/> is reserved for
///     full axis selection in Phase 3b.
///   </para>
/// </remarks>
public sealed class FontProvider : IDisposable
{
    private const string EmbeddedResourceName = "ReceiptToolkit.Core.Resources.InterVariable.ttf";
    private const string SupportedFamily = "Inter";

    private readonly byte[] _fontBytes;
    private readonly ConcurrentDictionary<(string Family, SKFontStyleWeight Weight), SKTypeface> _cache = new();
    private bool _disposed;

    /// <summary>
    ///   Initialises a new <see cref="FontProvider"/> and eagerly loads the embedded
    ///   Inter font bytes into memory (typefaces are created lazily on first request).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when the embedded Inter font resource cannot be found in the assembly
    ///   manifest, indicating a broken build configuration.
    /// </exception>
    public FontProvider()
    {
        using Stream? stream = typeof(FontProvider).Assembly
            .GetManifestResourceStream(EmbeddedResourceName);

        if (stream is null)
        {
            throw new InvalidOperationException("Embedded Inter font resource missing.");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _fontBytes = ms.ToArray();
    }

    /// <summary>
    ///   Returns a cached <see cref="SKTypeface"/> for the specified
    ///   <paramref name="family"/> and <paramref name="weight"/>.
    /// </summary>
    /// <param name="family">
    ///   The font family name.  Only <c>"Inter"</c> is accepted; any other value
    ///   throws <see cref="ArgumentException"/> — system fallbacks are never used.
    /// </param>
    /// <param name="weight">
    ///   The desired font weight (e.g. <see cref="SKFontStyleWeight.Normal"/>,
    ///   <see cref="SKFontStyleWeight.Bold"/>).  Reserved for full variable-font axis
    ///   selection in Phase 3b; the current implementation returns the VF default instance.
    /// </param>
    /// <returns>
    ///   A cached <see cref="SKTypeface"/> owned by this <see cref="FontProvider"/>.
    ///   Do <em>not</em> dispose the returned instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   Thrown when <paramref name="family"/> is not <c>"Inter"</c>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   Thrown when called after <see cref="Dispose"/> has been called.
    /// </exception>
    public SKTypeface GetTypeface(string family, SKFontStyleWeight weight)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!string.Equals(family, SupportedFamily, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Only the embedded \"Inter\" family is supported.",
                nameof(family));
        }

        return _cache.GetOrAdd(
            (family, weight),
            static (_, bytes) =>
            {
                using var ms = new MemoryStream(bytes, writable: false);
                return SKTypeface.FromStream(ms, index: 0)
                    ?? throw new InvalidOperationException("Failed to create Inter typeface from embedded resource.");
            },
            _fontBytes);
    }

    /// <summary>
    ///   Disposes all cached typefaces and releases the font bytes buffer.
    ///   After disposal, calls to <see cref="GetTypeface"/> will throw
    ///   <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (SKTypeface tf in _cache.Values)
        {
            tf.Dispose();
        }

        _cache.Clear();
    }
}
