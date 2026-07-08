# Evals

Behavioral test cases for the Common profile. No automated runner — an eval is a **scenario + expected behavior + pass criteria** a human or a fresh agent session can check.

Use evals to catch regressions after editing `context/` or `skills/`.

## Files
- [routing.md](routing.md) — intent → correct skill selection.
- [skills.md](skills.md) — per-skill behavior and guardrail (DO NOT) enforcement.
- [conventions.md](conventions.md) — `Conventions.md` + `Rules.md` enforcement.

## Case Format
```
### EVAL-<area>-NN: <title>
- Intent: <what the user says/does>
- Expected: <behavior the runtime must produce>
- Pass: [ ] checkable assertions
- Must NOT: anti-patterns that fail the case
```

## How to Run
Manual: trace the runtime against a case; confirm each `Pass` and that no `Must NOT` happens. Agent self-check: give a fresh session the `Intent`, compare its response to `Pass`/`Must NOT` (don't reveal `Expected` first). A case fails if any `Pass` is unmet or any `Must NOT` occurs.
