# Skill: migrate

Convert a system at scale incrementally, preserving behavior at every step.

## Procedure
1. **Scope & Target**: State old → new and why. Enumerate ALL call sites via reference search. Do NOT start before the full site list exists.
2. **Plan Increments**: Break into small, independently-verifiable steps. Order by risk: leaf/low-coupling sites first, shared/base last.
3. **Compatibility Shim**: Keep old and new paths working together during transition where possible. Avoid big-bang swaps.
4. **Migrate One Increment**: Apply one step, keep behavior identical, verify before the next.
5. **Cleanup**: Remove the old path ONLY after every site is migrated and verified.

## Anti-Hallucination Guardrails
- **DO NOT** big-bang migrate — increments only.
- **DO NOT** migrate a site before all sites are enumerated.
- **DO NOT** change behavior during a structural migration.
- **STOP** if the site count is far larger than expected; report and re-plan.

## Output
- **Migration Map** (all sites, `file:line`); **Increment Plan**; **Per-step Result**; **Cleanup** confirmation.
