using NSubstitute;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;
using ReceiptToolkit.TelegramBot.Polling;
using ReceiptToolkit.TelegramBot.Tests.Handlers;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Tests.Polling;

public sealed class BotUpdateRouterTests
{
    private sealed class FakeHandler : IUpdateHandler
    {
        public bool ClaimsAll { get; init; }
        public string? MatchVerb { get; init; }
        public int HandleCount { get; private set; }

        public bool CanHandle(Update update)
        {
            if (ClaimsAll) return true;
            return update.Message?.Text == MatchVerb;
        }

        public Task HandleAsync(Update update, ITelegramSender sender, CancellationToken cancellationToken = default)
        {
            HandleCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task FirstMatchingHandlerWins()
    {
        var startHandler = new FakeHandler { MatchVerb = "/start" };
        var fallback = new FakeHandler { ClaimsAll = true };
        var router = new BotUpdateRouter([startHandler, fallback]);
        var sender = Substitute.For<ITelegramSender>();

        await router.RouteAsync(
            HandlerTestBase.TextMessage("/start"),
            sender,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, startHandler.HandleCount);
        Assert.Equal(0, fallback.HandleCount);
    }

    [Fact]
    public async Task FallbackTakesUnclaimedUpdate()
    {
        var startHandler = new FakeHandler { MatchVerb = "/start" };
        var fallback = new FakeHandler { ClaimsAll = true };
        var router = new BotUpdateRouter([startHandler, fallback]);
        var sender = Substitute.For<ITelegramSender>();

        await router.RouteAsync(
            HandlerTestBase.TextMessage("hello"),
            sender,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, startHandler.HandleCount);
        Assert.Equal(1, fallback.HandleCount);
    }

    [Fact]
    public async Task NoHandlerClaim_NoOp()
    {
        var startHandler = new FakeHandler { MatchVerb = "/start" };
        var router = new BotUpdateRouter([startHandler]);
        var sender = Substitute.For<ITelegramSender>();

        await router.RouteAsync(
            HandlerTestBase.TextMessage("hello"),
            sender,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, startHandler.HandleCount);
        await sender.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
