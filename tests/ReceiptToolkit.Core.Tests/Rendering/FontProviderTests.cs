// Purpose: RED-phase tests for Phase 3 (T3.5–T3.6) — FontProvider typeface loading.
// Categories: Unit — in-process embedded resource loading; tests that the Inter variable
//             font is loaded from the embedded resource stream (not a system fallback)
//             and that repeated calls with the same key return the same cached instance.
// Edge cases: typeface family name identity check, reference equality for cache hits.

using SkiaSharp;
using ReceiptToolkit.Core.Rendering.Assets;

namespace ReceiptToolkit.Core.Tests.Rendering;

public sealed class FontProviderTests
{
    // T3.5 — GetTypeface("Inter", Normal) returns a non-null typeface whose FamilyName
    //         contains "Inter", proving the embedded Inter font was loaded rather than
    //         falling back to a system typeface (which would be e.g. ".AppleSystemUIFont"
    //         on macOS or "DejaVu Sans" on Linux — neither contains "Inter").
    //         The Inter variable font (rsms/inter v4.1, divergence #2 / ADR 0004)
    //         reports FamilyName "Inter Variable" via SkiaSharp 3.119.2; static cuts
    //         report "Inter". `Contains` covers both shapes without coupling the test
    //         to font-table layout choices.
    [Fact]
    public void FontProvider_Inter_LoadsEmbeddedTypeface()
    {
        using var provider = new FontProvider();

        SKTypeface tf = provider.GetTypeface("Inter", SKFontStyleWeight.Normal);

        Assert.NotNull(tf);
        Assert.Contains("Inter", tf.FamilyName, StringComparison.OrdinalIgnoreCase);
    }

    // T3.6 — Two calls with the same (family, weight) key return the same cached instance
    //         (reference equality), so SkiaSharp typeface objects are not duplicated.
    [Fact]
    public void FontProvider_GetTypeface_CachesPerKey()
    {
        using var provider = new FontProvider();

        SKTypeface first = provider.GetTypeface("Inter", SKFontStyleWeight.Normal);
        SKTypeface second = provider.GetTypeface("Inter", SKFontStyleWeight.Normal);

        Assert.True(ReferenceEquals(first, second));
    }
}
