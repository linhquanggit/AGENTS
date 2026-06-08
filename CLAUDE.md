# CLAUDE.md — Claude Code Bootstrap

Claude-specific loader. The canonical bootstrap is **[AGENTS.md](AGENTS.md)** — read it first. This file does not duplicate its content.

## Start Here
1. Read [AGENTS.md](AGENTS.md) and follow the Required Runtime Workflow.
2. Before any task, load the AI Runtime context:
   - [Conventions.md](Assets/AI/context/Conventions.md)
   - [Rules.md](Assets/AI/context/Rules.md)
   - [Workflows.md](Assets/AI/context/Workflows.md)
3. [skills/](Assets/AI/skills/) holds task execution workflows.
4. [commands/](Assets/AI/commands/) holds command → skill mappings.

## Claude Code Notes
- Batch independent reads/searches in a single turn.
- Use symbol/reference search before opening files; avoid repo-wide scans.
- Use the minimum reads needed, then act. Preserve existing architecture and conventions.
- Commands `bug` / `feature` / `refactor` / `review` map to the matching skill.

## Verification
Once the three `context/` files are loaded, prepend your first response of the session with `[AI_RUNTIME_LOADED]` (once per session, verification only). See [AGENTS.md](AGENTS.md#verification).
