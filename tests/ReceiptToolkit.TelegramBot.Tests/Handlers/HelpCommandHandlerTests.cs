using NSubstitute;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

public sealed class HelpCommandHandlerTests
{
    [Fact]
    public async Task T6_2_Help_SendsCommandListAndJsonGuidance()
    {
        var sender = Substitute.For<ITelegramSender>();
        var handler = new HelpCommandHandler();
        var update = HandlerTestBase.TextMessage("/help");

        Assert.True(handler.CanHandle(update));
        await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

        await sender.Received(1).SendTextAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(s =>
                s.Contains("/start", StringComparison.Ordinal) &&
                s.Contains("/help", StringComparison.Ordinal) &&
                s.Contains("/sample", StringComparison.Ordinal) &&
                s.Contains("JSON", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void T6_2_Help_RejectsNonHelpUpdates()
    {
        var handler = new HelpCommandHandler();
        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/start")));
        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("help")));
    }
}
