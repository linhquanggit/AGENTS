# Common Workflows — routing + contracts

Pick one by intent; default tier in brackets (re-tier per Core §1). General gates come from Core Rules.

- **bug** [T2] — symptom vs expected (1 line each); root cause proven with `file:line` before any fix; smallest fix + cheap guard. Output: Symptom/Expected · Call path · Root cause · Fix.
- **brainstorm** [T2] — no code; one batch of design-changing questions → 2–3 options → recommendation + acceptance criteria → hand off to feature.
- **feature** [T2] — extend the closest existing module/pattern (cite it); no new deps/patterns. Output: changes, 1-line why each.
- **refactor** [T2] — dependency map + invariant first; small steps; re-check references of changed symbols.
- **migrate** [T3] — enumerate all sites; leaf → shared; old+new side by side; remove old path last.
- **review** [T2] — changed lines only (>10 files → ask to scope). Output: `[Blocker|Major|Minor|Nit] file:line — issue → fix`.
- **test** [T1–T2] — failing test for the right reason first; one behavior per test; follow the project's test layout.
- **explain** [T0] — requested scope only, ordered `file:line` hops.
- **optimize** [T2] — evidence or ask; no cold-path micro-opts. Output: bottleneck · root cause · change · risk · expected gain.
- **learn** [T1] — "ghi nhớ", record a fact → `skills/learn.md`.

## UI style profiles (`knowledge/styles/`)
- Style named → read `styles/<name>.md` and apply (visual rules only).
- UI task, no style named → list options from `styles/INDEX.md` before writing UI code; skipped → match the project's existing look.
- "học style UI này" → name it after the aesthetic (e.g. `cupertino-dark`); write `styles/<name>.md` with concrete rules + evidence; add one line to INDEX.
