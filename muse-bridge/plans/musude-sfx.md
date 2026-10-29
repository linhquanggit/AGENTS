# Âm thanh cho văn phòng 3D của Musude
goal: Tạo bộ sound effect + tiếng nhân viên "nói" (blip kiểu nhân vật hoạt hình) cho văn phòng 3D của Musude, tổng hợp bằng Web Audio (không file âm thanh, không thư viện ngoài), kèm bảng chỉnh âm lượng/nghe thử; sau đó nối vào các sự kiện trong văn phòng.
project: ~/AGENTS/muse-bridge
source: Project MUSUDE — claude/muse-collab-status.md + dò trực tiếp ~/AGENTS/muse-bridge/office3d

## T1 — Engine âm thanh sfx.js (Web Audio tổng hợp)
owner: muse
depends: -
context:
Tạo muse-bridge/office3d/sfx.js, ES5 (var + function, không import/module, không const/let/arrow) giống phong cách office3d.js. Chạy trong trình duyệt, KHÔNG nạp file mp3/wav, KHÔNG dùng CDN. Tất cả âm thanh được tổng hợp bằng oscillator + noise + envelope (gain) qua một AudioContext dùng chung.
Phơi ra 1 global `window.Sfx` với API đúng như sau (T3 sẽ gọi theo tên này, đừng đổi):
- Sfx.unlock(): tạo/resume AudioContext (gọi trong cử chỉ người dùng đầu tiên; tự gắn vào pointerdown/keydown của document nếu chưa unlock).
- Sfx.setVolume(v 0..1), Sfx.getVolume(); Sfx.setMuted(bool), Sfx.isMuted(); lưu localStorage khoá 'mb_sfx' = JSON {v, m}; bọc try/catch (localStorage có thể ném lỗi). Mặc định v=0.5, m=false.
- Sfx.play(name, opts): opts tuỳ chọn {gain 0..1, pan -1..1, rate 0.5..2}. Tên không tồn tại → bỏ qua, không ném lỗi. Nếu muted hoặc chưa unlock → bỏ qua im lặng.
- Sfx.talk(text, voice): phát chuỗi blip ngắn ứng với từng chữ cái của text (bỏ dấu cách → ngắt nhẹ; nguyên âm cao hơn phụ âm; tối đa ~14 blip mỗi câu, mỗi blip 35–60 ms). voice là số nguyên 0..5, mỗi voice 1 âm sắc/cao độ cơ bản khác nhau (vd 0 trầm vuông, 1 sine cao, 2 triangle trung, 3 sawtooth mỏng, 4 sine trầm, 5 pulse cao) để mỗi nhân viên một giọng. Có ngẫu nhiên ±8% cao độ để không robot.
- Sfx.list(): trả mảng tên các âm có trong play().
Danh sách âm cho play() (tên: mô tả ngắn, độ dài):
- type: tiếng gõ phím cơ 1 nhịp (clack ngắn ~40ms, noise band-pass + click)
- typeburst: 4–8 nhịp gõ nhanh liên tiếp (~0.5s)
- start: bấm "Làm việc" — whoosh đi lên + ding nhỏ (~0.5s)
- ping: có việc mới/được giao — 2 nốt ngắn đi lên (~0.3s)
- done: mission xong — hợp âm trưởng đi lên 3 nốt vui (~0.7s)
- error: lỗi/cần xem — buzz thấp 2 nhịp (~0.5s)
- think: "hmm" — sine trượt xuống nhẹ (~0.5s)
- sigh: thở dài — noise lọc thấp giảm dần (~0.9s)
- door: cửa kính trượt WC — whoosh ngắn + tick (~0.4s)
- flush: xả nước WC — noise lọc, dài ~1.4s, nhỏ tiếng
- step: 1 bước chân nhẹ (~60ms) dùng lặp khi đi
- punch: đấm — thump trầm + noise ngắn (~0.25s)
- ouch: "á" — sine trượt xuống nhanh (~0.3s)
- cheer: reo mừng — chuỗi nốt lên nhanh + noise vỗ tay ngắn (~0.9s)
- coin: leng keng — 2 nốt sáng, dùng cho thao tác nhỏ (~0.25s)
Yêu cầu chất lượng: mọi âm đều có attack/release (không click đột ngột), đỉnh tổng < 0.9 (không clip), dùng 1 DynamicsCompressor ở cuối chuỗi; giới hạn đồng thời tối đa 12 voice (vượt thì bỏ âm mới); mỗi tên âm có cooldown tối thiểu 40 ms (chống spam khi nhiều nhân viên cùng gọi). Không tạo AudioContext trước khi có cử chỉ người dùng (tránh cảnh báo autoplay).
verified:
office3d.js là 1 IIFE ES5 (var/function), nạp bằng <script src="/office3d/..."> trong office3d/index.html; server.py chỉ phục vụ file tĩnh nằm trong thư mục office3d/ (đường /office3d/<tên file>).
office3d/ hiện có: index.html, office3d.js, office3d.css, three.min.js, OrbitControls.js, README.md.
Trang 3D chạy trong iframe của app chính (office3d.js tự chuyển hướng nếu mở trực tiếp), nên cử chỉ người dùng (click) bên trong iframe là đủ để unlock.
refs: ~/AGENTS/muse-bridge/office3d/office3d.js (xem phong cách code), ~/AGENTS/muse-bridge/office3d/index.html
files: muse-bridge/office3d/sfx.js
done_when: `node --check sfx.js` pass. Chạy thử bằng Node với AudioContext giả HOẶC trong trình duyệt: Sfx.list() trả đủ 15 tên ở trên; gọi từng play() không ném lỗi; render OfflineAudioContext thử từng âm cho thấy đỉnh < 0.9 và không có NaN.

## T2 — Bảng âm thanh: loa, âm lượng, nghe thử
owner: muse
depends: T1
context:
Tạo muse-bridge/office3d/sfx-ui.js (ES5, tự dựng DOM, không sửa office3d.js). Nó tạo 1 nút loa nhỏ ở góc phải-dưới của trang 3D (cố định, không che thẻ nhân viên bên phải trên; bên trái hint ở đáy đã có). Bấm nút loa → mở panel nhỏ gồm: công tắc Bật/Tắt tiếng (Sfx.setMuted), thanh trượt âm lượng 0–100 (Sfx.setVolume), và lưới các nút nghe thử — mỗi nút tên 1 âm lấy từ Sfx.list(), bấm là Sfx.play(tên); riêng 1 nút "Nói thử" gọi Sfx.talk('Khó thế… làm thế nào nhỉ?', voice) với ô chọn voice 0..5.
Giao diện dùng biến CSS có sẵn của office3d.css (--ink, --line, --accent, --mute) và kiểu nút giống `.chips button` (bo tròn, nền tối mờ). Chữ tiếng Việt. Thêm CSS vào office3d/office3d.css (khối riêng cuối file, tiền tố `.sfx`). Thêm 2 dòng <script src="/office3d/sfx.js"> và <script src="/office3d/sfx-ui.js"> vào office3d/index.html TRƯỚC dòng office3d.js.
Biểu tượng nút: loa (SVG nội tuyến) — đổi sang loa gạch chéo khi tắt tiếng.
verified:
office3d.css có sẵn :root biến màu và `.chips button` ở dòng ~10; #card cố định ở right:14px top:12px bottom:14px width:330px; .hint cố định ở đáy trái.
refs: ~/AGENTS/muse-bridge/office3d/office3d.css, ~/AGENTS/muse-bridge/office3d/index.html
files: muse-bridge/office3d/sfx-ui.js, muse-bridge/office3d/office3d.css, muse-bridge/office3d/index.html
done_when: Mở /office3d/ trong app: thấy nút loa, bấm mở panel, mỗi nút nghe thử phát tiếng sau lần click đầu; kéo thanh trượt đổi âm lượng thật; tắt tiếng thì không phát gì; tải lại trang vẫn nhớ cài đặt; không lỗi trong console.

## T3 — Nối âm thanh vào sự kiện trong văn phòng
owner: claude
depends: T1, T2
context:
Claude sẽ sửa office3d.js (Muse không đụng file này). Bảng nối dự kiến:
- Bong bóng nói của nhân viên (chatTick: TALK.work/boss/pm) → Sfx.talk(câu, voice = hash(plan.id) % 6), pan theo vị trí x trên màn hình.
- Nhân viên đang làm → Sfx.play('typeburst') ngẫu nhiên thưa (1–2 người một lúc, nhỏ tiếng).
- Nút "Làm việc" trên đầu nhân viên → 'start'; mission chuyển sang đang chạy → 'ping'.
- Mission chuyển sang "xong" → 'done' + 'cheer' nhẹ; chuyển "cần xem/blocked" → 'error'.
- Nhân viên đi WC: 'door' lúc mở cửa, 'flush' khi xong; đi bộ trong lúc di chuyển: 'step' rất nhỏ cho nhân vật gần camera.
- Bố Linh đấm PM: 'punch' theo nhịp + 'ouch' từ PM; PM nghỉ/uống trà: 'sigh' thỉnh thoảng; nhân viên chờ Muse: 'think'.
- Mọi lời gọi đều qua guard `window.Sfx && Sfx.play(...)` để app vẫn chạy khi sfx.js lỗi.
verified:
office3d.js hiện có: chatTick (bong bóng nói), runFromHead (nút Làm việc), STUCK_KINDS/ceoPunishStart (đấm PM), startWC + anim cửa WC, stepMove (di chuyển).
refs: ~/AGENTS/muse-bridge/office3d/office3d.js
files: muse-bridge/office3d/office3d.js
done_when: `node --check office3d.js` pass; chạy trên bản thử (dữ liệu giả): mỗi sự kiện trong bảng gọi đúng Sfx.play/talk (đếm lời gọi bằng Sfx giả), không có lỗi JS; bật tắt tiếng bằng panel có hiệu lực ngay.

## T4 — Cân âm lượng và kiểm tra cuối
owner: claude
depends: T3
context:
Nghe thử toàn bộ trong app thật cùng Linh: cân lại gain từng âm cho đồng đều, kiểm tra CPU khi 10+ nhân viên cùng nói/gõ (không giật hình), và cập nhật office3d/README.md thêm mục "Âm thanh" (API Sfx, danh sách âm, cách tắt tiếng).
verified:
-
refs: -
files: muse-bridge/office3d/README.md
done_when: Linh nghe và xác nhận âm lượng ổn; README có mục Âm thanh; không còn file tạm nào trong muse-bridge/ (theo quy tắc dọn rác của AGENTS.md).

## Câu hỏi mở
- Muse có tạo được file âm thanh thật (wav/mp3) không? Nếu có và Linh thích chất lượng cao hơn, thay vì tổng hợp bằng code có thể thêm thư mục office3d/audio/ — hiện kế hoạch chọn tổng hợp bằng code vì nhẹ, không cần bản quyền, chạy offline.
- Tiếng nhân viên "nói" hiện là blip không có lời thật (kiểu nhân vật hoạt hình). Linh có muốn đọc thật bằng speechSynthesis tiếng Việt của trình duyệt không?
