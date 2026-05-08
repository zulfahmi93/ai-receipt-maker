using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>Handles <c>/start</c> by replying with the welcome message.</summary>
public sealed class StartCommandHandler : IUpdateHandler
{
    /// <inheritdoc />
    public bool CanHandle(Update update) => IsCommand(update, "/start");

    /// <inheritdoc />
    public Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(sender);
        long chatId = update.Message!.Chat.Id;
        return sender.SendTextAsync(chatId, BotMessages.Welcome, cancellationToken);
    }

    /// <summary>
    ///   Returns <see langword="true"/> when <paramref name="update"/> carries a
    ///   text message whose first whitespace-trimmed token equals
    ///   <paramref name="command"/>. Tolerates the Telegram <c>/cmd@botname</c>
    ///   addressing form by stripping anything after <c>@</c> on the head token.
    /// </summary>
    /// <param name="update">The incoming Telegram update.</param>
    /// <param name="command">The command verb to match (e.g. <c>"/start"</c>).</param>
    internal static bool IsCommand(Update update, string command)
    {
        ArgumentNullException.ThrowIfNull(update);
        string? text = update.Message?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        ReadOnlySpan<char> trimmed = text.AsSpan().Trim();
        int space = trimmed.IndexOf(' ');
        ReadOnlySpan<char> head = space < 0 ? trimmed : trimmed[..space];
        int at = head.IndexOf('@');
        ReadOnlySpan<char> verb = at < 0 ? head : head[..at];
        return verb.SequenceEqual(command);
    }
}
