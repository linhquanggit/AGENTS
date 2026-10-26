# Musude 3D — văn phòng IT

Nút `3D` trên header của Musude là nút bật/tắt. Bật thì khung 3D (`/office3d/`) phủ lên vùng nội dung,
header giữ nguyên và nút 3D sáng lên. Tắt thì về giao diện cũ. Trạng thái nhớ trong localStorage (`mb_3d`).
Chỉ có một cổng vào: bấm tên Musude trên header. Mở thẳng `/office3d/` sẽ chuyển về app chính ở chế độ 3D.

Chỉ đọc: gọi `/api/plans`, `/api/state`, `/api/usage`, `/api/trash`. Không có nút chạy/dừng/xoá nào trong 3D.

- Mỗi mission = một nhân viên ngồi bàn (2 màn hình hiển thị code), nhóm khu theo tiền tố tên plan.
- Trạng thái: gõ code (đang làm), spinner "chờ Muse", `ERROR` + giơ tay (cần xem), `PASSED` (xong), zZ (chưa chạy).
- Thanh trạng thái của Musude thành đồ trang trí trên tường: muse-cli, poll, 5 giờ, Tuần, Auto, Chat Muse,
  PING (tủ server + đèn quay), thùng rác, bảng ghim việc đang mở, cài đặt.
- Mặc định chỉ hiện mission đang làm + vài mission đã xong; nút "Hiện cả mission đã xong" mở hết.

Thư viện: Three.js r147 (MIT, `three.min.js`, `OrbitControls.js`) đóng gói local, không cần mạng.
