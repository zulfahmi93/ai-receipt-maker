namespace ReceiptToolkit.TelegramBot.Configuration;

/// <summary>
///   Bot startup options sourced from environment / appsettings via the
///   <c>Telegram</c> configuration section. Token also accepted via the
///   bare <c>TELEGRAM_BOT_TOKEN</c> environment variable per ADR 0003.
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>Configuration section name (<c>Telegram</c>).</summary>
    public const string SectionName = "Telegram";

    /// <summary>Telegram bot HTTP API token (BotFather-issued).</summary>
    public string Token { get; set; } = string.Empty;
}
