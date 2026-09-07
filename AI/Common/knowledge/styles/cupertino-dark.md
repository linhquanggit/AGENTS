# Style: cupertino-dark

Dark-mode, iOS/Cupertino-style card list UI — rounded cards of rows, pill toggle switches, segmented controls, soft accent glows. Evaluated from the popup UI of `/Users/mobione/VsCode/Extensions/Edge/MediaZoom` (popup.css, popup.html); not tied to that project's code.

## When to Apply
Invoke by name: "dùng style cupertino-dark" / "áp dụng style cupertino-dark cho ...". This is a **UI/visual style only** — it governs markup structure, CSS, and layout, not JS logic or code conventions (those still follow `../../context/Conventions.md`).

## Visual Rules
- Theme entirely via CSS custom properties on `:root`: `--bg`, `--card`, `--row`/`--row-hover`, `--text-primary/secondary/dim`, `--divider`, accent colors, one shared `--transition`. (popup.css:1-16)
- Palette: bg `#0d0d0f`, card `#171719`, row `#1f1f22` / hover `#26262a`, divider `rgba(255,255,255,.06)`, accents green `#34c759` / red `#ff3b30` / blue `#0a84ff`.
- Layout unit is a card (`border-radius:14px`) containing rows (`border-radius:11px`); each row = leading icon + text block + trailing control, spaced with flex `gap`, not margins.
- Controls mimic native iOS: pill toggle switch (`.switch`/`.track`/`.knob`, checked state slides the knob + adds a glow shadow), segmented buttons for presets, a stepper with ± buttons flanking a numeric input.
- Typography: small sizes (10–14.5px), 600–650 font-weight, slight negative `letter-spacing` on titles, system font stack (`-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, ...`).
- Every interactive transition uses one shared `--transition: 200ms cubic-bezier(0.2,0.8,0.2,1)` variable — no ad hoc durations.
- Micro-interactions: `:hover` lightens background one step, `:active` scales down (`transform: scale(0.92)`), invalid state = red border + soft red glow shadow (`box-shadow` in the accent color at low opacity).
- Copy is short and direct — one sentence per row explaining the feature; language matches the project (source used Vietnamese).

## Source
`/Users/mobione/VsCode/Extensions/Edge/MediaZoom` (popup.css, popup.html) — read 2026-09-07.
