using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ReceiptToolkit.Core.Generation;
using ReceiptToolkit.TelegramBot.Handlers;
using ReceiptToolkit.TelegramBot.Messaging;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

public sealed class JsonMessageHandlerTests
{
    private static JsonMessageHandler Build(out ReceiptGenerator gen)
    {
        gen = new ReceiptGenerator();
        return new JsonMessageHandler(gen, NullLogger<JsonMessageHandler>.Instance);
    }

    [Fact]
    public async Task T6_4_ValidJson_SendsPdfAndPng()
    {
        var sender = Substitute.For<ITelegramSender>();
        using var generator = new ReceiptGenerator();
        var handler = new JsonMessageHandler(generator, NullLogger<JsonMessageHandler>.Instance);

        string json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_receipt_data.json"),
            TestContext.Current.CancellationToken);
        var update = HandlerTestBase.TextMessage(json);

        Assert.True(handler.CanHandle(update));
        await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

        await sender.Received(1).SendDocumentAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(n => n.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<ReadOnlyMemory<byte>>(m => m.Length > 0),
            Arg.Any<CancellationToken>());
        await sender.Received(1).SendDocumentAsync(
            HandlerTestBase.TestChatId,
            Arg.Is<string>(n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<ReadOnlyMemory<byte>>(m => m.Length > 0),
            Arg.Any<CancellationToken>());
        await sender.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task T6_5_NotJson_SendsHint()
    {
        var sender = Substitute.For<ITelegramSender>();
        var handler = Build(out var generator);
        using (generator)
        {
            var update = HandlerTestBase.TextMessage("hello there, this is not JSON at all");
            Assert.True(handler.CanHandle(update));
            await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

            await sender.Received(1).SendTextAsync(
                HandlerTestBase.TestChatId,
                Arg.Is<string>(s =>
                    s.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("valid", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>());
            await sender.DidNotReceive().SendDocumentAsync(
                Arg.Any<long>(),
                Arg.Any<string>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task T6_5_MalformedJson_SendsHint()
    {
        var sender = Substitute.For<ITelegramSender>();
        var handler = Build(out var generator);
        using (generator)
        {
            var update = HandlerTestBase.TextMessage("{ this is not: valid json }");
            await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

            await sender.Received(1).SendTextAsync(
                HandlerTestBase.TestChatId,
                Arg.Is<string>(s => s.Contains("JSON", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task T6_6_ValidJsonButValidationFails_SendsFormattedErrorList()
    {
        // Minimal JSON that parses (FromJson succeeds) but fails validation:
        // empty businessName + empty items list (both rules trip).
        const string json = """
        {
          "schemaVersion": 1,
          "business": { "businessName": "" },
          "items": [],
          "totals": {
            "subtotal": "0.00",
            "taxAmount": "0.00",
            "discountAmount": "0.00",
            "total": "0.00"
          },
          "options": { "currency": "USD" }
        }
        """;

        var sender = Substitute.For<ITelegramSender>();
        var handler = Build(out var generator);
        using (generator)
        {
            var update = HandlerTestBase.TextMessage(json);
            await handler.HandleAsync(update, sender, TestContext.Current.CancellationToken);

            // Single text message containing the header + at least one bullet.
            await sender.Received(1).SendTextAsync(
                HandlerTestBase.TestChatId,
                Arg.Is<string>(s =>
                    s.Contains("validation failed", StringComparison.OrdinalIgnoreCase) &&
                    s.Contains('•')),
                Arg.Any<CancellationToken>());

            // No documents sent on validation failure.
            await sender.DidNotReceive().SendDocumentAsync(
                Arg.Any<long>(),
                Arg.Any<string>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public void T6_4_DoesNotClaimSlashCommands()
    {
        var handler = Build(out var generator);
        using (generator)
        {
            Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/start")));
            Assert.False(handler.CanHandle(HandlerTestBase.TextMessage("/sample")));
            Assert.False(handler.CanHandle(HandlerTestBase.TextMessage(string.Empty)));
        }
    }
}
