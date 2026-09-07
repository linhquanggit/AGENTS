# Style: monochrome-luxury

Black/white/muted-gold editorial aesthetic: large serif display type, thin hairline borders, sharp (unrounded) corners, generous whitespace — the fashion/luxury-brand register. A **UI/visual style only**.

## When to Apply
Invoke by name: "dùng style monochrome-luxury". Governs markup structure, CSS, and layout — not JS/code conventions (those stay in `../../context/Conventions.md`).

## Visual Rules
- Base canvas near-black (`#0B0B0C`); surfaces one step lighter (`#141414`) with a thin light-on-dark hairline border (`1px solid rgba(255,255,255,.12)`) — no shadows, no glow.
- **Zero border-radius** everywhere (cards, buttons, inputs, icon frames) — sharp corners are the single strongest signal of this style; rounding any element breaks it immediately.
- One muted-gold accent (`#C9A24B`) used sparingly — small tags, a hairline icon border, or a single CTA fill (`background:var(--accent); color:#0B0B0C` — dark text on gold, not white).
- Headings switch to a serif face — a system-safe stack is enough, no webfont required: `font-family: Georgia, 'Iowan Old Style', 'Times New Roman', serif;` — with a touch of extra `letter-spacing` (~0.02em). Body/UI text stays on the page's normal sans stack; only display headings go serif.
- Ink is warm off-white (`#F5F1EA`), not pure white — pairs with the gold without competing with it; secondary text is a warm grey (`#9B9690`).
- Icon/badge frames are outline-only (`border:1px solid var(--accent); background:transparent`) rather than filled — echoes the hairline-everywhere rule instead of using solid color blocks.
- Generous internal padding and letter-spaced uppercase micro-labels (eyebrows, tags) reinforce the editorial feel — avoid dense, tightly-packed layouts.

## Source
Synthesized reference — canonical monochrome/editorial luxury UI conventions assembled from common real-world examples of the aesthetic (not reverse-engineered from one project). Assembled 2026-09-07 for the style-profile library.
