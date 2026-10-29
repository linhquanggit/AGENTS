# Unity Rules (deltas on top of ../../Core/Rules.md)

- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta` (except to get a GUID), generated folders. Don't recursively grep all of `Assets/` unless a targeted search failed.
- Approval-class additionally: Build; renaming `[SerializeField]` fields. **Play Mode**: ask in interactive sessions; inside a Musude plan run (env `MUSE_PLAN` set) Play is allowed for verification — always exit Play, never save a scene while playing.
- ⚠️ **UnityEvent handlers** (`public void _OnClickX`) resolve by name from scenes/prefabs → "0 references" in code is normal. Never rename/re-sign/delete them as unused; a required rename needs approval + manual re-wiring.
- Serialized contracts (enum values, `Type:` fields, PopupId, IAP ids): before changing a number, grep `*.asset`, `*.prefab` **and** `*.unity`. Treat as T3.
- Changing a field default in C# does NOT update existing prefabs/scene instances → set the value on the prefab and every scene instance, then read it back. "Sửa rồi mà không ăn" → read the live value first.
- New `.md` in a **Plastic SCM** project: ask "add" or "private" (private → `<name>.md.private` if `ignore.conf` has `*.private`; otherwise ask before editing `ignore.conf`).
- After a code change that needs Editor/Play verification: numbered steps incl. fields to wire in the Inspector.

## Unity MCP — spend few tokens
- ⛔ **Ảnh chụp/capture KHÔNG BAO GIỜ lưu trong project** (kể cả `Assets/Screenshots`, gốc project, thư mục Capture tự tạo): lưu thẳng vào `~/AGENTS/muse-bridge/.agent/shots/` (hoặc scratch/`/tmp`). Tool MCP chỉ lưu được trong project → chuyển ảnh ra ngay, xoá ảnh + `.meta` + thư mục nó vừa tạo. Hook `capture-guard` tự dọn nếu sót, nhưng không được dựa vào nó.
- Compile check: `~/AGENTS/muse-bridge/bin/unity-errors --mark` → `refresh_unity` → `unity-errors` (one line). `read_console` only filtered to errors, small count, for runtime logs.
- Prefer ONE `execute_code` / `batch_execute` that does the whole check and returns a short string over many small calls. Read serialized/runtime values this way, not by reading `.prefab`/`.unity` YAML.
- Never dump whole hierarchies, scenes or component lists — query by name/path, return only the fields needed.
- One-off build/test logic → `execute_code` (no file). A file is unavoidable → `Assets/Editor/<mission>/`, removed in the plan's cleanup task.
- After testing, restore Editor state: exit Play, revert temporary values, camera/layers/selection.
