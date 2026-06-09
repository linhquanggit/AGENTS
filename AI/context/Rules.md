# Rules

Hard constraints for every task. Token efficiency is a primary goal.

## Reading
- Read the minimum number of files required to act. Stop when you can act.
- Budget: ≤ 5 files before a first diagnosis; ≤ 10 before proposing a fix. To exceed, name the extra files and why.
- Prefer symbol / reference search over opening files. Locate the symbol, then read only the relevant span.
- Use targeted reads (offset + limit) instead of whole-file reads on large files.
- Do NOT scan the repository. Avoid `find` / recursive `grep` over `Assets/` unless a targeted search has failed.
- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta` files, or generated folders.

## Runtime Disclosure
- Before using any `.md` runtime file, say `Using <path/to/file.md> — <short purpose>` in chat.
- This applies to bootstrap, context, command, and skill markdown files.
- When a command maps to a skill, disclose both files before following them.
- Keep disclosure short and status-like; do not explain the whole file.
- Do not repeat a disclosure for the same file inside one task unless the file is read again.

## Scope Discipline
- Answer ONLY the exact question asked. Do not expand to the full flow, callers, side-effects, save/event/UI, or "related" aspects unless explicitly requested.
- A narrow question ("điều kiện unlock", "giá trị mặc định", "hàm nào set X") gets a narrow trace — stop at the first authoritative `file:line` that answers it.
- When delegating to a sub-agent, the sub-task prompt must be as narrow as the user's question. Never hand a broad multi-part investigation to answer a single-point question.
- After answering, OFFER to go deeper (one line); do not pre-emptively investigate the deeper scope.

## Permission Modes
Follow these modes to balance autonomy and safety:
- **Approval Mode (Default for High Risk)**: MUST ask for user approval before:
    - Deleting any file or directory.
    - Modifying Public APIs, Base Classes, or Singletons.
    - Performing major architectural refactors.
    - Entering Play Mode or performing Build-related tasks (if applicable).
- **Auto Mode (Default for Low Risk)**: Act autonomously for:
    - Adding or fixing `DPDebug` logs.
    - Minor logic fixes inside private/protected methods.
    - Creating new files within established patterns (e.g., a new Skill or sibling implementation).
    - Fixing linting/naming convention issues.
- **Bypass Mode**: Proceed without asking for any of the above ONLY if the user gives an explicit directive like: "Do it all", "Skip approval", or "Bypass modes".

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
