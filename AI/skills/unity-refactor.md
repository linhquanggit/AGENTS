# Skill: unity-optimize

Improve performance based on empirical evidence. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Goal & Evidence**: 
   - State the optimization target (e.g., frame time, allocations).
   - **Evidence Check**: Identify proof of the bottleneck (Profiler data, repro steps, hot loop). **DO NOT** proceed without evidence.
2. **Perception & Path Analysis**: 
   - Search by symbol/reference to the hot path code.
   - Analyze the "Perception" of the surrounding system: Is this a global bottleneck or a local one?
3. **Cost-Benefit Estimation**: 
   - Judge the current cost vs. the potential recovery. 
   - Focus on "Batch-First" solutions if applicable (e.g., reducing `GetComponent` calls in loops).
4. **Surgical Implementation**: 
   - Propose/Apply the smallest optimization that addresses the root cause. 
   - **DO NOT** perform broad or speculative rewrites.
5. **Validation**: Confirm the fix addresses the evidence without breaking behavior.

## Anti-Hallucination Guardrails
- **DO NOT** micro-optimize cold paths (code that runs infrequently).
- **DO NOT** suggest optimizations that violate naming or logging conventions.
- **DO NOT** assume "common sense" optimizations (like caching) are needed without checking if they are already implemented or irrelevant.

## Output
- **Bottleneck Identified**: `file:line` with evidence.
- **Proposed Optimization**: Minimal code change.
- **Batch Impact**: How it handles repeated operations efficiently.
- **Expected Gain**: Estimated performance improvement.
sult**: Confirmation that behavior remains unchanged and all callers are valid.
