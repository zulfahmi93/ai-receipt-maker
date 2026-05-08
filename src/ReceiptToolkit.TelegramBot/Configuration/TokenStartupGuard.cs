using Microsoft.Extensions.Options;

namespace ReceiptToolkit.TelegramBot.Configuration;

/// <summary>
///   Hosted service that fires before <c>BotWorker</c> and forces resolution
///   of <see cref="TelegramOptions"/>. When the token is missing, the options
///   validator surfaces an <see cref="OptionsValidationException"/>; this
///   guard converts that into an <see cref="InvalidOperationException"/>
///   with the validator's user-facing message so host startup fails fast.
/// </summary>
internal sealed class TokenStartupGuard : IHostedService
{
    private readonly IOptions<TelegramOptions> _options;

    public TokenStartupGuard(IOptions<TelegramOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Touching .Value triggers IValidateOptions evaluation.
            _ = _options.Value.Token;
        }
        catch (OptionsValidationException ex)
        {
            string message = ex.Failures.FirstOrDefault() ?? ex.Message;
            throw new InvalidOperationException(message, ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
