# Muse Win — hướng dẫn cài trên Windows

App cửa sổ để dùng **muse-cli** trên Windows (Muse không có bản Windows): Cài đặt, đăng nhập muse.ai, chat với Muse, xem ảnh/video Muse trả về.

> Trạng thái: code đã test với muse-cli **giả** trên Linux (gửi, đọc history, ghi cookie, hiện ảnh/video). **Chưa chạy thật trên Windows và chưa gọi muse.ai thật.** Mục 6 là các chỗ có thể phải chỉnh.

## 1. Chuẩn bị (một lần)

1. **Python 3.11+** từ python.org — tick *Add python.exe to PATH*.
2. **muse-cli**: mở PowerShell
   ```
   pip install uv
   uv tool install muse-cli==0.3.2
   muse-cli --help
   ```
   (hoặc `pipx install muse-cli==0.3.2`). Chạy được `muse-cli --help` là đủ.
3. **WebView2 Runtime**: Windows 10/11 thường có sẵn (đi kèm Edge). Thiếu thì tải từ Microsoft.

## 2. Chạy thử không cần build

Chép thư mục `muse-win` sang máy Windows, nhấp đúp **run.bat** (lần đầu tự tạo venv + cài pywebview). Cửa sổ Muse Win mở ra.

## 3. Build ra file .exe

Nhấp đúp **build.bat** → xong có `dist\MuseWin.exe`. Chép file này đi đâu cũng chạy (vẫn cần muse-cli đã cài). Icon tuỳ chọn: đặt `assets\musewin.ico` trước khi build.

## 4. Đăng nhập lần đầu

1. Mở Muse Win → **Cài đặt** → **Đăng nhập muse.ai**.
2. Cửa sổ muse.ai hiện ra, đăng nhập như bình thường. App tự bắt cookie `hatch_sess`, ghi vào cấu hình muse-cli, đóng cửa sổ và kiểm tra status.
3. Nếu không đăng nhập được trong cửa sổ này (vd Google chặn): lấy cookie tay từ Edge — muse.ai → F12 → Application → Cookies → `hatch_sess` → dán vào ô **Cookie** → **Lưu cookie**. Đừng đăng xuất muse.ai trên Edge, vì có thể làm cookie hết hạn.
4. Bấm **Kiểm tra muse-cli status**: thấy JSON và chấm xanh ở góc trái là xong.

## 5. Dùng

- **+ Chat mới**: tạo side chat Muse mới (`muse-cli session-start`).
- **Thêm thread ID có sẵn**: dán ID chat đã có.
- Gõ tin, **Ctrl+Enter** để gửi. Tin tự làm mới mỗi 5 giây; Muse reply sẽ hiện trong khung chat.
- Ảnh/video: app tự hiện mọi link ảnh/video, markdown `![](url)` và các trường kiểu `url`/`image`/`video` trong JSON. Nút **JSON thô** trên mỗi tin cho thấy đúng dữ liệu muse-cli trả.
- Dữ liệu app ở `%APPDATA%\MuseWin\` (config.json, threads.json). Cookie nằm trong cấu hình của muse-cli, không lưu ở đây.

## 6. Nếu có lỗi

| Hiện tượng | Cách xử |
|---|---|
| "không tìm thấy muse-cli" | Điền đường dẫn đầy đủ vào Cài đặt → *Đường dẫn muse-cli* (thường `C:\Users\<tên>\.local\bin\muse-cli.exe`). |
| "không thấy thư mục cấu hình muse-cli" / "không tìm thấy hatch_sess" | Bấm **Dò cấu hình muse-cli**, xem nó liệt kê file nào có `hatch_sess`; điền thư mục đó vào *Thư mục cấu hình*. Nếu muse-cli chưa từng đăng nhập, chạy `muse-cli auth export` một lần rồi thử lại. **Nếu Dò không ra file nào**, muse-cli trên Windows lưu cookie kiểu khác (vd keyring): gửi kết quả Dò cho người đã đưa app cho bạn để chỉnh. |
| Status `401/unauthorized` | Cookie hết hạn → Đăng nhập lại. |
| `404 session not found` | Chat đã mất trên muse.ai → **+ Chat mới**. |
| Ảnh/video báo "không tải được — có thể cần đăng nhập" | Link media cần cookie. Bấm **JSON thô** ở tin đó và gửi cho người đã đưa app để làm bước tải qua cookie. |
| Cửa sổ trắng khi mở | Cài WebView2 Runtime (mục 1.3). |
| Nút Đăng nhập bị mờ | Đang chạy bằng `--browser`; chạy bằng run.bat / .exe. |

Debug bằng trình duyệt: `python musewin.py --browser` rồi mở URL in ra.

## 7. Có gì và chưa có gì

Có: Cài đặt, đăng nhập, chat nhiều thread, xem ảnh/video. Chưa có: Hoạt động/Kế hoạch/Task/Terminal của Musude, chạy Claude Code từ app.
