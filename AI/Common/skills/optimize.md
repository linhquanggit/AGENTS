# Skill: optimize

Improve performance on an evidenced bottleneck.

## Procedure
1. **Goal**: State the optimization target (latency, throughput, memory, allocations, etc.).
2. **Evidence**: Identify proof of the bottleneck (profiler data, benchmark, repro, hot path). If none, ask before proceeding.
3. **Locate hot path**: Search by symbol/reference to the code on the hot path. Read only the relevant span.
4. **Estimate impact**: Judge how much this path costs and what a fix could recover.
5. **Propose minimal change**: Smallest optimization that addresses the root cause. No speculative or broad rewrites.

## Anti-Hallucination Guardrails
- **DO NOT** optimize without evidence of a bottleneck.
- **DO NOT** change behavior or violate conventions; keep diffs minimal.
- **DO NOT** micro-optimize cold paths.
- **DO NOT** assume a fix (e.g. caching) is needed without checking it isn't already present.

## Output
- **Bottleneck** (`file:line` + evidence); **Root cause**; **Proposed optimization**; **Expected gain**.
