---
name: ll-bridge-prompt-generate
description: Tạo plan task cho Musude (Muse Bridge) từ knowledge của project hoặc mục tiêu Linh nêu (Unity, app, tool…), ghi vào ~/AGENTS/muse-bridge/plans/ để Claude Code + Muse thực hiện. Dùng khi gọi //ll-bridge-prompt-generate hoặc "tạo task/plan cho bridge".
---

# Tạo plan cho Musude

Linh gọi: `//ll-bridge-prompt-generate <mục tiêu>` (vd "tạo task world boss Rồng Lửa", "tạo icon Dock cho app Musude").
Không hỏi lại trừ khi mục tiêu mơ hồ đến mức không chia được task — khi đó hỏi đúng 1 câu.

## 1. Lấy kiến thức
- Knowledge của project claude.ai hiện tại (Cowork: `project_search` → `project_read` doc khớp; chat thường: tài liệu trong ngữ cảnh) + thư viện local `/Users/mobione/AGENTS/AI/Unity/knowledge/` khi là việc Unity.
- Chỉ dùng điều có thật. Thiếu → "Câu hỏi mở", không bịa tên class/file/số liệu.

## 2. Xác định đích (project)
- Đích là thư mục trên máy Linh mà plan sẽ thay đổi: project Unity (vd `~/wkspaces/WB`) hoặc bất kỳ app/tool nào (vd `~/AGENTS/muse-bridge` cho app Musude).
- Linh nêu đích thì dùng đúng; không nêu và là việc Unity → `~/wkspaces/WB`.

## 3. Dò trước trong đích (bắt buộc, chỉ đọc)
Mục tiêu: task đưa sang chỉ còn THỰC THI — Claude Code và Muse không phải điều tra lại.
- Quyền: xin folder đích (`device_request_folder_access`) một lần; Unity thì có thể dùng Unity MCP chỉ-đọc (`find_in_file`, `manage_asset` search) nếu Editor mở đúng project.
- Dò bằng grep/find/đọc file nhỏ, không đọc cả file lớn. Unity: đường dẫn thật, class + base, signature hàm (`file:line`), field/enum, GUID script → prefab/scene. App/tool: cấu trúc bundle, file cấu hình (vd `Info.plist`, key đang có), chỗ file mới phải nằm, công cụ build có sẵn.
- Ghi vào `verified:` của từng task (kèm `file:line` hoặc đường dẫn). Không dò được → "Câu hỏi mở".
- Không sửa file nào khi dò.

## 4. Chia task
- 3–8 task, mỗi task 1 kết quả kiểm chứng được, sắp theo phụ thuộc. Task chỉ chứa việc THỰC THI.
- `owner` (gợi ý, Claude Code được đổi nếu có lý do):
  - `muse` — tạo nội dung: code, logic, ảnh/asset, văn bản. Muse ghi file trực tiếp lên máy Linh.
  - `claude` — thao tác trên máy và kiểm chứng: Unity Editor qua MCP, di chuyển/chuyển đổi file, cấu hình app, build, cài đặt, test (compile, chạy, chụp màn hình).
  - `any` — Claude Code chọn.
- Task tạo file (code, ảnh, asset) BẮT BUỘC có:
  - `outputs:` từng file — đường dẫn đích, định dạng, thông số (vd `~/AGENTS/muse-bridge/assets/icon-1024.png · PNG 1024×1024 · nền trong suốt`).
  - `handoff:` một trong:
    - `muse-writes` — Muse ghi thẳng vào `outputs` (mặc định khi Muse ghi được vào đích).
    - `claude-moves <nguồn> → <đích>` — Muse để file ở chỗ nó tạo được (hoặc đưa link), Claude tải/di chuyển vào đích. Chọn khi đích nhạy cảm (trong bundle app, thư mục hệ thống) hoặc cần chuyển đổi định dạng sau đó.
  - Ảnh/asset: mô tả đủ để tạo — phong cách, màu, chủ thể, ràng buộc (đọc rõ ở 16px, không chữ nhỏ…), và yêu cầu Muse báo đã tạo bằng cách nào.
- `verify:` nói rõ kiểu: `compile` | `run` | `visual` (chụp màn hình rồi nhìn, ghi cần thấy gì) | `file` (tồn tại, đúng kích thước/định dạng).
- Bước ảnh hưởng hệ thống/ngoài đích (khởi động lại Dock/Finder, cài đặt, xoá) ghi `approval: Linh`.
- Task cuối luôn là kiểm chứng tổng để chốt plan hoàn tất.

## 5. Định dạng file (bắt buộc đúng — Musude parse theo đây)
```
# <Tiêu đề plan>
goal: <1–2 câu>
project: <đường dẫn đích trên máy Linh>
source: <project claude.ai / doc đã dùng, hoặc "yêu cầu của Linh">

## T1 — <tiêu đề task>
owner: claude|muse|any
depends: -            (hoặc T1, T2)
context:
<sự thật then chốt>
verified:
<kết quả dò, kèm file:line / đường dẫn>
refs: <file kiến thức .md nếu có>
outputs: <file · định dạng · thông số>                    (task tạo file)
handoff: muse-writes | claude-moves <nguồn> → <đích>      (task tạo file)
verify: compile|run|visual|file — <cần thấy gì>
approval: Linh                                             (nếu có bước ảnh hưởng hệ thống)
done_when: <tiêu chí>

## T2 — ...

## Câu hỏi mở
- ...
```
Heading task đúng dạng `## T<số> — <tiêu đề>`. Mục "Câu hỏi mở" không bắt đầu bằng T.

## 6. Ghi vào Musude
- Plan id: slug ngắn không dấu (vd `musude-dock-icon`). Trùng tên → thêm `-2`.
- Ghi `~/AGENTS/muse-bridge/plans/<id>.md` (xin quyền `~/AGENTS` nếu chưa có). Không có đường vào máy → xuất file `<id>.md` để Linh tải vào thư mục đó.

## 7. Báo lại (ngắn)
- Plan id, đích, bảng 1 dòng/task (id · tiêu đề · owner · handoff/verify).
- Câu hỏi mở (nếu có).
- Bước tiếp: Musude → tab Kế hoạch → "Chạy plan".

## Ví dụ rút gọn — icon Dock cho Musude
```
# Icon Dock cho app Musude
goal: Musude có icon riêng trên Dock/Finder.
project: ~/AGENTS/muse-bridge

## T1 — Thiết kế ảnh icon
owner: muse
outputs: ~/AGENTS/muse-bridge/assets/icon-1024.png · PNG 1024×1024 · nền trong suốt, bo góc kiểu macOS
handoff: muse-writes
context: Musude = cầu nối Claude ↔ Muse điều phối làm game Unity. Rõ ở 16px, không chữ.
verify: file — PNG 1024×1024 tồn tại

## T2 — Tạo .icns và gắn vào app
owner: claude
depends: T1
verified: Musude.app/Contents/Info.plist chưa có CFBundleIconFile
outputs: Musude.app/Contents/Resources/AppIcon.icns
handoff: claude-moves assets/icon-1024.png → iconset (sips) → iconutil → Resources/AppIcon.icns
verify: file — AppIcon.icns tồn tại; Info.plist có CFBundleIconFile=AppIcon

## T3 — Kiểm tra hiển thị
owner: claude
depends: T2
approval: Linh (làm mới cache icon / khởi động lại Dock)
verify: visual — chụp Dock khi Musude đang mở, thấy icon mới
```
