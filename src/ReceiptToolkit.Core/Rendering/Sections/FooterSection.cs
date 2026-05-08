using ReceiptToolkit.Contracts;
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

        List<string> bodyLines = MaterializeBodyLines(data);
        List<string> contactLines = data.Options?.ShowFooterContact == true
            ? MaterializeContactLines(data.Business)
            : new List<string>();

        float height = 0f;

        for (int i = 0; i < bodyLines.Count; i++)
        {
            if (i > 0)
            {
                height += LineGap;
            }

            height += LineHeight;
        }

        if (contactLines.Count > 0)
        {
            height += ContactBlockTopGap;
            for (int i = 0; i < contactLines.Count; i++)
            {
                if (i > 0)
                {
                    height += LineGap;
                }

                height += LineHeight;
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

        SKTypeface semiBoldFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.SemiBold);
        SKTypeface normalFace = ctx.Fonts.GetTypeface(FontFamily, SKFontStyleWeight.Normal);

        float y = origin.Y;
        bool firstLine = true;

        // ThankYouMessage — SemiBold, slightly larger font.
        string? thankYou = data.Footer?.ThankYouMessage;
        if (!string.IsNullOrWhiteSpace(thankYou))
        {
            if (!firstLine)
            {
                y += LineGap;
            }

            DrawLine(canvas, origin.X, y, thankYou, semiBoldFace, ThankYouFontSize, textColor);
            y += LineHeight;
            firstLine = false;
        }

        // Remaining body lines — Normal weight, BodyFontSize.
        FooterInfo footer = data.Footer ?? new FooterInfo();

        List<string> remainingBodyLines = MaterializeRemainingBodyLines(footer);
        foreach (string line in remainingBodyLines)
        {
            if (!firstLine)
            {
                y += LineGap;
            }

            DrawLine(canvas, origin.X, y, line, normalFace, BodyFontSize, textColor);
            y += LineHeight;
            firstLine = false;
        }

        // Contact sub-block — smaller muted font with a top gap.
        if (data.Options?.ShowFooterContact == true)
        {
            List<string> contactLines = MaterializeContactLines(data.Business);
            if (contactLines.Count > 0)
            {
                y += ContactBlockTopGap;
                bool firstContact = true;

                foreach (string line in contactLines)
                {
                    if (!firstContact)
                    {
                        y += LineGap;
                    }

                    DrawLine(canvas, origin.X, y, line, normalFace, ContactFontSize, mutedColor);
                    y += LineHeight;
                    firstContact = false;
                }
            }
        }
    }

    // Builds the ordered list of all body lines including ThankYouMessage (for Measure).
    // Return type is List<string> to satisfy CA1859 — do not widen to IReadOnlyList<string>.
    private static List<string> MaterializeBodyLines(ReceiptData data)
    {
        FooterInfo footer = data.Footer ?? new FooterInfo();
        var lines = new List<string>(capacity: 8);

        if (!string.IsNullOrWhiteSpace(footer.ThankYouMessage))
        {
            lines.Add(footer.ThankYouMessage!);
        }

        if (!string.IsNullOrWhiteSpace(footer.FooterNote))
        {
            lines.Add(footer.FooterNote!);
        }

        if (!string.IsNullOrWhiteSpace(footer.ReturnPolicy))
        {
            lines.Add(footer.ReturnPolicy!);
        }

        if (!string.IsNullOrWhiteSpace(footer.LegalNote))
        {
            lines.Add(footer.LegalNote!);
        }

        foreach (string line in footer.CustomFooterLines)
        {
            lines.Add(line);
        }

        return lines;
    }

    // Builds the body lines that follow ThankYouMessage (FooterNote, ReturnPolicy,
    // LegalNote, CustomFooterLines) for use in the Draw pass only.
    // Return type is List<string> to satisfy CA1859 — do not widen to IReadOnlyList<string>.
    private static List<string> MaterializeRemainingBodyLines(FooterInfo footer)
    {
        var lines = new List<string>(capacity: 7);

        if (!string.IsNullOrWhiteSpace(footer.FooterNote))
        {
            lines.Add(footer.FooterNote!);
        }

        if (!string.IsNullOrWhiteSpace(footer.ReturnPolicy))
        {
            lines.Add(footer.ReturnPolicy!);
        }

        if (!string.IsNullOrWhiteSpace(footer.LegalNote))
        {
            lines.Add(footer.LegalNote!);
        }

        foreach (string line in footer.CustomFooterLines)
        {
            lines.Add(line);
        }

        return lines;
    }

    // Builds the ordered list of contact lines from the business object.
    // Return type is List<string> to satisfy CA1859 — do not widen to IReadOnlyList<string>.
    private static List<string> MaterializeContactLines(BusinessInfo business)
    {
        var lines = new List<string>(capacity: 5);

        if (!string.IsNullOrWhiteSpace(business.BusinessAddress))
        {
            lines.Add(business.BusinessAddress!);
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessEmail))
        {
            lines.Add(business.BusinessEmail!);
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessPhone))
        {
            lines.Add(business.BusinessPhone!);
        }

        if (!string.IsNullOrWhiteSpace(business.BusinessWebsite))
        {
            lines.Add(business.BusinessWebsite!);
        }

        if (!string.IsNullOrWhiteSpace(business.SocialHandle))
        {
            lines.Add(business.SocialHandle!);
        }

        return lines;
    }

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
