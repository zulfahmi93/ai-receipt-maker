using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

public sealed class SampleCommandHandlerTests
{
    private sealed class StaticSampleFixture : ISampleFixtureProvider
    {
        public ReceiptData Load() => HandlerTestBase.LoadSample();
    }

    [Fact]
    public async Task T6_3_Sample_SendsPdfAndPngDocuments()
    {
        var sender = Substitute.For<ITelegramSender>();
        using var generator = new ReceiptGenerator();
        var handler = new SampleCommandHandler(
            generator,
            new StaticSampleFixture(),
            NullLogger<SampleCommandHandler>.Instance);
        var update = HandlerTestBase.TextMessage("/sample");

        Assert.True(handler.CanHandle(update));
        await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

        // Expect exactly 2 SendDocumentAsync calls — PDF then PNG, both non-empty.
        await sender.Received(2).SendDocumentAsync(
            HandlerTestBase.TestChatId,
            Arg.Any<string>(),
            Arg.Is<ReadOnlyMemory<byte>>(m => m.Length > 0),
            Arg.Any<CancellationToken>());

        await sender.Received(1).SendDocumentAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(n => n.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());

        await sender.Received(1).SendDocumentAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void T6_3_Sample_RejectsNonSampleUpdates()
    {
        using var generator = new ReceiptGenerator();
        var handler = new SampleCommandHandler(
            generator,
            new StaticSampleFixture(),
            NullLogger<SampleCommandHandler>.Instance);

        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/help")));
        Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/start")));
    }
}
