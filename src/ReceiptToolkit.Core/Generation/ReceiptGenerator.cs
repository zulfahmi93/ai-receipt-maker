using ReceiptToolkit.Contracts;
using ReceiptToolkit.Contracts.Time;
using ReceiptToolkit.Core.Calculation;
using ReceiptToolkit.Core.Rendering.Assets;
using ReceiptToolkit.Core.Rendering.Exporters;
using ReceiptToolkit.Core.Time;
using ReceiptToolkit.Core.Validation;
using SkiaSharp;

namespace ReceiptToolkit.Core.Generation;

/// <summary>
///   Top-level façade that turns a <see cref="ReceiptData"/> instance into PDF, PNG,
///   or SVG bytes. Owns the <see cref="ReceiptValidator"/>, <see cref="ReceiptCalculator"/>,
///   <see cref="LogoResolver"/>, and exporter wiring so callers (CLI, API, Telegram bot,
///   Flutter demo) consume a single entry point.
/// </summary>
/// <remarks>
///   <para>
///     Each generate call validates the input, recalculates totals when
///     <see cref="ReceiptOptions.AutoCalculateTotals"/> is <see langword="true"/>,
///     resolves the business logo once via <see cref="LogoResolver"/>, and feeds
///     the prepared <see cref="ReceiptData"/> to the matching exporter.
///   </para>
///   <para>
///     <see cref="IClock"/> injection threads the document creation timestamp through
///     <see cref="PdfExporter"/> so deterministic test clocks can produce byte-equal
///     output across runs (golden tests + idempotence checks rely on this).
///   </para>
///   <para>
///     <b>Lifetime:</b> the parameterless constructor allocates a private
///     <see cref="FontProvider"/> that is disposed when the generator is disposed.
///     The <see cref="ReceiptGenerator(IClock, FontProvider, ReceiptValidator?)"/>
///     overload treats the supplied <see cref="FontProvider"/> as caller-owned.
///   </para>
/// </remarks>
public sealed class ReceiptGenerator : IDisposable
{
    private readonly IClock _clock;
    private readonly FontProvider _fonts;
    private readonly bool _ownsFonts;
    private readonly ReceiptValidator _validator;
    private bool _disposed;

    /// <summary>
    ///   Initialises a new <see cref="ReceiptGenerator"/> with a <see cref="SystemClock"/>,
    ///   a freshly allocated <see cref="FontProvider"/>, and the default validator rule
    ///   set. The font provider's lifetime is bound to this generator instance.
    /// </summary>
    public ReceiptGenerator()
        : this(new SystemClock(), new FontProvider(), validator: null, ownsFonts: true) { }

    /// <summary>
    ///   Initialises a new <see cref="ReceiptGenerator"/> with caller-supplied
    ///   <paramref name="clock"/> and <paramref name="fonts"/>. The font provider is
    ///   treated as caller-owned and is not disposed by this generator.
    /// </summary>
    /// <param name="clock">Source of the document creation timestamp.</param>
    /// <param name="fonts">Caller-owned font provider.</param>
    /// <param name="validator">
    ///   Optional custom validator. When <see langword="null"/> the default rule set
    ///   is used.
    /// </param>
    public ReceiptGenerator(IClock clock, FontProvider fonts, ReceiptValidator? validator = null)
        : this(clock, fonts, validator, ownsFonts: false) { }

    private ReceiptGenerator(IClock clock, FontProvider fonts, ReceiptValidator? validator, bool ownsFonts)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(fonts);
        _clock = clock;
        _fonts = fonts;
        _ownsFonts = ownsFonts;
        _validator = validator ?? new ReceiptValidator();
    }

    /// <summary>
    ///   Validates <paramref name="data"/>, recalculates totals when configured, and
    ///   renders to a PDF byte stream.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF bytes (starting with the <c>%PDF-</c> magic).</returns>
    /// <exception cref="ReceiptValidationException">
    ///   Thrown when validation fails; <see cref="ReceiptValidationException.Errors"/>
    ///   carries the full violation list.
    /// </exception>
    public Task<byte[]> GeneratePdfAsync(ReceiptData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceiptData prepared = ValidateAndCalculate(data);
        using SKImage? logo = ResolveLogo(prepared);
        using SKImage? paymentIcon = ResolvePaymentIcon(prepared);
        var exporter = new PdfExporter(_clock, _fonts);
        return Task.FromResult(exporter.Export(prepared, logo, paymentIcon));
    }

    /// <summary>
    ///   Validates <paramref name="data"/>, recalculates totals when configured, and
    ///   renders to a PNG byte stream at the exporter's default 2× scale with shadow.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PNG bytes (starting with the <c>89 50 4E 47</c> magic).</returns>
    /// <exception cref="ReceiptValidationException">
    ///   Thrown when validation fails; <see cref="ReceiptValidationException.Errors"/>
    ///   carries the full violation list.
    /// </exception>
    public Task<byte[]> GeneratePngAsync(ReceiptData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceiptData prepared = ValidateAndCalculate(data);
        using SKImage? logo = ResolveLogo(prepared);
        using SKImage? paymentIcon = ResolvePaymentIcon(prepared);
        var exporter = new PngExporter(_fonts);
        return Task.FromResult(exporter.Export(prepared, logo, paymentIcon));
    }

    /// <summary>
    ///   Validates <paramref name="data"/>, recalculates totals when configured, and
    ///   renders to an SVG byte stream.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SVG bytes (UTF-8 XML starting with <c>&lt;svg</c>).</returns>
    /// <exception cref="ReceiptValidationException">
    ///   Thrown when validation fails; <see cref="ReceiptValidationException.Errors"/>
    ///   carries the full violation list.
    /// </exception>
    public Task<byte[]> GenerateSvgAsync(ReceiptData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceiptData prepared = ValidateAndCalculate(data);
        using SKImage? logo = ResolveLogo(prepared);
        using SKImage? paymentIcon = ResolvePaymentIcon(prepared);
        var exporter = new SvgExporter(_fonts);
        return Task.FromResult(exporter.Export(prepared, logo, paymentIcon));
    }

    /// <summary>
    ///   Generates a PDF and writes it to <paramref name="path"/>, auto-creating the
    ///   parent directory when missing.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="path">Absolute or relative file path. Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SavePdfAsync(ReceiptData data, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        byte[] bytes = await GeneratePdfAsync(data, cancellationToken).ConfigureAwait(false);
        EnsureDirectoryExists(path);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///   Generates a PNG and writes it to <paramref name="path"/>, auto-creating the
    ///   parent directory when missing.
    /// </summary>
    /// <param name="data">The receipt model.</param>
    /// <param name="path">Absolute or relative file path. Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SavePngAsync(ReceiptData data, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        byte[] bytes = await GeneratePngAsync(data, cancellationToken).ConfigureAwait(false);
        EnsureDirectoryExists(path);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    private ReceiptData ValidateAndCalculate(ReceiptData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        IReadOnlyList<ValidationError> errors = _validator.Validate(data);
        if (errors.Count > 0)
        {
            throw new ReceiptValidationException(errors);
        }

        // ReceiptCalculator.CalculateTotals already short-circuits when
        // options.AutoCalculateTotals != true, so the call is safe in both branches.
        return ReceiptCalculator.CalculateTotals(data);
    }

    private static SKImage? ResolveLogo(ReceiptData data)
    {
        if (data.Options?.ShowLogo != true)
        {
            return null;
        }

        return LogoResolver.Resolve(data.Business.BusinessLogoUrl);
    }

    // PaymentSection only renders the first payment entry per Phase 3c-polish
    // cluster D (multi-payment height-growth invariant retired). Resolve the icon
    // for that single entry so the same handle threads through PDF + PNG + SVG
    // without per-format re-decode.
    private static SKImage? ResolvePaymentIcon(ReceiptData data)
    {
        if (data.Payments.Count == 0)
        {
            return null;
        }

        return LogoResolver.Resolve(data.Payments[0].Icon);
    }

    private static void EnsureDirectoryExists(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    ///   Disposes the privately-allocated <see cref="FontProvider"/> when this generator
    ///   was created via the parameterless constructor. No-op when fonts were supplied
    ///   externally.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsFonts)
        {
            _fonts.Dispose();
        }
    }
}
