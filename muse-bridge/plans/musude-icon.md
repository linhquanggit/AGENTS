# Tạo icon cho app Musude
goal: Thiết kế và gắn icon cho Musude.app (Muse Bridge) — icon macOS (.icns) trong bundle + favicon cho index.html, cùng bộ màu với giao diện hiện có.
project: ~/AGENTS/muse-bridge
source: Project Agents — claude/muse-collab-status.md + dò trực tiếp ~/AGENTS/muse-bridge

## T1 — Thiết kế icon gốc SVG 1024×1024
owner: muse
depends: -
context:
Musude = cầu nối Claude ↔ Muse ↔ Unity (điều phối plan/task). Icon cần đọc được ở 16px: 1 hình khối đơn giản (gợi ý: 2 nút nối bằng một nhịp cầu / chữ "M" cách điệu), không chữ nhỏ.
Bảng màu lấy từ index.html: accent #2f5bd3 (dark: #7d9cf5), info #6b4fc7, nền #f6f5f2 / #141413, ink #1d1d1b.
Khung theo quy ước icon macOS: squircle bo góc, nội dung trong vùng ~824px giữa, chừa lề.
verified:
muse-bridge/assets/ đang rỗng (chưa có asset icon nào).
index.html:6 <title>Musude</title>; index.html:92 <h1>Musude</h1>; biến màu ở khối :root đầu file.
refs: ~/AGENTS/muse-bridge/index.html (khối :root)
files: muse-bridge/assets/musude-icon.svg
done_when: File SVG mở được, viewBox 0 0 1024 1024, nhìn rõ khi thu nhỏ 32px (chụp so sánh 1024/128/32).

## T2 — Xuất PNG các cỡ + .icns
owner: claude
depends: T1
context:
Cỡ iconset chuẩn: 16,32,128,256,512 (@1x và @2x) → icon_16x16.png … icon_512x512@2x.png.
iconutil/sips chỉ có trên macOS → chạy trong Terminal của Mac, không chạy trong VM Cowork. Render SVG→PNG: `rsvg-convert` hoặc `qlmanage -t -s 1024`, hoặc Python (cairosvg) trong .venv.
Lệnh: `iconutil -c icns assets/Musude.iconset -o assets/Musude.icns`.
verified:
VM Cowork có python3 + Pillow 12.3.0 nhưng không có iconutil/sips.
refs: -
files: muse-bridge/assets/Musude.iconset/*.png, muse-bridge/assets/Musude.icns, muse-bridge/assets/favicon.png (64px)
done_when: Musude.icns tồn tại, `file` báo "Mac OS X icon", đủ 10 PNG trong iconset.

## T3 — Gắn icon vào Musude.app
owner: claude
depends: T2
context:
Thêm vào Info.plist: <key>CFBundleIconFile</key><string>Musude</string>. Copy Musude.icns vào Contents/Resources/ (tạo thư mục). Sau đó `touch Musude.app` và khởi động lại Dock/Finder cache nếu icon chưa đổi (`killall Dock`).
Giữ nguyên CFBundleIdentifier, không đổi launcher.
verified:
Musude.app/Contents chỉ có MacOS/MuseBridge + Info.plist; chưa có Resources/ và chưa có key CFBundleIconFile.
Info.plist: CFBundleName=Musude, CFBundleIdentifier=com.mobione.musebridge, CFBundleVersion=1.0.
MacOS/MuseBridge là bash launcher (chạy app.py qua .venv) — không cần sửa.
refs: -
files: muse-bridge/Musude.app/Contents/Info.plist, muse-bridge/Musude.app/Contents/Resources/Musude.icns
done_when: Finder và Dock hiển thị icon mới; `plutil -lint Info.plist` OK; app vẫn mở bình thường.

## T4 — Favicon + icon cửa sổ web
owner: muse
depends: T2
context:
Thêm <link rel="icon" href="assets/favicon.png"> (hoặc SVG) vào <head> của index.html. Có thể đặt logo nhỏ cạnh <h1>Musude</h1>.
Cần kiểm server.py có phục vụ thư mục assets/ không; nếu không thì thêm route tĩnh hoặc nhúng data URI.
verified:
index.html chưa có thẻ icon/favicon (grep "icon|favicon" trên index.html, app.py, server.py: 0 kết quả).
refs: ~/AGENTS/muse-bridge/server.py, index.html
files: muse-bridge/index.html, (có thể) muse-bridge/server.py
done_when: Mở app, cửa sổ/tab hiển thị favicon; không lỗi 404 trong app.log.

## T5 — Kiểm thử cuối
owner: claude
depends: T3, T4
context:
Mở lại Musude.app từ Finder, chụp Dock + cửa sổ app ở chế độ sáng/tối.
verified:
-
refs: -
files: -
done_when: Ảnh chụp cho thấy icon đúng ở Dock, Finder, trong app; app.log không có lỗi mới.

## Câu hỏi mở
- Ý tưởng hình ảnh: Linh có muốn biểu tượng cụ thể (cầu nối, chữ M, robot…) hay để Muse tự đề xuất 2–3 phương án?
- Muse có xuất file ảnh/SVG trực tiếp vào ~/AGENTS được không (status ghi: chưa có repo chung để Muse gửi asset)? Nếu không, T1 chuyển owner sang claude.
