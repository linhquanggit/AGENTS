# Capture trước merge main -> /main/Mail (Events, 2026-10-21)
Base = cs:8873, Mail = cs:8877. Chỉ còn 5 conflict thật; cảnh báo "no longer apply" (AppLovinSettings.asset, BattlePassData.cs, CSVToChips.cs) không nằm trong thay đổi của Mail.

Phần Mail cần giữ lại sau merge (patch: conflict-*.real.patch / conflict-*.patch):
- DHS.cs: chỉ xuống dòng chữ ký ClaimEvent (format) -> lấy bản main.
- HolidayNoel.cs: thêm `GrantFromMail(IAP_Product, out string)` sau MarkGiftPackPurchased.
- PVP.cs: thêm `GrantFromMail()` (premium booster 7 ngày) sau hàm booster; phần còn lại chỉ là CRLF->LF.
- Shops.cs: thêm `GemPacks => asset.Value.gemPacks` (vùng GOLD & GEM) và `GrantFromMail()` cho PackData (trước `OpenContainer`); còn lại là CRLF/BOM.
- Sweep.cs: thêm `GrantFromMail(SweepType)` sau PurchasedSweepSub.
Full: mail-changes.patch, bản gốc base/ và mail/, changed-files.txt, cs8877-all.txt (cả file Added/Deleted).
