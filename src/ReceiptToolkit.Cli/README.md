# ReceiptToolkit.Cli

Command-line interface for validating receipt JSON and generating PDF/PNG
artifacts.

Run with `dotnet run` during development:

```bash
dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- --help
```

After publish, invoke the produced executable or `dotnet receipt-toolkit.dll`.

## Commands

### `validate`

Validate a receipt JSON file against schema parsing and business rules.

```bash
dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- validate \
  --input examples/sample_receipt_data.json
```

Options:

| Option | Required | Description |
|---|---:|---|
| `--input`, `-i` | yes | Path to receipt JSON. |

Exit codes:

| Code | Meaning |
|---:|---|
| `0` | Valid. |
| `1` | Input file missing or unreadable JSON. |
| `2` | Validation failed; errors are written to stderr. |

### `generate`

Generate PDF and/or PNG output from receipt JSON.

```bash
dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- generate \
  --input examples/sample_receipt_data.json \
  --pdf artifacts/receipt.pdf \
  --png artifacts/receipt.png
```

Options:

| Option | Required | Description |
|---|---:|---|
| `--input`, `-i` | yes | Path to receipt JSON. |
| `--pdf` | no | Output path for PDF. |
| `--png` | no | Output path for PNG. |
| `--force`, `-f` | no | Overwrite existing output files. |

At least one of `--pdf` or `--png` is required. Parent directories are created
as needed. Existing output files are refused unless `--force` is supplied.

Exit codes:

| Code | Meaning |
|---:|---|
| `0` | Requested files generated. |
| `1` | Missing input, invalid JSON, or no output selected. |
| `2` | Validation failed; errors are written to stderr. |
| `3` | Output exists and `--force` was not supplied. |

### `sample`

Render the bundled `examples/sample_receipt_data.json` fixture to `sample.pdf`
and `sample.png`.

```bash
dotnet run --project src/ReceiptToolkit.Cli/ReceiptToolkit.Cli.csproj -- sample \
  --output artifacts/sample
```

Options:

| Option | Required | Description |
|---|---:|---|
| `--output`, `-o` | yes | Directory for `sample.pdf` and `sample.png`. |

The sample fixture is copied next to the CLI assembly, so the command works
after `dotnet publish`.
