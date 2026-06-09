# Skill: unity-feature

Add new functionality by extending existing patterns, not inventing new ones.

## Procedure
1. **Scope & Acceptance**:
   - Clarify what the feature must do and its acceptance criteria. Bound it; ignore unrelated systems.
2. **Find the Pattern to Extend**:
   - Search by symbol/reference for the closest existing system (e.g. a sibling popup extending `PopupBase`, an existing Manager). **DO NOT** scan `Assets/`.
3. **Reuse First**:
   - Reuse base classes, Managers, and helpers. **DO NOT** introduce a new pattern if an established one fits.
4. **Implement Minimally**:
   - Add the fewest new files and lines. Match surrounding naming and structure per `../context/Conventions.md`.
   - Add `DPDebug` logging (correct color + `[]` rules) where it aids debugging.
5. **Editor Wiring**:
   - List any `[SerializeField]` / serialized-field connections the user must wire in the Unity Editor.

## Anti-Hallucination Guardrails
- **DO NOT** invent a new architecture when a sibling/base class already exists — find it first.
- **DO NOT** add libraries or patterns not already present in the project.
- **DO NOT** modify Public APIs, Base Classes, or Singletons without approval (Permission Modes).
- **STOP** and ask if acceptance criteria are ambiguous.

## Output
- **Scope & Acceptance**: Bounded summary.
- **Pattern Reused**: `file:line` of the base class/sibling extended.
- **Changes**: Files added/modified with a one-line rationale each.
- **Editor Wiring**: Serialized-field connections needed.
- **Batch Efficiency**: Brief note on how the feature handles multiple items efficiently.
