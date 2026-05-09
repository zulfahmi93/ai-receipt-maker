# Receipt Toolkit — Session-load context

Stable, cross-cluster discipline. Auto-loaded each session — do **not** duplicate
into the user's resume prompt.

## Resume protocol

1. Read `docs/PROGRESS.md` — current phase row + carry-over section.
2. Read the **relevant section only** of the plan (`/Users/zulfahmi/.Codex/plans/you-are-a-distinguished-squishy-yao.md`).
   Do NOT read the full 725-line plan unless explicitly asked.
3. Read sub-cluster anchor files for naming/style alignment with the in-progress
   work (file list comes from PROGRESS.md or the user's prompt).
4. Run the baseline gate: `dotnet build receipt-toolkit.sln` + last-known
   green test counts. If any fail, STOP and surface — fix regressions before
   starting new code.
5. Confirm scope with the user before spawning agents on a new sub-cluster.

## Project pointers

- Plan: `/Users/zulfahmi/.Codex/plans/you-are-a-distinguished-squishy-yao.md` —
  156 TDD tasks across 10 phases.
- Progress: `docs/PROGRESS.md` — phase status, divergences, build sanity.
- Mockup: `mockups/receipt.png` — design source of truth.
- Sample fixture: `examples/sample_receipt_data.json` — copied to test bin via
  csproj `Content Include`. Reuse via `SectionTestBase.LoadSampleData()`. Never
  fabricate per-test JSON.
- ADRs: `docs/adr/0001-skiasharp-as-render-engine.md`,
  `0002-decimal-money-string-json.md`, `0003-bot-polling-vs-webhook.md`,
  `0004-font-embedding.md`.

## Tech stack (locked — see `Directory.Packages.props` for pins)

.NET SDK 10.0.105 · Flutter 3.41.9 stable · SkiaSharp 4.147.0-preview.1.1
(preview line, required for VF axis API per divergence #16) · QRCoder 1.8.0 ·
Telegram.Bot 22.9.6.2 · xUnit v3 3.2.2 · xunit.runner.visualstudio 3.1.5 ·
Microsoft.NET.Test.Sdk 17.14.1 · PdfPig 0.1.10 · NSubstitute 5.3.0 · Inter VF
(rsms/inter v4.1, OFL-1.1, embedded). Verify live versions before quoting
PROGRESS.md divergences (user upgrades mid-project).

## Hard rules — never recompromise

- `TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended`. **No**
  blanket-suppress CA rules to make builds pass — fix root causes. CS1591
  suppressed in test/exe projects only (legit).
- Money: `decimal` end-to-end + JSON **string** serialization (ADR 0002).
- Rounding: `MidpointRounding.AwayFromZero` (consumer round-half-up; supersedes
  plan's original `ToEven`, divergence #11). Applies to both `ReceiptCalculator`
  and `MoneyFormatter`.
- Logo source: file path or `data:` base64 only. **No** HTTP fetch in renderer.
- `IClock` injection for deterministic rendering. Golden tests gated to Linux CI.
- Render via SkiaSharp once → PDF + PNG + SVG via three canvas backends.
- Fonts: `FontProvider.GetTypeface` only. **No** `SKTypeface.Default`, **no**
  system fallback. **No** `SKFont.MeasureText` in section code — use
  `TextMeasurer.Measure` / `TextMeasurer.WrapLines`.
- Theme colours via `ThemeColors.ResolveOrDefault` only. **No** per-section
  `ResolveColor` helpers, **no** raw `SKColor.Parse` calls outside theme code.
- No silent compromises: never defer plan items, never suppress analyzers, never
  rewrite tests to dodge bugs, never widen private helper return types from
  `List<T>` to `IReadOnlyList<T>` (CA1859), never add private members "for later
  use" without a referencing call site (IDE0051). User catches both.

## Orchestrator discipline

- After every Code Reviewer pass run `dotnet build receipt-toolkit.sln` —
  **not** `dotnet test --no-build`. The `--no-build` path runs against the
  previous artifact and masks analyzer-as-error regressions. Code Reviewer
  agent has **no Bash tool** by definition — it cannot run gates itself.
  Orchestrator-side `dotnet build` is the **only** authoritative gate after
  REFACTOR.
- Verify subagent claims independently after every batch: re-run the targeted
  test filter + inspect `git status` / `git diff --stat` before treating any
  agent's "all green" report as authoritative.
- Watch carefully for drift in `src/ReceiptToolkit.Core/Resources/` (font
  assets), `Directory.Packages.props` (versions), `Directory.Build.*`
  (analyzer config), and ADR files — none expected; investigate any.

## Analyzer trap family (TreatWarningsAsErrors hits)

- **CA1859** — private/internal helpers returning a list MUST be `List<T>`,
  not `IReadOnlyList<T>`. Sub-cluster A tripped this.
- **IDE0051** — every `private const` / `private` member MUST have a
  referencing call site. Sub-cluster C tripped this on a stray `RowGap`.
- **IDE0005** — no redundant using directives. Test csproj has
  `<Using Include="Xunit" />` as a global — **never** add `using Xunit;` to
  test files. Sub-cluster C tripped this.

A transient IDE0005 on `using ReceiptToolkit.Core.Rendering.Sections;` during
RED is acceptable — auto-clears when GREEN lands the type.

## TDD model strategy (per plan §"Model & Token Strategy")

| Cluster tier | RED | GREEN | REFACTOR |
|---|---|---|---|
| Trivial (T2.x, T2c.x, T4.x, T6.1–6.3, T1.1–1.2) | Haiku | Sonnet | Sonnet (cluster batch) |
| Default (T1.3–7, T2b.x, T5.x, T7.x, T3.1–9, T3d.x, T6.4–9) | Sonnet | Sonnet | Sonnet |
| Complex (T3b.x, T3c.x, T3e.x, T3.7–8) | Sonnet | Sonnet (Opus only on block) | Sonnet |
| Docs D8.x | Haiku | — | — |

- Refactor at **cluster boundary**, not per-task.
- Targeted test runs (`--filter`), not full suite per cycle.
- One Test Engineer call drafts ALL tests in a cluster (batch RED).
- One Code Reviewer call reviews the whole cluster (batch REFACTOR).
- `Edit` over `Write` for existing files. Tight agent prompts with file paths
  + line ranges — never "read the whole solution".

## Receipt section conventions (Phase 3b)

- Sections READ from `ReceiptData`. Zero hardcoded user-visible strings, zero
  hardcoded theme colours. Layout numerics may be local consts in 3b; 3c will
  pull them from `data.Layout`.
- Omitted-section contract: when a toggle is off (e.g. `ShowQrCode=false`),
  `Measure` returns `0f` and `Draw` performs no canvas operations. Same for
  sub-blocks (e.g. footer contact when `ShowFooterContact=false` contributes
  0 to overall footer height).
- File-scoped namespace `ReceiptToolkit.Core.Rendering.Sections`.
  `public sealed class XxxSection : IReceiptSection`.
- XML doc on class + Measure + Draw matches `TotalsSection.cs` /
  `PaymentSection.cs` style.
- Sample fixture nullability (mutate via record `with`):
  `data.Footer`, `data.Qr`, `data.Layout`, `data.Options`, `data.Theme` are
  ALL nullable. `data.Business` is non-nullable (init = `new()`). Pattern for
  nullable parents: `(data.X ?? new XInfo()) with { ... }`.

## Skill / slash-command notes

- `/tdd` — strict red-green-refactor cycle. Routes to Test Engineer + stack
  Expert + Code Reviewer. Use for new TDD cycles within a cluster.
- `/caveman` — token-saving caveman mode. Active when SessionStart hook fires.
- Visual review is its own budget — UI/UX Expert sign-off comparing rendered
  output to `mockups/receipt.png`. Don't bundle into a code TDD cycle.
