# Conventions (Unity)

Apply on every edit. Match surrounding code first.

## ⛔ OneGame: CẤM ODIN
Project có `companyName: OneGame` trong `ProjectSettings/ProjectSettings.asset` (mọi product của công ty, vd `1941 Air Attack` — Events, Main, WorldBoss…): Odin đã gỡ vì bản quyền. KHÔNG `using Sirenix.*`, attribute Odin (`[Button]`, `[ShowIf]`, `[ShowInInspector]`, `[ReadOnly]`, `[BoxGroup]`, `[TableList]`, `[LabelText]`…), `OdinEditorWindow`/`SerializedMonoBehaviour` — kể cả khi copy/khôi phục code cũ, snapshot, knowledge còn dùng. Thay bằng `[ContextMenu]`, `[Header]`, `[SerializeField]`, custom `Editor`/`EditorWindow`. Không đề xuất cài lại Odin. Project ngoài OneGame đang có Odin thì được dùng.

## Naming
- Local variables `_` prefix: `_count`, `_target`. Private fields camelCase. Public fields/properties PascalCase. `[SerializeField]` follows private camelCase.
- `public void _OnClickX` = UnityEvent handler wired in the Editor — never rename/delete as unused (see Rules.md).

## Logging
- `DPDebug` (`using DP.Utilities;`) if the project has it, else `Debug`; never mix. Only `.Log(...)` — never `LogWarning`/`LogError`; severity by color: normal `#4aff21` (by-design gate), warning `#ffd900` (recoverable skip/retry), error `#ff3838` (breaking null/missing ref).
- `[...]` holds ONLY a GameObject name or tag; values go outside. Message in English.
  `DPDebug.Log($"<color=#ff3838>[{gameObject.name}]</color> null ref on {_field}");`

## Coroutines (default for time-based logic; `async`/Addressables only where already used)
- Keep a `Coroutine` handle; stop the old one before starting (`StopCoroutine(handle)`, not by name / `StopAllCoroutines`).
- Dies on `SetActive(false)`/Destroy, not on `enabled = false`; never start on an inactive or pooled/temp object.
- `WaitForSecondsRealtime` when `timeScale = 0`; cache reused `WaitForSeconds`; `yield return null` (never `0`).
- Uncaught exception silently kills it; no `yield` inside `try` with `catch`.

## Code generation
- No comments of any kind unless asked: `//`, `/* */`, `///`/XML docs, `[Tooltip]`, descriptive `[Header]`, markers like `// [G8]`, `// TEMP`. Rewriting a file removes old comments in the rewritten part. Plans/prompts must not ask for comments (Linh chốt 2026-10-16).
- Extend existing architecture; fewest files/lines; reuse managers, base classes (`PopupBase`), helpers.
- Stack: Unity C#, I2 Localization (`I2.Loc`); UI popups extend `PopupBase`.
