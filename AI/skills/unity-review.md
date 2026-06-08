# Skill: unity-review

Review changed code for convention and correctness issues. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Scope**: Review only the changed files/lines. Don't audit the whole project.
2. **Conventions check**:
   - Naming: `_local`, camelCase private, PascalCase public.
   - Logging: `DPDebug` only; correct color (#4aff21/#ffd900/#ff3838); `[]` holds only GameObject name/tag; values outside `[]`.
   - No comments and no XML/`<summary>` docs unless explicitly requested.
3. **Correctness check**: null safety, state/order, off-by-one, leaks, event un/subscription.
4. **Architecture check**: reuse existing managers/base classes; flag new patterns duplicating existing systems; flag unnecessary new files; flag non-minimal file modifications.
5. **Dependency check**: confirm changes don't break known callers (reference search if risky).

## Guardrails
- Targeted reads only; no repo-wide scan.
- Distinguish blocking issues from nits.

## Output
Prioritized list (in order Blocker → Major → Minor → Nit): `[Blocker|Major|Minor|Nit] file:line — issue → suggestion`.
