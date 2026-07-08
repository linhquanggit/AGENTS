# AGENTS.md — Runtime Selector

Multi-profile AI runtime for all coding agents (Claude Code, Gemini CLI, Codex, others). Each profile under `AI/<Profile>/` is a **self-contained runtime** (its own `AGENTS.md` + `context/` + `skills/` + `knowledge/` + `evals/`). This root file only selects one profile and defers to it.

## Language Policy (all profiles)
- Respond in **Vietnamese** by default, regardless of the input language.
- Keep unchanged: code, APIs, class / method / file / package names, namespaces, logs, technical identifiers.
- Technical terms may stay in English when translating reduces clarity.
- Honor an explicit request for another language.

## Profile Selection
Pick the profile matching the project, then read that profile's `AGENTS.md` and follow it:
- **Unity** (`AI/Unity/`) — if the project is a Unity C# project (has `Assets/` + `ProjectSettings/`).
- **Common** (`AI/Common/`) — the language-agnostic default for any other project.

Add a sibling folder per new domain; each mirrors the same layout.

## Verification
After loading the active profile's three `context/` files, prepend your **first response of the session** with `[AI_RUNTIME_LOADED · global · <Profile>]` — where `<Profile>` is `Unity` or `Common` (confirms the global runtime was read). Once per session, verification only.
