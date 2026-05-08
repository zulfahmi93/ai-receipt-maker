using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.TelegramBot.Handlers;

/// <summary>
///   Loads the bundled sample <see cref="ReceiptData"/> fixture used by the
///   <c>/sample</c> command. Abstracted so tests can substitute an in-memory
///   fixture without touching the filesystem.
/// </summary>
public interface ISampleFixtureProvider
{
    /// <summary>Returns the parsed sample receipt data.</summary>
    ReceiptData Load();
}
