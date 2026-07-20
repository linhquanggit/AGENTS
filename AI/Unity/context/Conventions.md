# Conventions

Authoritative coding conventions. Apply on every edit. Match surrounding code first.

## Naming
- Local variables: `_` prefix → `_count`, `_target`, `_rs`
- Private fields: camelCase → `itemPlayerShort`, `popupAirport`
- Public fields / properties: PascalCase → `IsShow`, `MaxLevel`
- `[SerializeField]` fields follow private-field camelCase (with or without explicit `private`).
- **Public methods with `_` prefix** (e.g. `_OnClickPlay`, `_OnToggleSound`) are **UnityEvent handlers wired in the Editor** (Button.onClick, Toggle, EventTrigger, Animation Event, etc.). They usually show **no code references** because the call lives in a scene/prefab, not in C#. ⚠️ **Never rename, change the signature of, or delete them based on "0 references"** — doing so silently breaks the Editor wiring (the event resolves the handler by name and drops it). See [Rules.md](Rules.md) → Editing.

## Debug Logging
- Use `DPDebug` if the project has it (namespace `DP.Utilities` — add `using DP.Utilities;` if missing); otherwise fall back to `UnityEngine.Debug`. Match whichever the project already uses; never mix the two.
- Use the `.Log(...)` method ONLY — never `LogWarning` / `LogError` (applies to both DPDebug and Debug). Express severity through the text color below.
- Colors via rich text:
  - Normal: `<color=#4aff21>`
  - Warning: `<color=#ffd900>`
  - Error: `<color=#ff3838>`
- Severity: error (#ff3838) = breaking null/missing ref; warning (#ffd900) = recoverable skip/retry or abnormal gate; normal (#4aff21) = by-design gate (locked, condition not met).
- `[...]` contains ONLY a GameObject name or tag. Nothing else.
- Detailed values go OUTSIDE the brackets.
- Log message text is in **English** by default.

Examples (`.Log` only, severity by color):
```csharp
DPDebug.Log($"<color=#4aff21>[{gameObject.name}]</color> loaded level {_level}");
DPDebug.Log($"<color=#ffd900>[{tag}]</color> retry count {_retry}");
DPDebug.Log($"<color=#ff3838>[{gameObject.name}]</color> null ref on {_field}");
```
No DPDebug → same rules with `Debug.Log`:
```csharp
Debug.Log($"<color=#ff3838>[{gameObject.name}]</color> null ref on {_field}");
```
Wrong: `DPDebug.LogError(...)` / `Debug.LogWarning(...)` — use `.Log` + color. Wrong: value inside `[]`.

## Code Generation
- No comments unless explicitly requested.
- No XML/`<summary>` docs unless explicitly requested.
- Prefer modifying existing architecture over introducing new patterns.
- Minimize the number of files touched and lines changed.
- Reuse existing managers, base classes (e.g. `PopupBase`), and helpers before adding new ones.

## Project Stack (for editing decisions only)
- Unity C#, Odin Inspector (`Sirenix.OdinInspector`), I2 Localization (`I2.Loc`).
- UI popups extend `PopupBase`.
