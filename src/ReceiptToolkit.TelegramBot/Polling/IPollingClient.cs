namespace ReceiptToolkit.TelegramBot.Polling;

/// <summary>
///   Wraps the Telegram long-polling loop so <see cref="BotWorker"/> can be
///   tested without a live <c>ITelegramBotClient</c>. Production wires
///   <see cref="TelegramPollingClient"/>; tests substitute a fake that
///   awaits cancellation.
/// </summary>
public interface IPollingClient
{
    /// <summary>
    ///   Runs the long-polling loop until <paramref name="cancellationToken"/>
    ///   is signalled. Returns gracefully on cancellation; rethrows any other
    ///   transport failure to surface in <c>BotWorker.ExecuteAsync</c>'s log.
    /// </summary>
    /// <param name="cancellationToken">Stop signal.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
