# Style: vaporwave

Retro 80s–90s "sunset" gradient (purple → magenta → pink), glowing cyan neon text and edges, translucent dark panels. Softer and more pastel-retro than `cyberpunk-neon` — this is nostalgic, not dystopian. A **UI/visual style only**.

## When to Apply
Invoke by name: "dùng style vaporwave". Governs markup structure, CSS, and layout — not JS/code conventions (those stay in `../../context/Conventions.md`).

## Visual Rules
- Backdrop is a vertical sunset gradient, e.g. `linear-gradient(180deg, #2B0A45 0%, #7A1E6E 55%, #FF5DA2 100%)` — deep purple at top fading to hot pink at the bottom.
- Panels/cards are translucent dark, not solid: `background:rgba(15,4,32,.6)` with a glowing cyan border `border:1px solid rgba(0,229,255,.5)` and an outer pink glow `box-shadow:0 0 24px rgba(255,45,149,.3)`.
- Two-tone accent system: cyan (`#00E5FF`) for primary actions/highlights, warm pink/magenta from the backdrop for secondary glow — cyan is the "electric" accent, pink is the "atmosphere" color, don't swap their roles.
- Headings/important text get a cyan glow: `text-shadow:0 0 8px rgba(0,229,255,.5)`.
- Buttons are outlined (transparent fill, cyan border, cyan glow `box-shadow`), matching the neon-outline convention rather than solid fills.
- Text/ink is a warm off-white/pink-white (`#FDEBFF`) with a dimmer lavender-pink for secondary text (`#D9A8E8`) — never pure white or grey, everything is tinted by the palette.
- Corners stay small-radius (6–10px) — soft enough to feel retro-friendly, not sharp like cyberpunk, not fully rounded like a modern SaaS app.
- Optional period-correct extras when there's room: a horizon grid (perspective lines fading toward a vanishing point) or a simple sun/circle motif — use sparingly, only in hero-scale contexts, not on small controls.

## Source
Synthesized reference — canonical vaporwave/retrowave UI conventions assembled from common real-world examples of the aesthetic (not reverse-engineered from one project). Assembled 2026-09-07 for the style-profile library.
