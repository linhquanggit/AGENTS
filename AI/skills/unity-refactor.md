# Skill: unity-refactor

Restructure code without changing behavior. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Define target**: State what is being refactored and the invariant (behavior must stay identical unless told otherwise).
2. **Map dependencies FIRST**: Reference-search every caller, override, and usage of the symbols you'll touch. Do not edit before this map is complete.
3. **Assess impact**: List affected `file:line`. If public API changes, confirm with the user.
4. **Refactor in steps**: Small, self-contained edits. Keep names per conventions. Modify existing patterns; don't introduce new ones.
5. **Verify references**: Re-run reference search to confirm no caller is broken and signatures still match.

## Guardrails
- Investigate dependencies before refactoring (hard rule).
- Keep diffs minimal; no opportunistic rewrites.
- Preserve `DPDebug` logging and conventions.

## Output
- Dependency map (callers touched).
- Changes with `file:line` and confirmation behavior is unchanged.
