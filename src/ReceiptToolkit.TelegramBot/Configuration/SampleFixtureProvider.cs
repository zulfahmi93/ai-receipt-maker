using ReceiptToolkit.Contracts;
using ReceiptToolkit.TelegramBot.Handlers;

namespace ReceiptToolkit.TelegramBot.Configuration;

/// <summary>
///   Loads the bundled <c>examples/sample_receipt_data.json</c> fixture from
///   the bot's content root directory. Wired in DI as the production
///   <see cref="ISampleFixtureProvider"/>.
/// </summary>
internal sealed class SampleFixtureProvider : ISampleFixtureProvider
{
    private readonly string _path;

    public SampleFixtureProvider(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    public ReceiptData Load() => ReceiptData.FromJson(File.ReadAllText(_path));
}
