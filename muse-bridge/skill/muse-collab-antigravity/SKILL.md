---
name: muse-collab
description: Phối hợp với Muse (Phoenix — coding agent trên muse.ai) qua muse-cli. Dùng khi Linh giao một mục tiêu Unity/app/tool/asset, một plan trong ~/AGENTS/muse-bridge/plans/, hoặc nhắc tới Muse/Phoenix/muse-ask.
---

# Antigravity ↔ Muse

Antigravity là người điều phối và chịu trách nhiệm kết quả. Linh giao **mục tiêu hoặc plan**, không giao từng bước.
Mỗi bước chọn bên làm nhanh và chắc hơn; ưu tiên đẩy việc tạo nội dung (code dài, asset, tài liệu) sang Muse.

## Lệnh gọi Muse — LUÔN dùng đường dẫn tuyệt đối + `--agent antigravity`
```
M=/Users/mobione/AGENTS/muse-bridge/bin/muse-ask
$M --agent antigravity "<task>"                       # id tự sinh A01, A02…
$M --agent antigravity --bug A03 "<lỗi + log + file:line>"
$M --agent antigravity --reply A03 "<trả lời ASK-CLAUDE>"
$M --agent antigravity --wait A03 --since <SEQ>       # khi TIMEOUT
$M --agent antigravity --history 10
$M --agent antigravity --log "<việc + kết quả>" --id A03 --kind unity|file|test|verdict
```
- Chạy trong thư mục project (vd `~/wkspaces/WB`) để muse-ask tự nhận project Unity; project khác: thêm `--project <path>`.
- Lệnh chờ lâu (mặc định tới 540s). Chạy lệnh ở chế độ nền/không chặn rồi kiểm trạng thái lệnh định kỳ; KHÔNG kill lệnh khi chưa có dòng `STATUS:`. Muốn ngắn hơn: `--timeout 300`.
- Không gửi trùng: trước khi gửi lại task đã từng gửi, xem `--history 10`.
- Muse dùng thẻ cũ `[ASK-CLAUDE-<id>]` nghĩa là "hỏi người điều phối" (tức Antigravity).

Đọc dòng đầu `STATUS:`
- `DONE` (exit 0) → kiểm file ở `files:` có thật → verify → bước kế.
- `BLOCKED` (exit 1) → bổ sung thông tin, hoặc tự làm / đổi cách.
- `ASK_CLAUDE` (exit 3) → làm phần Muse nhờ → `--reply`.
- `TIMEOUT` (exit 2) → `--wait <id> --since <SEQ>` (SEQ in sẵn ở output); quá 30 phút báo Linh.
- `ERROR` (exit 4): `401/unauthorized` = cookie `hatch_sess` hết hạn → báo Linh copy lại từ Edge; `404 session not found` = side chat mất → báo Linh tạo side chat mới.

## Nền tảng
- Muse ghi file trực tiếp lên máy Linh qua app Muse desktop; Muse KHÔNG thấy Unity Editor, không chạy lệnh trên máy.
- Project Unity mặc định `~/wkspaces/WB` (Unity 2021.3, Plastic SCM, không phải git). WB: đọc `~/wkspaces/Brain/BR_WorldBoss/AGENT.md` trước.
- Kiến thức Unity: `/Users/mobione/AGENTS/AI/Unity/knowledge/` (grep INDEX.md theo từ khoá, chỉ mở entry khớp). Muse cũng đọc được.
- Mọi tin gửi đi hiện trong side chat Muse dưới tên Linh.

## Ai làm gì
| Antigravity tự làm | Nhờ Muse |
|---|---|
| Tra cứu (grep, đọc file) rồi đưa kết quả vào task | Code/logic dài, nhiều file; sửa code theo log lỗi rõ |
| Thao tác máy: di chuyển/đổi tên/chuyển đổi file, build, chạy lệnh | Tạo ảnh/asset theo spec, văn bản/tài liệu |
| Unity Editor (nếu Antigravity có Unity MCP), kiểm chứng | Đọc nhiều file kiến thức rồi áp dụng |
Tự viết code chỉ khi việc rất nhỏ (≤10 dòng) hoặc Muse BLOCKED/TIMEOUT — ghi lý do bằng `--log`.

**1 task = 1 kết quả.** Task phải ghi: mục tiêu, file được phép tạo/sửa, file kiến thức cần đọc (đường dẫn), quy ước, tiêu chí xong; task tạo file thêm `outputs` (đường dẫn · định dạng · thông số). Không bắt Muse dò cả project (rất chậm).

## Kiểm chứng
- `compile` (Unity): có Unity MCP → refresh_unity + read_console. Không có → nhờ Linh focus cửa sổ Unity (để Unity tự refresh), rồi:
  `grep -E "error CS[0-9]+" ~/Library/Logs/Unity/Editor.log | tail -20` (0 dòng mới = đạt).
- `run`: chạy lệnh/app, xem output. `visual`: `screencapture -x <file>` rồi xem ảnh. `file`: tồn tại + định dạng + kích thước (`file`, `sips -g pixelWidth -g pixelHeight`).
- FAIL → `--bug` kèm bằng chứng; tối đa 3 lần cùng lỗi, sau đó tự sửa hoặc báo Linh.

## Tránh giẫm chân
- Một file chỉ một bên sửa tại một thời điểm; đang chờ Muse thì không đụng file đã giao.
- Muse không sửa `.unity`/`.prefab`/`.asset` trừ khi task ghi rõ.
- Claude Code và Antigravity dùng chung side chat: không chạy cả hai cùng một plan.

## Chạy plan
Plan ở `~/AGENTS/muse-bridge/plans/<id>.md`, task dạng `## Tn — tiêu đề`.
- Đọc: `$M --agent antigravity --plan <id> --show`. Bỏ qua task `done`.
- Plan có side chat riêng (`plans/<id>.thread.json`): thêm `--plan <id>` vào lệnh gửi/chờ để dùng đúng thread.
- Task `doing` (bị dừng giữa chừng): kiểm `--history 10` và file `outputs` trước; đã `[DONE-<id>]` → verify; Muse đang làm → `--wait`; chưa gửi → làm tiếp.
- `owner: muse` → bắt buộc giao Muse. `owner: claude`/`any` → Antigravity làm hoặc chọn. `approval: Linh` → dừng hỏi Linh.
- Trạng thái: `$M --agent antigravity --plan <id> --task Tn --status doing|done|blocked "ghi chú"`; chỉ `done` khi verify đạt.
- Xong plan: `--log "<tóm tắt>" --id <plan> --kind verdict` → báo Linh: ai làm gì, file nào đổi, đã verify gì, còn gì chưa verify.

## Điểm dừng (hỏi Linh trước)
Xoá file/asset; sửa scene chính hoặc prefab dùng chung; đổi public API/serialize field/enum; Play/Build; thay đổi hệ thống; tải file từ internet; việc ngoài mục tiêu.
