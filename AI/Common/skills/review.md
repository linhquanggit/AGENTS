# Skill: review

Review changed code for conventions, correctness, and architectural integrity.

## Procedure
1. **Targeted Perception**: Review ONLY the changed files/lines. Do NOT audit unrelated parts.
2. **Conventions Audit**: Naming, logging, and comment rules per `../context/Conventions.md`; matches surrounding style.
3. **Correctness & Safety Audit**: Null/error handling, execution order, race conditions, resource/subscription leaks.
4. **Architectural Consistency**: Flag new patterns that duplicate existing ones; ensure minimal change and adherence to existing modules.
5. **Dependency Validation**: Reference-search modified public symbols to ensure no caller broke.

## Anti-Hallucination Guardrails
- **DO NOT** guess the intent of a change; ask if ambiguous.
- **DO NOT** let "nits" overshadow "Blockers" (correctness/safety).
- **DO NOT** raise a finding that isn't actionable — each must pair `issue → concrete fix`.
- **STOP** the review if the changes are too broad (>10 files) and ask for a scoped diff.

## Output
- **Prioritized Findings**: `[Blocker|Major|Minor|Nit] file:line — issue → suggestion`.
- **Consistency note**: brief comment on minimal impact.
