using ReceiptToolkit.TelegramBot.Configuration;

// Phase 6 entry point. AddReceiptBot wires:
// - TelegramOptions (validated; missing TELEGRAM_BOT_TOKEN fails fast at startup)
// - ReceiptGenerator + ISampleFixtureProvider (Core façade)
// - ITelegramBotClient + ITelegramSender (Telegram.Bot 22.9 wrapper)
// - Command handlers + JSON message handler routed through BotUpdateRouter
// - BotWorker hosted service running the long-polling loop (ADR 0003).
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddReceiptBot(builder.Configuration, builder.Environment.ContentRootPath);

IHost host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
