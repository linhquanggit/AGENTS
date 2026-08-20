# Request Template — form chung cho mọi loại yêu cầu

Copy 1 trong 2 form dưới, điền vào, gửi. Field nào không có thì xóa dòng đó (đừng ghi "N/A").

---

## FORM ĐẦY ĐỦ (task lớn / rủi ro / nhiều file)

```
[LOẠI] bug | feature | refactor | migrate | review | test | explain | optimize | brainstorm | learn

[MỤC TIÊU]
<1–2 câu: muốn đạt được gì, không phải muốn làm thế nào>

[BỐI CẢNH]
- Project / module:
- File / hàm / symbol liên quan:
- Hiện tại đang:
- Mong muốn thành:

[REPRO / INPUT]        (bắt buộc nếu là bug)
- Bước tái hiện:
- Log / stacktrace / mã lỗi:
- Môi trường (branch, config, dữ liệu):

[PHẠM VI]
- ĐƯỢC sửa:
- KHÔNG đụng vào:

[RÀNG BUỘC]
- Giữ backward-compat: có / không
- Không thêm dependency mới: có / không
- Khác:

[TIÊU CHÍ HOÀN THÀNH]
- Verify bằng: <test nào / thao tác nào / kết quả nào coi là pass>

[QUYỀN]  auto | approval | bypass
   auto     = tự làm các sửa nhỏ, nội bộ
   approval = hỏi trước khi xóa file, đổi API public, refactor lớn, chạy/deploy
   bypass   = "làm hết, không cần hỏi"

[OUTPUT MONG MUỐN]  plan trước rồi mới code | code luôn | chỉ phân tích, không sửa code
```

---

## FORM NGẮN (task nhỏ, 1–2 file)

```
[LOẠI] <bug/feature/...>
[VIỆC] <việc cần làm, kèm file:line nếu biết>
[PHẠM VI] chỉ sửa <...>, không đụng <...>
[VERIFY] <cách kiểm tra là xong>
[QUYỀN] auto | approval | bypass
```

---

## Quy ước bổ sung

- **Câu hỏi hẹp → trả lời hẹp.** Muốn phân tích rộng (callers, side-effects, luồng đầy đủ) thì phải ghi rõ trong `[PHẠM VI]`, mặc định sẽ chỉ trả lời đúng câu hỏi.
- **Đính kèm ưu tiên cao nhất:** log lỗi thật, `file:line`, screenshot, diff. Có 1 dòng log > 3 đoạn mô tả.
- **Nếu chưa rõ muốn gì:** dùng `[LOẠI] brainstorm` — sẽ đưa ra các phương án + tradeoff trước, không code ngay.
- **Không ghi `[QUYỀN]`** → mặc định `approval` cho việc rủi ro, `auto` cho việc nhỏ.
- **Không ghi `[OUTPUT]`** → task không tầm thường sẽ trình plan trước và chờ duyệt.

---

## Ví dụ

**Bug**
```
[LOẠI] bug
[VIỆC] Admin tab Players báo 500 khi bấm sang trang 2 — core/admin/players.py
[REPRO] mở /admin → tab Players → chọn page 2. Log: KeyError 'total'
[PHẠM VI] chỉ sửa phần paginate, không đụng schema
[VERIFY] page 2–5 render đúng số lượng player
[QUYỀN] auto
```

**Feature**
```
[LOẠI] feature
[MỤC TIÊU] QA đổi được giờ server giữa VN và UTC ngay trên admin, không cần restart
[BỐI CẢNH] hiện tại timezone hardcode trong core/config; muốn thành switch global
[PHẠM VI] được sửa core/config + admin UI; KHÔNG đụng apis/
[VERIFY] đổi switch → mọi timestamp trong tab Players đổi theo, reload vẫn giữ
[QUYỀN] approval
[OUTPUT] plan trước rồi mới code
```

**Explain**
```
[LOẠI] explain
[VIỆC] luồng từ request /api/match tới khi trả response — chỉ liệt kê các chặng chính
[PHẠM VI] không cần đọc phần persistence
[QUYỀN] auto
```
