namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Source-generated structured log entries for the bot. Uses
///   <see cref="LoggerMessageAttribute"/> so each callsite is allocation-free
///   and the analyzer-as-error CA1848 stays green.
/// </summary>
internal static partial class BotLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Sample generation failed.")]
    public static partial void SampleGenerationFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Receipt generation failed for chat {ChatId}.")]
    public static partial void GenerationFailed(ILogger logger, long chatId, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Bot polling worker stopping (cancellation observed).")]
    public static partial void PollingStopping(ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Polling loop encountered an unhandled exception.")]
    public static partial void PollingFaulted(ILogger logger, Exception exception);
}
