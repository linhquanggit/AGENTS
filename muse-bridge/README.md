# Muse Bridge

Web app local điều phối vòng **Claude Code ↔ Muse ↔ Unity**. Chỉ dùng Python chuẩn, chỉ nghe `127.0.0.1`.

## Vòng chạy

```
[+ Task] → muse-cli send "[TASK-001] ..." (không chờ)
      → poll history mỗi 30s → thấy "[DONE-001] ... | commit <sha>"
      → (Auto) git pull --ff-only → claude -p "<review prompt>" (Unity MCP)
      → VERDICT: PASS  → xong
      → VERDICT: FAIL  → gửi "[BUG-001] ..." → Muse fix → [DONE-001] → review lại
      → quá max_fix_attempts → trạng thái "cần Linh"
```
Tắt Auto thì mọi bước do anh bấm tay trong giao diện.

## Cài đặt một lần (Terminal trên Mac)

1. muse-cli (pin version đã kiểm tra):
   ```
   uv tool install muse-cli==0.3.2      # hoặc: pipx install muse-cli==0.3.2
   npm i -g agent-browser
   ```
2. Chrome: mở `chrome://inspect/#remote-debugging` → bật remote debugging → mở https://muse.ai, đăng nhập, để tab mở → `muse-cli auth export` → `muse-cli status`.
3. Claude Code phải dùng được Unity MCP **trong thư mục Unity repo**: `cd <repo> && claude mcp list` phải thấy server Unity. Tên server khác `unityMCP` thì sửa `claude_allowed_tools` trong Cài đặt (vd `mcp__<tên>`).
4. Chạy app: nhấp đúp `start.command` (hoặc `python3 server.py`) → mở http://127.0.0.1:8765.
5. Trong app → **Cài đặt**: Kiểm tra status → Tạo side chat mới (hoặc dán thread ID) → điền đường dẫn Unity repo → Lưu.
6. Bấm **PING** → side chat phải hiện `[DONE-PING] PONG`.

## Lịch sử
Mặc định ở `<repo>/.agent/`:
`state.json`, `events.jsonl`, `tasks/`, `results/`, `bugs/`, `reviews/`.
Muốn commit làm audit trail thì giữ trong repo; không muốn thì thêm `.agent/` vào `.gitignore`.

## Lưu ý
- `claude -p` chạy headless: chỉ dùng được tool có trong `claude_allowed_tools`. Mặc định chỉ đọc + `git` + Unity MCP, không sửa code.
- muse-cli là tool không chính thức; cookie hết hạn thì chạy lại `muse-cli auth export`.
- Mọi tin gửi đi hiện trong side chat Muse dưới tên Linh.
- `config.json` được tạo cạnh `server.py` sau lần chạy đầu.
