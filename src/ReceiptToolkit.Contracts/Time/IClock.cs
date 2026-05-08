namespace ReceiptToolkit.Contracts.Time;

/// <summary>
///   Abstraction over the current point in time. Inject into renderers, exporters, and the
///   generator façade so deterministic test clocks can produce byte-equal output across
///   runs (e.g. PDF <c>/CreationDate</c>, future-dated audit metadata).
/// </summary>
/// <remarks>
///   Implementations must always return a value with <see cref="DateTimeOffset.Offset"/> set.
///   Production callers should resolve through <c>SystemClock</c>; tests should pass a fixed
///   <see cref="DateTimeOffset"/> via a stub.
/// </remarks>
public interface IClock
{
    /// <summary>
    ///   The current instant. Implementations should return UTC unless a specific offset
    ///   is contractually required by the caller.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
