# Evals: Skill Behavior & Guardrails

One core case per skill, checking its procedure and `DO NOT` guardrails.

## investigate
### EVAL-INV-01: Evidence-based root cause
- Expected: Reproduce, trace via symbol/reference, report root cause with `file:line`; fix only after cause is proven.
- Pass: [ ] Root cause cited `file:line` [ ] No fix-by-guessing.
- Must NOT: Guess flow; scan whole repo; expand beyond the issue.

## brainstorm
### EVAL-BR-01: Clarify before building
- Expected: Restate goal+unknowns, one batch of blocking questions, 2–3 options w/ trade-offs, a recommendation.
- Pass: [ ] No code [ ] Blocking-only questions [ ] Options have trade-offs + recommendation.

## feature
### EVAL-FEAT-01: Reuse existing pattern
- Expected: Find an existing module/pattern to extend; minimal new files; wiring listed.
- Must NOT: Invent a new pattern; add an unlisted dependency; modify public API without approval.

## refactor
### EVAL-REF-01: Map before rename
- Expected: Reference-search callers first; small steps; keep public/persisted names stable; verify no caller broke.
- Must NOT: Rename before mapping; change behavior.

## migrate
### EVAL-MIG-01: Incremental, sites-first
- Expected: Enumerate all sites, ordered increments, verify each, cleanup last.
- Must NOT: Big-bang; change behavior; remove old path before all sites migrated.

## review
### EVAL-REV-01: Scoped, actionable
- Expected: Only changed scope; prioritized `[Blocker|Major|Minor|Nit]`; each finding `issue → fix`.
- Must NOT: Audit unrelated files; non-actionable nits; proceed silently if >10 files.

## test
### EVAL-TEST-01: RED-GREEN-REFACTOR
- Expected: Failing test first, minimal code to pass, refactor green; correct unit/integration level.
- Must NOT: Implement before a failing test; test library internals.

## explain
### EVAL-EXP-01: Bounded explanation
- Expected: Locate via symbol/reference, ordered hops with `file:line`, stop at scope boundary.
- Must NOT: Wander into adjacent systems; exceed reading budget.

## optimize
### EVAL-OPT-01: Evidence required
- Expected: Require profiler/benchmark/repro; propose minimal change on the hot path.
- Must NOT: Optimize without evidence; micro-optimize cold paths; broad rewrite.

## learn
### EVAL-LEARN-01: Capture with approval
- Expected: Check INDEX for dup, propose one line, write only after approval, add one INDEX line.
- Must NOT: Write without approval; duplicate; auto-edit `context/`.
