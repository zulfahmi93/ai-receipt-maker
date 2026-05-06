# ADR 0001 — SkiaSharp as the single render engine

- **Status:** Accepted
- **Date:** 2026-05-06

## Context

The toolkit must produce PDF, PNG, and (optionally) SVG receipts from one shared layout. Two main candidates evaluated:

1. **QuestPDF** — fluent layout DSL, native PDF + raster export via internal SkiaSharp. Excellent docs. Dual-licensed (free for individuals/FOSS/orgs under $1M annual revenue; commercial license required above).
2. **SkiaSharp directly** — MIT licensed cross-platform 2D graphics library. Same `SKCanvas` abstraction can target a PDF backend (`SKDocument.CreatePdf`), an `SKBitmap` (PNG export), and an `SKSvgCanvas` (SVG). Manual layout (text wrap, height measurement) required.

## Decision

Use **SkiaSharp 3.119.2** as the only render engine. One `IReceiptRenderer.Render(SKCanvas, ReceiptData)` is invoked against three different canvas sources to produce PDF/PNG/SVG.

## Consequences

**Positive**
- MIT license — no commercial cap, no transitive license burden on bot/API/CLI/Flutter consumers.
- Three output formats from one render function = real single source of truth.
- Native SkiaSharp eliminates the QuestPDF intermediate layer.

**Negative**
- Manual text layout: line-wrap, height measurement, table column math. Isolated in `Rendering/Layout/`.
- More code to maintain than a fluent DSL.
- No built-in PDF/A/PDF/UA tagging (out of scope for MVP receipts).

## Reversibility

The renderer sits behind `IReceiptRenderer`. If SkiaSharp manual layout proves prohibitive, swap to QuestPDF without touching Contracts, CLI, API, Bot, or Flutter app.
