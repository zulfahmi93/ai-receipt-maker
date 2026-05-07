# ADR 0004 — Embed Inter font as resource

- **Status:** Accepted
- **Date:** 2026-05-06

## Context

The receipt renderer must produce deterministic output. System font fallbacks vary across macOS, Linux containers, Windows, and CI runners. Golden-byte hashes break the moment a system font substitution happens.

## Decision

- Embed **Inter** (variable font, OFL-1.1 license) as `EmbeddedResource` inside `ReceiptToolkit.Core`.
- File: `src/ReceiptToolkit.Core/Resources/InterVariable.ttf` (879,708 bytes).
- Source: **rsms/inter v4.1 upstream release** (`https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip`, asset `InterVariable.ttf`, published 2024-11-16). Not the Google Fonts mirror.
- Renderer loads the font via `SKTypeface.FromStream` from the embedded resource and selects weight via `SKTypeface.Clone(ReadOnlySpan<SKFontVariationPositionCoordinate>)` on the variable font's `wght` axis (Regular=400, Medium=500, SemiBold=600, Bold=700). Requires SkiaSharp 4.x preview line — see PROGRESS divergence #16.
- Renderer **never** falls back to a system font; missing glyphs render `.notdef`.

## License

OFL-1.1 permits embedding in software. License text shipped at `docs/licenses/Inter-OFL.txt` (Phase 8 deliverable).

## Consequences

**Positive**
- Deterministic across all OSes. Golden tests work.
- ~880KB DLL size increase. Acceptable.
- Single TTF for all weights via variable axis.

**Negative**
- One additional file to keep in sync if Inter releases breaking changes.
- Variable-font axis selection requires SkiaSharp **4.x** (`SKFontArguments` / `SKTypeface.Clone(...)` first appear in `4.147.0-preview.1.1`). The 3.x .NET wrapper exposes no axis API on desktop targets, despite the upstream Skia native side supporting it. Pinned at `4.147.0-preview.1.1` in `Directory.Packages.props`; revisit at next SkiaSharp 4 release. PROGRESS divergence #16 documents the upgrade and the static-cuts fallback path if the preview line is abandoned.

## Why Inter

Open license, optimized for screen + small print, full metric set including tabular numbers (important for the items table).
