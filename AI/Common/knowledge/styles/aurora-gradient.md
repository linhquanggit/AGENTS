# Style: aurora-gradient

Blurred, colorful mesh-gradient "aurora" blobs as a decorative backdrop, with clean solid-color surfaces (cards, modals) floating on top. A **UI/visual style only**.

## When to Apply
Invoke by name: "dùng style aurora-gradient". Governs markup structure, CSS, and layout — not JS/code conventions (those stay in `../../context/Conventions.md`).

## Visual Rules
- Backdrop is built from several overlapping `radial-gradient` blobs at low-to-mid opacity over a dark base, e.g.:
  `background: radial-gradient(circle at 15% 20%, rgba(139,92,246,.55), transparent 55%), radial-gradient(circle at 85% 15%, rgba(56,189,248,.45), transparent 50%), radial-gradient(circle at 75% 90%, rgba(244,114,182,.45), transparent 55%), #120E1F;`
  — 3 blobs of different hues (violet, cyan, pink) positioned at different corners is enough; more than 4 gets muddy.
- Unlike glassmorphism, the surface sitting on the backdrop is a **solid, opaque, clean card** (`#FFFFFF` or near-white) — not a translucent/blurred panel. The gradient art lives only in the background layer.
- Card radius is generous (14–18px) with a soft, neutral-dark drop shadow (`0 16px 30px rgba(0,0,0,.35)`) to separate it from the busy backdrop.
- Ink on the white card is a deep violet-black (e.g. `#1B1230`), not pure black — ties the text back to the accent family.
- One accent color drawn from the gradient family (violet, e.g. `#8B5CF6`) is used for buttons, links, and small tags on the card — never a color outside the blob palette.
- Labels/tags sitting directly on the gradient backdrop (not on the white card) use a translucent light color (`rgba(255,255,255,.65)`) since they have no fixed background to contrast against.
- Best used for hero/landing sections and modals where the backdrop has room to breathe; avoid it behind dense data or long text — the moving color field competes with reading.

## Source
Synthesized reference — canonical mesh-gradient/"aurora" hero conventions assembled from common real-world examples of the aesthetic (not reverse-engineered from one project). Assembled 2026-09-07 for the style-profile library.
