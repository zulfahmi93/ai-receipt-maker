using ReceiptToolkit.TelegramBot.Handlers;

namespace ReceiptToolkit.TelegramBot.Polling;

/// <summary>
///   Long-polling background service. Delegates the actual loop to
///   <see cref="IPollingClient"/>; this worker exists to bind the loop to
///   <see cref="BackgroundService.ExecuteAsync"/> so the host's
///   <c>IHostApplicationLifetime.StopAsync</c> tears it down cleanly.
/// </summary>
public sealed class BotWorker : BackgroundService
{
    private readonly IPollingClient _client;
    private readonly ILogger<BotWorker> _logger;

    /// <summary>Initialises a new <see cref="BotWorker"/>.</summary>
    /// <param name="client">Polling client (real or test fake).</param>
    /// <param name="logger">Structured logger.</param>
    public BotWorker(IPollingClient client, ILogger<BotWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _client.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            BotLog.PollingStopping(_logger);
        }
        catch (Exception ex)
        {
            BotLog.PollingFaulted(_logger, ex);
            throw;
        }
    }
}
