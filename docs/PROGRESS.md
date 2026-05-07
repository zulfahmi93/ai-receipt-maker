# Receipt Toolkit — Progress & Resume Guide

> **Read this first** in any new session. Tracks phase completion, decisions that diverged from the plan, and the exact commands to resume work.

## Plan reference

Full implementation plan: `/Users/zulfahmi/.claude/plans/you-are-a-distinguished-squishy-yao.md`

156 granular TDD tasks across 10 phases. Read it before resuming.

## Phase status

| # | Phase | Status | Notes |
|---|---|---|---|
| 0 | Solution scaffold (no TDD) | **DONE** | All 10 .NET projects + Flutter app build green, 0 warnings |
| 1 | Contracts + JSON parsing (T1.1–T1.7) | **DONE** | 7/7 tests, build 0/0. Phase 0 had a latent NoWarn-conditional bug that surfaced here — fixed (see divergences #9, #10) |
| 2 | Validation rules (T2.1–T2.14) | **DONE** | 16/16 tests (12 Fact + 2 Theory pairs), build 0/0. 12 rule classes under `Core/Validation/Rules/`, `ReceiptValidator` aggregates all errors. `ValidationError` record landed in Contracts; `CurrencyTable` lookup landed in `Core/Currency/`. |
| 2b | Calculation (T2b.1–T2b.10) | **DONE** | 11/11 cases (10 tasks; T2b.10 Theory expands to 2). `ReceiptCalculator` static class under `Core/Calculation/`. Decimal end-to-end, `MidpointRounding.AwayFromZero` (round-half-up — see divergence #11; supersedes plan's `ToEven`), currency decimal-places via `CurrencyTable`. `AutoCalculateTotals=false` returns input unchanged. Idempotence resolved with a "subtotal fingerprint" because the contract has a single `ReceiptTotals.DiscountTotal` that must double as receipt-level seed (input) and summed value (output) — see XML doc on `CalculateTotals`. Future contract change (separate `ReceiptLevelDiscount` field) would let the fingerprint be deleted; deferred. |
| 2c | Formatting (T2c.1–T2c.5) | **DONE** | 5/5 tests. `MoneyFormatter` + `DateTimeFormatter` static classes under `Core/Formatting/`. Rounding mode `MidpointRounding.AwayFromZero` (matches calculator — divergence #11). Symbol resolution: `options.CurrencySymbol` overrides built-in `FrozenDictionary` lookup (USD→$, EUR→€, GBP→£, JPY→¥, CNY→¥, MYR→RM, SGD→S$, IDR→Rp, KRW→₩); empty when both miss. Culture resolution extracted to shared `Core/Globalization/CultureResolver.cs` (used by both formatters; null/empty/unknown → `InvariantCulture`). DateTime parse uses `DateTimeStyles.RoundtripKind` alone — `AssumeLocal` removed because the two flags are mutually exclusive; bare local strings become `DateTimeKind.Unspecified` which is correct for display. Plan T2c.5 wording (`hh:mm a`) corrected — see divergence #12. |
| 3 | Render primitives (T3.1–T3.9) | pending | |
| 3b | Section renderers (T3b.1–T3b.23) | pending | |
| 3c | Theme/layout (T3c.1–T3c.8) | pending | |
| 3d | Exporters (T3d.1–T3d.9) | pending | |
| 3e | Generator + golden (T3e.1–T3e.9) | pending | |
| 4 | CLI (T4.1–T4.7) | pending | |
| 5 | API (T5.1–T5.12) | pending | |
| 6 | Telegram bot (T6.1–T6.9) | pending | |
| 7 | Flutter (T7.1–T7.14) | pending | |
| 8 | Docs (D8.1–D8.8) | pending | |
| 9 | E2E verification (V9.1–V9.7) | pending | |

## Decisions that diverged from the plan

These overrides matter — re-read them before re-deriving:

1. **xUnit v3** (not v2). Plan said `xunit` 2.9.3; switched to `xunit.v3` 3.2.2. Tests are `OutputType=Exe`. Runner: `xunit.runner.visualstudio` 3.1.5 supports both. Driven by user request — v3 is GA, right call for greenfield.
2. ~~Inter three static TTFs~~ Resolved 2026-05-06: **Inter Variable Font v4.1** is the accepted choice — single TTF (`InterVariable.ttf`, 879,708 bytes) sourced directly from rsms/inter upstream v4.1 release zip (not the Google Fonts mirror — earlier mirror copy was outdated). All weights via the `wght` axis selected through `SKFontStyle`. Authoritative reference: ADR 0004. Plan's three-static-TTF wording is superseded; renderer code (Phase 3) targets the variable font directly.
3. **`.sln` not `.slnx`**. `dotnet new sln` defaulted to slnx but `dotnet test` in SDK 10.0.105 can't process slnx. Recreated as classic sln. Use `receipt-toolkit.sln`.
4. ~~Flutter 3.41.7 installed~~ Resolved 2026-05-06: user upgraded to **Flutter 3.41.9 stable** (matches plan target). Verified via `flutter --version`.
5. **No `FluentAssertions`**. v8+ went commercial; v7 last MIT. Using plain xUnit `Assert` to avoid future license risk.
6. ~~No `System.CommandLine`~~ Resolved 2026-05-06: **`System.CommandLine` 2.0.7 stable** (released 2026-04-21) pinned in `Directory.Packages.props`. Phase 4 CLI uses it as planned.
7. **Bot template Worker.cs deleted** — fired CA1848/CA1727. Replaced with stub `Program.cs`. Phase 6 writes the real worker.
8. ~~Pubspec deps bumped by linter~~ Resolved 2026-05-06: **Flutter pubspec deps curated to latest** by the user — `http ^1.6.0`, `provider ^6.1.5+1`, `share_plus ^13.1.0`, `path_provider ^2.1.5`, `cupertino_icons ^1.0.9`, `flutter_lints ^6.0.0`, `file_picker ^12.0.0-beta.1` (prerelease, deliberate). Don't downgrade.
9. ~~Phase 0 NoWarn-conditional was broken~~ Resolved 2026-05-06: **`NoWarn` moved to `Directory.Build.targets`** (loads after csproj sets `OutputType`/`IsTestProject`). `Directory.Build.props` originally had a conditional `<PropertyGroup Condition="…OR '$(OutputType)' == 'Exe'">` that evaluated before the csproj body, so the suppression never applied — Phase 0 built clean only because no test project had public symbols. Targets now suppresses `CS1591;CA1707` for test/exe projects only. CA1707 added because xUnit test method names commonly use underscores (e.g. `T1_1_ParsesMinimalJson`).
10. ~~SchemaVersion sentinel hack~~ Resolved 2026-05-06: **Replaced with `[JsonConstructor]` + parameter default**. `ReceiptData(int schemaVersion = 1)` carries `[JsonConstructor]`; System.Text.Json honors the parameter default when JSON omits the field, and an explicit `"schemaVersion": 0` is preserved exactly. No `JsonNode` pre-pass, no second parse, no sentinel — STJ's canonical pattern for "default when missing".
11. **Rounding mode = `MidpointRounding.AwayFromZero`** (not `ToEven`). Decided 2026-05-07 at start of Phase 2c, supersedes plan T2b.10's original `ToEven` wording. Banker's rounding (`ToEven`) is an IEEE 754 / accounting-statistics convention that minimizes bias when many values are summed in books — it is **not** the consumer-receipt norm. Real-world POS, tax authorities, and shoppers expect round-half-up: Japan 四捨五入 (¥12.5 → ¥13), Malaysia LHDN guidance, US sales-tax practice, etc. `ToEven` would surprise users at every `.x5` midpoint (¥12.5 → ¥12, $1.245 → $1.24). The flip applies to both `ReceiptCalculator` (one line: `Math.Round(value, dp, AwayFromZero)`) and the upcoming `MoneyFormatter` so the displayed total can never disagree with the calculated total at a midpoint. T2b.10 theory data updated: `(12.45 → 1.25, 12.55 → 1.26)`. Plan file annotated; T2c.2 expectation `JPY 12.5 → ¥13` now matches the calculator.
12. **T2c.5 time-format spec corrected.** Plan originally wrote `hh:mm a` → `10:42 AM`. Two issues found at Phase 2c start (2026-05-07): (a) `.NET DateTime.ToString` does not recognize a lone `a` as the AM/PM designator — the specifier is `tt`. Lone `a` is emitted literally (probe: `"10:42 a"`). (b) The plan implied the same `ms-MY` plumbing as T2c.4, but `ms-MY`'s AM designator is `"PG"` (Pagi), not `"AM"`. Resolution: T2c.5 fixture uses `TimeFormat="hh:mm tt"` and `Locale=null` (invariant fallback) to produce `"10:42 AM"`. The formatter still routes through `options.Locale → CultureInfo.GetCultureInfo` with invariant fallback — same plumbing as T2c.4, just exercised with a different locale value.

## Hard rules (don't recompromise)

- `TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended`. Do **not** blanket-suppress CA rules to make builds pass — fix root causes. CS1591 is suppressed only in test/exe projects (legit; non-public API doesn't need XML docs).
- Money fields = `decimal` end-to-end + JSON **string** serialization (ADR 0002).
- Logo source: file path + `data:` base64 only. No HTTP fetch in renderer.
- `IClock` injection for deterministic rendering. Golden tests gated to Linux CI only.
- Render via SkiaSharp once, export to PDF + PNG + SVG via three canvas backends.

## Tech stack (locked)

| Component | Version |
|---|---|
| .NET SDK | 10.0.105 (LTS, EOL 2028-11-14) |
| Flutter | 3.41.9 stable |
| SkiaSharp | 3.119.2 |
| QRCoder | 1.8.0 |
| Telegram.Bot | 22.9.6.2 |
| xUnit | 3.2.2 (v3) |
| xunit.runner.visualstudio | 3.1.5 |
| Microsoft.NET.Test.Sdk | 17.14.1 |
| PdfPig | 0.1.10 (PDF text extraction in tests) |
| NSubstitute | 5.3.0 |
| Inter VF | OFL-1.1, embedded |

All versions pinned in `Directory.Packages.props` (Central Package Management).

## Solution layout (current state)

```
ai-receipt-maker/
├── receipt-toolkit.sln                        all 10 .NET projects
├── Directory.Build.props                      strict analyzers + doc gen
├── Directory.Packages.props                   CPM with pinned versions
├── .editorconfig, .gitignore
├── docs/
│   ├── PROGRESS.md                            (this file)
│   └── adr/
│       ├── 0001-skiasharp-as-render-engine.md
│       ├── 0002-decimal-money-string-json.md
│       ├── 0003-bot-polling-vs-webhook.md
│       └── 0004-font-embedding.md
├── mockups/receipt.png                        design source of truth
├── examples/sample_receipt_data.json          primary fixture (matches mockup)
├── src/
│   ├── ReceiptToolkit.Contracts/              EMPTY — Phase 1 fills
│   ├── ReceiptToolkit.Core/                   stub + InterVariable.ttf in Resources/ (rsms/inter v4.1)
│   ├── ReceiptToolkit.Cli/                    templated Program.cs (Hello World)
│   ├── ReceiptToolkit.Api/                    templated Program.cs (Hello World)
│   └── ReceiptToolkit.TelegramBot/            stub Program.cs (empty host)
├── tests/                                     5 xUnit v3 test projects, no tests yet
└── apps/receipt_demo_flutter/                 Flutter macOS scaffold, deps resolved
```

## Resume command sequence

From a fresh session:

```bash
cd /Users/zulfahmi/Desktop/ai-receipt-maker

# 1. Read this file + plan
cat docs/PROGRESS.md
cat /Users/zulfahmi/.claude/plans/you-are-a-distinguished-squishy-yao.md | head -100

# 2. Verify build still green
dotnet build receipt-toolkit.sln

# 3. Check git history for what's been done
git log --oneline

# 4. Continue from next pending task
# Phase 1 starts at T1.1 — see plan for task breakdown
```

## TDD model strategy reminder

Per plan's "Model & Token Strategy" section:

| Cluster | RED | GREEN | REFACTOR |
|---|---|---|---|
| Trivial (T2.x, T2c.x, T4.x, T6.1–6.3, T1.1–1.2) | Haiku | Sonnet | Sonnet (cluster batch) |
| Default (T1.3–7, T2b.x, T5.x, T7.x, T3.1–9, T3d.x, T6.4–9) | Sonnet | Sonnet | Sonnet |
| Complex (T3b.x, T3c.x, T3e.x, T3.7–8) | Sonnet | Sonnet (Opus only on block) | Sonnet |
| Docs D8.x | Haiku | — | — |

Refactor at **cluster boundary**, not per-task. Targeted test runs (`--filter`), not full suite per cycle. Edit over Write. Tight agent prompts.

## Build sanity

Last verified: 2026-05-07 (Phase 2c close).

```bash
dotnet build receipt-toolkit.sln
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)
```

If this fails, the resume target is broken — fix before adding new code.
