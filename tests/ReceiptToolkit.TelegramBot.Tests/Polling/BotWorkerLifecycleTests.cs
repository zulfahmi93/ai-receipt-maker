using Microsoft.Extensions.Logging.Abstractions;
using ReceiptToolkit.TelegramBot.Polling;

namespace ReceiptToolkit.TelegramBot.Tests.Polling;

public sealed class BotWorkerLifecycleTests
{
    /// <summary>
    ///   Polling client that blocks until the supplied <c>CancellationToken</c>
    ///   fires, then returns gracefully — mirrors the real long-polling shape.
    ///   Exposes a <see cref="Started"/> signal so callers can deterministically
    ///   wait for the polling loop to engage before triggering shutdown.
    /// </summary>
    private sealed class CancellingPollingClient : IPollingClient
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int RunCount { get; private set; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            RunCount++;
            Started.TrySetResult();
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
        // Wait deterministically for the fake's polling loop to engage before
        // stopping. Without this, slow runners (Linux CI) can race past the
        // RunCount increment and observe an unstarted polling client.
        await pollingClient.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await worker.StopAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(worker.ExecuteTask);
        // BackgroundService cancellation can resolve as either RanToCompletion (the
        // catch swallowed the OCE) or Canceled (the OCE bubbled out) depending on the
        // platform's task-completion ordering. Both are "clean exit"; only Faulted
        // signals an unhandled error. Linux CI exposed this — macOS dev observed
        // RanToCompletion exclusively.
        Assert.True(worker.ExecuteTask!.IsCompleted, "Worker task should reach a terminal state.");
        Assert.False(worker.ExecuteTask!.IsFaulted, "Worker should not fault on cancellation.");
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
