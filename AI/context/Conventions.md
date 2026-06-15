# Conventions

Authoritative coding conventions. Apply on every edit. Match surrounding code first.

## Naming
- Local variables: `_` prefix → `_count`, `_target`, `_rs`
- Private fields: camelCase → `itemPlayerShort`, `popupAirport`
- Public fields / properties: PascalCase → `IsShow`, `MaxLevel`
- `[SerializeField]` fields follow private-field camelCase (with or without explicit `private`).

## Debug Logging
- Prefer `DPDebug.Log(...)` / `DPDebug.LogWarning(...)` / `DPDebug.LogError(...)` (namespace `DP.Utilities` — add `using DP.Utilities;` if missing).
- **Fallback**: if the project has no `DPDebug` class, use `UnityEngine.Debug.Log/LogWarning/LogError` instead. Match whichever the project already uses; never mix the two.
- The color and bracket rules below apply to whichever logger is used.
- Colors via rich text:
  - Normal: `<color=#4aff21>`
  - Warning: `<color=#ffd900>`
  - Error: `<color=#ff3838>`
- `[...]` contains ONLY a GameObject name or tag. Nothing else.
- Detailed values go OUTSIDE the brackets.

Examples:
```csharp
DPDebug.Log($"<color=#4aff21>[{gameObject.name}]</color> loaded level {_level}");
DPDebug.LogWarning($"<color=#ffd900>[{tag}]</color> retry count {_retry}");
DPDebug.LogError($"<color=#ff3838>[{gameObject.name}]</color> null ref on {_field}");
```
Wrong: `DPDebug.Log($"[{gameObject.name} level {_level}]")` — value inside `[]`.

## Code Generation
- No comments unless explicitly requested.
- No XML/`<summary>` docs unless explicitly requested.
- Prefer modifying existing architecture over introducing new patterns.
- Minimize the number of files touched and lines changed.
- Reuse existing managers, base classes (e.g. `PopupBase`), and helpers before adding new ones.

## Project Stack (for editing decisions only)
- Unity C#, Odin Inspector (`Sirenix.OdinInspector`), I2 Localization (`I2.Loc`).
- UI popups extend `PopupBase`.
