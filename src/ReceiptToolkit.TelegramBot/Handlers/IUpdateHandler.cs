using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Handles a single Telegram <see cref="Update"/> by dispatching to the
///   matching command or text-message branch and emitting replies via
///   the supplied <see cref="ITelegramSender"/>.
/// </summary>
public interface IUpdateHandler
{
    /// <summary>Returns <see langword="true"/> when this handler should process the update.</summary>
    /// <param name="update">The incoming Telegram update.</param>
    bool CanHandle(Update update);

    /// <summary>Processes the update and emits any replies via <paramref name="sender"/>.</summary>
    /// <param name="update">The incoming Telegram update.</param>
    /// <param name="sender">Reply channel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default);
}
