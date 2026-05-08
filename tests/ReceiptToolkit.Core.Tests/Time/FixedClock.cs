// Purpose: Deterministic IClock stub shared across exporter and generator tests.
// Promoted from inline PdfExporterTests at Phase 3e open so determinism + golden
// suites can pin a single fixed instant without per-file duplication.

using ReceiptToolkit.Contracts.Time;

namespace ReceiptToolkit.Core.Tests.Time;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
