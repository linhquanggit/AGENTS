# Style: graphite-minimal

Near-black, hairline-bordered, small precise typography with a single subtle accent glow reserved for the primary action — the modern dev-tool/SaaS-dashboard dark aesthetic (Linear/Vercel/Raycast-adjacent). A **UI/visual style only**.

## When to Apply
Invoke by name: "dùng style graphite-minimal". Governs markup structure, CSS, and layout — not JS/code conventions (those stay in `../../context/Conventions.md`).

## Visual Rules
- Base canvas near-black (`#0B0C0E`); surfaces one step lighter (`#111318`) — no gradients, no textures.
- Separation comes from a **1px hairline border**, not shadow: `border:1px solid rgba(255,255,255,.08)`. Default `box-shadow` is none; reserve shadow/glow entirely for the one interactive element that needs emphasis.
- Ink is a soft off-white (`#EDEEF0`), never pure `#FFFFFF`; secondary/muted text is a cool grey (`#8A8F98`).
- One accent color only (e.g. `#6C5CE7`), spent almost exclusively on the primary CTA — everything else stays monochrome grey/white. This is the opposite of cyberpunk/vaporwave: restraint, not glow-everywhere.
- The primary CTA is the single place allowed a soft glow: `box-shadow:0 0 0 1px rgba(<accent>,.4), 0 0 16px rgba(<accent>,.35)` — no glow anywhere else on the page.
- Radius is small and consistent (8–10px) across cards, buttons, and inputs — never fully rounded/pill, never sharp 0px.
- Typography runs small (11–13px body) with tight, precise spacing — this style reads as "information-dense tool", not "marketing page"; avoid large display type except for a page-level h1.
- Inputs match the surface convention: dark fill, hairline border, no inner shadow.

## Source
Synthesized reference — canonical modern dark dev-tool/SaaS-dashboard conventions assembled from common real-world examples of the aesthetic (not reverse-engineered from one project). Assembled 2026-09-07 for the style-profile library.
