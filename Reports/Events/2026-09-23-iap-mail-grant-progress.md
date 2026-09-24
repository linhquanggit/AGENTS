# Mail trả quà bằng IAP pack — báo cáo tiến trình 2026-09-23

Chi tiết đầy đủ (plan/checkpoint sống, đọc tiếp ở phiên sau): `/Users/mobione/wkspaces/Events/MAIL_IAP_FEATURE_PROGRESS.md.private` + `IAP_MAIL_GRANT_PLAN.csv.private`. File này chỉ tóm tắt phần đã làm trong phiên hôm nay.

**Tóm tắt trạng thái toàn feature (chốt cuối phiên):**
| Phase | Tổng | Đã xong | Còn lại |
|---|---|---|---|
| Phase 1 | 87 | 78 (test OK) | 5 BLOCKED (thiếu data) + 1 tạm hoãn (CS xử lý tay) |
| Phase 2 | 5 (loại 2 SKU ngừng bán) | 5 (test OK) | HOÀN TẤT |
| Phase 3 | 40 | 40 (test OK) | HOÀN TẤT |
| Phase 4 | 22 | 0 | Chưa bắt đầu — việc lớn duy nhất còn lại |

## Phase 1 (87 product) — HOÀN TẤT 78/87
Toàn bộ TestGroup 1-9 đã user xác nhận test OK qua MockServer (claim thật trong game, verify log qua `flow-capture.log`).
- Còn lại: `aa_airdefense6-10` (5 SKU) — **BLOCKED, thiếu data** trong `AirDefense.asset` (list `Purchases` chỉ có idProduct 83-87 + 148-150, không có 88-92). Code đúng, chỉ thiếu data. User xác nhận 5 SKU này chưa active trong game, bỏ qua không chặn tiến độ.
- `aa_bmbuyall` (1 SKU, BlackMarket) — tạm hoãn, để CS xử lý tay.

## 3 bug phát hiện + fix trong lúc test (đã test lại OK cả 3)
1. **`SevenDaysMission.cs`** — case `EVENT_POINT`/`FIGHTER_6`/`WINGMAN_4` tự mở popup game (đúng khi mua IAP thật, SAI khi claim mail). Fix: thêm `GrantFromMail(IAP_Product)` + helper `OnAddRewardSilent()` (method mới, không sửa `OnBuyIap`/`OnAddReward` cũ — vẫn dùng cho 3 nơi khác trong game).
2. **`RtfSpawnReward.cs:52`** — thiếu guard cho `REWARD_TYPE.IAP_PRODUCT` (chỉ có cho `REMOVE_ADS`) → `NullReferenceException` khi Claim All nhiều mail cùng lúc. Fix: thêm 1 dòng skip, giống pattern `REMOVE_ADS` có sẵn.
3. **`IapProductRewards.cs` (`GrantAircraftConvert`, aa_aircraft6discount1-2)** — cùng loại bug #1, khác module. Fix: thêm `GrantAircraftConvertFromMail()` (method mới), bỏ dòng `.Show()` mở popup.

Nguyên tắc chung đã chốt với user cho cả 3 fix: **mail chỉ thuần cấp thưởng, tuyệt đối không trigger UI riêng của module** — method cũ luôn giữ nguyên (dùng chung với luồng mua IAP thật), chỉ thêm method mới song song cho nhánh mail.

**Convention tên method đã chốt:** mọi module tự expose đúng tên `GrantFromMail(...)` làm entry point cấp thưởng qua mail (không phải `<TênHàmCũ>FromMail`) — khớp `WorldCup`/`HolidayNoel`/`MilestoneProgression`/`PiggyBank`/`Sweep`/`SevenDaysMission`.

## Tooling mới: FlowCaptureLogger
`Assets/QL_Tools/FlowCaptureLogger.cs` (editor-only, `#if UNITY_EDITOR`) — `IapProductRewards.GrantLog()` (gate mới, thay 37 chỗ gọi `DPDebug.Log` trực tiếp) vừa log Console vừa ghi `~/.claude/logs/flow-capture.log`. Từ giờ verify log bằng cách đọc file này (agent tự đọc được) thay vì hỏi user copy Console.

## Known issue — không sửa (theo quyết định user)
Claim All không set `isread` (chỉ set `isrewardclaimed`) — `isread` chỉ set qua bấm mở từng mail. Sau Claim All, icon notify mail ở `UIHome` không bao giờ tắt, list mail không hiện "đã đọc". Bug thuộc hệ mailbox gốc, không riêng feature này.

**TODO cuối cùng (chưa làm, chưa có thiết kế thay thế):** user quyết định **bỏ hẳn tính năng Claim All** (`UIMailBox._OnClaimAllClick`) thay vì sửa bug isread ở trên — cần bàn thiết kế UI thay thế khi tới lượt.

## Phase 2 (5 product sau khi loại 2 SKU, R1) — HOÀN TẤT 5/5 WIRE + TEST OK
Audit thẳng code thay vì tin risk-note cũ trong CSV — rủi ro hẹp hơn tưởng:
- **An toàn, wire thẳng không cần guard:** `aa_milestoneboost1/2` (cờ bool vĩnh viễn, không thể hạ cấp), `aa_monthlycard` (cộng dồn ngày, không ghi đè).
- **Rủi ro thật, cần guard:** `aa_premiumsweepsub` — `Sweep.GrantFromMail(SweepType)` (method mới, `Sweep.cs`) cộng dồn hạn thay vì reset cứng 30 ngày. `aa_7dayultrabooster` — `PVP.GrantFromMail()` (method mới, `PVP.cs`) cộng dồn 7 ngày dựa trên `PremiumFinish` hiện có thay vì reset cứng.
- Cả 5 SKU đã claim thật qua MockServer, log khớp Log Target.

**Phát hiện quan trọng khi audit Sweep:** `aa_basicsweepsub`/`aa_advancedsweepsub` — tra `SweepSubscription.cs` (UI mua thật) thấy **chỉ còn đúng 1 sản phẩm bán được là `aa_premiumsweepsub`** (`idProductPremium`, hardcode `SweepType.Premium`). `SweepType.Basic`/`Advance` + data trong `Sweeps.asset` vẫn còn tồn tại trong code nhưng không còn đường mua thật nào dẫn tới 2 SKU này — tàn dư từ bản cũ (từng bán 3 gói, giờ chỉ còn Free/Premium). **Quyết định:** loại 2 SKU này khỏi Phase 2, không wire.

## Nguyên tắc chung mới chốt (2026-09-23): cộng dồn hạn khi claim mail
Áp dụng cho MỌI module có state thời hạn, không riêng Sweep/PVP:
- **"Duration" (N ngày kể từ lúc mua, không gắn season)** → **CỘNG DỒN**: hạn hiện tại còn > now thì lấy hạn đó làm mốc +N ngày. Ví dụ: Sweep subscription, PVP booster (`PremiumFinish` = +7 ngày kể từ lúc mua, không gắn season — đã audit code xác nhận).
- **"Season" (gắn mùa giải/chu kỳ cố định, vd Mission/Battle Pass premium)** → **KHÔNG CỘNG DỒN**: chỉ là cờ unlock cho mùa hiện tại, không có duration để cộng. Ví dụ: `aa_premium_missionpass` (đã wire ở Phase 1, đúng bản chất vì chỉ set cờ).
- Cách phân biệt: đọc code thật xem field hạn tính bằng `now + N` hay theo lịch mùa cố định — không đoán.
