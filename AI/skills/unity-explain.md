# Skill: unity-explain

Explain a system, architecture, execution flow, dependencies, or code behavior. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Scope**: Restate exactly what to explain. Bound it; ignore unrelated systems.
2. **Locate entry point**: Search by symbol/reference for where the requested scope begins. Don't scan `Assets/`.
3. **Trace**: Follow the execution path by reference search, one hop at a time. Read only the relevant span.
4. **Explain**: Cover only the requested scope. Stop at its boundary.

## Guardrails
- Symbol/reference search before opening files (investigation order: symbol → references → callers → callees → open source).
- Minimize file reads; obey the reading budget.
- Don't explain or wander into adjacent systems.

## Output
- High-level explanation.
- Execution flow (ordered hops with `file:line`).
- Relevant dependencies.
- Files involved.
