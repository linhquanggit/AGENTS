# AOV — Làm đẹp style UI (Glassmorphism game)
goal: Thay style UI web "Liên Quân — Mô phỏng Cấm/Chọn" bằng phong cách GLASSMORPHISM game (Linh chốt 2026-10-27): kính mờ nhiều lớp trên nền gradient tối, rõ team Xanh/Đỏ, bỏ emoji làm icon, chuẩn a11y/responsive, KHÔNG đổi logic ban/pick, thuật toán recommend, id DOM, hay key localStorage.
project: /Users/mobione/Games/AOV
source: Project MUSUDE — claude/muse-collab-status.md (quy trình Bridge). Không có doc AOV trong Project nên mọi sự thật lấy từ dò trực tiếp project + ui-ux-pro-max (design-system "esports draft dashboard", fonts Russo One/Chakra Petch).

## T1 — Chụp baseline trước khi sửa
owner: claude
depends: -
context:
Đây là WEB tĩnh (HTML/CSS/JS thuần), KHÔNG phải Unity: bỏ qua skill unity:ui / ui-imgui / ui-ugui, không dùng Unity MCP. Tab Playing là màn chính (draft 3 cột).
verified:
Thư mục chỉ có app.js, data.js, index.html, index.single-backup.html, style.css (không ProjectSettings/Assets/package.json).
style.css rất ngắn dòng (2 dòng, minified): :root token ở style.css:1, toàn bộ rule ở style.css:2. Token sẵn có: --bg --surface --surface2 --btn --border --border-strong --ink --ink-soft --accent(#ffc93c) --blue(#38b6ff) --red(#ff4d5e) --green --warn --ease --pop.
index.html:4 link style.css; script data.js rồi app.js ở cuối index.html.
refs: -
files: (chỉ đọc) toàn bộ; tạo ảnh vào ~/AGENTS/muse-bridge/.agent/shots/aov-base-*.png
done_when: có ảnh baseline tab Playing + Cẩm nang + Tướng tủ + Cài đặt ở 375, 768, 1024, 1440 px (mở index.html bằng file:// hoặc server tĩnh); ghi 3 điểm xấu nhất vào báo cáo.

## T2 — Design tokens + font esports
owner: muse
depends: T1
context:
Style đã chốt: Glassmorphism game. Nền: gradient tối chàm + 2–3 blob màu mờ cố định (xanh #38b6ff góc trái, đỏ #ff4d5e góc phải, vàng rất nhạt dưới) bằng body::before, position:fixed, pointer-events:none. Bề mặt kính: background rgba(255,255,255,.06–.10), backdrop-filter: blur(14px) saturate(140%), viền 1px rgba(255,255,255,.14), highlight inset 0 1px 0 rgba(255,255,255,.18), bo 16–24px. Token mới: --glass-1/2/3 (độ mờ nền), --glass-border, --blur-1/2, --glow-blue/red/gold. Bắt buộc fallback @supports not (backdrop-filter) → nền đặc var(--surface). Giữ vàng #ffc93c làm accent hành động; không dùng tím neon. Font: Russo One (tiêu đề, timer, nhãn) + Chakra Petch (body) qua Google Fonts display=swap, fallback system-ui. Số timer dùng font-variant-numeric: tabular-nums (đã có). Thêm token: --font-display, --font-body, --radius-sm/md/lg, --shadow-1/2/glow-blue/glow-red, --space-1..6, --surface3. Mọi hex trong rule phải thay bằng var(--token) (hiện còn hex rời: #0f1530, #3a467e, #22c55e, #ef4444, #f59e0b, #fff, #38b6ff, #ff4d5e).
verified:
style.css:1 :root (chưa có font/radius/shadow token). index.html:5 inline style có #3a467e/#0f1530 ở ô skinSearch (section skins). data.js:1 LANE_COLOR dùng hex riêng cho 5 lane.
refs: -
files: style.css, index.html (thẻ <link> font trong <head>)
done_when: mở trang không lỗi console; chữ tiêu đề dùng Russo One, body Chakra Petch; card hiện hiệu ứng kính, @supports fallback có mặt; grep style.css không còn hex ngoài khối :root; giao diện chưa đổi bố cục.

## T3 — Thay emoji bằng SVG sprite
owner: muse
depends: T2
context:
Quy tắc skill: không dùng emoji làm icon. Dùng SVG sprite inline (<svg><symbol id="i-...">) kiểu Lucide, stroke currentColor, 20px, aria-hidden="true"; nút chỉ-icon phải có aria-label. Icon cần: play, reset, search, book, star, settings, ban, shield(pick), pin(khoá vị trí), arrow-up/arrow-down/refresh (patch badge), swords/logo. Giữ nguyên toàn bộ id/onclick/data-* hiện có.
verified:
index.html: 27 chỗ emoji trong index.html + app.js (🎮📖⭐⚙️🔍🚫🔵🔴▶🔄📊📏👗📍). Nav tại index.html:7 (4 nút data-tab: playing, camnang, comfort, settings).
app.js:11 miniCard và app.js:12 emptySlot chứa nút posIcon với emoji 📍 (class lockedOn).
app.js:10 avatar(): ảnh class "ava" có chuỗi onerror fallback nhiều bước (data-step/data-tier/data-splash) — KHÔNG đụng.
app.js:36 render() ghi lại innerHTML nhiều khối mỗi lần — SVG dùng <use href="#i-..."> để rẻ.
star: ngôi sao tướng tủ đã là SVG (.star svg style.css:2), giữ.
refs: -
files: index.html, app.js (chỉ chuỗi template ở dòng 11, 12 và chỗ tạo patchBadge)
done_when: không còn emoji icon trong index.html/app.js (chữ tiếng Việt giữ nguyên); mọi nút icon có aria-label; test bấm Pick/Ban/khoá vị trí/Start/Reset vẫn chạy; console 0 lỗi.

## T4 — Màn Draft: khung, slot, timer, ban
owner: muse
depends: T2
context:
Mục tiêu nhìn (kính): mọi .card/.slot/.draftFrame là tấm kính nhiều lớp, KHÔNG lồng quá 2 lớp backdrop-filter (tránh nặng); .pickList/.recRow/.slot dùng nền rgba không blur. Khung giữa (.draftFrame) như bảng điều khiển đấu trường — header timer lớn Russo One, đổi màu theo ngưỡng (≤10s vàng, ≤5s đỏ nhấp nháy nhẹ, tôn trọng prefers-reduced-motion); cột Xanh/Đỏ có viền trên + glow theo team; slot pick có số thứ tự mờ và avatar tròn viền theo team; slot ban dùng avatar xám bớt + vạch chéo đỏ; trạng thái .sel (vàng) .locked (xanh lá) .usedPick/.usedBan giữ ngữ nghĩa nhưng thêm dấu hiệu không chỉ bằng màu (viền nét/đường chéo/icon). Hero list: avatar hover/active có vòng sáng, ô đã dùng mờ + nhãn. Chỉ sửa CSS (dùng class đã tồn tại); nếu cần hook mới thì thêm class, không đổi id.
verified:
Class hiện có ở style.css:2: .draft5 .draftFrame .draftTimerRow .draftTeams .teamCol .slot(.filled .sel .locked .justIn) .banSlot .pickItem(.used .usedPick .usedBan) .usedTint .avatarWrap(.xs .sm .lg) .laneBadge .patchBadge(.up .down .rework) .recRow .recIcon .recRank .pickLabel #timerBig .timer #timerPhase #myLaneIcons .pickLaneIcons.
app.js:35 banHtml tạo .banSlot; app.js:36 render() điền #banBlue #banRed #pickBlue #pickRed.
Id timer: timerBig, timerPhase, timerStart, timerReset (index.html tab playing).
refs: -
files: style.css
done_when: ảnh tab Playing ở 1440 và 375 rõ hơn baseline; tap target ≥44px (posIcon, timerStart, timerReset, pickLaneIcons button); không đổi lệch layout (không CLS); reduced-motion tắt nhấp nháy/scale.

## T5 — Nav, card, bảng dữ liệu, bỏ inline style
owner: muse
depends: T3, T4
context:
Nav pill: trạng thái active rõ, focus-visible ring, scroll ngang trên mobile thay vì xuống dòng nếu >4 nút. Card = kính (blur ở card ngoài cùng, phần tử con không blur), tiêu đề section có gạch accent. Nav pill là kính, pill active nền vàng đặc. Bảng dữ liệu dùng nền kính đậm hơn (.10) để chữ đủ tương phản. Bảng #skinTable/#dataTable/stats: header sticky, zebra nhẹ, số căn phải + tabular-nums, ô "chưa xác minh" dùng màu mờ + chữ nghiêng. Chuyển toàn bộ inline style="..." sang class (.row, .muted, .input-full, .hero-note…).
verified:
index.html: 8 dòng còn style=" (vd card flex ở tab Playing, #algoNote, #lockAlgo, input#skinSearch có oninput inline).
index.html:15-17 có section id="data", "stats", "skins" nhưng nav chỉ có 4 nút (playing/camnang/comfort/settings) nên 3 section này không truy cập được qua UI — xem Câu hỏi mở.
localStorage: key "lq_comfort" ở app.js:4,41,42 — không đổi.
refs: -
files: index.html, style.css
done_when: index.html không còn style=" (trừ giá trị JS-driven nếu có, ghi rõ); nav hoạt động bằng bàn phím (Tab/Enter); không đổi id/data-tab; bảng đọc được ở 375px không tràn ngang trang.

## T6 — QA a11y + responsive + hồi quy chức năng
owner: claude
depends: T5
context:
Checklist skill: contrast text ≥4.5:1 ĐO TRÊN NỀN KÍNH ở chỗ blob sáng nhất (nhất là --ink-soft, chữ trên avatar fallback); FPS/độ mượt khi cuộn danh sách ~130 tướng (nếu giật → giảm blur 14→8px hoặc bỏ blur ở .pickList); kiểm tra fallback khi tắt backdrop-filter, focus ring thấy rõ, tap ≥44px, không dựa chỉ màu (lane, team, patch up/down), reduced-motion, 375/768/1024/1440, không cuộn ngang. Hồi quy: Pick/Ban đủ lượt, đổi first-pick, timer Start/Reset/beep, khoá lane, tìm tướng, sao tướng tủ lưu lại sau F5, Recommend vẫn render (#pickRec #banRec #algoNote).
verified:
(điền khi chạy) Cập nhật bằng ảnh sau và console log.
refs: -
files: (chỉ đọc) ảnh sau → ~/AGENTS/muse-bridge/.agent/shots/aov-after-*.png
done_when: 0 lỗi console; mọi mục checklist PASS (liệt kê); so sánh ảnh trước/sau cho Linh; nếu FAIL → sửa nhỏ ở style.css rồi chụp lại.

## T7 — Dọn đồ tạm
owner: claude
depends: T6
context:
Tự xoá sau khi T6 PASS, không hỏi lại, không đặt blocked chờ OK. Xin device_request_delete_permission đúng 1 lần nếu bị chặn.
verified: -
refs: -
files: XOÁ: ~/AGENTS/muse-bridge/.agent/shots/aov-base-* và aov-after-*; ~/.claude/logs/aov-ui-restyle-*; mọi *.before-*, *.bak, *.tmp, test-*.html Muse/Claude tạo trong /Users/mobione/Games/AOV. GIỮ: style.css, index.html, app.js, data.js, index.single-backup.html (có từ trước), file plan này.
done_when: không còn file tạm trong project/logs/shots; báo cáo cuối có dòng `Đã dọn: …`.

## Câu hỏi mở
- 3 section data / stats / skins (index.html:15-17) đang mồ côi (không có nút nav). Muốn thêm vào nav, gộp thành tab "Dữ liệu", hay giữ ẩn? Mặc định plan: KHÔNG đổi, chỉ style.
- LANE_ICON (data.js:2) và ảnh avatar hotlink từ lienquan.garena.vn; có muốn tải về nội bộ không? Mặc định: không, giữ nguyên.
- index.single-backup.html là bản cũ 1 file; mặc định không đụng.
