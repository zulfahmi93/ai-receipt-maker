using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Messaging;

/// <summary>
///   Production <see cref="ITelegramSender"/> wrapping the real
///   <see cref="ITelegramBotClient"/>.
/// </summary>
internal sealed class TelegramBotClientSender : ITelegramSender
{
    private readonly ITelegramBotClient _client;

    public TelegramBotClientSender(ITelegramBotClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        _client.SendMessage(chatId, text, cancellationToken: cancellationToken);

    public Task SendDocumentAsync(
        long chatId,
        string fileName,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        // InputFile.FromStream copies once; MemoryStream over the array is safe for the upload duration.
        var stream = new MemoryStream(bytes.ToArray(), writable: false);
        InputFile file = InputFile.FromStream(stream, fileName);
        return _client.SendDocument(chatId, file, cancellationToken: cancellationToken);
    }
}
