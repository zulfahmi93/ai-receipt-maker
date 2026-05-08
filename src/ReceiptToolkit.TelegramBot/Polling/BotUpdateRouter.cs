using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Polling;

/// <summary>
///   Dispatches an inbound <see cref="Update"/> to the first registered
///   <see cref="IUpdateHandler"/> whose <see cref="IUpdateHandler.CanHandle"/>
///   returns <see langword="true"/>. Handlers registered earlier in DI win,
///   so command handlers (specific) precede the JSON fallback (broad).
/// </summary>
public sealed class BotUpdateRouter
{
    private readonly IReadOnlyList<IUpdateHandler> _handlers;

    /// <summary>Initialises a new <see cref="BotUpdateRouter"/>.</summary>
    /// <param name="handlers">Registered handlers in dispatch priority order.</param>
    public BotUpdateRouter(IEnumerable<IUpdateHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _handlers = [.. handlers];
    }

    /// <summary>
    ///   Routes <paramref name="update"/> to the first matching handler.
    ///   No-ops when no handler claims the update.
    /// </summary>
    /// <param name="update">The incoming Telegram update.</param>
    /// <param name="sender">Reply channel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task RouteAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(sender);

        foreach (IUpdateHandler handler in _handlers)
        {
            if (handler.CanHandle(update))
            {
                return handler.HandleAsync(update, sender, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }
}
