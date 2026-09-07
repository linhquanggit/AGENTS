# Workflows — Intent Router

Route by intent to one skill. Natural language and the optional command shortcut behave identically. Always obey [Rules.md](Rules.md) and [Conventions.md](Conventions.md). Each skill holds its own procedure and output — open only the selected one.

## Routing
| Command | Route when (intent / trigger phrases) | Skill |
|---|---|---|
| `bug` | Bug, crash, unexpected behavior, repro | `investigate` |
| `brainstorm` | Ambiguous feature/design, "clarify first", explore options | `brainstorm` |
| `feature` | New functionality, extend a system | `feature` |
| `refactor` | Restructure, rename, clean up | `refactor` |
| `migrate` | Large/incremental conversion, project-wide swap | `migrate` |
| `review` | Review changed code / a commit / a diff | `review` |
| `test` | Write/run tests, TDD | `test` |
| `explain` | Explain a system, module, or execution flow | `explain` |
| `optimize` | Performance, latency, memory, allocations | `optimize` |
| `learn` | Record a discovered pattern/fact | `learn` |

## Style Profiles
- Named, reusable **UI/visual** style presets live in `../knowledge/styles/` — index at `styles/INDEX.md`. UI style only (markup structure, CSS, layout, visual language) — never JS/code conventions, which stay in `Conventions.md`.
- When the user names a profile (e.g. "dùng style cupertino-dark", "áp dụng style X cho ..."), read `knowledge/styles/<name>.md` and apply its visual rules for the task.
- **When a task involves UI/visual work (new UI, restyle, popup/screen design) and the user has NOT named a profile**: read `styles/INDEX.md` and list the available profiles (name + hook) for the user to pick from, before writing any UI code. If none fit or the user says to skip, fall back to matching the surrounding project's existing style.
- To add a new one (e.g. "học style UI này"): evaluate the design language actually shown (palette, shape language, control affordances, typography) and name it after that aesthetic — not after the source project — using recognized design-style terms where one fits (e.g. `cupertino-dark`, `neumorphic-light`, `material-dense`). Write one file under `knowledge/styles/<name>.md` (concrete visual rules with source evidence) + one line in `styles/INDEX.md`.

## Continuous Improvement
- **Before acting**: scan `knowledge/learnings/INDEX.md`; read any entry relevant to the task (avoids rediscovery).
- **After a task**: if a reusable, non-obvious fact emerged (root-cause pattern, hard-to-find location, undocumented convention, or a correction), propose `learn` before finishing — one line, then wait for approval. Skip trivial facts.

## Built-in Command Compatibility
Built-in commands (e.g. `/review`) do NOT bypass the runtime — they still enforce [Conventions.md](Conventions.md), [Rules.md](Rules.md), this router, and skill selection.
