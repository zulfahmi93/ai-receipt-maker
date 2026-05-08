using Microsoft.Extensions.Options;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;
using ReceiptToolkit.TelegramBot.Polling;
using Telegram.Bot;

namespace ReceiptToolkit.TelegramBot.Configuration;

/// <summary>DI registration for the Receipt Toolkit Telegram bot.</summary>
public static class BotServiceCollectionExtensions
{
    /// <summary>
    ///   Registers options + validation, command handlers, the update router,
    ///   the polling client, and <see cref="BotWorker"/> as a hosted service.
    ///   The <c>TELEGRAM_BOT_TOKEN</c> environment variable is read into
    ///   <see cref="TelegramOptions.Token"/> when the <c>Telegram:Token</c>
    ///   configuration key is missing.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="contentRootPath">Bot content root used to locate the sample fixture.</param>
    public static IServiceCollection AddReceiptBot(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(contentRootPath);

        services.AddOptions<TelegramOptions>()
            .Configure(opts =>
            {
                opts.Token = configuration[$"{TelegramOptions.SectionName}:Token"]
                    ?? configuration["TELEGRAM_BOT_TOKEN"]
                    ?? string.Empty;
            })
            .Services
            .AddSingleton<IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();

        // Token guard runs before BotWorker — converts validation failures to
        // user-facing InvalidOperationException at host startup.
        services.AddHostedService<TokenStartupGuard>();

        services.AddSingleton<ReceiptGenerator>();
        services.AddSingleton<ISampleFixtureProvider>(_ =>
            new SampleFixtureProvider(Path.Combine(contentRootPath, "examples", "sample_receipt_data.json")));

        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            TelegramOptions opts = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
            return new TelegramBotClient(opts.Token);
        });
        services.AddSingleton<ITelegramSender, TelegramBotClientSender>();

        // Handler ordering controls dispatch priority (BotUpdateRouter first-match wins).
        services.AddSingleton<IUpdateHandler, StartCommandHandler>();
        services.AddSingleton<IUpdateHandler, HelpCommandHandler>();
        services.AddSingleton<IUpdateHandler, SampleCommandHandler>();
        services.AddSingleton<IUpdateHandler, JsonMessageHandler>();

        services.AddSingleton<BotUpdateRouter>();
        services.AddSingleton<IPollingClient, TelegramPollingClient>();
        services.AddHostedService<BotWorker>();

        return services;
    }
}
