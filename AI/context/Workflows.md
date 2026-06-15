# Workflows — Intent Router

Route by intent to one skill. Natural language and the optional command shortcut behave identically. Always obey [Rules.md](Rules.md) and [Conventions.md](Conventions.md). Each skill holds its own procedure — open only the selected one.

## Routing
| Command | Route when (intent / trigger phrases) | Skill | Output |
|---|---|---|---|
| `bug` | Bug, crash, unexpected behavior, "không hoạt động", repro steps | `unity-investigate` | Root cause (`file:line`) + minimal fix |
| `feature` | New functionality, "thêm <X>", extend a system | `unity-feature` | Minimal change extending existing architecture |
| `refactor` | Restructure, rename, "dọn lại", "đổi tên" | `unity-refactor` | Dependency-mapped, behavior-preserving change |
| `review` | Review changed code / a commit / a diff | `unity-review` | Prioritized findings with `file:line` |
| `explain` | Explain a system, class, or execution flow | `unity-explain` | Targeted explanation (ordered hops) |
| `optimize` | Performance, FPS drop, allocations, "tối ưu" | `unity-optimize` | Evidence-based optimization (minimal change) |
| `advisory` | "tư vấn kiến trúc", "nên thiết kế X thế nào", design review | `unity-advisory` | Prioritized Why/How recommendation (no code) |
| `perception` | "kiểm tra sức khỏe project", "quét lỗi meta", "script này dùng ở đâu", "scan broken references" | `unity-perception` | Health snapshot + dependency map |
| `learn` | "ghi nhớ", "lưu lại", record a discovered pattern/fact | `unity-learn` | Approved entry in `knowledge/learnings/` |

## Continuous Improvement
After any task, if a non-obvious, reusable fact emerged (a root-cause pattern, a hard-to-find code location, an undocumented convention, or a correction you made), propose `unity-learn` before finishing — one line, then wait for approval. Skip trivial or one-off facts.

## Built-in Command Compatibility
Claude Code built-ins (e.g. `/review`, `/deep-research`) do NOT bypass the runtime. They still enforce [Conventions.md](Conventions.md), [Rules.md](Rules.md), this router, and skill selection — naming, DPDebug rules, no-comment policy, existing-architecture reuse, and token efficiency all apply.
