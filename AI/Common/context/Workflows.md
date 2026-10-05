# Common Workflows — router + skill contracts

Pick ONE skill by intent. General gates come from Core Rules; default tier shown, re-tier per Core §1.

| Skill | Route when | Tier |
|---|---|---|
| `bug` → investigate | bug, crash, unexpected behavior | T2 |
| `brainstorm` | ambiguous design, explore options | T2 |
| `feature` | new functionality | T2 |
| `refactor` | restructure, rename, clean up | T2 |
| `migrate` | large conversion, project-wide swap | T3 |
| `review` | review diff/commit | T2 |
| `test` | write/run tests, TDD | T1–T2 |
| `explain` | explain module/flow | T0 |
| `optimize` | latency, memory, allocations | T2 |
| `learn` | "ghi nhớ", record a fact | T1 → `skills/learn.md` |

## Contracts
- **investigate** — Symptom vs expected (1 line each). Root cause proven with `file:line` before any fix. Smallest fix + cheap guard. Output: Symptom/Expected · Call path · Root cause · Fix.
- **brainstorm** — No code. One batch of design-changing questions → 2–3 options with tradeoffs → recommendation + acceptance criteria → hand off to `feature`.
- **feature** — Find the closest existing module/pattern to extend (cite it). No new deps/patterns. Output: changes, 1-line why each.
- **refactor** — Dependency map before edits; state the invariant; small steps; re-check references of changed symbols.
- **migrate** — Enumerate all sites first; leaf → shared; shim old+new; one verified increment at a time; remove old path last.
- **review** — Changed lines only (>10 files → ask to scope). Output: `[Blocker|Major|Minor|Nit] file:line — issue → fix`.
- **test** — Failing test for the right reason first (TDD); one behavior per test; follow the project's test layout.
- **explain** — Requested scope only, ordered hops with `file:line`.
- **optimize** — Require evidence or ask; no cold-path micro-opts. Output: bottleneck · root cause · change · risk · expected gain.

## UI style profiles (`knowledge/styles/`)
- User names a style → read `styles/<name>.md` and apply (visual rules only; code conventions stay in Conventions.md).
- UI/visual task with no style named → list options from `styles/INDEX.md` (name + hook) before writing UI code; skip → match the project's existing look.
- "học style UI này" → name it after the aesthetic (e.g. `cupertino-dark`), not the source project; write `styles/<name>.md` with concrete rules + evidence, add one line to INDEX.
