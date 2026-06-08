# Rules

Hard constraints for every task. Token efficiency is a primary goal.

## Reading
- Read the minimum number of files required to act. Stop when you can act.
- Budget: ≤ 5 files before a first diagnosis; ≤ 10 before proposing a fix. To exceed, name the extra files and why.
- Prefer symbol / reference search over opening files. Locate the symbol, then read only the relevant span.
- Use targeted reads (offset + limit) instead of whole-file reads on large files.
- Do NOT scan the repository. Avoid `find` / recursive `grep` over `Assets/` unless a targeted search has failed.
- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta` files, or generated folders.

## Searching
- Investigation order: symbol → references → callers → callees → open source. Open source last whenever possible.
- Scope searches to the smallest plausible directory (the feature folder, not all of `Assets/`).
- Batch independent searches/reads in one step.

## Editing
- Investigate dependencies (callers, references, base classes) BEFORE refactoring or renaming.
- Modify existing patterns; do not introduce new ones.
- Minimize files modified and lines changed.
- Follow [Conventions.md](Conventions.md) exactly (naming, DPDebug, no comments).
- Do not change behavior outside the task scope.
- Do not edit anything outside the requested scope without asking.

## Planning
- For non-trivial tasks, present an investigation/implementation plan and wait for approval before modifying code.
- Skip planning for: explicit direct-implementation requests, small isolated changes, user-requested emergency fixes.

## Uncertainty
- When confidence is below 80%: state assumptions explicitly, then ask targeted questions.
- Do not invent architecture details or assume execution flow without `file:line` evidence.

## Output
- Be concise. Report what changed and why, with `file:line` references.
- No speculative refactors, no unrequested cleanups.
