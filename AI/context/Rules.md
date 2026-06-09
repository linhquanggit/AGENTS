# Rules

Hard constraints for every task. Token efficiency is a primary goal.

## Reading & Searching
- Read the minimum to act; stop when you can act. Budget: ≤ 5 files before a diagnosis, ≤ 10 before a fix — to exceed, name the extra files and why.
- Investigation order: symbol → references → callers → callees → open source. Open source last.
- Prefer search over opening files; then read only the relevant span (offset + limit).
- Scope searches to the smallest plausible directory. Batch independent searches/reads in one step.
- Do NOT scan the repo (`find` / recursive `grep` over `Assets/`) unless a targeted search failed.
- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta`, or generated folders.

## Runtime Disclosure
- Before using any runtime `.md` (bootstrap, context, skill), say `Using <path> — <short purpose>` in chat — short and status-like.
- Do not repeat the disclosure for the same file within one task unless it is read again.

## Scope Discipline
- Answer ONLY the exact question. Do not expand to full flow, callers, side-effects, save/event/UI, or "related" aspects unless asked.
- A narrow question gets a narrow trace — stop at the first authoritative `file:line` that answers it.
- A sub-agent's sub-task must be as narrow as the user's question.
- After answering, OFFER to go deeper in one line; do not pre-emptively investigate.

## Permission Modes
- **Approval** (high risk): MUST ask before deleting files, modifying Public APIs / Base Classes / Singletons, major refactors, or entering Play/Build.
- **Auto** (low risk): act autonomously for `DPDebug` logs, minor fixes in private/protected methods, new files within established patterns, and lint/naming fixes.
- **Bypass**: skip approvals only on explicit directive ("Do it all", "Skip approval", "Bypass modes").

## Editing
- Map dependencies (callers, references, base classes) BEFORE refactoring or renaming.
- Modify existing patterns; do not introduce new ones. Minimize files and lines touched.
- Follow [Conventions.md](Conventions.md) exactly. Do not change or edit anything outside the requested scope without asking.

## Planning & Uncertainty
- Non-trivial tasks: present a plan and wait for approval before modifying code. Skip for explicit direct-implementation requests, small isolated changes, and emergency fixes.
- Below 80% confidence: state assumptions, then ask. Never invent architecture or assume flow without `file:line` evidence.

## Output
- Be concise. Report what changed and why, with `file:line`. No speculative refactors or unrequested cleanups.
