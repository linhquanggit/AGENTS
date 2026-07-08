# AGENTS.md — Common Profile

Self-contained, language-agnostic AI runtime for any codebase. The default profile when no domain-specific profile matches the project. Conventions, rules, and routing all live in this folder.

## Structure
```
Common/
├── context/    # Conventions.md, Rules.md, Workflows.md — load all three before any task
├── skills/     # one execution procedure per task type
├── knowledge/  # deep reference, pulled in by skills on demand
└── evals/      # behavioral checks — run after editing this profile
```

## Required Workflow (every task)
1. Load `context/Conventions.md`, `context/Rules.md`, `context/Workflows.md`.
2. Before acting, scan `knowledge/learnings/INDEX.md`; read any entry relevant to the task.
3. Route the request to one skill via `context/Workflows.md`.
4. Open that skill and follow its procedure; open the minimum source files (see `context/Rules.md`).

## Verification
After loading the three `context/` files, prepend your **first response of the session** with `[AI_RUNTIME_LOADED · global · Common]` (confirms the global runtime was read; once per session, verification only).
