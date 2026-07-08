# Skill: feature

Add new functionality by extending existing patterns, not inventing new ones.

## Procedure
1. **Scope & Acceptance**: Clarify what the feature must do and its acceptance criteria; bound it. If requirements are ambiguous, run `brainstorm` first, then return here.
2. **Find the Pattern to Extend**: Search for the closest existing system/module to extend. Do NOT scan the whole repo.
3. **Reuse First**: Reuse base modules and helpers. Do NOT introduce a new pattern if an established one fits.
4. **Implement Minimally**: Add the fewest new files/lines; match surrounding style per `../context/Conventions.md`.
5. **Wiring**: List any config/registration/injection the user must do to activate it.

## Anti-Hallucination Guardrails
- **DO NOT** invent a new architecture when an existing module/base fits — find it first.
- **DO NOT** add dependencies not already in the project.
- **DO NOT** modify public APIs / shared base modules without approval.
- **STOP** and ask if acceptance criteria are ambiguous.

## Output
- **Scope & Acceptance**; **Pattern Reused** (`file:line`); **Changes** (files + one-line rationale); **Wiring**.
