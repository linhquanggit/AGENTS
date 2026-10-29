# Unity Workflows — routing + contracts

Pick one by intent; default tier in brackets (re-tier per Core §1). General gates come from Core Rules.

- **bug** [T2] — symptom vs expected (1 line each); prove root cause with `file:line` hops (cause ≠ symptom; check `knowledge/anti-patterns.md`: event leaks, Unity fake-null, lifecycle order); smallest fix + cheap guard/log. Output: Symptom/Expected · Call path · Root cause · Fix.
- **brainstorm** [T2] — no code; one batch of design-changing questions → 2–3 options (reuse `PopupBase`/managers first) → recommendation + acceptance criteria → hand off to feature.
- **feature** [T2] — extend the closest sibling/base (cite `file:line`); no new libraries/patterns. Output: changes (file + why) · **Editor wiring** list.
- **refactor** [T2] — dependency map (callers, overrides, serialized refs) + invariants (public API, `[SerializeField]` names, execution order) first; small steps; re-search references per changed symbol; stop if the map is wider than expected.
- **migrate** [T3] — enumerate ALL sites; leaf → shared; old+new work side by side; remove old path last.
- **review** [T2] — changed lines only (>10 files → ask to scope); check Conventions, null/lifecycle/race/unsubscribe, broken callers. Output: `[Blocker|Major|Minor|Nit] file:line — issue → fix`.
- **test** [T1–T2] — EditMode for logic, PlayMode for lifecycle/coroutine/physics; failing test for the right reason first; `Method_Scenario_Expected`; one behavior per test.
- **explain** [T0] — requested scope only, ordered `file:line` hops.
- **optimize** [T2] — needs evidence (profiler, repro, hot loop) or ask; no cold-path micro-opts. Output: bottleneck · root cause · change · risk · expected gain. Build size → `knowledge/addressables.md`.
- **advisory** [T3, no code] — read relevant `knowledge/{architecture,performance,ui-patterns,anti-patterns,libraries}.md`; mobile constraints, no over-engineering, no new third-party libs.
- **perception** [T2] — project health, missing meta/refs, "script này dùng ở đâu" → `skills/unity-perception.md`.
- **learn** [T1] — "ghi nhớ", "lưu lại" → `skills/unity-learn.md`.

Knowledge pointers (open only when relevant): bullets/shoot → `knowledge/snapshots/shoot-templates/00-overview.md`; scroller lists → `knowledge/snapshots/enhancedscroller-architecture.md`; others → grep `knowledge/snapshots/INDEX.md`.
