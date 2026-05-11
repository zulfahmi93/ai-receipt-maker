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
///       <item><description>LegalNote — Normal, <c>LegalNoteFontSize</c> (smaller than BodyFontSize to de-emphasise fine print).</description></item>
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
///     Each line's slot height = <c>fontSize × LineSpacingFactor</c>.  Inter-line gap
///     within a block = <c>slot - fontSize</c>.  This gives a tight but readable rhythm
///     that scales with the per-entry font size (tighter than the legacy fixed
///     LineHeight/LineGap pair).
///   </para>
///   <para>
///     Layout numerics are local constants in Phase 3b; Phase 3c may pull them from
///     <see cref="ReceiptLayout"/>.
///   </para>
/// </remarks>
public sealed class FooterSection : IReceiptSection
{
    // Multiplier applied to each entry's font size to derive its line slot height.
    // 1.10 gives a tight but readable rhythm (tighter than the legacy 1.15 factor).
    // slot = fontSize * LineSpacingFactor; inter-line gap = slot - fontSize.
    private const float LineSpacingFactor = 1.1f;

    // Font sizes — body lines use 11pt, thank-you slightly larger at 13pt for emphasis.
    // Legal note uses a smaller 9pt to visually de-emphasise fine print.
    private const float BodyFontSize = 11f;
    private const float ThankYouFontSize = 13f;
    private const float LegalNoteFontSize = 9f;

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

        float height = MeasureBlock(MaterializeBodyEntries(data), width, ctx);

        if (data.Options?.ShowFooterContact == true)
        {
            float contactHeight = MeasureBlock(MaterializeContactEntries(data.Business), width, ctx);
            if (contactHeight > 0f)
            {
                height += ContactBlockTopGap + contactHeight;
            }
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
            float slot = entry.FontSize * LineSpacingFactor;
            float gap = slot - entry.FontSize;
            SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, entry.Weight);
            IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
            foreach (string line in wrapped)
            {
                if (!firstBodyLine)
                {
                    y += gap;
                }

                float lineWidth = TextMeasurer.Measure(line, face, entry.FontSize).Width;
                float centeredX = origin.X + ((width - lineWidth) / 2f);
                DrawLine(canvas, centeredX, y, line, face, entry.FontSize, textColor);
                y += slot;
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
                    float slot = entry.FontSize * LineSpacingFactor;
                    float gap = slot - entry.FontSize;
                    IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
                    foreach (string line in wrapped)
                    {
                        if (!firstContactLine)
                        {
                            y += gap;
                        }

                        float lineWidth = TextMeasurer.Measure(line, face, entry.FontSize).Width;
                        float centeredX = origin.X + ((width - lineWidth) / 2f);
                        DrawLine(canvas, centeredX, y, line, face, entry.FontSize, mutedColor);
                        y += slot;
                        firstContactLine = false;
                    }
                }
            }
        }
    }

    // Sums slot heights for every wrapped line in each entry at the given width.
    // Each line's slot = entry.FontSize * LineSpacingFactor; inter-line gap = slot - fontSize.
    // Returns total block height (0f when entries is empty).
    private static float MeasureBlock(List<LineSpec> entries, float width, RenderContext ctx)
    {
        float height = 0f;
        bool first = true;
        foreach (LineSpec entry in entries)
        {
            float slot = entry.FontSize * LineSpacingFactor;
            float gap = slot - entry.FontSize;
            SKTypeface face = ctx.Fonts.GetTypeface(FontFamily, entry.Weight);
            IReadOnlyList<string> wrapped = TextMeasurer.WrapLines(entry.Text, width, face, entry.FontSize);
            foreach (string _ in wrapped)
            {
                if (!first)
                {
                    height += gap;
                }

                height += slot;
                first = false;
            }
        }

        return height;
    }

    // Builds the ordered list of body entries (text + weight + font size) in render order:
    // ThankYouMessage (SemiBold, ThankYouFontSize), FooterNote, ReturnPolicy,
    // LegalNote (LegalNoteFontSize — smaller to de-emphasise fine print),
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
            entries.Add(new LineSpec(footer.LegalNote!, SKFontStyleWeight.Normal, LegalNoteFontSize));
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
