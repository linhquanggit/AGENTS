# AGENTS.md — AI Runtime Bootstrap

Single source of truth for all coding agents (Claude Code, Gemini CLI, Codex, others). This is a loader — conventions, rules, and routing live in the AI Runtime, not here.

## Language Policy
- Respond in **Vietnamese** by default, regardless of the input language.
- Keep unchanged: code, APIs, class / method / file / package names, namespaces, logs, technical identifiers.
- Technical terms may stay in English when translating reduces clarity.
- Honor an explicit request for another language.

## AI Runtime
```
AI/
├── context/    # Conventions.md, Rules.md, Workflows.md — load all three before any task
├── skills/     # one execution procedure per task type
├── knowledge/  # deep reference, pulled in by skills on demand
└── evals/      # behavioral checks — run after editing the runtime
```

## Required Workflow (every task)
1. Load `AI/context/Conventions.md`, `AI/context/Rules.md`, `AI/context/Workflows.md`.
2. Route the request to one skill via `Workflows.md`.
3. Open that skill and follow its procedure.
4. Open the minimum source files required (see `Rules.md`).

## Verification
After loading the three `context/` files, prepend your **first response of the session** with `[AI_RUNTIME_LOADED]` (once per session, verification only).
