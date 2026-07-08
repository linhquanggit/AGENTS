# Knowledge: Project Libraries (Odin, I2 Localization)

Usage conventions for the third-party libraries in the stack (`Conventions.md`). Pull in for tasks touching Inspector tooling or localized text.

## Odin Inspector (`Sirenix.OdinInspector`)
- Use `[Button]` for editor actions, `[ShowInInspector]` to surface non-serialized state, `[ReadOnly]` for debug fields.
- Organize with `[BoxGroup]`, `[FoldoutGroup]`, `[TabGroup]` instead of many headers.
- Editor windows/tools extend `OdinEditorWindow`.
- **DO NOT** put heavy Odin attribute evaluation (`[ShowIf]`, value resolvers) on hot runtime paths — Inspector-only.
- Prefer `[SerializeField] private` + Odin attributes over `public` for serialized data.

## I2 Localization (`I2.Loc`)
- All user-facing text goes through I2 — never hardcode display strings.
- Read via `LocalizationManager.GetTranslation(term)` or a `Localize` component; set via the term, not the literal.
- **DO NOT** concatenate localized fragments (word order differs per language) — use a single term with `{0}` parameters.
- Follow the project's term naming scheme; reuse existing terms before adding new ones.
