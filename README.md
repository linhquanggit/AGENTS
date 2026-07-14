# AI Runtime

**A shared, token-efficient runtime that routes any coding agent — Claude Code, Gemini CLI, Codex — through consistent conventions, rules, and skills.**

Install it once per machine and *every* agent session in *every* project auto-loads the right profile, follows your rules, and routes each request to a purpose-built skill — no per-project setup.

```sh
brew install https://raw.githubusercontent.com/linhquanggit/AGENTS/main/packaging/ai-runtime.rb
ai-runtime install
```

That's it. Open a new session in any project and you'll see the verification marker `[AI_RUNTIME_LOADED · global · <Profile>]` — the runtime is live.

---

## Why

Coding agents forget your conventions, re-derive the same decisions, and behave differently in every project. This runtime fixes that by putting one shared brain behind all of them:

- 🎯 **Profile-based** — the right ruleset auto-selects per project (Unity vs. anything else). Add a profile per domain.
- 🪶 **Token-efficient first** — only three small `context/` files load on every task; everything else is pulled in on demand.
- 🧠 **Self-improving** — agents record durable lessons to `knowledge/learnings/` and read them before acting.
- ✅ **Guardrailed & tested** — per-skill "DO NOT" lists plus `evals/` behavioral checks.
- 🌐 **Machine-wide** — one install wires `~/.claude` so every project routes through it automatically.

---

## How it works

```
New session ──► ~/.claude/CLAUDE.md + SessionStart hook
                        │
                        ▼
              AGENTS.md  (Runtime Selector)
                        │  picks a profile
          ┌─────────────┴─────────────┐
          ▼                           ▼
     AI/Unity/                    AI/Common/
   (Unity C# projects)        (everything else)
          │                           │
          ▼                           ▼
   load context/  ─►  scan learnings/  ─►  route to one skill  ─►  execute
```

1. **Select** — `AGENTS.md` picks a profile: **Unity** if the project has `Assets/` + `ProjectSettings/`, else **Common**.
2. **Load** — read that profile's three `context/` files (`Conventions`, `Rules`, `Workflows`).
3. **Recall** — scan `knowledge/learnings/INDEX.md` for anything relevant to the task.
4. **Route** — map the request to exactly one skill via `Workflows.md`.
5. **Execute** — follow that skill's procedure, opening the minimum source files.

A **SessionStart hook** injects this directive into every new session, so the agent reliably loads the runtime instead of hoping it remembers to.

---

## Profiles

Each profile under `AI/<Profile>/` is a **self-contained runtime**: its own `AGENTS.md` + `context/` + `skills/` + `knowledge/` + `evals/`.

| Profile | When it's used | Skills |
|---|---|---|
| **Common** | Language-agnostic default for any project | `investigate` · `brainstorm` · `feature` · `refactor` · `migrate` · `review` · `test` · `explain` · `optimize` · `learn` |
| **Unity** | Unity C# projects (`Assets/` + `ProjectSettings/`) | `unity-investigate` · `unity-brainstorm` · `unity-feature` · `unity-refactor` · `unity-migrate` · `unity-review` · `unity-test` · `unity-explain` · `unity-optimize` · `unity-learn` · `unity-advisory` · `unity-perception` |

> Add a sibling folder per new domain — each mirrors the same layout and plugs in automatically.

---

## Repository layout

```
AGENTS/
├── AGENTS.md            # Runtime selector — picks a profile, defers to it
├── CLAUDE.md            # Claude Code bootstrap (points at AGENTS.md)
├── GEMINI.md            # Gemini CLI bootstrap (points at AGENTS.md)
├── AI/
│   ├── Common/          # language-agnostic profile
│   │   ├── context/     #   Conventions.md · Rules.md · Workflows.md  (loaded every task)
│   │   ├── skills/      #   one procedure per task type
│   │   ├── knowledge/   #   deep reference + learnings/ (pulled in on demand)
│   │   └── evals/       #   behavioral checks
│   └── Unity/           # Unity profile — same layout
├── bin/ai-runtime       # install / uninstall / status / update CLI
├── packaging/           # Homebrew formula
├── INSTALL.md           # full install guide (Homebrew, dev, maintainer)
└── SOURCES.md           # repos studied to improve this runtime
```

---

## Install & manage

Full guide in **[INSTALL.md](INSTALL.md)**. Quick reference:

```sh
ai-runtime install      # wire ~/.claude: symlink + managed block + read scope + SessionStart hook
ai-runtime status       # show what's wired
ai-runtime update       # git pull (dev) — or: brew upgrade ai-runtime
ai-runtime uninstall    # remove everything it added
```

**Developers** (run from a checkout, no Homebrew):

```sh
git clone https://github.com/linhquanggit/AGENTS.git
cd AGENTS && ./bin/ai-runtime install
```

---

## Self-improvement

The runtime gets smarter as you use it:

- The `learn` / `unity-learn` skill captures a durable lesson whenever something non-obvious is discovered (always asks first).
- Lessons land in `knowledge/learnings/`, indexed by `INDEX.md`.
- Every task **reads** relevant learnings before acting and **writes** new ones after — so mistakes aren't repeated across sessions or projects.

---

## Conventions

- **Language** — agents respond in **Vietnamese** by default; code, APIs, and identifiers stay unchanged.
- **Verification** — the first response of each session is prefixed with `[AI_RUNTIME_LOADED · global · <Profile>]`.
- **Sources** — repos studied to improve this runtime are logged in **[SOURCES.md](SOURCES.md)**.

---

<sub>Built to make every coding-agent session consistent, cheap on tokens, and a little smarter than the last.</sub>
