# ReceiptToolkit.TelegramBot

Hosted Telegram bot that receives receipt JSON over chat and returns generated
PDF and PNG documents. The MVP uses long polling, not webhooks.

## Configuration

| Setting | Environment variable | Required | Description |
|---|---|---:|---|
| `Telegram:Token` | `TELEGRAM_BOT_TOKEN` | yes | BotFather-issued Telegram bot token. |

`TELEGRAM_BOT_TOKEN` is accepted directly and mapped into the `Telegram` options
section. Startup fails fast when the token is missing or whitespace.

## Run

```bash
TELEGRAM_BOT_TOKEN=123456:bot-token \
dotnet run --project src/ReceiptToolkit.TelegramBot/ReceiptToolkit.TelegramBot.csproj
```

The bot runs as a `BackgroundService` and polls Telegram through
`Telegram.Bot` 22.9.6.2. See `docs/adr/0003-bot-polling-vs-webhook.md` for the
polling decision and webhook migration path.

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
