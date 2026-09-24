# AGENTS.md — Runtime Selector

Multi-profile AI runtime for all coding agents (Claude Code, Gemini CLI, Codex, others). Each profile under `AI/<Profile>/` is a **self-contained runtime** (its own `AGENTS.md` + `context/` + `skills/` + `knowledge/` + `evals/`). This root file only selects one profile and defers to it.

## Language Policy (all profiles)
- Respond in **Vietnamese** by default, regardless of the input language.
- Keep unchanged: code, APIs, class / method / file / package names, namespaces, logs, technical identifiers.
- Technical terms may stay in English when translating reduces clarity.
- Honor an explicit request for another language.

## Request Template (all profiles)
User requests follow [REQUEST_TEMPLATE.md](REQUEST_TEMPLATE.md) — fields `[LOẠI]`, `[MỤC TIÊU]`, `[BỐI CẢNH]`, `[REPRO]`, `[PHẠM VI]`, `[RÀNG BUỘC]`, `[VERIFY]`, `[QUYỀN]`, `[OUTPUT]`.
- A request in template form is authoritative: `[LOẠI]` selects the skill (skip the router's inference), `[PHẠM VI]` bounds every file read and edit, `[QUYỀN]` overrides the default permission mode, `[OUTPUT]` decides plan-first vs. code-now.
- A free-form request: map it to the template, state any field you had to assume, and confirm before editing code. Skip the mapping for trivial or read-only asks.
- Never widen `[PHẠM VI]` on your own — ask first.

## Flow Capture File (all profiles)
- File cố định, dùng chung cho MỌI loại project (Unity, Common, hay bất kỳ profile nào sau này): `~/.claude/logs/flow-capture.log` (nằm ngoài mọi git repo — một file duy nhất, không phân theo project/profile).
- Khi được yêu cầu "log để test luồng / check bug": clear (truncate) file này trước khi bắt đầu, rồi đọc lại sau khi user reproduce xong — thay vì xin log device/console/terminal.
- Ghi gì vào file, ghi bằng cách nào là việc riêng của từng project (code do user tự viết) — quy ước này chỉ định nghĩa vị trí + vòng đời (clear-before-use) của file, không định nghĩa cơ chế viết.

## Reports Folder (all profiles)
- Thư mục cố định, dùng chung cho MỌI project, nằm trong repo AGENTS này (KHÔNG nằm trong SCM riêng của từng project) — dùng khi project đó có connect SCM và không muốn người khác đọc được tiến trình/báo cáo công việc: [Reports/](Reports/INDEX.md), tổ chức theo `Reports/<tên-project>/`.
- `<tên-project>` = tên thư mục gốc của project đang làm việc (basename của working directory), tự suy ra, không hỏi lại trừ khi không xác định được.
- Khi được yêu cầu "lưu báo cáo / ghi lại tiến trình đã làm" cho project đó: tạo file `Reports/<tên-project>/<YYYY-MM-DD>-<slug>.md` tóm tắt phần đã làm — không viết report này vào trong chính project đó.
- Sau khi tạo, thêm 1 dòng trỏ tới file mới vào `Reports/INDEX.md` (cùng format với các INDEX khác trong repo: `- [title](path) — hook`).

## Profile Selection
Pick the profile matching the project, then read that profile's `AGENTS.md` and follow it:
- **Unity** (`AI/Unity/`) — if the project is a Unity C# project (has `Assets/` + `ProjectSettings/`).
- **Common** (`AI/Common/`) — the language-agnostic default for any other project.

Add a sibling folder per new domain; each mirrors the same layout.

## Verification
After loading the active profile's three `context/` files, prepend your **first response of the session** with `[AI_RUNTIME_LOADED · global · <Profile>]` — where `<Profile>` is `Unity` or `Common` (confirms the global runtime was read). Once per session, verification only.
