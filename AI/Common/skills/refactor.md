# Skill: refactor

Restructure code while preserving behavior.

## Procedure
1. **Map Dependencies**: Find all callers, references, and overrides of the target via reference search. Do NOT edit before the map is complete.
2. **Confirm Invariant**: Confirm behavior must stay identical. State explicitly what must NOT change (public API, persisted/serialized names, execution order).
3. **Refactor in Small Steps**: Smallest verifiable change at a time. Modify existing patterns; do NOT introduce new ones. Keep diffs minimal.
4. **Keep API Stable**: Keep public signatures and persisted names stable unless a change was explicitly requested.
5. **Verify**: Re-run reference search on every changed symbol to confirm no caller broke.

## Anti-Hallucination Guardrails
- **DO NOT** rename or move a symbol before mapping its callers.
- **DO NOT** change behavior, only structure, unless the task says otherwise.
- **DO NOT** perform major architectural refactors without approval.
- **STOP** if the dependency map reveals the change is broader than expected.

## Output
- **Dependency Map** (`file:line`); **Invariant**; **Changes** (minimal steps); **Verification Result**.
