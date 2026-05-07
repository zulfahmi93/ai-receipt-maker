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
///     Weight selection runs through SkiaSharp 4's
///     <see cref="SKTypeface.Clone(System.ReadOnlySpan{SKFontVariationPositionCoordinate})"/>
///     against the VF's <c>wght</c> axis.  The base typeface (loaded from the embedded
///     stream) is owned separately from the cache; cloned per-weight instances live in
///     the cache and are disposed alongside it.
///   </para>
/// </remarks>
public sealed class FontProvider : IDisposable
{
    private const string EmbeddedResourceName = "ReceiptToolkit.Core.Resources.InterVariable.ttf";
    private const string SupportedFamily = "Inter";

    private static readonly SKFourByteTag WeightAxisTag = new(
        ((uint)'w' << 24) | ((uint)'g' << 16) | ((uint)'h' << 8) | (uint)'t');

    private readonly byte[] _fontBytes;
    private readonly Lazy<SKTypeface> _baseTypeface;
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

        _baseTypeface = new Lazy<SKTypeface>(
            () =>
            {
                using var bs = new MemoryStream(_fontBytes, writable: false);
                return SKTypeface.FromStream(bs, index: 0)
                    ?? throw new InvalidOperationException("Failed to create Inter typeface from embedded resource.");
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
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
    ///   <see cref="SKFontStyleWeight.Bold"/>).  Resolved against the Inter VF's
    ///   <c>wght</c> axis via <see cref="SKTypeface.Clone(System.ReadOnlySpan{SKFontVariationPositionCoordinate})"/>.
    /// </param>
    /// <returns>
    ///   A cached <see cref="SKTypeface"/> owned by this <see cref="FontProvider"/>.
    ///   Do <em>not</em> dispose the returned instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   Thrown when <paramref name="family"/> is not <c>"Inter"</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when SkiaSharp cannot select the requested wght-axis instance from the
    ///   embedded variable font.
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

        return _cache.GetOrAdd((family, weight), CreateTypefaceForWeight);
    }

    private SKTypeface CreateTypefaceForWeight((string Family, SKFontStyleWeight Weight) key)
    {
        SKTypeface baseTf = _baseTypeface.Value;

        Span<SKFontVariationPositionCoordinate> position =
        [
            new SKFontVariationPositionCoordinate { Axis = WeightAxisTag, Value = (int)key.Weight },
        ];

        SKTypeface? resolved = baseTf.Clone(position);
        if (resolved is null || resolved.FontWeight != (int)key.Weight)
        {
            resolved?.Dispose();
            throw new InvalidOperationException(
                $"SkiaSharp could not select the Inter VF wght-axis instance for {key.Weight}.");
        }

        return resolved;
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

        if (_baseTypeface.IsValueCreated)
        {
            _baseTypeface.Value.Dispose();
        }
    }
}
