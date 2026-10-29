---
name: muse-collab
description: Phối hợp với Muse (Phoenix — agent trên muse.ai) qua Musude. Dùng khi Linh giao một mục tiêu (Unity, app, tool, asset…), một plan của Musude, hoặc nhắc tới Muse/Phoenix; Claude tự điều phối, tự quyết việc nào mình làm, việc nào nhờ Muse.
---

# Phối hợp Claude ↔ Muse

Claude là người điều phối và chịu trách nhiệm kết quả. Linh giao **mục tiêu hoặc plan**, không giao từng bước.
Không phân công cố định: mỗi bước, chọn bên làm nhanh và chắc hơn.

## Nền tảng
- **Đích (project)** = thư mục Claude Code đang chạy. Musude chạy plan thì đặt sẵn `MUSE_PROJECT` = dòng `project:` của plan; `muse-ask` tự gắn đích này vào task. Tự chạy ngoài plan: Unity được tự nhận (thư mục có `Assets/`+`ProjectSettings/`), đích khác dùng `muse-ask --project <path>`.
- Đích có thể là project Unity (mặc định `~/wkspaces/WB`, Unity 2021.3, Plastic) hoặc app/tool bất kỳ (vd `~/AGENTS/muse-bridge` = app Musude).
- **Muse ghi file trực tiếp lên máy Linh** qua app Muse desktop. Muse KHÔNG thấy Unity Editor, không chạy được lệnh trên máy Linh.
- Kiến thức Unity: `/Users/mobione/AGENTS/AI/Unity/knowledge/` (`learnings/INDEX.md`, `snapshots/INDEX.md`). Grep INDEX theo từ khoá, chỉ mở entry khớp. Muse cũng đọc được.

## Ai làm gì (mặc định, được phép đổi)
| Claude tự làm | Nhờ Muse |
|---|---|
| Unity Editor qua MCP: import, prefab, animator, scene, config, refresh, console, test | Code/logic dài, nhiều file; sửa code theo log lỗi rõ |
| Thao tác máy: di chuyển/đổi tên/chuyển đổi file (sips, iconutil…), sửa cấu hình app (Info.plist…), build, chạy | Tạo ảnh/asset theo spec, văn bản/tài liệu |
| Kiểm chứng: compile, chạy, chụp màn hình (`screencapture`) rồi xem ảnh | Đọc nhiều file kiến thức rồi áp dụng |
**Token Claude là thứ đắt nhất; Muse dư token.** Mặc định giao Muse mọi việc Muse làm được: code, đọc/điều tra code (dò caller, GUID, grep), tài liệu, báo cáo, library/brief, tự review code của chính nó. Claude chỉ làm việc Muse KHÔNG làm được: Unity Editor qua MCP, chạy/kiểm chứng trên máy, chuyển/chuyển đổi file. Claude tự viết chỉ khi việc ≤10 dòng hoặc Muse BLOCKED/TIMEOUT — ghi lý do bằng `muse-ask --log`.

## Giữ ngữ cảnh Claude mỏng (bắt buộc)
- Điều phối plan: mỗi task giao 1 subagent (Agent tool) với brief ngắn (plan id, Tn, "dùng skill muse-collab", đọc `muse-ask --plan <id> --task Tn --show`, làm + verify + `--status`, trả ≤10 dòng). Không tự đọc lại thứ subagent đã đọc.
- Không `--show --full`; đọc 1 task bằng `--task Tn --show`.
- Không mở file dài để "hiểu bối cảnh" — hỏi Muse tóm tắt (Muse trả lời ≤15 dòng, kèm file:line).
- Output tool phải ngắn: Read theo đoạn (offset/limit), Bash kèm `| head`/grep, `read_console` chỉ lọc error, không dump hierarchy/scene.
- Gộp lệnh: `--status` kèm ghi chú đủ, không thêm `--log` trùng nội dung.
- Review code Muse: Muse tự review theo Conventions trước khi DONE; Claude chỉ kiểm compile + done_when, không đọc diff trừ khi verify FAIL.
- Model subagent (tham số `model` của Agent): `haiku` cho việc vặt (cập nhật status, kiểm file có/kích thước, chuyển file, chạy unity-errors, chờ Muse); `sonnet` cho task có thao tác Unity MCP hoặc verify khó; không dùng `opus` trừ khi Linh yêu cầu.
- Plan có dòng `approvals:`/`test:` ở đầu → đó là quyết định Linh đã chốt sẵn (được lưu scene/prefab nào, cách spawn để test): làm theo, không hỏi lại.
- Lệnh bị plan-guard chặn (deny) → không tìm cách khác; `--status blocked` kèm câu hỏi cụ thể rồi làm task khác.

## Gọi Muse
`muse-ask` (timeout Bash 600000ms):
```
muse-ask "<task>"                       # id tự sinh C01…
muse-ask --bug C03 "<lỗi + log + file:line>"
muse-ask --reply C03 "<trả lời ASK-CLAUDE>"
muse-ask --wait C03 --since <SEQ>      # khi TIMEOUT
```
**1 task = 1 kết quả đúng như plan.** Cần điều tra (dò GUID, caller, grep) → giao Muse thành task riêng trả kết quả ngắn; chậm hơn nhưng tiết kiệm token Claude. Claude chỉ tự Grep khi 1–2 lệnh là ra.
Task gửi Muse phải ghi: mục tiêu, file được phép tạo/sửa, file kiến thức cần đọc (đường dẫn), quy ước, tiêu chí xong. Task tạo file/asset thêm: `outputs` (đường dẫn · định dạng · thông số), `handoff`, và yêu cầu Muse báo đã tạo bằng cách nào.

Đọc `STATUS:`
- `DONE` → nhận file (xem "Bàn giao file") → kiểm chứng theo `verify` → bước kế.
- `BLOCKED` → bổ sung thông tin, hoặc tự làm / đổi cách (vd Muse không tạo được ảnh → Claude chọn cách khác: vẽ SVG rồi chuyển PNG, `generate_image` của Unity MCP nếu có, hoặc hỏi Linh nguồn ảnh).
- `ASK_CLAUDE` → làm phần Muse nhờ → `--reply`.
- `TIMEOUT` → `--wait`; quá 30 phút báo Linh.

## Bàn giao file (handoff)
- `muse-writes`: kiểm file ở `outputs` có thật, đúng định dạng/kích thước (`file`, `sips -g pixelWidth -g pixelHeight`, compile…).
- `claude-moves <nguồn> → <đích>`: Claude lấy file từ nơi Muse báo (đường dẫn trên máy hoặc link — tải về cần Linh duyệt), chuyển đổi nếu cần, đặt vào đích. Không ghi đè file có sẵn mà không giữ bản cũ (đổi tên `.bak`).
- Muse báo file nằm chỗ khác plan → Claude tự chuyển vào đúng đích, ghi `--log`.

## Kiểm chứng (verify)
- `compile` (Unity): `~/AGENTS/muse-bridge/bin/unity-errors --mark` → refresh_unity → đợi compile → `~/AGENTS/muse-bridge/bin/unity-errors` (1 dòng). Chỉ dùng read_console khi cần log runtime.
- `run`: chạy app/lệnh, xem output/log.
- `visual`: chụp màn hình (`screencapture -x ~/AGENTS/muse-bridge/.agent/shots/<plan>-<tên>.png` hoặc vùng cụ thể) → đọc ảnh → đối chiếu; ghi đường dẫn vào `--log`. ⛔ KHÔNG lưu ảnh trong project (kể cả qua tool MCP lưu vào `Assets/Screenshots`) — chuyển ra ngay và xoá thư mục + `.meta` vừa tạo. Không giao Muse việc chụp lưu vào project.
- `file`: tồn tại + định dạng + thông số.
FAIL → `--bug` cho Muse kèm bằng chứng (log/ảnh); tối đa 3 lần cùng lỗi, sau đó tự sửa hoặc báo Linh.

## Tránh giẫm chân
- Một file chỉ một bên sửa tại một thời điểm. Đang chờ Muse thì Claude không đụng file đã giao Muse.
- Muse không sửa `.unity`/`.prefab`/`.asset`, bundle app hay file hệ thống trừ khi task ghi rõ.

## Chạy plan
Plan ở `~/AGENTS/muse-bridge/plans/<id>.md`, task dạng `## Tn — tiêu đề`.
- Đọc: `muse-ask --plan <id> --show` (bản gọn: tiêu đề + trạng thái); làm task nào đọc `--task Tn --show`. Bỏ qua task `done`.
- **Chạy lại/tiếp tục sau khi bị dừng**: task `doing` KHÔNG làm lại ngay — kiểm trước: `muse-ask --history 10` (chat của plan) xem Muse đã `[DONE-<id>]` chưa, và file trong `outputs` đã có chưa. Đã xong → dùng kết quả rồi verify; Muse đang làm → `muse-ask --wait <id>`; chưa từng gửi → làm tiếp. Không gửi trùng task cho Muse.
- `verified:` = đã dò sẵn lúc lập plan → dùng thẳng, KHÔNG điều tra lại; chỉ kiểm lại khi thực tế mâu thuẫn và ghi chênh lệch bằng `--log`.
- `owner: muse` → BẮT BUỘC giao Muse qua `muse-ask`. `owner: claude` → Claude làm. `owner: any` → Claude chọn, ưu tiên Muse nếu là tạo nội dung.
- `approval: Linh` → dừng hỏi Linh trước đúng bước đó.
- Trạng thái: `muse-ask --plan <id> --task Tn --status doing|done|blocked "ghi chú"`. Task chỉ `done` khi `verify` đạt.
- Plan thiếu thông tin → `blocked` kèm câu hỏi cụ thể, làm tiếp task độc lập khác.
- **Gặp Error/Exception khi Play (Linh chốt 2026-10-29)**: mỗi lần vào Play phải kiểm console lọc error (`read_console` types=error, count nhỏ) ngay sau khi chạy; có Error/Exception mới → THOÁT Play NGAY (`manage_editor stop`), không chạy tiếp, không hack in-memory để chạy qua; ghi `--status blocked` kèm 1–2 dòng lỗi `file:line`. Không bao giờ set `blocked`/kết thúc lượt khi Editor còn đang Play.
- **Hoàn tất plan**: mọi task `done` → `muse-ask --log "<tóm tắt kết quả>" --id <plan> --kind verdict` → báo Linh: Claude làm gì, Muse làm gì, file nào đổi, đã verify gì (kèm ảnh nếu visual), còn gì chưa verify.

## Ghi hoạt động
Mốc quan trọng Claude tự làm: `muse-ask --log "<việc + kết quả>" --id <id> --kind <plan|unity|file|test|verdict>`.

## Điểm dừng (hỏi Linh trước)
Xoá file/asset; sửa scene chính hoặc prefab dùng chung; đổi public API/serialize field/enum; Play/Build; khởi động lại Dock/Finder hay thay đổi hệ thống; tải file từ internet; mọi việc ngoài mục tiêu.
