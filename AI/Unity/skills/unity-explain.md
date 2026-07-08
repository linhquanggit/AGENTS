# Skill: unity-explain

Explain a system, architecture, execution flow, dependencies, or code behavior.

## Procedure
1. **Scope**: Restate exactly what to explain. Bound it; ignore unrelated systems.
2. **Locate entry point**: Search by symbol/reference for where the requested scope begins. Don't scan `Assets/`.
3. **Trace**: Follow the execution path by reference search, one hop at a time. Read only the relevant span.
4. **Explain**: Cover only the requested scope. Stop at its boundary.

## Anti-Hallucination Guardrails
- **DO NOT** open files before symbol/reference search (order: symbol → references → callers → callees → open source).
- **DO NOT** exceed the reading budget; use targeted reads (offset + limit).
- **DO NOT** explain or wander into adjacent systems beyond the requested scope.
- **DO NOT** state execution flow without `file:line` evidence; ask if confidence is below 80%.

## Output
- High-level explanation.
- Execution flow (ordered hops with `file:line`).
- Relevant dependencies.
- Files involved.
