# Style: solar-system-orbit

Mặt trời đứng yên ở giữa, 8 hành tinh thật (ảnh vệ tinh, không phải minh hoạ) bay quỹ đạo elip quanh nó và tự quay quanh trục — dùng làm nền động phía sau các card kính (glass). **Một UI/visual style — chỉ áp dụng khi được gọi tên** ("dùng style solar-system-orbit"), không tự động bật.

## Trạng thái
Đã build và tinh chỉnh qua nhiều vòng trong dự án Utilities (`Utilities/UI/`), sau đó bị RÚT lại theo yêu cầu người dùng (thấy vẫn "hoạt hình", chưa đủ thật) — giữ lại đầy đủ ở đây để dùng lại sau này hoặc làm nền tảng cho ai muốn tiếp tục đẩy độ thật lên. Đừng áp dụng lại mà không hỏi trước, vì đây là bản dở dang (xem "Hạn chế còn tồn tại").

## Cấu trúc HTML
```html
<div class="bgfx" aria-hidden="true">
  <span class="orbit-ring r1"></span> ... <span class="orbit-ring r8"></span>
  <span class="sun"></span>
  <span class="planet mercury"></span>
  <span class="planet venus"></span>
  <span class="planet earth"></span>
  <span class="planet mars"></span>
  <span class="planet jupiter"></span>
  <span class="planet saturn"></span>
  <span class="planet uranus"></span>
  <span class="planet neptune"></span>
  <span class="bgfx-sweep"></span>
  <span class="bgfx-veil"></span>
</div>
```
`.bgfx` là `position: fixed; inset:0; z-index:0` nằm sau toàn bộ UI thật (`.app { position:relative; z-index:1 }`), `pointer-events:none`.

## Asset cần có
8 ảnh bản đồ kinh độ (equirectangular, 2:1) cho Mercury→Neptune — **không dùng ảnh full-disk** (ảnh chụp tròn một mặt kiểu NASA hero shot), vì không cuộn ngang được để giả tự quay (xem "Vì sao equirectangular"). Nguồn: `solarsystemscope.com/textures` (2k, CC BY 4.0 — ghi nguồn khi công bố), lấy bản `2k_venus_atmosphere.jpg` cho Venus (không phải `2k_venus_surface.jpg` — bản surface là radar map lộ địa hình, không giống ảnh mây trắng quen mắt). Mặt trời vẫn dùng ảnh full-disk tĩnh bình thường (không cần cuộn).

Dùng thẳng file gốc 2k tải về (đổi tên `<planet>_map.jpg`), **không tự resize/nén lại** — thử downscale xuống 512×256 rồi nén JPEG q82 từng làm mất nét, nhìn phẳng/giả hơn hẳn so với dùng thẳng bản gốc.

## CSS lõi
```css
.bgfx {
  position: fixed; inset: 0; z-index: 0;
  overflow: hidden; pointer-events: none;
  background: rgba(8, 9, 11, .82);
  --orbit-cx: 50vw; --orbit-cy: 42vh;   /* tâm dùng chung cho mặt trời + mọi ring + mọi offset-path */
}
.sun {
  position: absolute; top: var(--orbit-cy); left: var(--orbit-cx); width: 5vw; height: 5vw;
  transform: translate(-50%, -50%);
  display: block; border-radius: 50%;
  background: url("assets/planets/sun.png") center / cover;
  box-shadow: 0 0 24px 6px rgba(255,214,140,.55), 0 0 70px 22px rgba(255,180,90,.22);
  animation: sun-pulse calc(6s / var(--bg-speed, 1)) ease-in-out infinite alternate;
  z-index: 1;
}
.orbit-ring {
  position: absolute; top: var(--orbit-cy); left: var(--orbit-cx);
  transform: translate(-50%, -50%);
  display: block; border-radius: 50%;   /* khung chữ nhật rộng≠cao + border-radius:50% tự ra ELIP */
  pointer-events: none;
}
.r1 { width: 18vw; height: 10vw; border: 1px solid rgba(255,255,255,.11); }
/* r2..r8: width/height tăng dần theo cấp số ~1.2-1.25x, alpha giảm dần — xem file gốc */

.planet {
  position: absolute; top: 0; left: 0; display: block; border-radius: 50%;
  offset-rotate: 0deg; z-index: 1;   /* KHÔNG tự xoay theo hướng path — giữ mặt luôn thẳng */
  /* 3 lớp nền: 2 radial-gradient giả sáng/tối + var(--tex) là ảnh riêng từng hành tinh */
  background-image:
    radial-gradient(circle at 32% 28%, rgba(255,255,255,.85), rgba(255,255,255,0) 55%),
    radial-gradient(circle at 72% 78%, rgba(0,0,0,.92), rgba(0,0,0,0) 65%),
    var(--tex);
  background-blend-mode: screen, multiply, normal;  /* KHÔNG để mặc định normal — xem "Blend-mode" */
  background-repeat: repeat-x; background-position-y: center;
  background-size: auto, auto, 200% 100%;           /* % chứ không phải px cố định — xem "Sizing" */
  animation-name: orbit, spin; animation-timing-function: linear, linear;
  animation-iteration-count: infinite, infinite;
}
.mercury {
  width: 4.5vw; height: 4.5vw;
  offset-path: ellipse(9vw 5vw at var(--orbit-cx) var(--orbit-cy));
  animation-duration: calc(9s / var(--bg-speed, 1)), calc(70s / var(--bg-speed, 1));
  transform: rotate(.03deg);                         /* độ nghiêng trục thật — xem "Nghiêng trục" */
  --tex: url("assets/planets/mercury_map.jpg");
}
/* venus (7vw, 13/7 ellipse, 13s/90s, reverse spin, tilt 2.6deg, venus_map.jpg)
   earth  (7.5vw, 17/9, 17s/40s, tilt 23.4deg, earth_map.jpg)
   mars   (5.5vw, 21/12, 21s/42s, tilt 25.2deg, mars_map.jpg)
   jupiter(15vw, 27/15, 27s/18s, tilt 3.1deg, jupiter_map.jpg)
   saturn (13vw, 33/18, 33s/20s, tilt 26.7deg, saturn_map.jpg)
   uranus (9vw, 39/21, 39s/30s, reverse spin, tilt 97.8deg, uranus_map.jpg)
   neptune(8.5vw, 45/25, 45s/26s, tilt 28.3deg, neptune_map.jpg)
   — bán kính quỹ đạo/kích thước/chu kỳ tăng dần Mercury→Neptune, đúng thứ tự thật */

@keyframes orbit  { from { offset-distance: 0%; }        to { offset-distance: 100%; } }
@keyframes spin   { from { background-position-x: 0%; }  to { background-position-x: 200%; } }
@keyframes sun-pulse { from { transform: translate(-50%,-50%) scale(1); opacity:.92; }
                       to   { transform: translate(-50%,-50%) scale(1.06); opacity:1; } }
```
`--bg-speed` (mặc định 1 qua `var(--bg-speed, 1)`) là biến điều tốc chung, set từ JS nếu có settings panel (`document.documentElement.style.setProperty('--bg-speed', pct/100)`).

## Vì sao equirectangular (không phải full-disk + transform)
Ảnh full-disk (chụp một mặt, có sẵn vùng sáng/tối bản thân ảnh) không thể "tự quay" đúng bằng CSS:
- `transform: rotate()` xoay phẳng cả tấm ảnh quanh tâm như đồng xu — không phải tự quay quanh trục.
- `transform: rotateY()` (giả phối cảnh 3D) ép ảnh dẹt lại rồi lật gương nửa sau khi qua 90° — sai vì ảnh chỉ có một mặt.
- Equirectangular (bản đồ trải kinh độ, ghép liền mép trái/phải) CUỘN NGANG bằng `background-position-x` + `background-repeat: repeat-x` mới đúng: bề mặt trôi ngang liên tục, không giới hạn góc, không lật gương.

## Sizing: % chứ không phải px cố định
`background-size` của lớp texture phải là **%** (vd `200% 100%`), KHÔNG ép px cố định (vd từng thử `512px 256px`) — hành tinh nhỏ (Mercury ~4.5vw) với size px cố định chỉ lọt một mảnh bé tí của bản đồ, nhìn như khối màu phẳng lì mất hết hình khối. Dùng % thì hành tinh nhỏ hay to đều thấy cùng tỉ lệ bản đồ tại mỗi thời điểm.

Công thức lặp liền mạch: nếu `background-size` lớp texture = `K%` (K>100), animate `background-position-x` từ `0%` đến `(K)%`... — cụ thể: offset = -(khung)×(p/100) khi ảnh rộng gấp K/100 lần khung; cần dịch đúng 1 bề rộng ảnh để lặp mượt ⇒ `p = 100 × K/100 / (K/100 - 1)`. Với K=200% (ảnh rộng gấp đôi khung): p = 200%. Xem `@keyframes spin` ở trên.

Đặt 2 lớp gradient shading ở `background-size: auto` (tự phủ kín khung) để chúng KHÔNG bị ảnh hưởng khi `spin` animate `background-position-x` dùng chung 1 giá trị cho cả 3 lớp (offset = 0 bất kể % vì kích thước layer = kích thước khung).

## Blend-mode: chỗ quyết định "thật hay giả"
Quan trọng hơn cả độ phân giải ảnh. Lớp gradient sáng/tối phủ lên texture với `background-blend-mode` mặc định (`normal`) sẽ đè một mảng xám/trắng phẳng LÊN TRÊN màu thật — nhìn như dán sticker, mất màu, phẳng lì. Đổi sang:
- Lớp sáng (highlight): `screen` — sáng lên mà không cháy trắng, giữ được màu gốc.
- Lớp tối (shadow): `multiply` — tối đi mà không bạc màu.
Alpha 2 lớp gradient để cao (~.85/.92) vì blend-mode cần alpha mạnh hơn plain-alpha mới ra hiệu ứng rõ.

## Nghiêng trục (axial tilt) — tránh "trục quay cứng, giống nhau"
Nếu tất cả hành tinh đều cuộn texture theo đúng 1 hướng ngang (không `transform: rotate()` riêng), nhìn RẤT máy móc/giả — 8 hành tinh xoay y hệt nhau không có gì phân biệt. Set `transform: rotate(Xdeg)` TĨNH (không animate) riêng từng class theo độ nghiêng trục thật ngoài đời: Mercury .03°, Venus 2.6°, Earth 23.4°, Mars 25.2°, Jupiter 3.1°, Saturn 26.7°, Uranus 97.8° (gần như nằm ngang — có thật, không phải lỗi), Neptune 28.3°.

## Hạn chế còn tồn tại (chưa giải quyết được, cần cân nhắc trước khi dùng lại)
- **Ánh sáng không theo vị trí quỹ đạo thật**: gradient sáng/tối cố định theo góc riêng của từng `.planet` (32%/28% và 72%/78%), KHÔNG xoay theo vị trí hành tinh so với mặt trời (đáng lẽ mặt sáng luôn hướng về tâm/mặt trời) — vì mặt trời ở tâm cố định còn hành tinh bay quanh liên tục, hướng sáng "đúng vật lý" phải đổi theo góc quỹ đạo tại mỗi thời điểm, cần JS tính góc theo thời gian thực chứ CSS thuần không làm được. Đây có thể là phần gây cảm giác "trục quay cố định, sai sai" mà người dùng phản hồi, dù bản sửa `transform: rotate()` (nghiêng trục) đã giảm bớt phần nào.
- **Uranus/Neptune vốn dĩ gần như trơn láng**: ảnh THẬT (không dàn dựng) của 2 hành tinh này gần như phẳng một màu (đã kiểm chứng trực tiếp từ file gốc solarsystemscope) — khác hẳn Jupiter/Saturn/Mars/Earth có texture rõ. Thử tăng contrast mạnh (autocontrast, cutoff thấp) để lộ chi tiết ẩn → ảnh vỡ màu, ra neon giả trân, KHÔNG dùng được. Nguồn duy nhất có Uranus/Neptune "chi tiết" hơn (`planetpixelemporium.com`) là ảnh HOẠ SĨ VẼ TAY dựa theo tham khảo, không phải ảnh chụp thật — đánh đổi giữa "thật nhưng trơn" và "chi tiết nhưng không thật", chưa quyết định hướng nào khi rút lại style này.
- Ở kích thước icon nhỏ (40-120px, tương ứng 4.5-15vw trong cửa sổ ~820px), ảnh thật của bất kỳ vật thể nào cũng mất phần lớn chi tiết — một phần cảm giác "hoạt hình" có thể là giới hạn tự nhiên của kích thước hiển thị, không phải lỗi kỹ thuật có thể sửa thêm bằng CSS.

## Nguồn
Tự xây và tinh chỉnh trong dự án Utilities (`Utilities/UI/style.css`, `Utilities/UI/index.html`, `Utilities/UI/assets/planets/`) qua nhiều vòng phản hồi trực tiếp, rút lại (chưa merge vào bản chính) ngày 2026-09-11.
