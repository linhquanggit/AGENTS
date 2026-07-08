# Workflows — Intent Router

Route by intent to one skill. Natural language and the optional command shortcut behave identically. Always obey [Rules.md](Rules.md) and [Conventions.md](Conventions.md). Each skill holds its own procedure and output — open only the selected one.

## Routing
| Command | Route when (intent / trigger phrases) | Skill |
|---|---|---|
| `bug` | Bug, crash, unexpected behavior, "không hoạt động", repro | `unity-investigate` |
| `brainstorm` | Ambiguous feature/design, "bàn ý tưởng", "nên làm thế nào", clarify first | `unity-brainstorm` |
| `feature` | New functionality, "thêm <X>", extend a system | `unity-feature` |
| `refactor` | Restructure, rename, "dọn lại", "đổi tên" | `unity-refactor` |
| `migrate` | Large/incremental conversion, "chuyển đổi", "migrate", project-wide swap | `unity-migrate` |
| `review` | Review changed code / a commit / a diff | `unity-review` |
| `test` | Write/run tests, "viết test", EditMode/PlayMode, TDD | `unity-test` |
| `explain` | Explain a system, class, or execution flow | `unity-explain` |
| `optimize` | Performance, FPS drop, allocations, "tối ưu" | `unity-optimize` |
| `advisory` | "tư vấn kiến trúc", "nên thiết kế X thế nào", design review | `unity-advisory` |
| `perception` | "kiểm tra sức khỏe project", "quét lỗi meta", "script này dùng ở đâu" | `unity-perception` |
| `learn` | "ghi nhớ", "lưu lại", record a discovered pattern/fact | `unity-learn` |

## Continuous Improvement
- **Before acting**: scan `knowledge/learnings/INDEX.md`; read any entry relevant to the task (it holds facts already discovered — avoids rediscovery).
- **After a task**: if a reusable, non-obvious fact emerged (root-cause pattern, hard-to-find location, undocumented convention, or a correction), propose `unity-learn` before finishing — one line, then wait for approval. Skip trivial facts.

## Built-in Command Compatibility
Claude Code built-ins (e.g. `/review`) do NOT bypass the runtime — they still enforce [Conventions.md](Conventions.md), [Rules.md](Rules.md), this router, and skill selection.
