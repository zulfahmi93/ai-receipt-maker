// Purpose: RED-phase tests for Phase 3c-polish A T3cP.2 — ThemeColors.ResolveBodyColor
//          + ThemeColors.DefaultBodyColor + ReceiptTheme.BodyColor.
// T3cP.2a: ResolveBodyColor(null) returns DefaultBodyColor.
// T3cP.2b: ResolveBodyColor(theme with null BodyColor) returns DefaultBodyColor.
// T3cP.2c: ResolveBodyColor(theme with "#444444") parses to R=G=B=0x44.

using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering;
using SkiaSharp;

namespace ReceiptToolkit.Core.Tests.Rendering.Theme;

/// <summary>
///   Pins the <see cref="ThemeColors.ResolveBodyColor"/> resolver and
///   <see cref="ThemeColors.DefaultBodyColor"/> constant added in Phase
///   3c-polish A (T3cP.2).
/// </summary>
public sealed class ThemeBodyColorTests
{
    // T3cP.2a — null theme argument returns DefaultBodyColor.
    [Fact]
    public void ResolveBodyColor_NullThemeReturnsDefault()
    {
        SKColor result = ThemeColors.ResolveBodyColor(null);
        Assert.Equal(ThemeColors.DefaultBodyColor, result);
    }

    // T3cP.2b — theme with BodyColor=null returns DefaultBodyColor.
    [Fact]
    public void ResolveBodyColor_NullBodyColorReturnsDefault()
    {
        var theme = new ReceiptTheme { BodyColor = null };
        SKColor result = ThemeColors.ResolveBodyColor(theme);
        Assert.Equal(ThemeColors.DefaultBodyColor, result);
    }

    // T3cP.2c — theme with BodyColor="#444444" parses to SKColor R=G=B=0x44.
    [Fact]
    public void ResolveBodyColor_ParsesHexCode()
    {
        var theme = new ReceiptTheme { BodyColor = "#444444" };
        SKColor result = ThemeColors.ResolveBodyColor(theme);

        Assert.Equal((byte)0x44, result.Red);
        Assert.Equal((byte)0x44, result.Green);
        Assert.Equal((byte)0x44, result.Blue);
    }
}
