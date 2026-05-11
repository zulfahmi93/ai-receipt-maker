# 001 — Car Workshop Job-Card Vision

> **Status:** vision draft — not yet planned for execution. Filed 2026-05-11.
> **Origin:** Zulfahmi Ahmad (workflow), transcribed for repo archival.
> **Scope:** evolution of bucket D (templates) into a conversational
> job-card system for a friend's car workshop. Generic-business support is a
> stated future goal but explicitly out of scope for the first slice.

## One-line summary

Pivot the receipt toolkit from a hand-fed JSON renderer into a **conversational
job-card system**: photos in via Telegram, vision-LLM extraction, persistent
job state, automatic receipt issuance on completion.

## Personas

| Persona | Surface | Role |
|---|---|---|
| MyF (workshop operator) | Telegram bot DM | Captures the car at intake, captures each part as it goes in, captures the final car when done. |
| Customer | Receipt artifact (PDF/PNG) | Receives the completed receipt — does not interact with the bot. |

## Happy-path flow (12 steps as filed)

1. **Intake.** Customer walks in with a car to repair.
2. **Intake capture.** MyF photographs the car and sends the image to the
   Telegram bot.
3. **Plate extraction.** Backend receives the image, forwards it to a vision
   LLM, and extracts:
   - **Must:** plate number.
   - **Nice to have:** make, model, colour.
4. **Job persisted.** Backend stores the vehicle details against a new job
   keyed on plate number.
5. **Repair begins.** MyF starts the inspection and repair process.
6. **Part captured.** For each part replaced, MyF photographs the part and
   sends the bot a message with the part picture, the plate number, and the
   price.
7. **Part recognition.** Backend forwards the part image to the vision LLM and
   extracts the part name (brake disc, brake pad, lower arm, etc.).
8. **Part persisted.** Backend stores the part name + price against the job.
9. **Repeat 6–8** until all parts are logged.
10. **Completion capture.** When the car is fully repaired, MyF photographs
    the car and sends the image with caption `<plate> done`.
11. **Job closed.** Backend marks the job complete.
12. **Receipt issued.** Backend replies in the bot DM with a completed receipt
    (PDF + PNG). MyF forwards it directly to the customer.

## Architectural deltas vs current toolkit

| Concern | Today | After |
|---|---|---|
| Bot interaction | Stateless — paste JSON, get PDF/PNG back. | Stateful — multi-message conversation per car job. |
| Vision LLM | Not used. | New seam `IVisionAnalyzer` with `ExtractCarDetails(image)` and `RecognizePartName(image)`. |
| Persistence | None — every call is in-memory. | Database for jobs + parts + media references. |
| Storage | None — bytes returned inline. | Object storage for intake / part / completion photos. |
| Auth | Open bot — any chat may use it. | Allow-list of Telegram user IDs; one workshop initially. |
| Receipt source | User-supplied JSON. | Composed from DB rows at completion time. |
| Receipt layout | Single consumer-style mockup. | Workshop layout: vehicle block, line items per part, labour, tax. |

## Open questions and refinements

These need decisions before a real implementation plan is drafted.

1. **Persistence stack.** Supabase (Postgres + Storage + Auth — aligned with
   the project's stated tech stack) vs lightweight self-hosted (SQLite +
   local filesystem — zero extra infra). Recommend Supabase.
2. **Vision LLM provider.** Anthropic Claude vision (consistent with the
   wider Claude tooling), OpenAI vision, or Google Gemini. Tradeoffs: cost
   per image, plate OCR accuracy, latency, regulatory geography. Recommend
   start with Claude vision and benchmark against a held-out plate-number /
   part-name set.
3. **Caption parsing.** `<plate> done` and `<plate> <price>` share the same
   first token. Malaysian plates contain a space (e.g. `WXX 1234`). Need
   canonical normalisation (strip whitespace, uppercase) and tolerant matching
   against open jobs.
4. **Correction flow.** Vision LLM will sometimes read the plate wrong, or
   name a part wrong. Need a "fix part name" / "fix plate" command. Minimum
   viable: reply-to-message with corrected text triggers an update on the
   referenced job row.
5. **Labour cost.** Real workshop receipts include labour, not just parts.
   Source of truth: flat per-job, per-part, or manually entered at completion?
   Recommend explicit `/labour <plate> <amount>` command before `/done`.
6. **Tax.** Malaysia SST 8% on workshop services. Auto-apply on close or rely
   on a per-job toggle? Recommend auto-apply with override.
7. **Customer identity.** Receipt needs a customer name + contact. Today
   there is no capture step. Recommend `/customer <plate> <name> <phone>`
   command after intake.
8. **Generic businesses later.** Workflow is currently car-workshop-shaped
   (plate, parts). The toolkit can extend to other service businesses
   (laundry, tailoring, repairs) — but only if the domain model stays generic
   (`Job { externalRef, items[], assets[] }` with workshop-specific semantics
   in a thin adapter layer). Bake "plate" / "part" into adapter, not core.
9. **Receipt template selection.** Workshop receipts may want a different
   layout from the current consumer receipt. **Bucket D (templates) becomes a
   prerequisite, not a peer.**
10. **Bot deployment.** Today the bot runs local-dev only (see divergence #28
    on `dotnet exec` vs `dotnet run`). Production needs a hosting decision:
    Fly.io / Railway / a Raspberry Pi at the workshop / a small VPS.

## How this maps to existing feature buckets

- **Bucket A (vision LLM):** required — this is the AI half.
- **Bucket D (templates):** required — workshop layout differs from the
  consumer mockup.
- **Bucket C (SaaS / persistence):** required minimum — DB + storage + auth.
- **Bucket E (delivery channels):** WhatsApp delivery to the customer is the
  natural next channel once the workshop flow is live.
- **Bucket B (mockup-parity polish):** unrelated; ships first regardless.

## Suggested staging once approved

1. **Bucket B polish** — already planned, ships first.
2. **Bucket A foundations** — `IVisionAnalyzer` seam, Claude-vision adapter,
   evaluation harness over a held-out plate + part image set.
3. **Bucket C minimum** — Supabase schema for `jobs` + `job_parts`, storage
   bucket for media, allow-list auth.
4. **Bot conversation refactor** — extend `BotUpdateRouter` to track per-chat
   job state, handle the three message shapes (intake photo / part with
   `<plate> <price>` caption / `<plate> done` caption).
5. **Bucket D thin slice** — workshop receipt template (one new layout)
   wired into `ReceiptGenerator`.
6. **End-to-end pilot** — drive the live workshop, iterate from MyF's
   feedback.

## Decision log

| Date | Decision | Rationale |
|---|---|---|
| 2026-05-11 | Filed as vision-only; no implementation plan yet. | Refinements 1–10 must be resolved first. |

## Status

Filed for future planning. Do not start implementation until refinements 1–10
are answered and a dedicated plan file is drafted alongside this vision.
