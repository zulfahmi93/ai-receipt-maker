namespace ReceiptToolkit.TelegramBot.Messaging;

/// <summary>
///   Narrow test seam over <c>ITelegramBotClient</c> exposing only the operations
///   that bot handlers actually invoke (text reply + binary document upload).
///   Tests substitute a fake via NSubstitute; production wires
///   <c>TelegramBotClientSender</c> against the real <c>ITelegramBotClient</c>
///   registered by <c>BotWorker</c>.
/// </summary>
public interface ITelegramSender
{
    /// <summary>Sends a plain-text message to <paramref name="chatId"/>.</summary>
    /// <param name="chatId">Target chat id.</param>
    /// <param name="text">Plain text body. Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default);

    /// <summary>Sends a binary document (e.g. PDF or PNG) to <paramref name="chatId"/>.</summary>
    /// <param name="chatId">Target chat id.</param>
    /// <param name="fileName">Display file name (must include extension).</param>
    /// <param name="bytes">File payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendDocumentAsync(
        long chatId,
        string fileName,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default);
}
