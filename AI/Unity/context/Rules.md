# Unity Rules (deltas on top of ../../Core/Rules.md)

- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta` (except to get a GUID), generated folders. Don't recursively grep all of `Assets/` unless a targeted search failed.
- Approval-class additionally: entering Play Mode / Build; renaming `[SerializeField]` fields (breaks serialized data).
- ⚠️ **UnityEvent handlers** (`public void _OnClickX`) resolve by name from scenes/prefabs → "0 references" in code is normal. Never rename/re-sign/delete them as unused. A required rename needs approval + manual re-wiring of every Editor reference.
- Serialized contracts (enum values, `Type:` fields, PopupId, IAP ids): before changing a number, grep `*.asset`, `*.prefab` **and** `*.unity` for usages. Treat as T3.
- New `.md` in a **Plastic SCM** project: ask "add" (tracked) or "private". Private → if root `ignore.conf` already has `*.private`, name it `<name>.md.private`; otherwise ask before editing `ignore.conf`.
- After a code change that needs Editor/Play verification: numbered steps incl. fields to wire in the Inspector.
