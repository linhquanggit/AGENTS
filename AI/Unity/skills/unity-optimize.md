# Skill: unity-optimize

Improve performance on an evidenced bottleneck.

## Procedure
1. **Goal**: State the optimization target (frame time, allocations, load time, etc.).
2. **Evidence**: Identify proof of the bottleneck (profiler data, repro, hot loop). If none, ask before proceeding.
3. **Locate hot path**: Search by symbol/reference to the code on the hot path. Read only the relevant span.
4. **Estimate impact**: Judge how much this path costs and what a fix could recover.
5. **Propose minimal change**: Smallest optimization that addresses the root cause. No speculative or broad rewrites.

## Anti-Hallucination Guardrails
- **DO NOT** optimize without evidence of a bottleneck (profiler data, repro, hot loop).
- **DO NOT** change behavior or violate conventions; keep diffs minimal.
- **DO NOT** micro-optimize cold paths (code that runs infrequently).
- **DO NOT** assume "common sense" fixes (e.g. caching) are needed without checking they aren't already present.

## Output
- Bottleneck.
- Root cause (`file:line`).
- Proposed optimization.
- Risk assessment.
- Expected gain.
