# Receipt Toolkit — Progress & Resume Guide

> **Read this first** in any new session. Tracks phase completion, decisions that diverged from the plan, and the exact commands to resume work.

## Plan reference

Full implementation plan: `/Users/zulfahmi/.claude/plans/you-are-a-distinguished-squishy-yao.md`

156 granular TDD tasks across 10 phases. Read it before resuming.

## Phase status

| # | Phase | Status | Notes |
|---|---|---|---|
| 0 | Solution scaffold (no TDD) | **DONE** | All 10 .NET projects + Flutter app build green, 0 warnings |
| 1 | Contracts + JSON parsing (T1.1–T1.7) | pending | Start here |
| 2 | Validation rules (T2.1–T2.14) | pending | |
| 2b | Calculation (T2b.1–T2b.10) | pending | |
| 2c | Formatting (T2c.1–T2c.5) | pending | |
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
2. **Inter Variable Font** (single TTF, all weights via wght axis). Plan said three static TTFs (Regular/Medium/SemiBold). v4.1 zip (33MB) timed out twice, so switched to Google Fonts mirror's variable font (876KB). ADR 0004 documents this. SkiaSharp picks weight via `SKFontStyle`.
3. **`.sln` not `.slnx`**. `dotnet new sln` defaulted to slnx but `dotnet test` in SDK 10.0.105 can't process slnx. Recreated as classic sln. Use `receipt-toolkit.sln`.
4. **Flutter 3.41.7 installed** (plan target 3.41.9). Same minor, both stable. Acceptable — `flutter upgrade` to bump if needed.
5. **No `FluentAssertions`**. v8+ went commercial; v7 last MIT. Using plain xUnit `Assert` to avoid future license risk.
6. **No `System.CommandLine`**. Still RC, plan policy forbids preview. CLI will use manual arg parsing.
7. **Bot template Worker.cs deleted** — fired CA1848/CA1727. Replaced with stub `Program.cs`. Phase 6 writes the real worker.
8. **Pubspec deps bumped by linter** (file_picker beta, share_plus 13.x, etc.). Linter-applied, intentional, leave alone.

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
| Flutter | 3.41.7 stable (target 3.41.9) |
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
│   ├── ReceiptToolkit.Core/                   stub + Inter-Variable.ttf in Resources/
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

Last verified: 2026-05-06.

```bash
dotnet build receipt-toolkit.sln
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)
```

If this fails, the resume target is broken — fix before adding new code.
