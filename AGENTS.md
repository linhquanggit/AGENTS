# AGENTS.md — AI Runtime Bootstrap

Canonical entry point for all coding agents (Claude Code, Gemini CLI, Codex, others).
This file is a **loader only**. It does not contain conventions, rules, or workflows — those live in the AI Runtime.

## Language Policy
- Respond in **Vietnamese** by default, regardless of the language of the user's input.
- Keep unchanged: code, APIs, class / method / file / package names, namespaces, logs, and technical identifiers.
- Technical terminology may stay in English when translating would reduce clarity.
- If the user explicitly requests another language, honor it.

## AI Runtime Location
```
AI/
├── context/    # Conventions.md, Rules.md, Workflows.md  (read before any task)
├── skills/     # task execution workflows
└── commands/   # command → skill mappings
```

- `context/` — authoritative conventions, hard rules, and high-level workflows.
- `skills/` — detailed step-by-step execution procedures (`unity-investigate`, `unity-feature`, `unity-refactor`, `unity-review`, `unity-explain`, `unity-optimize`).
- `commands/` — thin entry points mapping a command to its skill (`bug`, `feature`, `refactor`, `review`, `explain`, `optimize`).

## Required Runtime Workflow (every task)
1. Load AI Runtime context.
2. Read `AI/context/Conventions.md`.
3. Read `AI/context/Rules.md`.
4. Read `AI/context/Workflows.md`.
5. Select the appropriate skill (via `commands/` or `skills/`).
6. Follow the skill's workflow.
7. Open the minimum source files required.

## Investigation Rules
- Prefer symbol search before opening files.
- Prefer reference search before opening files.
- Avoid repository-wide searches unless necessary.
- Minimize token usage.
- Minimize file reads.
- Preserve existing architecture.
- Follow project conventions.

## Verification
After successfully loading the AI Runtime (the three `context/` files), prepend your **first response of the session** with:

```
[AI_RUNTIME_LOADED]
```

Emit this marker only once per session, for verification only.
