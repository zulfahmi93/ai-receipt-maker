using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Messaging;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Handles <c>/sample</c> by loading the bundled sample fixture, generating
///   PDF + PNG via <see cref="ReceiptGenerator"/>, and uploading both as
///   documents to the calling chat.
/// </summary>
public sealed class SampleCommandHandler : IUpdateHandler
{
    private readonly ReceiptGenerator _generator;
    private readonly ISampleFixtureProvider _fixture;
    private readonly ILogger<SampleCommandHandler> _logger;

    /// <summary>Initialises a new <see cref="SampleCommandHandler"/>.</summary>
    /// <param name="generator">Receipt generator façade.</param>
    /// <param name="fixture">Sample fixture loader.</param>
    /// <param name="logger">Structured logger.</param>
    public SampleCommandHandler(
        ReceiptGenerator generator,
        ISampleFixtureProvider fixture,
        ILogger<SampleCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(logger);
        _generator = generator;
        _fixture = fixture;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(Update update) => StartCommandHandler.IsCommand(update, "/sample");

    /// <inheritdoc />
    public async Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(sender);
        long chatId = update.Message!.Chat.Id;

        try
        {
            ReceiptData data = _fixture.Load();
            byte[] pdf = await _generator.GeneratePdfAsync(data, cancellationToken).ConfigureAwait(false);
            byte[] png = await _generator.GeneratePngAsync(data, cancellationToken).ConfigureAwait(false);

            await sender.SendDocumentAsync(chatId, BotMessages.SampleCaptionPdf, pdf, cancellationToken).ConfigureAwait(false);
            await sender.SendDocumentAsync(chatId, BotMessages.SampleCaptionPng, png, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            BotLog.SampleGenerationFailed(_logger, ex);
            await sender.SendTextAsync(chatId, BotMessages.GenerationFailed, cancellationToken).ConfigureAwait(false);
        }
    }
}
