# Evals

Behavioral test cases for the AI Runtime. The runtime is markdown (no automated runner), so an eval is a **scenario + expected behavior + pass criteria** that a human — or a fresh agent session — can check by hand.

Use evals to catch regressions after editing `context/` or `skills/`.

## Files
- [routing.md](routing.md) — intent → correct skill selection.
- [skills.md](skills.md) — per-skill behavior and guardrail (DO NOT) enforcement.
- [conventions.md](conventions.md) — `Conventions.md` + `Rules.md` enforcement (DPDebug, naming, scope, budget).

## Case Format
```
### EVAL-<area>-NN: <title>
- Intent: <what the user says / does>
- Expected: <the behavior the runtime must produce>
- Pass: [ ] checkable assertions
- Must NOT: anti-patterns that fail the case
```

## How to Run
**Manual review** — read a case, mentally trace the runtime against it, confirm each `Pass` box and that no `Must NOT` happens.

**Agent self-check** — in a fresh session, give the agent the `Intent`, let it respond, then compare the response to `Pass` / `Must NOT`. Do NOT show it the `Expected` block first (that would leak the answer).

A case **fails** if any `Pass` box is unmet OR any `Must NOT` occurs. Fix the responsible runtime file, then re-run.
