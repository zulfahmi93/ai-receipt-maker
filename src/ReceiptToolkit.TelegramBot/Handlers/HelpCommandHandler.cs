using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>Handles <c>/help</c> by replying with the command list and JSON guidance.</summary>
public sealed class HelpCommandHandler : IUpdateHandler
{
    /// <inheritdoc />
    public bool CanHandle(Update update) => StartCommandHandler.IsCommand(update, "/help");

    /// <inheritdoc />
    public Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(sender);
        long chatId = update.Message!.Chat.Id;
        return sender.SendTextAsync(chatId, BotMessages.Help, cancellationToken);
    }
}
