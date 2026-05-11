# receipt_demo_flutter

Flutter macOS demo for Receipt Toolkit. The app loads the bundled sample JSON,
lets you edit receipt/theme/layout fields, calls the ASP.NET API for validation
and rendering, previews the PNG, and shares generated artifacts.

The app does not render receipts locally. It uses the API endpoints for
validation, PNG generation, and PDF generation.

## Prerequisites

- Flutter 3.41.9 stable with macOS desktop support enabled.
- Xcode command-line tools.
- ReceiptToolkit.Api running locally or at a reachable URL.

Start the API from the repository root:

```bash
dotnet run --project src/ReceiptToolkit.Api/ReceiptToolkit.Api.csproj
```

The default app API base URL is `http://localhost:5273`.

## Run on macOS

```bash
cd apps/receipt_demo_flutter
flutter pub get
flutter run -d macos
```

Point the app at a different API host with `API_BASE_URL`:

```bash
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5273
```

Build a release app:

```bash
flutter build macos --dart-define=API_BASE_URL=http://localhost:5273
```

## Test and Analyze

```bash
flutter test
flutter analyze
flutter build macos
```

## App Surface

- State is managed with `provider` and a `ChangeNotifier` (`ReceiptState`).
- The initial document loads from bundled `assets/sample_receipt_data.json`.
- The API client calls `/api/receipts/validate`, `/api/receipts/png`, and
  `/api/receipts/pdf`, applies a 30-second timeout, and throws typed
  `ReceiptApiException` values for non-2xx responses.
- The UI contains a PNG preview, JSON editor, theme panel, and actions row.
- The theme panel exposes four color fields (`accentColor`, `highlightColor`,
  `paperColor`, `textColor`), QR/logo/footer-contact toggles, and receipt width
  choices `360` and `420`.
- Share PDF always requests fresh PDF bytes from the API. Share PNG reuses the
  preview bytes and regenerates only when the preview is missing.

## Sharing

Sharing uses `share_plus` through `SharePlus.instance.share`, `ShareParams`, and
in-memory `XFile.fromData` values for `receipt.pdf` and `receipt.png`.

## macOS Entitlements

The demo is sandboxed and keeps outbound network access enabled:

- `macos/Runner/DebugProfile.entitlements`
- `macos/Runner/Release.entitlements`

Both include `com.apple.security.network.client` so the app can reach the local
or remote Receipt Toolkit API. The Debug/Profile entitlement also allows JIT for
Flutter debugging. Network server entitlement is not required.

File sharing is handled by `share_plus` using generated in-memory files. No
extra file-read entitlement is required for the current share flow.
