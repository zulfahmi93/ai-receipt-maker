# ADR 0003 — Telegram bot uses long polling for MVP

- **Status:** Accepted
- **Date:** 2026-05-06

## Context

Telegram bots receive updates two ways:
- **Long polling** (`getUpdates`): client periodically asks Telegram for new messages. Works behind NAT, no public URL needed.
- **Webhook**: Telegram POSTs to a public HTTPS URL whenever an update happens. Lower latency, requires TLS-terminated public endpoint and a secret token.

Local dev requirement is paramount per project spec — bot must run on a developer laptop without ngrok/tunneling.

## Decision

MVP uses **long polling** via `Telegram.Bot.ITelegramBotClient.GetUpdatesAsync` in a hosted `BackgroundService`.

Webhook migration is **possible but not implemented** in MVP.

## Migration path (future)

To switch to webhook:
1. Deploy `ReceiptToolkit.Api` (or a dedicated bot endpoint) to a public HTTPS host.
2. Add `POST /api/telegram/webhook` endpoint that validates `X-Telegram-Bot-Api-Secret-Token`.
3. Call `setWebhook` with that URL on bot startup.
4. Drop the polling worker.

The `ITelegramSender` abstraction means handler logic stays identical — only the update-source changes.

## Consequences

**Positive**
- Zero infrastructure for local dev. Just `TELEGRAM_BOT_TOKEN`.
- Polling is robust to flaky networks.

**Negative**
- ~100-1000ms higher message latency vs webhook.
- Wastes a small amount of bandwidth even when idle.
- One-bot-per-token: cannot horizontally scale polling without coordination.
