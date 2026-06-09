# Skill: unity-review

Review changed code for conventions, correctness, and architectural integrity.

## Procedure
1. **Targeted Perception**: 
   - Review ONLY the changed files/lines. **DO NOT** audit unrelated parts of the project.
2. **Conventions Audit**:
   - Naming: `_local`, camelCase private, PascalCase public.
   - Logging: `DPDebug` ONLY; correct color (#4aff21/#ffd900/#ff3838); `[]` holds ONLY GameObject name/tag.
   - **NO** comments / XML docs unless explicitly requested.
3. **Correctness & Safety Audit**: 
   - Check null safety, execution order, race conditions, and memory leaks (event un/subscription).
4. **Architectural Consistency Audit**: 
   - Flag any new patterns that duplicate existing ones. 
   - Check for **Batch-First** compliance: Could this change have been more efficient?
   - Ensure minimal file modification and adherence to base classes.
5. **Dependency Validation**: 
   - Perform reference search on modified public symbols to ensure no callers are broken.

## Anti-Hallucination Guardrails
- **DO NOT** guess the intent of a change; ask if ambiguous.
- **DO NOT** let "nits" (minor issues) overshadow "Blockers" (correctness/safety).
- **STOP** the review if the changes are too broad (>10 files) and ask for a scoped PR.

## Output
- **Prioritized Findings**: List by severity `[Blocker|Major|Minor|Nit] file:line — issue → suggestion`.
- **Architectural Score**: Brief comment on consistency and minimal impact.
