# ADR 0002 — Decimal money type + string JSON serialization

- **Status:** Accepted
- **Date:** 2026-05-06

## Context

Receipt totals, item prices, and payment amounts are money. .NET `decimal` is the correct in-memory type (base-10, no float drift). However:

- JSON's number type is IEEE-754 double on most parsers (JS, Dart `num`, Go `float64` by default). A receipt total of `56.73` round-trips as `56.729999999999...` in some consumers.
- Consumers (Flutter app, Telegram bot in any language, future external integrations) must read totals exactly.

## Decision

- Use `decimal` end-to-end inside the SDK.
- Serialize and deserialize all monetary fields as JSON **strings** ("56.73"), not numbers.
- Tax rates remain as JSON numbers (they're ratios, not currency).
- Provide a `JsonConverter<decimal>` that reads either string or number for tolerance, and writes string only.

## Consequences

**Positive**
- No precision loss across any consumer.
- Explicit + auditable in JSON.

**Negative**
- Slight ergonomic friction: writing JSON by hand requires quotes around money values.
- The sample JSON example matches this — `"unitPrice": "12.50"` not `12.50`.

## Affected fields

`items[].unitPrice`, `items[].discount`, `items[].total`, `totals.subtotal`, `totals.discountTotal`, `totals.serviceCharge`, `totals.taxTotal`, `totals.roundingAdjustment`, `totals.grandTotal`, `payments[].amount`.
