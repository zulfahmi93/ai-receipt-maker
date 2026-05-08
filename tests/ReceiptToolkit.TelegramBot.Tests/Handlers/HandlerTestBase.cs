using ReceiptToolkit.Contracts;
using Telegram.Bot.Types;

namespace ReceiptToolkit.TelegramBot.Tests.Handlers;

/// <summary>
///   Shared helpers for handler tests — minimal <see cref="Update"/> and
///   <see cref="ReceiptData"/> fixture builders.
/// </summary>
internal static class HandlerTestBase
{
    public const long TestChatId = 12345L;

    /// <summary>Builds a text-message <see cref="Update"/> for <see cref="TestChatId"/>.</summary>
    public static Update TextMessage(string text, long chatId = TestChatId) =>
        new()
        {
            Id = 1,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = chatId, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                Text = text,
            },
        };

    /// <summary>Loads <c>Fixtures/sample_receipt_data.json</c> from the test output directory.</summary>
    public static ReceiptData LoadSample()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_receipt_data.json");
        return ReceiptData.FromJson(File.ReadAllText(path));
    }
}
