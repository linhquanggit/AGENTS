# Skill: learn

Capture a durable, project-specific fact so future sessions skip the rediscovery. Records to `../knowledge/learnings/` — **always with user approval**.

## When to Trigger
- A non-trivial bug's root cause is a reusable pattern, not a one-off.
- An undocumented architecture, convention, or code location was discovered (with `file:line`).
- The user corrected your behavior or output (strong signal).
- You had to search repeatedly to locate something worth remembering.
Skip trivial, obvious, or single-use facts.

## Procedure
1. **Validate**: Non-obvious, reusable, and NOT already in `../knowledge/learnings/INDEX.md`? Scan the index first. If it exists → update that entry. If trivial → skip.
2. **Draft**: One fact, with `file:line` evidence and scope (the system/folder it applies to).
3. **Propose** (always): Show one line — `Record learning: <title> → knowledge/learnings/<slug>.md? (y/n)`. Wait for approval.
4. **Write** (on approval): Create the fact file + add ONE line to `INDEX.md` (newest first). Keep both minimal.
5. **Promotion**: If the fact is a project-wide rule, do NOT auto-edit `context/`. Propose updating `Conventions.md` / `Rules.md` and wait.

## Anti-Hallucination Guardrails
- **DO NOT** record without `file:line` evidence or a concrete trigger.
- **DO NOT** duplicate an existing `INDEX.md` entry — update it.
- **DO NOT** write to `context/` automatically; propose and wait.
- **DO NOT** write anything before approval; one fact per file, hook ≤ 1 line.

## Output
- Proposed learning (title + fact + evidence); after approval, confirmation of the file + INDEX line added.
