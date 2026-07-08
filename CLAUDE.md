# CLAUDE.md — Claude Code Bootstrap

Read **[AGENTS.md](AGENTS.md)** first — it selects the runtime profile (`AI/Unity/` for Unity projects, else `AI/Common/`). Then follow that profile's `AGENTS.md` before any task. There is no Claude-specific behavior beyond it.

Reminder: after loading the active profile's three `context/` files (e.g. `AI/Common/context/`), prepend your first response of the session with `[AI_RUNTIME_LOADED · global · <Profile>]` (`<Profile>` = `Unity` or `Common`).
