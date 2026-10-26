# Reports Index

Bộ nhớ báo cáo dùng chung cho mọi project, nằm ngoài SCM của từng project (xem [AGENTS.md](../AGENTS.md#reports-folder-all-profiles)). Tổ chức theo `<tên-project>/`, mỗi report là 1 file `<YYYY-MM-DD>-<slug>.md`.

Entry line format: `- [title](<tên-project>/file.md) — hook (project)`

<!-- entries below, one per report, newest first -->
- [IapProductRewards.Grant() test Play mode (Events)](Events/2026-10-23-iap-grant-playmode-test.md) — 130 product theo TestGroup; 2 lỗi map `_origin` (supplypack_origin, powerboostoffer5_origin), supplypack không guard, 7day cần UIManager (Events)
- [Mail trả quà bằng IAP pack — tiến trình 2026-09-23](Events/2026-09-23-iap-mail-grant-progress.md) — Phase 1 hoàn tất 78/87 + 3 bug fix (UI-popup-side-effect x2, IAP_PRODUCT crash) + FlowCaptureLogger; Phase 2 đang audit, phát hiện Sweep Basic/Advance đã ngừng bán (Events)
- [Merge main→Mail capture (Events)](Events/2026-10-21-merge-main-to-mail-capture/SUMMARY.md) — 5 conflict + phần GrantFromMail cần giữ
