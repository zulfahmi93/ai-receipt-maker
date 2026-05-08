using NSubstitute;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

public sealed class StartCommandHandlerTests
{
    [Fact]
    public async Task T6_1_Start_SendsWelcomeText()
    {
        var sender = Substitute.For<ITelegramSender>();
        var handler = new StartCommandHandler();
        var update = HandlerTestBase.TextMessage("/start");

        Assert.True(handler.CanHandle(update));
        await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

        await sender.Received(1).SendTextAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(s => s.Contains("Welcome", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void T6_1_Start_RejectsNonStartUpdates()
    {
        var handler = new StartCommandHandler();
        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/help")));
        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("hello")));
    }

    [Fact]
    public void T6_1_Start_AcceptsCommandWithBotMention()
    {
        var handler = new StartCommandHandler();
        Assert.True(handler.CanHandle(HandlerTestBase.TextMessage("/start@receipt_bot")));
    }
}
