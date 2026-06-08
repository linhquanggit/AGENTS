# Skill: unity-investigate

Find the root cause of a bug with minimal reads. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Frame**: Restate symptom, expected vs actual, repro trigger. Ask only if blocking.
2. **Locate**: Search for the symbol named in the report (method, field, log string, UI name). Do NOT scan `Assets/`. Use reference search to find the entry point.
3. **Narrow**: Open only the file(s) containing the entry point. Read the relevant span (offset+limit), not the whole file.
4. **Trace**: Follow the call path by reference search, one hop at a time. Read each hop's minimal span. Stop at the fault.
5. **Confirm root cause**: State it with `file:line` evidence (null path, wrong state, bad order, missing guard).
6. **Propose fix**: Smallest change that resolves it. List affected `file:line`. Apply only if the user asked to fix.

## Guardrails
- Symbol/reference search before opening files.
- No repository-wide search unless a targeted search failed.
- Don't fix unrelated issues found along the way — note them separately.

## Output
- Root cause (1–2 lines) + evidence.
- Proposed fix (diff-level) or applied change.
