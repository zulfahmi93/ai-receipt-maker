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
| 3 | Render primitives (T3.1–T3.9) | **DONE** | 9/9 tests across `LogoResolver`, `FontProvider`, `TextMeasurer`, `QrPainter` (under `Core/Rendering/{Assets,Layout}/` + `Core/Rendering/QrPainter.cs`). Build 0/0. T3.5 assertion relaxed from `==` to `Contains("Inter")` because Inter VF reports `FamilyName=="Inter Variable"` via SkiaSharp 3.119.2 — intent is "embedded font loaded, not system fallback" and `Contains` proves that without coupling to font-table layout. T3.8 word-wrap divergence: see #13 below. |
| 3b | Section renderers (T3b.1–T3b.23) | **DONE (V9.3 visual review signed off 2026-05-08)** | Pre-step P3b.0 scaffold (`92fceba`) + P3b.0a FontProvider wght axis on SkiaSharp 4 preview (`df44d6d`, divergence #15 + #16) + Sub-cluster A header-area T3b.1–T3b.5 (`1bf8b87`) + Sub-cluster B content-area T3b.6–T3b.10 (`fbd313d`) + Sub-cluster C totals-area T3b.11–T3b.17 (`4854096`) + Sub-cluster D qr/footer/perforation T3b.18–T3b.23 + V9.3 visual sign-off cluster (B1 gutter / B2 footer wrap / B3 combined Date & Time — see divergences #17/#18/#19) all DONE. Build 0/0; Core 72/72 (5 new: 1 visual harness + B1 pixel-mode gutter + B2 long-line wrap + B3 combined-row + B3 separate-row guard); Contracts 7/7. Public API landed: `IReceiptSection`, `RenderContext`, `ThemeColors`, `HeaderSection`, `TitleSection`, `MetaSection`, `CustomerCashierSection`, `ItemTableSection`, `TotalsSection`, `PaymentSection`, `QrSection`, `FooterSection`, `PerforationSection`. Visual harness `tests/.../Sections/Phase3bVisualPreview.cs` retained (gated by env var `RECEIPT_VISUAL_PREVIEW=1`, output via `RECEIPT_VISUAL_PREVIEW_OUT`) for future visual passes. |
| 3c | Theme/layout (T3c.1–T3c.8) | **in progress (T3c.8 deferred to Phase 3d)** | Sub-cluster A (T3c.1–T3c.2) DONE 2026-05-08 — `SkiaReceiptRenderer` composes 10 sections in mockup order + paints `paperColor` background via Option B `DrawRect(0,0,W,H)` (no `canvas.Clear`; forward-compatible with T3c.7 RoundRect clip + T3c.8 shadow margin). `ThemeColors.DefaultPaperColor = SKColors.White` added. Sub-cluster B (T3c.3–T3c.4) DONE 2026-05-08 — characterization tests pin `theme.highlightColor` flow through `SkiaReceiptRenderer.Render` to the TOTAL bar fill (T3c.3) and confirm paper-paint layering invariant on a non-magenta paper colour, asserting both corner = paper AND TOTAL band ≠ paper (T3c.4). No production changes — TOTAL-bar highlight binding shipped in Phase 3b sub-cluster C and paper paint shipped in 3c sub-cluster A; tests pin those wirings through the renderer surface. Plan T3c.3 wording (`accentColor`) vs codebase truth (`highlightColor`) resolved as divergence #20. Sub-cluster C (T3c.5–T3c.7) DONE 2026-05-08 — T3c.5 characterization pins `layout.receiptWidth` flow through `SkiaReceiptRenderer.Measure(...).Width` + paper-paint right-edge at 480px (no production change). T3c.6 RED→GREEN→REFACTOR — composer-level divider rendering at the gap preceding any section whose `IReceiptSection.RequiresLeadingDivider` is true (interface default `false`, `TotalsSection` overrides `true`). Style mapping `solid`/`dashed`/`dotted` via `SKPathEffect.CreateDash` (`DashIntervals=[6,4]`, `DotIntervals=[2,3]` static-readonly per CA1861). Stroke colour from `theme.dividerColor` → `ThemeColors.DefaultDividerColor` (LightGray) fallback. Null/empty/whitespace/unknown styles suppress draw. `dividerY = MathF.Round(gapStartY + sectionGap/2f)` snaps to integer row to kill half-pixel drift hazard for odd `sectionGap`. T3c.7 RED→GREEN→REFACTOR — paper paint switches to `DrawRoundRect` when `layout.BorderRadius > 0`; corners outside curve stay at bitmap default (transparent black, alpha=0). Caller-bitmap-must-be-default precondition + over-large-radius Skia clamp behaviour documented in class `<remarks>`. T3c.8 (`showShadow` PNG-only) DEFERRED to Phase 3d — renderer is canvas-agnostic and cannot distinguish PDF vs PNG output; the toggle naturally lives in the exporter layer. See divergence #21. Per-task /tdd cycles (RED+GREEN+REFACTOR each) with two Code Reviewer passes — overrides cluster-boundary default at user direction. All review nits + trade-offs + coverage gaps actioned (no defer): `RequiresLeadingDivider` placement, divider doc clarity, `IReceiptSection` default-false documentation, `MathF.Round` snap, null/empty/unknown-style suppression theory, non-requiring-section spurious-divider guard, target-typed `new SKRect`, negative `BorderRadius` theory (`0`/`-1`/`-5`), boundary `BorderRadius=180=W/2` fact (asserts corners alpha=0 + at-least-one paper pixel along scan row — proves clamp didn't skip the draw without over-asserting on section composition). Build 0/0; Core 76 → **91/91** (+15: T3c.5 +1, T3c.6 +8 [3 styles + 5 negative + 1 non-requiring], T3c.7 +5 [1 base + 3 negative + 1 boundary]); Contracts 7/7. Phase 3c remaining: T3c.8 in Phase 3d alongside `PngExporter`. |
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

13. **`TextMeasurer.WrapLines` does NOT break single oversize words at glyph boundary.** Decided 2026-05-07 at Phase 3 close. The original plan asked for both (a) glyph-boundary breaking when one word exceeds `maxWidth` AND (b) a `string.Join(" ", lines) == originalText` round-trip invariant (T3.8). The two are mutually exclusive — splitting a word into multiple line-segments and rejoining with spaces re-inserts a space that wasn't in the source. Kept the round-trip invariant (test T3.8 holds); oversize words are placed alone on their own line and accept visual overflow. XML doc on `WrapLines` documents this. Phase 3b's section renderers can revisit if a real fixture demonstrates a pathological long-word case (e.g. an SKU/URL longer than column width); fix at the renderer layer (truncate with ellipsis, or different break rule for known long-word columns) rather than mutating WrapLines into a stateful "split mode" toggle.

14. **T3.5 FamilyName assertion relaxed from `==` to `Contains` (case-insensitive).** Decided 2026-05-07 at Phase 3 close. SkiaSharp 3.119.2 reports the Inter variable font's `FamilyName` as `"Inter Variable"` (matches the VF's typographic family record), not the bare `"Inter"` the original RED spec assumed. Static Inter cuts report `"Inter"`. The test's intent is "embedded font loaded, not a system fallback" — `Contains("Inter")` proves that for both VF and any future static-cut fallback without re-flapping if SkiaSharp's name-table mapping ever changes. Inter VF (rsms/inter v4.1, divergence #2 / ADR 0004) remains the embedded asset; no font swap.

15. ~~FontProvider weight axis selection deferred to Phase 3b.~~ Resolved 2026-05-08 at Phase 3b open: implemented via `SKTypeface.Clone(ReadOnlySpan<SKFontVariationPositionCoordinate>)` on the Inter VF's `wght` axis. Two new tests landed alongside (`FontProvider_Bold_SelectsWeightAxis` → 700, `FontProvider_SemiBold_SelectsWeightAxis` → 600); both also keep the `Contains("Inter")` embedded-font assertion from T3.5. The originally-suggested `SKFontManager.MatchTypeface` API does not exist on SkiaSharp 3.119.2's net8.0 wrapper (only the android target's xml docs mention it; the actual desktop dll lacks the symbol), and the same wrapper exposes no `SKFontArguments` / VF axis types at all. See divergence #16 for the SkiaSharp version bump that unlocked this.

16. **SkiaSharp upgraded to `4.147.0-preview.1.1`.** Decided 2026-05-08 at Phase 3b open. The `3.119.2` net8.0 .NET wrapper does not expose the variable-font axis API surface (`SKFontArguments`, `SKFontVariationAxis`, `SKFontVariationPositionCoordinate`, `SKTypeface.Clone(...)`) needed to satisfy divergence #15's "all weights via the wght axis" contract — the methods only land in 4.0+ (SkiaSharp 4 / Skia M147 binding). Per [SkiaSharp 4 preview announcement](https://devblogs.microsoft.com/dotnet/welcome-to-skia-sharp-40-preview1/) and the [v4.147.0-preview.1.1 release](https://github.com/mono/SkiaSharp/releases/tag/v4.147.0-preview.1.1), this preview ships full OpenType variable-font axis control. All four pins (`SkiaSharp` + `SkiaSharp.NativeAssets.{macOS,Linux,Win32}`) bumped together in `Directory.Packages.props`. Phase 3 primitive tests (LogoResolver, FontProvider Normal-weight, TextMeasurer, QrPainter) re-verified green on v4 with no source changes — only `FontProvider` GREEN code uses the new axis API. **Risk:** preview bits move; revisit at next SkiaSharp 4 release. **Fallback path** (documented but not taken): switch the embedded asset from VF to static cuts (Inter-Regular/SemiBold/Bold) and resolve weight via per-resource lookup — a partial reversal of divergence #2 / ADR 0004 only if the SkiaSharp 4 preview line is abandoned upstream.

17. **CustomerCashierSection — fixed `ColumnGutter = 16f` between left and right columns.** Decided 2026-05-08 at Phase 3b V9.3 sign-off (UI/UX Expert blocker B1). Original implementation split the section width 50/50 with `LeftColumnFraction = 0.5f`; with a long left-column value (e.g. `customerName="Walk-in Customer"`) the value's right-edge abutted the right column's label left-edge at the midpoint, so PdfPig text extraction read the row as `"Walk-in CustomerCashier"` and the rendered PNG showed the two columns visually touching. Fix: replaced the fraction with a fixed 16px gutter; both columns now share `(width - ColumnGutter) / 2`. Regression guard is **pixel-mode** rather than PDF text — PdfPig's `Page.Text` strips horizontal whitespace within a row and concatenates adjacent draws regardless of the on-canvas gap, so a substring assertion would never fail. The new `CustomerCashierSection_LeavesGutter_BetweenColumns` renders the section onto an `SKBitmap` and asserts a 5-column-wide vertical strip centred on midX is paper-coloured.

18. **FooterSection — body and contact lines now wrap via `TextMeasurer.WrapLines`.** Decided 2026-05-08 at Phase 3b V9.3 sign-off (UI/UX Expert blocker B2). Original implementation drew each body string as a single un-wrapped row, so the sample fixture's legal note (`"This receipt is computer generated and does not require a signature."`) overflowed past the canvas right edge as `"…does not require a signa"`. Refactor introduced a `private readonly record struct LineSpec(string Text, SKFontStyleWeight Weight, float FontSize)` and unified body + contact rendering through `MaterializeBodyEntries` / `MaterializeContactEntries`. `Measure` sums `WrapLines(...).Count` across all entries; `Draw` paints each wrapped line at its own typeface + size. New regression test `FooterSection_WrapsLongBodyLines_AtAvailableWidth` asserts `Measure(long) > Measure(short)` at narrow width and uses **per-word presence** rather than full-string substring — PdfPig drops whitespace at line-wrap boundaries (concatenates `"note"` and `"used"` across rows into `"noteused"`), so a verbatim contains check on the source line is unreliable; per-word is robust to that quirk. The existing T3b.20 verbatim assertion at default width=360 still passes because the sample legal note happens to fit on one line at 360px width — under the new wrap path, only narrower widths trigger the wrap.

20. **T3c.3 plan wording — TOTAL bar binds `theme.highlightColor`, not `theme.accentColor`.** Decided 2026-05-08 at Phase 3c sub-cluster B open. Plan line 532 reads `Changing theme.accentColor to red changes TOTAL bar pixel sample.` Codebase truth (shipped in Phase 3b sub-cluster C, T3b.11–T3b.17): `TotalsSection.Draw` binds the TOTAL bar fill to `data.Theme?.HighlightColor` → `ThemeColors.DefaultHighlightColor` (LightGray fallback). `QrSection.Draw` binds QR modules to `data.Theme?.AccentColor` → `ThemeColors.DefaultAccentColor` (DarkSlateGray). Sample fixture has two distinct keys: `accentColor=#3F6F63` (QR), `highlightColor=#E8F0EC` (TOTAL band). Re-binding the TOTAL bar to `accentColor` would collide with QrSection's accent binding and visually break the mockup (TOTAL band is a light tint; QR modules are dark). Resolution: T3c.3 test mutates `HighlightColor` and leaves `AccentColor` untouched so QrSection is unaffected. Plan wording is stale; codebase wins.

19. **MetaSection — combined "Date & Time" row when both formats produce output.** Decided 2026-05-08 at Phase 3b V9.3 sign-off (UI/UX Expert blocker B3). Mockup pairs the two values into a single row labelled `"DATE & TIME"`; original implementation emitted two separate `Date` / `Time` rows. New behaviour: when both `options.DateFormat` and `options.TimeFormat` produce non-empty formatted strings, emit one row labelled `"Date & Time"` with value `"{date} · {time}"` (separator U+00B7 MIDDLE DOT). When only one format produces output, fall through to the prior single-`Date` or single-`Time` row — verified by a regression test (`MetaSection_EmitsSeparateDateRow_WhenOnlyDateFormatPresent`). Combined-row test asserts both label substring and exact joined value substring in PdfPig text plus a height-equality check against `dateFormat`-only fixture (proves the combined row is exactly one row, not two).

21. **T3c.8 (`layout.showShadow`) deferred to Phase 3d.** Decided 2026-05-08 at Phase 3c sub-cluster C open. Plan line 537 reads "Changing `layout.showShadow=true` draws shadow on PNG only (PDF unaffected)." `SkiaReceiptRenderer.Render` operates against an `SKCanvas` and is canvas-agnostic by design (one render function drives PDF + PNG + SVG via three different `SKCanvas` sources — see ADR 0001). Shadow-on-PNG-only requires the renderer to know its output target, which violates that contract. Two paths considered: (a) defer until Phase 3d when `PngExporter` / `PdfExporter` exist and the export target can carry an `EmitShadow` flag through `RenderContext`; (b) implement output-agnostic shadow now, accepting that PDF would also draw a shadow until Phase 3d suppresses it. Chose (a) — keeps the renderer pure, lets the natural Phase 3d split own export-target-aware behaviour. Phase 3c sub-cluster A's scaffold note already flagged the "shadow margin" forward-compat trade-off; this divergence formalises the deferral. T3c.8 will land alongside T3d.5 (`PngExporter.Export` 89 50 4E 47 prefix) — `PngExporter` will set `RenderContext.EmitShadow = true` (or pass an explicit flag through `Render`) and the composer will draw the shadow rect before the paper paint. PDF / SVG exporters omit the flag.

## Phase 3b V9.3 follow-up (decisions to revisit in Phase 3c)

- **MetaSection narrow-width wrap.** B3 introduces a longer combined `"date · time"` value. At the 360px section width the combined value fits, but no wrap path exists in MetaSection — if a future fixture or smaller render width pushes the value past the value column (≈ 65% of width minus right padding), it will overflow the same way Footer did pre-B2. Phase 3c (`data.Layout` numerics + per-section-width injection) is the natural place to either widen the value column or route the combined value through `TextMeasurer.WrapLines`, mirroring the Footer fix. Watch list, not blocking 3b close.

## Phase 3c polish backlog (deferred from V9.3 sign-off)

These visual deltas vs. `mockups/receipt.png` are owned by Phase 3c when layout numerics move into `data.Layout`:

- Tighter inter-section vertical rhythm; replace fixed `sectionGap` with a layout-driven token.
- Header logo placement and tagline justification (mockup right-aligns tagline below the wordmark).
- Title "RECEIPT" tracking + horizontal rule lines above and below.
- Meta label casing/weight (mockup uses uppercase muted labels — `"RECEIPT NO."`, `"DATE & TIME"`).
- ItemTable column header casing/weight (`"ITEM / QTY / PRICE / TOTAL"` uppercase muted).
- Totals divider rule above `Subtotal` and TOTAL band tint/contrast tuning.
- Payment section: icon column + 2×2 labelled grid (PAYMENT METHOD / AMOUNT PAID / CARD ENDING / AUTH CODE), replacing the current single-column stack.
- Footer line height + glyph alignment tightening; smaller `BodyFontSize` for legal note.
- QR block size — mockup's QR is more compact.
- Body font weight / colour intensity — mockup body reads slightly lighter (muted neutral) than the near-black render.

## Phase 3b carry-over (open follow-ups, do not block sub-cluster B/C/D)

- **CA1859 reviewer-trap pattern** (caught at sub-cluster A REFACTOR close). Code Reviewer agent emitted `private static IReadOnlyList<...> Materialize(...)` which compiled and tests passed under `--no-build`, but `dotnet build` fired `CA1859 — change return type to List<...> for improved performance` (analyzer-as-error under `TreatWarningsAsErrors=true + AnalysisLevel=latest-recommended`). Fix is one-line. Lesson: orchestrator-side `dotnet build` is the only authoritative gate after every agent batch (REFACTOR especially); subagent "tests pass" reports are necessary but not sufficient. Apply to all remaining sub-clusters.
- ~~**T3b.3 SKImage double-dispose nit**~~ Resolved 2026-05-08 between sub-clusters A and B. Outer `using var imageEnabled = …` / `using var imageDisabled = …` blocks dropped from `HeaderSectionTests.cs`; `RenderContext` is now the sole owner of each `SKImage` handle. Test still 48/48; comment updated to reflect single-owner contract.
- ~~**MetaSection time-row prints `10:42 a` with the sample fixture.**~~ Resolved 2026-05-08 between sub-clusters A and B. `examples/sample_receipt_data.json` `options.timeFormat` updated `"hh:mm a"` → `"hh:mm tt"` to match divergence #12's correction (already in test fixtures + plan). MetaSection now renders `10:42 AM` from the sample. Round-trip JSON tests (T1.2, T1.7) compare model values verbatim and are insensitive to the value change. No section-test assertion touches the time-row string, so re-rendering remains green.

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
| SkiaSharp | 4.147.0-preview.1.1 (preview line — required for VF axis API; see divergence #16) |
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

Last verified: 2026-05-08 (Phase 3c sub-cluster C close — divider rendering + RoundRect clip; Build 0/0; Core 91/91; Contracts 7/7. T3c.8 deferred to Phase 3d per divergence #21).

```bash
dotnet build receipt-toolkit.sln
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)
```

If this fails, the resume target is broken — fix before adding new code.
