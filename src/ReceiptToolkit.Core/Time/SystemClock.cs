using ReceiptToolkit.Contracts.Time;

namespace ReceiptToolkit.Core.Time;

/// <summary>
///   Default <see cref="IClock"/> implementation backed by
///   <see cref="DateTimeOffset.UtcNow"/>. Use in production wiring; tests should substitute
///   a fixed-time stub.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
