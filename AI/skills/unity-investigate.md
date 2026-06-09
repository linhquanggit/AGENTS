# Skill: unity-investigate

Find the root cause of a bug or unexpected behavior with minimal reads.

## Procedure
1. **Restate**: State the symptom and the expected behavior in one line each. Bound the scope to exactly the reported issue.
2. **Locate Entry Point**:
   - Search by symbol/reference for where the behavior originates. **DO NOT** scan `Assets/`.
   - Investigation order: symbol → references → callers → callees → open source.
3. **Trace Minimal Path**:
   - Follow the call path one hop at a time. Read only the relevant span (offset + limit).
   - Stop at the first authoritative `file:line` that explains the fault.
4. **Root Cause**:
   - State the cause with `file:line` evidence. Distinguish the cause from its symptoms.
5. **Propose Fix**:
   - Describe the smallest fix that addresses the root cause. Apply only if requested (see Permission Modes).

## Anti-Hallucination Guardrails
- **DO NOT** guess execution flow; every claim needs a `file:line`.
- **DO NOT** propose a fix before the root cause is proven by evidence.
- **DO NOT** expand into callers, side-effects, save/event/UI, or "related" systems unless asked (Scope Discipline).
- **STOP** and ask if confidence is below 80%, stating assumptions explicitly.

## Output
- **Symptom vs. Expected**: One line each.
- **Call Path**: Ordered hops with `file:line`.
- **Root Cause**: `file:line` with evidence.
- **Proposed Fix**: Minimal change. One line offering to apply or go deeper.
