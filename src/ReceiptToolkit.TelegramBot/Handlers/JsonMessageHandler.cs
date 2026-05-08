using System.Text.Json;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Handles plain text messages that are NOT slash commands. Attempts to parse
///   the body as <see cref="ReceiptData"/> JSON and either returns the rendered
///   PDF + PNG, a "not JSON" hint, a formatted validation-error list, or a
///   user-friendly generation-failure fallback.
/// </summary>
public sealed class JsonMessageHandler : IUpdateHandler
{
    private static readonly char[] JsonBodyStart = ['{', '['];

    private readonly ReceiptGenerator _generator;
    private readonly ILogger<JsonMessageHandler> _logger;

    /// <summary>Initialises a new <see cref="JsonMessageHandler"/>.</summary>
    /// <param name="generator">Receipt generator façade.</param>
    /// <param name="logger">Structured logger.</param>
    public JsonMessageHandler(ReceiptGenerator generator, ILogger<JsonMessageHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(logger);
        _generator = generator;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(Update update)
    {
        ArgumentNullException.ThrowIfNull(update);
        string? text = update.Message?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Slash-command updates are owned by command handlers; JsonMessageHandler
        // claims everything else (including non-JSON, which gets the hint).
        return !text.AsSpan().TrimStart().StartsWith("/");
    }

    /// <inheritdoc />
    public async Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(sender);
        long chatId = update.Message!.Chat.Id;
        string body = update.Message.Text!;

        if (!LooksLikeJson(body))
        {
            await sender.SendTextAsync(chatId, BotMessages.NotJsonHint, cancellationToken).ConfigureAwait(false);
            return;
        }

        ReceiptData data;
        try
        {
            data = ReceiptData.FromJson(body);
        }
        catch (JsonException)
        {
            await sender.SendTextAsync(chatId, BotMessages.NotJsonHint, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            byte[] pdf = await _generator.GeneratePdfAsync(data, cancellationToken).ConfigureAwait(false);
            byte[] png = await _generator.GeneratePngAsync(data, cancellationToken).ConfigureAwait(false);
            await sender.SendDocumentAsync(chatId, BotMessages.ReceiptCaptionPdf, pdf, cancellationToken).ConfigureAwait(false);
            await sender.SendDocumentAsync(chatId, BotMessages.ReceiptCaptionPng, png, cancellationToken).ConfigureAwait(false);
        }
        catch (ReceiptValidationException ex)
        {
            string formatted = ValidationErrorFormatter.Format(ex.Errors);
            await sender.SendTextAsync(chatId, formatted, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            BotLog.GenerationFailed(_logger, chatId, ex);
            await sender.SendTextAsync(chatId, BotMessages.GenerationFailed, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool LooksLikeJson(string text)
    {
        ReadOnlySpan<char> trimmed = text.AsSpan().Trim();
        return trimmed.Length > 0 && JsonBodyStart.AsSpan().Contains(trimmed[0]);
    }
}
