# Workflows

High-level workflows. Each maps to a skill in `../skills/`. Always obey [Rules.md](Rules.md) and [Conventions.md](Conventions.md).

## Intent Routing
Route by user intent — natural language works identically to slash commands; commands are optional shortcuts.
- Bug / unexpected behavior → `unity-investigate`
- New functionality → `unity-feature`
- Code explanation → `unity-explain`
- Performance concern → `unity-optimize`
- Refactor request → `unity-refactor`
- Code review request → `unity-review`

## Built-in Command Compatibility
Claude Code built-in commands (e.g. `/review`, `/deep-research`) do NOT bypass the runtime. They still enforce [Conventions.md](Conventions.md), [Rules.md](Rules.md), this file, and skill selection — naming, DPDebug rules, no-comment policy, existing-architecture reuse, and token efficiency all apply.

## Bug Investigation → unity-investigate
1. Restate the symptom and expected behavior.
2. Locate the entry point by symbol/reference search (not file scan).
3. Trace the minimal call path to the fault.
4. Identify root cause with `file:line` evidence.
5. Propose the smallest fix. Apply only if requested.

## Feature Development → unity-feature
1. Clarify scope and acceptance criteria.
2. Find the existing pattern/system to extend (e.g. a sibling popup, manager).
3. Reuse base classes and helpers; do not invent new patterns.
4. Implement with minimal new files.
5. Add `DPDebug` logging per conventions where it aids debugging.

## Refactoring → unity-refactor
1. Map all dependencies (callers, references, overrides) before touching code.
2. Confirm behavior must stay identical.
3. Refactor in small, verifiable steps.
4. Keep public API stable unless change is requested.
5. Verify no caller broke via reference search.

## Code Review → unity-review
1. Review only the changed scope.
2. Check conventions: naming, DPDebug usage, color/bracket rules, no stray comments.
3. Check correctness, null safety, and dependency impact.
4. Flag new patterns that duplicate existing ones.
5. Report findings as a prioritized list with `file:line`.
