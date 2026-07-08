# Knowledge: Authoring Skills

How to add or edit a skill in this profile so it stays consistent and lean. Use when creating/changing anything in `../skills/`.

## Format
```
# Skill: <verb>
<one-line purpose>

## Procedure
1. **Step**: ... (numbered, bold sub-steps; reference search before opening files)

## Anti-Hallucination Guardrails
- **DO NOT** ... (the traps specific to this skill)

## Output
- ... (what the user gets)
```

## Rules
- Keep the skill lean (~15–35 lines). Push deep reference into `../knowledge/`, not the skill body.
- **DO NOT** restate global `Rules.md` / `Conventions.md` — reference them.
- Every skill needs ≥1 `DO NOT` guardrail and at least one eval case.

## Wiring (required for a new skill)
1. Add a route row in `../context/Workflows.md` (command + trigger phrases + skill).
2. Add eval cases: a routing case in `../evals/routing.md` and a behavior case in `../evals/skills.md`.
3. If it relies on domain facts, reference the relevant `../knowledge/` file.
