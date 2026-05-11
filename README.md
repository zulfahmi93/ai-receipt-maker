# Receipt Toolkit

Receipt Toolkit turns structured receipt JSON into validated PDF, PNG, and SVG
artifacts. The repository includes the shared .NET SDK, an ASP.NET Core API, a
System.CommandLine CLI, a Telegram long-polling bot, and a Flutter macOS demo
that consumes the API.

## Architecture

```text
ReceiptData JSON
  -> ReceiptToolkit.Contracts   camelCase models, decimal money string JSON
  -> ReceiptToolkit.Core        validate -> calculate -> render once with SkiaSharp
       -> PDF exporter          SKDocument PDF canvas
       -> PNG exporter          bitmap canvas, 2x scale, shadow by default
       -> SVG exporter          SKSvgCanvas, no shadow
  -> ReceiptToolkit.Api         HTTP endpoints + generated OpenAPI
  -> ReceiptToolkit.Cli         validate/generate/sample commands
  -> ReceiptToolkit.TelegramBot long polling bot commands + JSON message handler
  -> receipt_demo_flutter       macOS API client and preview/share UI
```

The renderer uses embedded Inter Variable Font through `FontProvider`; it does
not rely on system fonts. Logo sources are local file paths or `data:` image
URIs only. HTTP/HTTPS logo fetches are rejected to keep rendering deterministic
and SSRF-safe.

## Versions

| Component | Version |
|---|---:|
| .NET SDK | 10.0.105 |
| Flutter | 3.41.9 stable |
| SkiaSharp | 4.147.0-preview.1.1 |
| QRCoder | 1.8.0 |
| Telegram.Bot | 22.9.6.2 |
| System.CommandLine | 2.0.7 |
| xUnit | 3.2.2 |
| Microsoft.NET.Test.Sdk | 17.14.1 |
| Inter Variable Font | v4.1, OFL-1.1, embedded |

Pinned package versions live in `Directory.Packages.props`.

## Setup

Prerequisites:

- .NET SDK 10.0.105 or compatible 10.x patch.
- Flutter 3.41.9 stable for the macOS demo.
- macOS, Linux, or Windows for the .NET projects. The Flutter demo target here is
  macOS.

Restore and build:

```bash
dotnet restore receipt-toolkit.sln
dotnet build receipt-toolkit.sln
```

Run the API:

```bash
dotnet run --project src/ReceiptToolkit.Api/ReceiptToolkit.Api.csproj
```

The development profile listens on `http://localhost:5273`. OpenAPI is served at
`http://localhost:5273/openapi/v1.json` and checked in at
`docs/api/openapi.json`.

Run the CLI:

```bash
dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- validate \
  --input examples/sample_receipt_data.json

dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- generate \
  --input examples/sample_receipt_data.json \
  --pdf artifacts/sample.pdf \
  --png artifacts/sample.png
```

Run the Telegram bot:

```bash
TELEGRAM_BOT_TOKEN=123456:bot-token \
dotnet run --project src/ReceiptToolkit.TelegramBot/ReceiptToolkit.TelegramBot.csproj
```

Run the Flutter macOS demo:

```bash
cd apps/receipt_demo_flutter
flutter pub get
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5273
```

## Tests

Baseline gates:

```bash
dotnet build receipt-toolkit.sln
dotnet test receipt-toolkit.sln

cd apps/receipt_demo_flutter
flutter test
flutter analyze
flutter build macos
```

Golden PDF/PNG byte tests are Linux-only. Local macOS runs skip them; the
checked-in bytes under `examples/golden/` are regenerated through the manual
`regen-goldens` GitHub workflow when rendering intentionally changes.

## Examples

- `examples/sample_receipt_data.json` is the canonical receipt fixture and is
  also bundled into the CLI, API, bot, and Flutter demo.
- `examples/sample_receipt_long_items.json` exercises multi-page PDF output.

Money fields are JSON strings, for example `"unitPrice": "12.50"` and
`"grandTotal": "57.05"`. Tax rates are ratios and remain JSON numbers.

## Limitations

- No remote logo fetching. Use a local path or `data:image/png;base64,...` /
  `data:image/jpeg;base64,...`.
- PDF/A, PDF/UA tagging, and accessibility metadata are outside the MVP.
- Telegram uses long polling; webhook deployment is documented as future work in
  ADR 0003.
- A small handful of mockup nits remain — see the "Phase 3c-polish" follow-up
  section in `docs/PROGRESS.md` (header logo image rendering, payment
  card-icon glyph, customer/cashier column order, footer block centering,
  ms-MY time-suffix glyph).

## Future Work

- Add webhook mode for hosted Telegram deployments (ADR 0003).
- Promote the SkiaSharp 4 preview dependency when a stable line exposes the
  variable-font axis API used by `FontProvider`.
- Vision-LLM intake flow described in `docs/plans/001-car-workshop-jobcard.md`
  (Telegram photo → vision model → structured job data → renderer).

## Version Decision Log

- xUnit v3 is used for all .NET tests.
- FluentAssertions is intentionally not used because v8+ moved to a commercial
  license; tests use plain xUnit assertions.
- System.CommandLine 2.0.7 is pinned now that the GA API is available.
- Rounding uses `MidpointRounding.AwayFromZero` for consumer round-half-up
  behavior in calculation and formatting.
- SkiaSharp 4 preview is pinned because Inter VF weight-axis selection requires
  the newer wrapper API.

## Documentation Map

- Core SDK: `src/ReceiptToolkit.Core/README.md`
- HTTP API: `src/ReceiptToolkit.Api/README.md`
- CLI: `src/ReceiptToolkit.Cli/README.md`
- Telegram bot: `src/ReceiptToolkit.TelegramBot/README.md`
- Flutter macOS demo: `apps/receipt_demo_flutter/README.md`
- Architecture decisions: `docs/adr/`
