using Microsoft.Extensions.Options;

namespace ReceiptToolkit.TelegramBot.Configuration;

/// <summary>
///   Validates <see cref="TelegramOptions"/> at startup. Surfaces a clear
///   <see cref="OptionsValidationException"/> when the token is missing —
///   the host's options machinery converts this to an
///   <see cref="InvalidOperationException"/> on first resolution.
/// </summary>
internal sealed class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    /// <summary>Failure message surfaced when the token is missing.</summary>
    public const string MissingTokenMessage =
        "TELEGRAM_BOT_TOKEN is not configured. Set the environment variable or " +
        "Telegram:Token in configuration before starting the bot.";

    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return string.IsNullOrWhiteSpace(options.Token)
            ? ValidateOptionsResult.Fail(MissingTokenMessage)
            : ValidateOptionsResult.Success;
    }
}
