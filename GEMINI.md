# GEMINI.md — Gemini CLI Bootstrap

Gemini-specific loader. The canonical bootstrap is **[AGENTS.md](AGENTS.md)** — read it first. This file does not duplicate its content.

## Start Here
1. Read [AGENTS.md](AGENTS.md) and follow the Required Runtime Workflow.
2. Before any task, load the AI Runtime context:
   - `Assets/AI/context/Conventions.md`
   - `Assets/AI/context/Rules.md`
   - `Assets/AI/context/Workflows.md`
3. `Assets/AI/skills/` holds task execution workflows.
4. `Assets/AI/commands/` holds command → skill mappings.

## Gemini CLI Notes
- Prefer symbol search, then reference search, before reading files.
- Avoid repository-wide searches unless necessary; minimize token usage and file reads.
- Preserve existing architecture and follow project conventions.
- Commands `bug` / `feature` / `refactor` / `review` map to the matching skill.

## Verification
Once the three `context/` files are loaded, prepend your first response of the session with `[AI_RUNTIME_LOADED]` (once per session, verification only). See [AGENTS.md](AGENTS.md#verification).
