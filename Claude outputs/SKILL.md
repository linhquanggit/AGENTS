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

## 2. Chia task
- 3–8 task, mỗi task 1 kết quả kiểm chứng được. Sắp theo thứ tự phụ thuộc.
- `owner` là gợi ý: `claude` (việc trong Unity Editor qua MCP: asset, prefab, animator, scene, config, test), `muse` (script C# dài, logic, asset ảnh nếu làm được), `any`.
- `context` phải TỰ ĐỦ: chép nguyên văn phần kiến thức liên quan (đường dẫn, tên class, enum, số liệu, công thức, quy ước). Người làm không đọc được project này.
- Project Unity: `~/wkspaces/WB` (Unity 2021.3). Không đổi tên file/enum/serialize field có sẵn trừ khi tài liệu yêu cầu.

## 3. Định dạng file (bắt buộc đúng — Bridge parse theo đây)
```
# <Tiêu đề plan>
goal: <1–2 câu>
source: Project <tên> — <doc đã dùng>

## T1 — <tiêu đề task>
owner: claude|muse|any
depends: -            (hoặc T1, T2)
context:
<kiến thức chép từ tài liệu>
files: <file/đường dẫn sẽ tạo hoặc sửa>
done_when: <tiêu chí kiểm chứng: compile 0 error, test X pass, prefab chạy trong scene test…>

## T2 — ...

## Câu hỏi mở
- ...
```
Task heading phải đúng dạng `## T<số> — <tiêu đề>`. Mục "Câu hỏi mở" không bắt đầu bằng T.

## 4. Ghi vào Bridge
- Plan id: slug ngắn không dấu, vd `wb-boss-dragon`. Không ghi đè plan cùng tên đã có — thêm hậu tố `-2`.
- Có quyền vào máy Linh (folder `AGENTS` đã connect, hoặc tool device): ghi `~/AGENTS/muse-bridge/plans/<id>.md`. Nếu folder chưa connect: xin quyền `~/AGENTS` (device_request_folder_access) một lần.
- Không có đường vào máy: xuất file `<id>.md` để Linh tải và bỏ vào thư mục trên.

## 5. Báo lại (ngắn)
- Plan id, số task, bảng 1 dòng/task (id · tiêu đề · owner · depends).
- Câu hỏi mở (nếu có).
- Bước tiếp: mở Muse Bridge → tab Kế hoạch → "Chạy trong Terminal".
