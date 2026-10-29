Muse, việc: CHUẨN HOÁ DỮ LIỆU LANE cho web draft Liên Quân/AoV ở /Users/mobione/Games/AOV (file: data.js, app.js, index.html, seed.js, style.css).

## Vấn đề (đã đo trên data hiện tại)
- 130 tướng: H (38 tướng có số meta) + EXTRA1 (32) + EXTRA2 (60). 92 tướng EXTRA là dữ liệu điền tạm (win 50 / pick 0 / ban 0) nên lane của chúng nhiều khả năng bị gán bừa.
- Phân bố lane lệch bất thường: Caesar 33, Mid 31, Trợ thủ 26, Xạ thủ 21, Rừng chỉ 19.
- FLEX (data.js dòng 16) có 66 tướng, trong đó 29 tướng ok:0 ("mới 1 nguồn, chưa chắc") và nhiều chỗ mâu thuẫn trực tiếp với lane chính, ví dụ: Zill (lane chính Rừng, FLEX lại ghi Mid), Murad (Rừng / FLEX Caesar), Astrid (Caesar / FLEX Rừng), Lữ Bố (Caesar / FLEX Rừng), Arum & Baldum (Trợ thủ / FLEX Caesar), Toro (Trợ thủ / FLEX Caesar), Dolia & Rouie (Trợ thủ / FLEX Mid). Rất nhiều FLEX.lanes thật ra là lane chính đúng của tướng đó, tức lane chính ở H/EXTRA đang sai.
- App dùng lane ở: lanesOf() (lọc lane, gợi ý pick, Tướng tủ, Blacklist, Cẩm nang), avatar() (màu + icon lane), bảng Chỉ số (index.html, cột Lane viết cứng trong HTML), laneText().

## Việc cần làm
1. Lập nguồn lane chuẩn cho ĐỦ 130 tướng, mỗi tướng có: `main` (lane chính), `alt` (0–2 lane phụ đang chơi thật), `v` (1 = đã xác minh ≥2 nguồn đồng ý, 0 = chưa chắc), `src` (ngắn gọn: nguồn nào).
2. Nguồn theo thứ tự ưu tiên: (a) RoVGuide (bảng /meta + trang từng tướng, seed.js đã cào bảng này), (b) lienquan.garena.vn (class: Đấu sĩ→Caesar, Sát thủ→Rừng, Pháp sư→Mid, Xạ thủ→Xạ thủ, Trợ thủ→Trợ thủ — chỉ dùng làm gợi ý vì class không bằng lane), (c) AOV Guide aov-guide.vercel.app. KHÔNG tự suy đoán theo cảm giác; không có nguồn thì `v:0` và ghi rõ "chưa xác minh".
3. Quy ước lane tên đúng 5 giá trị: "Caesar", "Rừng", "Mid", "Xạ thủ", "Trợ thủ" (khớp LANE_COLOR / LANE_ICON). Không thêm lane mới.
4. Một nguồn sự thật duy nhất: tạo `const HERO_LANES={...}` trong data.js, rồi từ đó (a) gán `h.lane = HERO_LANES[name].main` cho mọi tướng, (b) sinh lại `FLEX` từ `alt` (`{lanes: alt, ok: v}`) — bỏ các FLEX.lanes trùng lane chính. Xoá lane viết rải rác trong H/EXTRA1/EXTRA2 nếu thấy an toàn, hoặc để lại nhưng phải bằng HERO_LANES (không được lệch nhau).
5. Bảng "Chỉ số cơ bản (Lv1)" trong index.html đang hardcode cột Lane → sửa cho khớp HERO_LANES (hoặc render từ data).
6. Flowborn và "Flowborn (Phép)" là 2 mục riêng: Xạ thủ và Mid. Giữ nguyên.

## Ràng buộc
- KHÔNG đổi: tên tướng, win/pick/ban, tags, counters, counteredBy, META2, thuật toán, UI, style.css, seed.js.
- Không thêm/xoá tướng. Brunhilda chưa có trong lưới thì bỏ qua.
- Trước khi sửa: copy data.js và index.html sang `data.js.before-lane`, `index.html.before-lane` cùng thư mục (Claude sẽ dọn sau khi Bố duyệt).
- Giữ nguyên hành vi `lanesOf(h, ver)` ở app.js (ver=1 chỉ lấy lane phụ khi ok=1).

## Tiêu chí xong
- Chạy được `node` kiểm tra (viết script nhỏ ở scratch, KHÔNG để trong thư mục AOV) và in: số tướng mỗi lane; tướng thiếu `main`; tướng có `main` nằm trong `alt`; tên lane sai chính tả; số tướng `v:0`.
- Mở index.html: lọc lane Cẩm nang, Tướng tủ, Blacklist, 2 danh sách Chọn Xanh/Đỏ hiển thị tướng đúng lane; không lỗi console.
- Phân bố lane mới hợp lý (Rừng không còn thiếu hụt bất thường), mọi thay đổi có nguồn.

## Báo cáo cuối `[DONE] … | files: …`
Gửi kèm bảng: Tướng | lane cũ → lane mới | alt | v | nguồn. Danh sách riêng các tướng `v:0` để Bố tự chốt. Nêu rõ thứ gì đã verify, thứ gì chưa.
