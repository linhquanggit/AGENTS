# Unity Workflows — router + skill contracts

Pick ONE skill by intent (command word or natural language). Each contract = what is specific to that skill; general gates come from Core Rules. Default tier shown; re-tier per Core §1.

| Skill | Route when | Tier |
|---|---|---|
| `bug` → investigate | bug, crash, "không hoạt động", repro | T2 |
| `brainstorm` | ambiguous design, "bàn ý tưởng", "nên làm thế nào" | T2 |
| `feature` | "thêm X", extend a system | T2 |
| `refactor` | restructure, "dọn lại", "đổi tên" | T2 |
| `migrate` | large conversion, project-wide swap | T3 |
| `review` | review diff/commit/changed code | T2 |
| `test` | "viết test", EditMode/PlayMode, TDD | T1–T2 |
| `explain` | explain system/class/flow | T0 |
| `optimize` | FPS, GC alloc, load time, build size, "tối ưu" | T2 |
| `advisory` | "tư vấn kiến trúc", design review | T3 (no code) |
| `perception` | project health, missing meta/refs, "script này dùng ở đâu" | T2 → `skills/unity-perception.md` |
| `learn` | "ghi nhớ", "lưu lại" | T1 → `skills/unity-learn.md` |

## Contracts
- **investigate** — Restate symptom vs expected (1 line each). Prove root cause with `file:line` before proposing a fix; distinguish cause from symptom. Cross-check `knowledge/anti-patterns.md` (event leaks, Unity fake-null, lifecycle order). Fix = smallest change + a cheap guard/log so it can't recur silently. Output: Symptom/Expected · Call path (`file:line` hops) · Root cause · Fix.
- **brainstorm** — No code. One batch of only design-changing questions → 2–3 options with tradeoffs (reuse `PopupBase`/Managers first) → recommendation + acceptance criteria → hand off to `feature`.
- **feature** — Find the closest sibling/base to extend first (cite `file:line`). No new libraries/patterns. Log per Conventions. Output: Changes (file + 1-line why) · **Editor wiring** list (serialized fields to assign).
- **refactor** — Dependency map (callers/overrides/serialized refs) before any edit; state the invariant (public API, `[SerializeField]` names, execution order). Small steps; re-run reference search on every changed symbol. Stop and ask if the map is wider than expected.
- **migrate** — Enumerate ALL sites first. Order leaf → shared. Keep old+new working (shim); one verified increment at a time; remove the old path only after every site is migrated. Re-plan if site count blows up.
- **review** — Changed lines only (>10 files → ask to scope). Check: Conventions (naming, `.Log`+color, `[]` content, no comments), null/lifecycle/race/event-unsubscribe, duplicated patterns, broken callers of changed public symbols. Output: `[Blocker|Major|Minor|Nit] file:line — issue → fix`.
- **test** — EditMode for pure logic, PlayMode for lifecycle/coroutine/physics. TDD = failing test for the right reason first. Test asmdef, `Method_Scenario_Expected`, setup/teardown for scene state. One behavior per test.
- **explain** — Only the requested scope, ordered hops with `file:line`, stop at the boundary.
- **optimize** — Require evidence (profiler, repro, hot loop) or ask. Check the fix isn't already present. No cold-path micro-opts. Build/bundle size → `knowledge/addressables.md`. Output: bottleneck · root cause `file:line` · change · risk · expected gain.
- **advisory** — Senior-architect advice; read relevant `knowledge/{architecture,performance,ui-patterns,anti-patterns,libraries}.md`. Respect mobile constraints, no over-engineering, no new third-party libs. Output: recommendation · perf impact · maintainability · offer a plan.

## Knowledge pointers (open only when relevant)
- Shoot/bullet work → `knowledge/snapshots/shoot-templates/00-overview.md` (reuse before writing new).
- Scroller lists → `knowledge/snapshots/enhancedscroller-architecture.md`.
- Addressables / build size → `knowledge/addressables.md`.
- Other snapshots → grep `knowledge/snapshots/INDEX.md`.
