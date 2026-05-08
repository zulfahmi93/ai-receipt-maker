using Microsoft.Extensions.Logging;
using NSubstitute;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

public sealed class GenerationFailureFallbackTests
{
    private sealed class ThrowingFixture : ISampleFixtureProvider
    {
        public ReceiptData Load() => throw new InvalidOperationException("simulated fixture failure");
    }

    /// <summary>
    ///   Captures log invocations so the test can assert that the fallback
    ///   path emitted a structured Error-level entry — proves T6.8's
    ///   "structured log entry" requirement.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, EventId Event, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider parent) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                parent.Entries.Add((logLevel, eventId, formatter(state, exception)));
            }
        }
    }

    [Fact]
    public async Task T6_8_GenerationException_SendsFriendlyFallbackAndLogsError()
    {
        var sender = Substitute.For<ITelegramSender>();
        using var generator = new ReceiptGenerator();
        var provider = new CapturingLoggerProvider();
        ILogger<SampleCommandHandler> logger = new CapturingLoggerAdapter<SampleCommandHandler>(provider);

        var handler = new SampleCommandHandler(generator, new ThrowingFixture(), logger);
        var update = HandlerTestBase.TextMessage("/sample");

        await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

        // Friendly fallback text reached the user.
        await sender.Received(1).SendTextAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(s =>
                s.Contains("couldn't", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("Sorry", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("try again", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());

        // No documents sent on failure.
        await sender.DidNotReceive().SendDocumentAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());

        // Structured log entry recorded at Error level.
        Assert.Contains(provider.Entries, e => e.Level == LogLevel.Error);
    }

    private sealed class CapturingLoggerAdapter<T>(CapturingLoggerProvider provider) : ILogger<T>
    {
        private readonly ILogger _inner = provider.CreateLogger(typeof(T).FullName!);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
