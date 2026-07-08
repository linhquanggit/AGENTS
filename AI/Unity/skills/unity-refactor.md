# Skill: unity-refactor

Restructure code while preserving behavior.

## Procedure
1. **Map Dependencies**:
   - Before touching code, find all callers, references, and overrides of the target via reference search. **DO NOT** edit before the dependency map is complete.
2. **Confirm Invariant**:
   - Confirm behavior must stay identical. State explicitly what must NOT change (public API, serialized field names, execution order).
3. **Refactor in Small Steps**:
   - Apply the smallest verifiable change at a time. Modify existing patterns; **DO NOT** introduce new ones.
   - Keep diffs minimal; touch the fewest files possible.
4. **Keep API Stable**:
   - Keep public signatures and `[SerializeField]` names stable unless a change was explicitly requested (renaming serialized fields breaks Editor references).
5. **Verify**:
   - Re-run reference search on every changed symbol to confirm no caller broke.

## Anti-Hallucination Guardrails
- **DO NOT** rename or move a symbol before mapping its callers — broken references are silent in Unity.
- **DO NOT** change behavior, only structure, unless the task says otherwise.
- **DO NOT** perform major architectural refactors without approval (Permission Modes).
- **STOP** if the dependency map reveals the change is broader than expected; report and ask.

## Output
- **Dependency Map**: Callers/references/overrides found, with `file:line`.
- **Invariant**: What stays identical.
- **Changes**: Step-by-step diff summary, minimal scope.
- **Verification Result**: Confirmation that behavior is unchanged and all callers are valid.
