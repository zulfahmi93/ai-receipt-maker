# ReceiptToolkit.Api

ASP.NET Core Minimal API for validating receipt JSON and generating PDF/PNG
outputs. The development profile listens on `http://localhost:5273`.

## Run

```bash
dotnet run --project src/ReceiptToolkit.Api/ReceiptToolkit.Api.csproj
```

OpenAPI is served at `/openapi/v1.json` and checked in at
`docs/api/openapi.json`.

## Endpoints

| Method | Path | Success | Description |
|---|---|---:|---|
| `GET` | `/` | `200 application/json` | Service name, version, and status. |
| `POST` | `/api/receipts/validate` | `200 application/json` | Validates a receipt. Business validation failures return `valid:false`, not HTTP 400. |
| `POST` | `/api/receipts/pdf` | `200 application/pdf` | Generates a PDF. Validation failures become RFC7807 `400`. |
| `POST` | `/api/receipts/png` | `200 image/png` | Generates a PNG. Validation failures become RFC7807 `400`. |
| `POST` | `/api/receipts/both` | `200 application/json` | Returns `pdfBase64` and `pngBase64`. |
| `POST` | `/api/receipts/sample` | `200 application/pdf` | Generates a PDF from the bundled sample fixture. |

Malformed JSON and framework binding errors are returned as ProblemDetails.
Unhandled generation errors return `500 ProblemDetails` with a `traceId`.

## Curl Examples

Validate:

```bash
curl -sS http://localhost:5273/api/receipts/validate \
  -H 'content-type: application/json' \
  --data-binary @examples/sample_receipt_data.json
```

Generate PDF:

```bash
curl -sS http://localhost:5273/api/receipts/pdf \
  -H 'content-type: application/json' \
  --data-binary @examples/sample_receipt_data.json \
  --output artifacts/receipt.pdf
```

Generate PNG:

```bash
curl -sS http://localhost:5273/api/receipts/png \
  -H 'content-type: application/json' \
  --data-binary @examples/sample_receipt_data.json \
  --output artifacts/receipt.png
```

Generate both:

```bash
curl -sS http://localhost:5273/api/receipts/both \
  -H 'content-type: application/json' \
  --data-binary @examples/sample_receipt_data.json
```

Bundled sample PDF:

```bash
curl -sS -X POST http://localhost:5273/api/receipts/sample \
  --output artifacts/sample.pdf
```

## CORS

Development allows any origin for local tools and the Flutter macOS demo.
Production uses `Cors:AllowedOrigins`; an empty allow-list emits no
`Access-Control-Allow-Origin` header.
