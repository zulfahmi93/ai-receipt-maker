using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReceiptToolkit.TelegramBot.Configuration;

namespace ReceiptToolkit.TelegramBot.Tests.Configuration;

public sealed class TokenStartupGuardTests
{
    private static IHost BuildHostWithToken(string? token)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Development",
            ContentRootPath = AppContext.BaseDirectory,
        });

        var config = new ConfigurationBuilder();
        if (token is not null)
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:Token"] = token,
            });
        }

        builder.Configuration.AddConfiguration(config.Build());
        builder.Services.AddOptions<TelegramOptions>()
            .Configure(opts =>
            {
                opts.Token = builder.Configuration[$"{TelegramOptions.SectionName}:Token"]
                    ?? builder.Configuration["TELEGRAM_BOT_TOKEN"]
                    ?? string.Empty;
            })
            .Services
            .AddSingleton<Microsoft.Extensions.Options.IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();
        builder.Services.AddHostedService<TokenStartupGuard>();
        return builder.Build();
    }

    [Fact]
    public async Task T6_7_MissingToken_ThrowsInvalidOperationExceptionWithClearMessage()
    {
        using IHost host = BuildHostWithToken(token: null);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TELEGRAM_BOT_TOKEN", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task T6_7_EmptyToken_ThrowsInvalidOperationException()
    {
        using IHost host = BuildHostWithToken(token: "   ");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TELEGRAM_BOT_TOKEN", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task T6_7_PresentToken_StartsCleanly()
    {
        using IHost host = BuildHostWithToken(token: "fake-token-12345:abc");
        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
