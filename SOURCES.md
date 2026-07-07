# SOURCES — Repos used to improve this runtime

A log of external repos studied to improve the AI Runtime, what was adopted, and what was deliberately skipped. Repo-only documentation — not part of the runtime and not copied into projects by the importer.

> Add newest entries at the top. For each: repo, when, what was adopted (→ where it landed), what was skipped (+ why).

---

## multica-ai/andrej-karpathy-skills — 2026-07
https://github.com/multica-ai/andrej-karpathy-skills · Karpathy's critique of LLM coding failures, distilled into 4 principles (Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution).
- **Already covered (~85%, not re-added — would bloat hot-path):** simplicity/minimal-scope, match-existing-style, test-first, verification, clarify-when-unsure — all present via existing Rules/Conventions + `unity-test`/`unity-brainstorm`.
- **Adopted (only the genuinely-new nuggets):**
  - Active push-back: surface tradeoff + alternative on a flawed/oversimplified request → `context/Rules.md` (Uncertainty).
  - Dead code / orphaned deps: remove only deps your change orphaned; flag pre-existing dead code → `context/Rules.md` (Editing). Evals EVAL-CONV-15/16.
- **Skipped:** nothing new beyond the above — the rest overlapped existing rules.

## obra/superpowers — 2026-07
https://github.com/obra/superpowers · agentic software-development methodology (TDD, planning, subagents, debugging discipline).
- **Adopted:**
  - Systematic 4-phase debugging (reproduce → hypothesis → root cause → fix + defend) → `skills/unity-investigate.md`.
  - `verification-before-completion` (no "done" without evidence) → `context/Rules.md`.
  - Brainstorming (Socratic clarify before building) → `skills/unity-brainstorm.md`.
  - TDD (RED-GREEN-REFACTOR) → `skills/unity-test.md`.
  - `writing-skills` (how to author a skill) → `knowledge/authoring-skills.md`.
  - Plan format (numbered tasks + file paths + verify step) → `context/Rules.md`.
  - Actionable code-review findings → `skills/unity-review.md`.
- **Skipped (too heavy for a lean single-project runtime):** subagent-driven-development, dispatching-parallel-agents, git-worktrees, batch executing-plans.

## jahro-console/unity-agent-skills — 2026-07
https://github.com/jahro-console/unity-agent-skills · skills for the Jahro debug console.
- **Adopted:**
  - Evals concept (behavioral test cases) → `evals/`.
  - Migration skill (incremental conversion) → `skills/unity-migrate.md`.
  - Troubleshooting decision-tree thinking → enriched `skills/unity-investigate.md`.
  - Logging severity classification → `context/Conventions.md`.
  - Production-safety discipline (define symbols, no stray debug) → `context/Rules.md`.
- **Skipped:** Jahro-product-specific skills (setup/watcher/snapshots) — not relevant to this project.

## Besty0728/Unity-Skills — 2026-07
https://github.com/Besty0728/Unity-Skills · REST-API engine + 750 skills to control the Unity Editor live.
- **Adopted (patterns, not the engine):**
  - Anti-hallucination `DO NOT` lists per skill → all `skills/*.md`.
  - Anti-patterns catalog idea → `knowledge/anti-patterns.md`.
  - Checkpoint-before-wide-change discipline → `context/Rules.md`.
- **Skipped:** the C#/Python control engine and 750 executable skills — different category (live Editor control), would break this markdown runtime's leanness.

---

## Template for a new entry
```
## <owner/repo> — <YYYY-MM>
<url> · <one-line what it is>
- Adopted: <thing> → <where it landed>.
- Skipped: <thing> (+ why).
```
