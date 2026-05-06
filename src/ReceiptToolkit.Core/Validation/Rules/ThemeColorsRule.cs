using System.Text.RegularExpressions;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Core.Validation.Rules;

/// <summary>Validates that every non-null color field in <see cref="ReceiptTheme"/> is a valid CSS hex color string.</summary>
public sealed partial class ThemeColorsRule : IValidationRule
{
    [GeneratedRegex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HexColorPattern();

    /// <inheritdoc/>
    public IEnumerable<ValidationError> Validate(ReceiptData data)
    {
        ReceiptTheme? theme = data.Theme;

        if (theme is null)
            yield break;

        foreach (ValidationError error in CheckColor(theme.PaperColor, "theme.paperColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.TextColor, "theme.textColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.MutedTextColor, "theme.mutedTextColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.AccentColor, "theme.accentColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.DividerColor, "theme.dividerColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.HighlightColor, "theme.highlightColor"))
            yield return error;

        foreach (ValidationError error in CheckColor(theme.BackgroundColor, "theme.backgroundColor"))
            yield return error;
    }

    private static IEnumerable<ValidationError> CheckColor(string? value, string field)
    {
        if (value is not null && !HexColorPattern().IsMatch(value))
            yield return new ValidationError(field, $"'{value}' is not a valid hex color. Expected format: #RGB, #RRGGBB, or #RRGGBBAA.");
    }
}
