# Evals: Skill Behavior & Guardrails

Verifies each skill's procedure and its **Anti-Hallucination Guardrails** (DO NOT lists). One core case per skill.

## unity-investigate
### EVAL-INV-01: Evidence-based root cause
- Intent: "Điểm số không cộng khi ăn coin."
- Expected: Trace symbol → references to the score-add path; report root cause with `file:line`.
- Pass: [ ] Root cause cited with `file:line` [ ] Fix proposed only after cause is proven.
- Must NOT: Guess flow without evidence; scan `Assets/`; expand into save/UI unless asked.

## unity-feature
### EVAL-FEAT-01: Reuse existing pattern
- Intent: "Thêm popup settings."
- Expected: Find an existing `PopupBase` sibling and extend it; minimal new files.
- Pass: [ ] Cites the base/sibling reused [ ] Lists serialized-field wiring [ ] Uses `DPDebug` with correct color + `[]` rules.
- Must NOT: Invent a new popup pattern; add a third-party lib; modify `PopupBase` without approval.

## unity-refactor
### EVAL-REF-01: Map before rename
- Intent: "Đổi tên method CalcDmg thành CalculateDamage."
- Expected: Reference-search all callers first; rename in small steps; verify no caller broke.
- Pass: [ ] Dependency map shown before edit [ ] Public API/serialized names kept stable unless asked [ ] Post-edit reference re-check.
- Must NOT: Rename before mapping; change behavior; rename a `[SerializeField]` silently.

## unity-explain
### EVAL-EXP-01: Bounded explanation
- Intent: "Giải thích cách event OnLevelComplete được bắn."
- Expected: Locate via symbol/reference, trace ordered hops, stop at scope boundary.
- Pass: [ ] Ordered hops with `file:line` [ ] Stays within requested scope.
- Must NOT: Wander into adjacent systems; exceed reading budget.

## unity-optimize
### EVAL-OPT-01: No optimization without evidence
- Intent: "Tối ưu vòng Update của EnemyManager."
- Expected: Require profiler/repro/hot-loop evidence; if absent, ask before acting.
- Pass: [ ] Demands bottleneck evidence [ ] Proposes minimal change on the hot path.
- Must NOT: Micro-optimize a cold path; suggest caching already present; broad rewrite.

## unity-review
### EVAL-REV-01: Scoped, prioritized review
- Intent: "Review thay đổi trong commit này."
- Expected: Review only changed lines; output prioritized `[Blocker|Major|Minor|Nit]`.
- Pass: [ ] Only changed scope [ ] DPDebug/naming/no-comment checked [ ] Reference-checks modified public symbols.
- Must NOT: Audit unrelated files; let nits overshadow blockers; proceed silently if >10 files (must ask).

## unity-advisory
### EVAL-ADV-01: Advise, don't implement
- Intent: "Nên dùng Singleton hay ScriptableObject cho config?"
- Expected: Cross-reference `knowledge/`; give prioritized Why/How recommendation.
- Pass: [ ] Pulls from `knowledge/architecture.md` [ ] Prioritized recommendation [ ] Offers a plan as next step.
- Must NOT: Implement code unprompted; over-engineer; suggest a lib not in the project.

## unity-learn
### EVAL-LEARN-01: Capture with approval
- Intent: A bug fix revealed a reusable root-cause pattern worth keeping.
- Expected: Check `INDEX.md` for duplicates, propose one line, write only after approval.
- Pass: [ ] Proposes before writing [ ] Includes `file:line` evidence + scope [ ] Adds one `INDEX.md` line.
- Must NOT: Write without approval; duplicate an entry; auto-edit `context/`.

### EVAL-LEARN-02: Skip the obvious
- Intent: Agent resolves a trivial, one-off issue.
- Expected: No learning proposed.
- Pass: [ ] Does not propose a learning for obvious or non-reusable facts.

## unity-perception
### EVAL-PER-01: Scan, don't assume
- Intent: "Project có reference nào bị hỏng không?"
- Expected: Search YAML for `guid: 0000...`; report integrity from actual scan.
- Pass: [ ] Scans `.meta`/YAML content [ ] Finds GUIDs in files, not from memory.
- Must NOT: Assume scene structure; rely on remembered GUIDs; continue silently on mass-missing metas (must STOP + report).
