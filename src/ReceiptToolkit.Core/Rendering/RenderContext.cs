using ReceiptToolkit.Core.Rendering.Assets;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering;

/// <summary>
///   Per-render resources shared by every section: font provider and a pre-resolved logo image.
/// </summary>
/// <remarks>
///   <para>
///     Theme, layout, and option values stay on <see cref="ReceiptToolkit.Contracts.ReceiptData"/>
///     and are read directly by sections — they are not duplicated here.
///   </para>
///   <para>
///     The logo is resolved once at the top of the render pipeline (via
///     <see cref="LogoResolver.Resolve"/>) and passed in as <see cref="ResolvedLogo"/> so
///     individual sections never re-resolve a file path or data URI.
///   </para>
///   <para>
///     <b>Lifetime:</b> the supplied <see cref="FontProvider"/> and
///     <see cref="ResolvedLogo"/> are both treated as caller-owned. <see cref="Dispose"/>
///     does not free either handle. The <c>ReceiptGenerator</c> façade owns the resolved
///     logo across PDF + PNG + SVG exporter calls so a single resolution serves every
///     output format without per-format re-allocation.
///   </para>
/// </remarks>
public sealed class RenderContext : IDisposable
{
    /// <summary>
    ///   Initialises a new <see cref="RenderContext"/>.
    /// </summary>
    /// <param name="fonts">
    ///   The shared <see cref="FontProvider"/>.  Caller-owned; not disposed by this context.
    /// </param>
    /// <param name="resolvedLogo">
    ///   The logo image already resolved from <c>business.businessLogoUrl</c>, or
    ///   <see langword="null"/> when no logo is configured or rendering is disabled.
    ///   Caller-owned; not disposed by this context.
    /// </param>
    public RenderContext(FontProvider fonts, SKImage? resolvedLogo)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        Fonts = fonts;
        ResolvedLogo = resolvedLogo;
    }

    /// <summary>The shared font provider.  Caller-owned; do not dispose via this context.</summary>
    public FontProvider Fonts { get; }

    /// <summary>The resolved logo image, or <see langword="null"/> when unavailable.</summary>
    public SKImage? ResolvedLogo { get; }

    /// <summary>
    ///   When <see langword="true"/>, the composer paints a drop-shadow rectangle
    ///   behind the paper and <see cref="ReceiptToolkit.Contracts.ReceiptLayout"/>'s
    ///   measured canvas size grows by the shadow offset on the right and bottom edges.
    ///   Set by raster exporters (PNG); vector exporters (PDF, SVG) leave the default
    ///   <see langword="false"/> so the receipt body remains flush to the page edges.
    /// </summary>
    public bool EmitShadow { get; init; }

    /// <summary>
    ///   No-op disposal: neither <see cref="Fonts"/> nor <see cref="ResolvedLogo"/> are
    ///   owned by this context. <see cref="IDisposable"/> is retained so existing
    ///   <c>using var ctx = new RenderContext(...)</c> call-sites compile unchanged.
    /// </summary>
    public void Dispose()
    {
        // Caller owns Fonts (long-lived FontProvider) and ResolvedLogo (generator-owned
        // across PDF+PNG+SVG export calls); nothing for this context to release.
    }
}
