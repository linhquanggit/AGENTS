---
name: ll-bridge-prompt-generate
description: Tạo plan task từ knowledge của project (vd 1941 – World Boss) và ghi vào Muse Bridge (~/AGENTS/muse-bridge/plans/) để Claude Code + Muse thực hiện. Dùng khi gọi //ll-bridge-prompt-generate hoặc "tạo task/plan cho bridge".
---

# Tạo plan cho Muse Bridge

Linh gọi: `//ll-bridge-prompt-generate <mục tiêu ngắn>` (vd "tạo task world boss Rồng Lửa").
Không hỏi lại trừ khi mục tiêu mơ hồ đến mức không chia được task — khi đó hỏi đúng 1 câu.

## 1. Lấy kiến thức
- Nguồn chuẩn là knowledge của project hiện tại (Cowork: `project_search` theo từ khoá mục tiêu, rồi `project_read` các doc khớp; chat thường: tài liệu project trong ngữ cảnh).
- Chỉ dùng điều có trong tài liệu. Thiếu → ghi vào mục "Câu hỏi mở", không bịa tên class/file/số liệu.

## 2. Dò trước trong project (bắt buộc, chỉ đọc)
Mục tiêu: task đưa sang chỉ còn THỰC THI — Claude Code và Muse không phải điều tra lại.
- Quyền: xin folder project Unity (vd `~/wkspaces/WB`, `device_request_folder_access`) một lần; hoặc dùng Unity MCP chỉ-đọc (`find_in_file`, `manage_asset` search, `find_gameobjects`) nếu Editor đang mở đúng project.
- Dò bằng grep/find (rẻ, nhanh), không đọc cả file lớn: đường dẫn file thật, tên class + base class, signature hàm cần override (kèm `file:line`), field/enum liên quan, GUID script (`.meta`) → prefab/scene nào tham chiếu, asset tồn tại hay không.
- Mỗi task ghi kết quả dò vào mục `verified:` (sự thật đã kiểm, có `file:line`). Không dò được → ghi vào `Câu hỏi mở`, không đoán.
- Tuyệt đối không sửa file nào khi dò.

## 3. Chia task
- 3–8 task, mỗi task 1 kết quả kiểm chứng được. Sắp theo thứ tự phụ thuộc.
- `owner` là gợi ý: `claude` (việc trong Unity Editor qua MCP: asset, prefab, animator, scene, config, test), `muse` (script C#, logic — Muse sửa file trực tiếp trong WB), `any`.
- Task chỉ chứa việc THỰC THI (sửa/tạo/test). Không đưa việc điều tra vào task.
- `context`: các sự thật then chốt (tên class, enum, số liệu, công thức, quy ước) — ngắn gọn, đúng nguyên văn.
- `refs`: đường dẫn file kiến thức local thay vì chép dài. Thư viện trên máy Linh: `/Users/mobione/AGENTS/AI/Unity/knowledge/` (`snapshots/worldboss-*`, `learnings/worldboss-*`, `INDEX.md`). Nếu đọc được folder này thì chọn đúng file; không thì ghi tên tài liệu nguồn. Cả Claude Code và Muse đều đọc được các file này.
- Project Unity: ghi vào dòng `project:` của plan. Linh nêu project trong lệnh thì dùng đúng đường dẫn đó; không nêu → `~/wkspaces/WB`. Không đổi tên file/enum/serialize field có sẵn trừ khi tài liệu yêu cầu.

## 4. Định dạng file (bắt buộc đúng — Bridge parse theo đây)
```
# <Tiêu đề plan>
goal: <1–2 câu>
project: <đường dẫn project Unity trên máy Linh, vd ~/wkspaces/WB>
source: Project <tên> — <doc đã dùng>

## T1 — <tiêu đề task>
owner: claude|muse|any
depends: -            (hoặc T1, T2)
context:
<sự thật then chốt từ tài liệu>
verified:
<kết quả dò trong project, mỗi dòng kèm file:line — vd "EnemyWorldBoss.cs:48 public override void Init(Node _node = null, …)", "EnemyWB5_P2.cs guid a1b2… dùng trong WB5_Phase2.prefab">
refs: <đường dẫn file .md trong AGENTS/AI/Unity/knowledge>
files: <file/đường dẫn sẽ tạo hoặc sửa>
done_when: <tiêu chí kiểm chứng: compile 0 error, test X pass, prefab chạy trong scene test…>

## T2 — ...

## Câu hỏi mở
- ...
```
Task heading phải đúng dạng `## T<số> — <tiêu đề>`. Mục "Câu hỏi mở" không bắt đầu bằng T.

## 5. Ghi vào Bridge
- Plan id: slug ngắn không dấu, vd `wb-boss-dragon`. Không ghi đè plan cùng tên đã có — thêm hậu tố `-2`.
- Có quyền vào máy Linh (folder `AGENTS` đã connect, hoặc tool device): ghi `~/AGENTS/muse-bridge/plans/<id>.md`. Nếu folder chưa connect: xin quyền `~/AGENTS` (device_request_folder_access) một lần.
- Không có đường vào máy: xuất file `<id>.md` để Linh tải và bỏ vào thư mục trên.

## 6. Báo lại (ngắn)
- Plan id, số task, bảng 1 dòng/task (id · tiêu đề · owner · depends).
- Câu hỏi mở (nếu có).
- Bước tiếp: mở Muse Bridge → tab Kế hoạch → "Chạy trong Terminal".
