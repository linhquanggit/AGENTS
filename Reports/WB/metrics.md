# Chỉ số hiệu quả điều phối Claude ↔ Muse (WB)
Mục đích: so mission trước/sau luật PM-Leader + hạn chế MCP (chốt 2026-10-23). Mỗi mission thêm 1 dòng; số lấy từ log (`muse-ask --log`, `status.json`), không ước lượng.

| Mission | Ngày | Model Claude | Task | Phút giao→xong | Lần gọi MCP | Lần Play | Vòng review (tổng) | Lỗi lọt qua review (lộ ở test) | Lỗi Linh phát hiện sau khi báo xong | Lần Claude tự viết code | Token Claude (explain-usage) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| wb5-p3-g9-spread (MỐC CŨ) | 2026-10-14 | n/a | 3 | ~8 (14:58→15:06) | không ghi | không ghi | không ghi | không ghi | không ghi | không ghi | không ghi |
| wb6-full | 2026-10-23 → | Opus 5.5 → Sonnet 5.5 (đổi giữa phiên) | 7 | | | | | | | | |

## Cách đọc
- Hiệu quả = (MCP, Play, token, phút giảm) VÀ (lỗi Linh phát hiện sau không tăng, mục tiêu 0).
- Giảm token mà lỗi sau-báo-xong tăng → review gate chưa đủ, siết lại; không tính là cải thiện.
- Lỗi lọt qua review cao → bổ sung điểm soát vào "Cổng review code".
- Mốc cũ thiếu số (trước đây không ghi) → chỉ so được số phút; từ wb6 trở đi mới có đủ cột. So sánh công bằng cần ≥2 mission mới.
- Đổi model giữa chừng làm nhiễu số liệu → luôn ghi cột Model; ưu tiên so các mission cùng model.
