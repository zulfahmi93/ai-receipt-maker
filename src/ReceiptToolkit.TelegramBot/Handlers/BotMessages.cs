namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Canned bot reply text. Centralised so tests can pin handler output
///   without hardcoding string literals across multiple files.
/// </summary>
internal static class BotMessages
{
    public const string Welcome =
        "Welcome to Receipt Toolkit! Send me a receipt JSON payload and I will return PDF + PNG.\n" +
        "Commands: /help · /sample";

    public const string Help =
        "Commands:\n" +
        "/start — show this welcome\n" +
        "/help — show this list\n" +
        "/sample — receive a sample PDF + PNG built from the bundled fixture\n\n" +
        "JSON format: send the full ReceiptData JSON in a single text message. " +
        "See examples/sample_receipt_data.json in the repo for the schema.";

    public const string NotJsonHint =
        "That doesn't look like JSON. Send a valid ReceiptData JSON payload " +
        "(see /help) or use /sample for an example.";

    public const string GenerationFailed =
        "Sorry — I couldn't generate the receipt. The team has been notified. " +
        "Please try again with /sample.";

    public const string ValidationHeader = "Receipt validation failed:";

    public const string SampleCaptionPdf = "sample.pdf";
    public const string SampleCaptionPng = "sample.png";
    public const string ReceiptCaptionPdf = "receipt.pdf";
    public const string ReceiptCaptionPng = "receipt.png";
}
