# AGENTS.md — Unity Profile

Self-contained AI runtime for Unity C# projects. Selected by the root selector when the project is Unity (`Assets/` + `ProjectSettings/`). Conventions, rules, and routing all live in this folder.

## Structure
```
Unity/
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
After loading the three `context/` files, prepend your **first response of the session** with `[AI_RUNTIME_LOADED]` (once per session, verification only).
