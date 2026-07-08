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

## Continuous Improvement
- **Before acting**: scan `knowledge/learnings/INDEX.md`; read any entry relevant to the task (avoids rediscovery).
- **After a task**: if a reusable, non-obvious fact emerged (root-cause pattern, hard-to-find location, undocumented convention, or a correction), propose `learn` before finishing — one line, then wait for approval. Skip trivial facts.

## Built-in Command Compatibility
Built-in commands (e.g. `/review`) do NOT bypass the runtime — they still enforce [Conventions.md](Conventions.md), [Rules.md](Rules.md), this router, and skill selection.
