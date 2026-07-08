# Skill: investigate

Find the root cause of a bug or unexpected behavior with minimal reads.

## Procedure
1. **Restate & Reproduce**: Symptom and expected behavior in one line each; confirm a reproduction (repro steps or observed evidence). Bound the scope to the reported issue.
2. **Locate Entry Point**: Search by symbol/reference; do NOT scan the whole repo. Order: symbol → references → callers → callees → source.
3. **Trace Minimal Path**: One hop at a time, relevant span only. Form a hypothesis and test it against evidence; if disproven, widen one hop. Stop at the first authoritative `file:line` that explains the fault.
4. **Root Cause**: State it with `file:line` evidence. Distinguish cause from symptom.
5. **Fix & Defend**: Smallest fix; where cheap, add a guard/assert/log so it cannot recur silently. Apply only if requested.

## Anti-Hallucination Guardrails
- **DO NOT** guess execution flow; every claim needs a `file:line`.
- **DO NOT** fix by guessing — reproduce the symptom and understand the cause first.
- **DO NOT** expand beyond the reported issue unless asked.
- **STOP** and ask if confidence is below 80%.

## Output
- **Symptom vs. Expected**; **Call Path** (`file:line`); **Root Cause** (evidence); **Proposed Fix** (minimal).
