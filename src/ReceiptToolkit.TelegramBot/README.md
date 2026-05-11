# ReceiptToolkit.TelegramBot

Hosted Telegram bot that receives receipt JSON over chat and returns generated
PDF and PNG documents. The MVP uses long polling, not webhooks.

## Configuration

| Setting | Environment variable | Required | Description |
|---|---|---:|---|
| `Telegram:Token` | `TELEGRAM_BOT_TOKEN` | yes | BotFather-issued Telegram bot token. |

`TELEGRAM_BOT_TOKEN` is accepted directly and mapped into the `Telegram` options
section. Startup fails fast when the token is missing or whitespace.

## Run (production)

```bash
dotnet publish src/ReceiptToolkit.TelegramBot --configuration Release
TELEGRAM_BOT_TOKEN=123456:bot-token \
dotnet exec src/ReceiptToolkit.TelegramBot/bin/Release/net10.0/receipt-toolkit-bot.dll
```

The bot runs as a `BackgroundService` and polls Telegram through
`Telegram.Bot` 22.9.6.2. See `docs/adr/0003-bot-polling-vs-webhook.md` for the
polling decision and webhook migration path.

## Local development

`dotnet run` does **not** propagate the parent shell's `TELEGRAM_BOT_TOKEN`
to the spawned bot process under SDK 10.0.105 (`TelegramOptionsValidator`
throws `OptionsValidationException: TELEGRAM_BOT_TOKEN is not configured`).
See `docs/PROGRESS.md` divergence #28 for the full trace.

Use `dotnet exec` against the built DLL instead:

```bash
dotnet build src/ReceiptToolkit.TelegramBot --configuration Release
TELEGRAM_BOT_TOKEN="$(grep ^TELEGRAM_BOT_TOKEN= .env | cut -d= -f2-)" \
dotnet exec src/ReceiptToolkit.TelegramBot/bin/Release/net10.0/receipt-toolkit-bot.dll
```

Alternative: put the token into a `Properties/launchSettings.json` profile
under `environmentVariables` and run with `dotnet run --launch-profile <name>`.

The API project (`src/ReceiptToolkit.Api`) is unaffected by this issue
because it reads `ASPNETCORE_URLS` rather than a custom env var.

## Commands

| Command | Response |
|---|---|
| `/start` | Welcome message and command summary. |
| `/help` | Command list and JSON usage hint. |
| `/sample` | Sends `sample.pdf` and `sample.png` generated from the bundled fixture. |
| Receipt JSON text | Validates and generates `receipt.pdf` and `receipt.png`. |

If the message is not JSON, the bot replies with a short hint. If validation
fails, the bot returns the full validation error list. Unexpected generation
errors are logged and reported with a generic failure message.

## Input Contract

Send the same `ReceiptData` JSON used by the SDK, CLI, and API. Start from
`examples/sample_receipt_data.json`. Money fields must be strings, for example
`"amount": "56.73"`.
