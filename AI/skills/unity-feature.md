# Skill: unity-feature

Add a feature by extending existing architecture. Load `../context/Rules.md` + `../context/Conventions.md`.

## Procedure
1. **Scope**: Restate the feature and acceptance criteria. Ask only if blocking.
2. **Find the pattern**: Locate the closest existing implementation to mirror (sibling popup, manager, item). Search by symbol; open one reference example.
3. **Plan minimal change**: Decide which existing files to extend. Prefer editing over new files. Identify base class (e.g. `PopupBase`) and helpers to reuse.
4. **Implement**:
   - Match naming: `_local`, camelCase fields, PascalCase public.
   - Reuse existing managers/services; do not add new patterns.
   - No comments / summaries unless requested.
5. **Logging**: Add `DPDebug` calls per conventions only where they aid debugging (correct color + `[GameObject/tag]` only in brackets).
6. **Wire-up**: Connect serialized fields / events following the existing example.

## Guardrails
- Minimize new files and modified lines.
- Don't refactor surrounding code beyond the feature.

## Output
- Files added/changed with `file:line`.
- Any serialized-field wiring the user must do in the Editor.
