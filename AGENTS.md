# AGENTS.md — Runtime Selector (v2)

Shared runtime for coding agents (Claude Code, Gemini CLI, Codex). This file picks a profile and sets global policy. Detail lives in the profile.

## Load order (once per session)
1. This file → pick profile: **Unity** (`AI/Unity/`) if project has `Assets/` + `ProjectSettings/`, else **Common** (`AI/Common/`).
2. `AI/Core/Rules.md` (shared gates) + profile `context/Conventions.md` + profile `context/Workflows.md`.
3. First reply of the session starts with `[AI_RUNTIME_LOADED · global · <Profile>]`. Nothing else to announce.

Everything else (`knowledge/`, `skills/`, snapshots) is pulled **on demand** — never preloaded.

## Language
Reply in **Vietnamese** by default. Keep code, identifiers, APIs, file names, logs as-is; English terms are fine when clearer. Honor an explicit language request.

## Request template
[REQUEST_TEMPLATE.md](REQUEST_TEMPLATE.md). When used, it is authoritative: `[LOẠI]` picks the skill, `[PHẠM VI]` bounds reads/edits (never widen without asking), `[QUYỀN]` sets the permission mode, `[OUTPUT]` decides plan-first vs code-now. Free-form requests: infer silently; only state assumptions when they change what you'll edit.

## Shared locations (all projects)
- **Flow capture log**: `~/.claude/logs/flow-capture.log`. For "log để test luồng / check bug": truncate it first, have the user reproduce, then read it — instead of asking for console/device logs. Writing to it is the project's own code.
- **Reports**: `Reports/<project-basename>/<YYYY-MM-DD>-<slug>.md` in THIS repo (never inside the project's SCM). On "lưu báo cáo / ghi tiến trình": write it, then add one line to `Reports/INDEX.md` (`- [title](path) — hook`).

## Project entry points
- **WB / World Boss / project "1941"** (`~/wkspaces/WB`): read `~/wkspaces/Brain/BR_WorldBoss/AGENT.md` FIRST — it is the map for the whole module. Follow its reading order (brief → library sections → design rows R… → Figma tiles). Do not scan WB code beyond what the brief/library points to; widen only when they don't cover the task, and say so.

## Muse (muse.ai)
- Claude Code: skill `muse-collab`. Antigravity: skill `~/.gemini/antigravity/skills/muse-collab` (nguồn `muse-bridge/skill/muse-collab-antigravity/`), gọi `muse-bridge/bin/muse-ask --agent antigravity`.
