using System.Text;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Renders a <see cref="ValidationError"/> list as a single bot-friendly text block:
///   one bullet per error, prefixed by <see cref="BotMessages.ValidationHeader"/>.
/// </summary>
internal static class ValidationErrorFormatter
{
    /// <summary>Formats <paramref name="errors"/> as a multiline bullet list with header.</summary>
    /// <param name="errors">Validation failures (must not be <see langword="null"/>).</param>
    public static string Format(IReadOnlyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.ValidationHeader);
        foreach (ValidationError error in errors)
        {
            sb.Append("• ");
            if (!string.IsNullOrWhiteSpace(error.Field))
            {
                sb.Append(error.Field);
                sb.Append(": ");
            }

            sb.AppendLine(error.Message);
        }

        return sb.ToString().TrimEnd();
    }
}
