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

PM · Claude (hành vi theo hoạt động thật, chỉ đọc `/api/activity`):
- Review (`claude_verdict`, `claude_bug`, `review`) → PM đi tới đúng bàn nhân viên của plan đó, đứng xem ~11–16s.
- Chạy MCP/Unity (`claude_unity`, `claude_test`, `claude_file`, `claude_plan`) → PM ngồi bàn máy tính của mình, đến 2 phút sau sự kiện cuối.
- Rảnh → góc nghỉ: ngả lưng, uống trà, boxing, hút điếu cày (đổi hoạt động mỗi 14–26s).

Bố Linh (CEO) và phòng riêng (góc trái phía sau, tường kính, neon):
- Hoạt động khi rảnh (đổi mỗi 14–38s): ký duyệt, xem TV, bar, chạy bộ, đánh golf, ghế massage, karaoke, ngắm cá, đọc sách; thỉnh thoảng đi kiểm tra bàn nhân viên.
- Task treo/pending (sự kiện `needs_human`, `muse_timeout`, `plan_blocked`, `muse_blocked`, hoặc plan "Cần xem" kéo dài >4 phút) → Bố ra tận chỗ PM · Claude đấm, Claude xin lỗi; nghỉ 90s giữa hai lần.
- Phòng 56x(≥40), neon quanh tường, góc nghỉ của PM, bàn bóng bàn, máy bán nước, ghế lười ở phía trước.

Bố cục công ty (vách kính, hành lang, nhiều phòng):
- Dãy sau: phòng Bố Linh | Phòng nghỉ (PM) | hành lang | Phòng PM · Claude (họp). Giữa: Dev floor (bàn nhân viên). Dãy trước: Game room | Pantry | WC | Lobby (lễ tân, cửa chính).
- Vách kính có khe cửa đúng chỗ các đường đi; đã kiểm tra tự động mọi lộ trình (Bố, PM, nhân viên đi giải trí/WC) không xuyên tường.
