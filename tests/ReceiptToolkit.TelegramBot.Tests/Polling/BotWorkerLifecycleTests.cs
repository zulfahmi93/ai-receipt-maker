using Microsoft.Extensions.Logging.Abstractions;
using ReceiptToolkit.TelegramBot.Polling;

namespace ReceiptToolkit.TelegramBot.Tests.Polling;

public sealed class BotWorkerLifecycleTests
{
    /// <summary>
    ///   Polling client that blocks until the supplied <c>CancellationToken</c>
    ///   fires, then returns gracefully — mirrors the real long-polling shape.
    /// </summary>
    private sealed class CancellingPollingClient : IPollingClient
    {
        public int RunCount { get; private set; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            RunCount++;
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Clean exit on host stop.
            }
        }
    }

    /// <summary>Polling client that throws unconditionally — exercises the fault path.</summary>
    private sealed class FaultingPollingClient : IPollingClient
    {
        public Task RunAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated polling failure");
    }

    [Fact]
    public async Task T6_9_Worker_ShutsDownCleanlyOnStop()
    {
        var pollingClient = new CancellingPollingClient();
        using var worker = new BotWorker(pollingClient, NullLogger<BotWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        Assert.False(worker.ExecuteTask?.IsCompleted ?? true, "Worker should be running.");

        await worker.StopAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(worker.ExecuteTask);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully, "Worker should exit cleanly on cancellation.");
        Assert.Equal(1, pollingClient.RunCount);
    }

    [Fact]
    public async Task T6_9_Worker_PropagatesNonCancellationFaults()
    {
        var pollingClient = new FaultingPollingClient();
        using var worker = new BotWorker(pollingClient, NullLogger<BotWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(worker.ExecuteTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.ExecuteTask!);
    }
}
