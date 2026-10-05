# Skill: unity-learn

Save a durable, non-obvious project fact so future sessions skip rediscovery. **Always ask first.**

Trigger: reusable root-cause pattern · hard-to-find location · undocumented convention · user corrected you · you searched repeatedly. Skip trivial/one-off facts.

1. Grep `../knowledge/learnings/INDEX.md`; existing entry → update it instead.
2. Propose one line: `Ghi learning: <title> → knowledge/learnings/<slug>.md? (y/n)` and wait.
3. On yes: write the file + one INDEX line at the top.
4. Project-wide rule? Propose editing `context/` — never edit it automatically.

File `learnings/<slug>.md`:
```
# <title>
Scope: <system/folder> | Evidence: <file:line>

<fact, 1–3 lines; link related entries>
```
INDEX line (keep hook ≤ ~120 chars, it is grepped every task): `- [<title>](<slug>.md) — <hook> (<scope>)`
Large multi-section captures go to `../knowledge/snapshots/`, with one line in `snapshots/INDEX.md`.
