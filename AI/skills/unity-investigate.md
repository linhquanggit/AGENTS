# Skill: unity-investigate

Find the root cause of a bug with minimal reads and high precision. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Perception & Framing**: 
   - Restate symptom, expected vs actual, and repro trigger.
   - **Perception Step**: Scan for related Managers, Singletons, or recent changes in the area to understand the "health" of the system before diving into code.
2. **Locate (Surgical Search)**: 
   - Search for the symbol named in the report (method, field, log string, UI name). 
   - Use symbol search first, then reference search to find the entry point. 
   - **DO NOT** scan `Assets/` or use recursive grep unless targeted searches fail.
3. **Trace & Narrow (Hop-by-Hop)**: 
   - Open only the file(s) containing the entry point. Read minimal spans (offset+limit).
   - Follow the call path one hop at a time via reference search. 
   - Validate state and data flow at each hop. Stop at the first authoritative `file:line` that identifies the fault.
4. **Diagnosis**: 
   - State the root cause clearly with `file:line` evidence.
   - Categorize the bug (e.g., Logic Error, Null Ref, Race Condition, Config Issue).
5. **Smallest Fix Proposal**: 
   - Propose the minimal change required. Apply only if requested.
   - If multiple similar bugs exist, propose a **Batch Fix** to save tokens.

## Anti-Hallucination Guardrails
- **DO NOT** assume execution flow without `file:line` evidence.
- **DO NOT** suggest a fix before identifying the root cause with certainty (>90% confidence).
- **DO NOT** open more than 5 files before providing a status update/initial diagnosis.
- **DO NOT** ignore existing patterns (e.g., `DPDebug`, naming conventions) in the proposed fix.
- **STOP** and ask for clarification if the repro trigger is ambiguous or logs are missing.

## Output
- **Root Cause**: 1-2 lines + `file:line` evidence.
- **Evidence Trace**: Concise list of hops leading to the bug.
- **Proposed/Applied Fix**: Minimal code change or configuration update.
