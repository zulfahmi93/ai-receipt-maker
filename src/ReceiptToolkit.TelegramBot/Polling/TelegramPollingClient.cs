using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramUpdateHandler = Telegram.Bot.Polling.IUpdateHandler;

namespace ReceiptToolkit.TelegramBot.Polling;

/// <summary>
///   Production <see cref="IPollingClient"/> implementation: wires the real
///   <see cref="ITelegramBotClient"/> long-polling loop to <see cref="BotUpdateRouter"/>.
/// </summary>
internal sealed class TelegramPollingClient : IPollingClient, TelegramUpdateHandler
{
    private static readonly ReceiverOptions ReceiverDefaults = new()
    {
        AllowedUpdates = [], // all update types
        DropPendingUpdates = true,
    };

    private readonly ITelegramBotClient _client;
    private readonly ITelegramSender _sender;
    private readonly BotUpdateRouter _router;
    private readonly ILogger<TelegramPollingClient> _logger;

    public TelegramPollingClient(
        ITelegramBotClient client,
        ITelegramSender sender,
        BotUpdateRouter router,
        ILogger<TelegramPollingClient> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _sender = sender;
        _router = router;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken) =>
        _client.ReceiveAsync(this, ReceiverDefaults, cancellationToken);

    Task TelegramUpdateHandler.HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken) =>
        _router.RouteAsync(update, _sender, cancellationToken);

    Task TelegramUpdateHandler.HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        BotLog.PollingFaulted(_logger, exception);
        return Task.CompletedTask;
    }
}
