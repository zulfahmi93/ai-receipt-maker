# ReceiptToolkit.Core

Core SDK for validating, calculating, rendering, and exporting receipts. The SDK
uses `ReceiptToolkit.Contracts` models and exposes a single high-level facade:
`ReceiptGenerator`.

## Main APIs

| API | Purpose |
|---|---|
| `ReceiptData.FromJson(string)` / `ToJson()` | Parse and serialize camelCase receipt JSON. |
| `ReceiptValidator.Validate(ReceiptData)` | Return all business-rule violations without throwing. |
| `ReceiptCalculator.CalculateTotals(ReceiptData)` | Recalculate item totals, tax, discounts, payments, and grand total when `options.autoCalculateTotals` is true. |
| `MoneyFormatter.Format(...)` | Format decimal money using currency, locale, and symbol options. |
| `DateTimeFormatter.FormatDate/FormatTime(...)` | Format receipt timestamps from receipt options. |
| `ReceiptGenerator.GeneratePdfAsync(...)` | Validate, calculate, resolve local/data URI logo, and return PDF bytes. |
| `ReceiptGenerator.GeneratePngAsync(...)` | Return PNG bytes at the default 2x scale with shadow. |
| `ReceiptGenerator.GenerateSvgAsync(...)` | Return SVG bytes from the same renderer, with shadow suppressed. |
| `ReceiptGenerator.SavePdfAsync(...)` / `SavePngAsync(...)` | Generate and write files, creating parent directories as needed. |

`ReceiptGenerator` throws `ReceiptValidationException` when validation fails.
The exception contains the full validation error list.
For hosted applications, use the constructor that accepts `IClock` and a
caller-owned `FontProvider`; this keeps PDF metadata deterministic in tests and
lets the process reuse embedded font resources.

## Example

```csharp
using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Generation;

ReceiptData data = ReceiptData.FromJson(
    await File.ReadAllTextAsync("examples/sample_receipt_data.json"));

using var generator = new ReceiptGenerator();
await generator.SavePdfAsync(data, "artifacts/receipt.pdf");
await generator.SavePngAsync(data, "artifacts/receipt.png");

byte[] svg = await generator.GenerateSvgAsync(data);
await File.WriteAllBytesAsync("artifacts/receipt.svg", svg);
```

## JSON Shape

The full schema is represented by the contract records in
`src/ReceiptToolkit.Contracts/Models/`. A minimal valid receipt looks like:

```json
{
  "schemaVersion": 1,
  "business": {
    "businessName": "Elevate Studio"
  },
  "receipt": {
    "receiptNumber": "INV-2025-06789",
    "dateTime": "2025-05-18T10:42:00"
  },
  "items": [
    {
      "name": "Premium Notebook",
      "quantity": 1,
      "unitPrice": "12.50",
      "discount": "0",
      "taxRate": 0.0825,
      "total": "12.50"
    }
  ],
  "totals": {
    "subtotal": "12.50",
    "discountTotal": "0",
    "serviceCharge": "0",
    "taxTotal": "1.03",
    "roundingAdjustment": "0",
    "grandTotal": "13.53"
  },
  "payments": [
    {
      "method": "Cash",
      "amount": "13.53"
    }
  ],
  "options": {
    "currency": "MYR",
    "currencySymbol": "RM",
    "locale": "ms-MY",
    "autoCalculateTotals": true
  }
}
```

Money values are `decimal` in .NET and JSON strings on the wire. Rounding is
`MidpointRounding.AwayFromZero`.

## Rendering Rules

- SkiaSharp is the only render engine. The same renderer is used for PDF, PNG,
  and SVG canvas backends.
- Inter Variable Font is embedded and loaded through `FontProvider`; system font
  fallback is intentionally avoided.
- Theme color fallback runs through `ThemeColors.ResolveOrDefault`.
- Logo sources are local file paths or PNG/JPEG `data:` URIs. HTTP/HTTPS sources
  are rejected.
- PDF timestamps use injected `IClock`; tests can supply a deterministic clock.
- PDF export uses a 1200px default page height and strip-based pagination; a
  very tall section can be split across pages.
- PNG export defaults to 2x scale with shadow enabled.
- SVG export suppresses shadow.
- Golden tests run on Linux only because Skia output is not byte-stable across
  OS/architecture combinations.
