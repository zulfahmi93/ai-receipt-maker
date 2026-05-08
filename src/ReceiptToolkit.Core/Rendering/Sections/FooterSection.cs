using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Rendering.Layout;
using SkiaSharp;

namespace ReceiptToolkit.Core.Rendering.Sections;

/// <summary>
///   Renders the footer band: optional thank-you message, body notes, custom lines,
///   and a conditional contact sub-block.
/// </summary>
/// <remarks>
///   <para>
///     Body lines are emitted in this order when non-blank:
///     <list type="number">
///       <item><description>ThankYouMessage — SemiBold, <c>ThankYouFontSize</c>.</description></item>
///       <item><description>FooterNote — Normal, <c>BodyFontSize</c>.</description></item>
///       <item><description>ReturnPolicy — Normal, <c>BodyFontSize</c>.</description></item>
///       <item><description>LegalNote — Normal, <c>BodyFontSize</c>.</description></item>
///       <item><description>Each CustomFooterLine — Normal, <c>BodyFontSize</c>.</description></item>
///     </list>
///   </para>
///   <para>
///     The contact sub-block (BusinessAddress, BusinessEmail, BusinessPhone,
///     BusinessWebsite, SocialHandle) is rendered when
///     <see cref="ReceiptOptions.ShowFooterContact"/> is true and at least one of the
///     five fields is non-blank.  Each contact line uses a smaller muted font.
///   </para>
///   <para>
///     All lines are left-aligned at <c>origin.X</c>.  Measure treats all body lines as
///     <c>LineHeight</c> (≥ both font sizes plus padding) to keep the geometric height
///     assertion T3b.22 working without per-line size logic in the measurement pass.
///   </para>
///   <para>
///     Layout numerics are local constants in Phase 3b; Phase 3c may pull them from
///     <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class FooterSection : IReceiptSection
{
    // Uniform line height used for all body and contact lines in Measure.
    // 14f is >= ThankYouFontSize=13 and >= BodyFontSize=11, so every line fits.
    private const float LineHeight = 14f;

    // Vertical gap between consecutive lines within the same block.
    private const float LineGap = 2f;

    // Font sizes — body lines use 11pt, thank-you slightly larger at 13pt for emphasis.
    private const float BodyFontSize = 11f;
    private const float ThankYouFontSize = 13f;

    // Contact sub-block uses a smaller muted font to visually de-emphasise.
    private const float ContactFontSize = 10f;

    // Vertical gap between the last body line and the first contact line.
    private const float ContactBlockTopGap = 6f;

    private const string FontFamily = "Inter";

    /// <inheritdoc />
    public float Measure(float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        int bodyLineCount = CountWrappedLines(MaterializeBodyEntries(data), width, ctx);
        int contactLineCount = data.Options?.ShowFooterContact == true
            ? CountWrappedLines(MaterializeContactEntries(data.Business), width, ctx)
            : 0;

        float height = 0f;

        if (bodyLineCount > 0)
        {
            height += (bodyLineCount * LineHeight) + ((bodyLineCount - 1) * LineGap);
        }

        if (contactLineCount > 0)
        {
            height += ContactBlockTopGap;
            height += (contactLineCount * LineHeight) + ((contactLineCount - 1) * LineGap);
        }

        return height;
    }

    /// <inheritdoc />
    public void Draw(SKCanvas canvas, SKPoint origin, float width, ReceiptData data, RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ctx);

        SKColor textColor = ThemeColors.ResolveOrDefault(data.Theme?.TextColor, ThemeColors.DefaultTextColor);
        SKColor mutedColor = ThemeColors.ResolveOrDefault(data.Theme?.MutedTextColor, ThemeColors.DefaultMutedTextColor);

        float y = origin.Y;
        bool firstBodyLine = true;

        foreach (LineSpec entry in MaterializeBodyEntries(data))
        {
            SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, entry.Weight);
            IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
            foreach (string line in wrapped)
            {
                if (!firstBodyLine)
                {
                    y += LineGap;
                }

                DrawLine(canvas, origin.X, y, line, face, entry.FontSize, textColor);
                y += LineHeight;
                firstBodyLine = false;
            }
        }

        if (data.Options?.ShowFooterContact == true)
        {
            List<LineSpec> contactEntries = MaterializeContactEntries(data.Business);
            if (contactEntries.Count > 0)
            {
                y += ContactBlockTopGap;
                bool firstContactLine = true;
                SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

                foreach (LineSpec entry in contactEntries)
                {
                    IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
                    foreach (string line in wrapped)
                    {
                        if (!firstContactLine)
                        {
                            y += LineGap;
                        }

                        DrawLine(canvas, origin.X, y, line, face, entry.FontSize, mutedColor);
                        y += LineHeight;
                        firstContactLine = false;
                    }
                }
            }
        }
    }

    // Sums wrapped line counts for every entry at the given width, using each entry's
    // own typeface weight + font size. Return type is List<...> upstream to satisfy
    // CA1859 (private/internal helpers must not widen to IReadOnlyList<T>).
    private static int CountWrappedLines(List<LineSpec> entries, float width, RenderContext ctx)
    {
        int count = 0;
        foreach (LineSpec entry in entries)
        {
            SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, entry.Weight);
            IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
            count += wrapped.Count;
        }

        return count;
    }

    // Builds the ordered list of body entries (text + weight + font size) in render order:
    // ThankYouMessage (SemiBold, ThankYouFontSize), FooterNote, ReturnPolicy, LegalNote,
    // and each non-blank custom line (Normal, BodyFontSize).
    private static List<LineSpec> MaterializeBodyEntries(ReceiptData data)
    {
        FooterInfo footer = data.Footer ?? new FooterInfo();
        var entries = new List<LineSpec>(capacity: 8);

        if (!string.IsNullOrWhiteSpace(footer.ThankYouMessage))
        {
            entries.Add(new LineSpec(footer.ThankYouMessage!, SKFontStyleWeight.SemiBold, ThankYouFontSize));
        }

        if (!string.IsNullOrWhiteSpace(footer.FooterNote))
        {
            entries.Add(new LineSpec(footer.FooterNote!, SKFontStyleWeight.Normal, BodyFontSize));
        }

        if (!string.IsNullOrWhiteSpace(footer.ReturnPolicy))
        {
            entries.Add(new LineSpec(footer.ReturnPolicy!, SKFontStyleWeight.Normal, BodyFontSize));
        }

        if (!string.IsNullOrWhiteSpace(footer.LegalNote))
        {
            entries.Add(new LineSpec(footer.LegalNote!, SKFontStyleWeight.Normal, BodyFontSize));
        }

        foreach (string line in footer.CustomFooterLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                entries.Add(new LineSpec(line, SKFontStyleWeight.Normal, BodyFontSize));
            }
        }

        return entries;
    }

    // Builds the ordered list of contact entries — all Normal weight, ContactFontSize.
    // Each non-blank business field becomes one entry in display order: address, email,
    // phone, website, social handle.
    private static List<LineSpec> MaterializeContactEntries(BusinessInfo business)
    {
        var entries = new List<LineSpec>(capacity: 5);

        if (!string.IsNullOrWhiteSpace(business.BusinessAddress))
        {
            entries.Add(new LineSpec(business.BusinessAddress!, SKFontStyleWeight.Normal, ContactFontSize));
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessEmail))
        {
            entries.Add(new LineSpec(business.BusinessEmail!, SKFontStyleWeight.Normal, ContactFontSize));
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessPhone))
        {
            entries.Add(new LineSpec(business.BusinessPhone!, SKFontStyleWeight.Normal, ContactFontSize));
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessWebsite))
        {
            entries.Add(new LineSpec(business.BusinessWebsite!, SKFontStyleWeight.Normal, ContactFontSize));
        }

        if (!string.IsNullOrWhiteSpace(business.SocialHandle))
        {
            entries.Add(new LineSpec(business.SocialHandle!, SKFontStyleWeight.Normal, ContactFontSize));
        }

        return entries;
    }

    private readonly record struct LineSpec(string Text, SKFontStyleWeight Weight, float FontSize);

    private static void DrawLine(
        SKCanvas canvas,
        float x,
        float topY,
        string text,
        SKTypeface typeface,
        float fontSize,
        SKColor color)
    {
        using var font = new SKFont(typeface, fontSize);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, topY + fontSize, font, paint);
    }
}
