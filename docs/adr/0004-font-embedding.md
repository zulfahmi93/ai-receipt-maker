# ADR 0004 — Embed Inter font as resource

- **Status:** Accepted
- **Date:** 2026-05-06

## Context

The receipt renderer must produce deterministic output. System font fallbacks vary across macOS, Linux containers, Windows, and CI runners. Golden-byte hashes break the moment a system font substitution happens.

## Decision

- Embed **Inter** (variable font, OFL-1.1 license) as `EmbeddedResource` inside `ReceiptToolkit.Core`.
- File: `src/ReceiptToolkit.Core/Resources/Inter-Variable.ttf`.
- Source: Google Fonts mirror (`google/fonts` GitHub repo, `ofl/inter/Inter[opsz,wght].ttf`), upstream from `rsms/inter` v4.x.
- Renderer loads the font via `SKTypeface.FromStream` from the embedded resource and selects weight via `SKFontStyle` against the variable font's `wght` axis (Regular=400, Medium=500, SemiBold=600).
- Renderer **never** falls back to a system font; missing glyphs render `.notdef`.

## License

OFL-1.1 permits embedding in software. License text shipped at `docs/licenses/Inter-OFL.txt` (Phase 8 deliverable).

## Consequences

**Positive**
- Deterministic across all OSes. Golden tests work.
- ~876KB DLL size increase. Acceptable.
- Single TTF for all weights via variable axis.

**Negative**
- One additional file to keep in sync if Inter releases breaking changes.
- Variable font support requires SkiaSharp 2.88+ (we target 3.119.2 — fine).

## Why Inter

Open license, optimized for screen + small print, full metric set including tabular numbers (important for the items table).
