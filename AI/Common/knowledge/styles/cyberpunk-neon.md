# Style: cyberpunk-neon

Near-black UI with glowing neon magenta/cyan edges and text, faint scanline texture. A **UI/visual style only**.

## When to Apply
Invoke by name: "dùng style cyberpunk-neon". Governs markup structure, CSS, and layout — not JS/code conventions (those stay in `../../context/Conventions.md`).

## Visual Rules
- Base canvas near-black (`#05050A`); panels one step lighter (`#0A0A14`) with a 1px neon border and an outer glow `box-shadow` in the same hue (e.g. `border:1px solid #FF2E9A; box-shadow:0 0 18px rgba(255,46,154,.4)`).
- Faint CRT scanline texture on panels: `background-image:repeating-linear-gradient(0deg, rgba(255,255,255,.035) 0px, rgba(255,255,255,.035) 1px, transparent 1px, transparent 3px)` layered under the base color — keep opacity very low so it reads as texture, not noise.
- Two-color neon system: magenta/pink (`#FF2E9A`) and cyan (`#00F0FF`) — use one per element to mark state (e.g. inactive vs. active toggle), never both on the same control.
- Glowing text: `color` in the neon hue + `text-shadow:0 0 6px <same color at ~70% alpha>`; labels/buttons are uppercase with `letter-spacing:.06em`.
- Buttons are outlined, not filled: transparent background, 1px neon border, neon text with glow, soft outer `box-shadow` glow — never a solid fill.
- Inputs: dark panel background, neon border in the secondary hue (magenta), neon text color, sharp or barely-rounded corners (`border-radius:2px`).
- Corners stay sharp to barely-rounded (0–2px) everywhere — roundness reads as friendly, which fights the aesthetic.
- Copy/labels short, technical, uppercase where used as a tag or button label.

## Source
Synthesized reference — canonical cyberpunk/neon-UI conventions assembled from common real-world examples of the aesthetic (not reverse-engineered from one project). Assembled 2026-09-07 for the style-profile library.
