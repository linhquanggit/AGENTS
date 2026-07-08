# Skill: unity-brainstorm

Refine an ambiguous feature or design through questions before any code is written.

## Procedure
1. **Restate**: State the goal and list the unknowns that block a confident design.
2. **Clarify**: Ask the few highest-leverage questions in ONE batch. Only blocking unknowns.
3. **Options**: Offer 2–3 design options with trade-offs. Reuse existing patterns/managers first.
4. **Recommend**: Pick one, bound the scope, and state acceptance criteria.
5. **Hand off**: On approval, route to `unity-feature` (or a plan). Do not build here.

## Anti-Hallucination Guardrails
- **DO NOT** write or modify code in this skill.
- **DO NOT** over-ask — only unknowns that change the design.
- **DO NOT** invent new patterns when existing ones (`PopupBase`, Managers) fit.
- **DO NOT** present options without trade-offs or a recommendation.

## Output
- **Goal & Unknowns**: bounded.
- **Questions**: one batch, only blocking ones.
- **Options**: 2–3 with trade-offs.
- **Recommendation**: chosen option + scope + acceptance criteria.
