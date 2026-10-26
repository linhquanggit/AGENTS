# IapProductRewards.Grant() — test Play mode theo TestGroup (Events)

Ngày: 2026-10-23 (lần chạy đầu 2026-10-21). Chỉ test và báo cáo, không sửa code.
Phương pháp: Play mode qua Unity MCP, snapshot JSON `Player.data` trước / sau lần gọi 1 / sau lần gọi 2. Phạm vi: 130 product có TestGroup (nhóm 1–13 và P4-OH). Không chạy product Phase 2–4 không có TestGroup.

## Kết luận chính
- 13 product chứng minh được guard chống cấp trùng: powerboostoffer2–9, limitedbundle, beginnerpack, goalpointpack4, 3dpremium, 3ddeluxe.
- 8 product xác minh đủ chu trình (reset flag → Grant → Grant lần 2): endlessgembanner, growthupgem1–3, growthuppiece1–3, hqupgrade.
- 28 product `aa_7day*supply*`: lần chạy đầu ném `NullReferenceException` do `UIManager.Instance == null` (scene không có UIManager). Chạy lại trong scene Menu (có UIManager): cả 28 trả `true` hai lần, không exception, reward cộng cả hai lần (không có guard chống trùng ở reward).
- Consumable cộng lại lần 2 theo thiết kế: gempack1–15, gridevent/dailygridevent/gridprogress, pvp exp, ticketsupply, airdefense1–5, turretpack1–3.
- airdefense6–10 fail cả hai lần vì thiếu `PurchaseShopData`, đúng như CSV (BLOCKED).

## Lỗi cấu hình/dữ liệu phát hiện
1. **`aa_supplypack_origin` bị map sai pack.** `Shops.OfferPacks` có hai pack cùng `IDProductOrigin = aa_supplypack_origin` (`POWER_BOOST_OFFER_9` và `SUPPLY_PACK`). `GrantOfferPack` dùng `FirstOrDefault` nên mail `aa_supplypack_origin` đi vào `POWER_BOOST_OFFER_9`.
2. **`aa_powerboostoffer5_origin` bị map sai pack.** Cùng `IDProductOrigin` trên 4 pack (`POWER_BOOST_OFFER_5/6/7/8`); log ghi nhận Grant đi vào `POWER_BOOST_OFFER_8`. Nếu `OFFER_8` chưa mua, mail origin5 sẽ cấp nhầm `OFFER_8`.
3. **`aa_supplypack` cấp trùng.** Asset có `OneTimePurchase=False` (`IsBought` luôn False) nên lần 2 vẫn cộng +50.000 gold, +500 gem. CSV ghi guard `IsBought (OneTimePurchase)`, lệch với asset. Cần xác nhận asset sai hay CSV sai.
4. **`aa_scrapfactory1–3` không chống trùng** (lần 2 cộng gem lần nữa). Có thể là thiết kế "cấp cố định", CSV chưa ghi chấp nhận.
5. **`aa_operationevent1`** fail "đã nhận mốc 2 (index 2)" trong khi `operationevent2–4` thành công. Chưa rõ có khớp với ghi chú CSV "không đối chiếu product".

## Chưa xác minh được (trạng thái save đã có sẵn, hoặc điều kiện không thỏa)
- WorldCup (4 SKU) và 3dbasic, chippremiumpack, offer pack đã mua (powerboostoffer1, removeads, starterpack): đã `IsBought` từ trước, đường cấp thưởng chưa chạy.
- eventcur1–5: `IsEventActive=false` (đúng CSV), đường cấp thưởng chưa chạy.
- goalpointpack1 (fail "hết lượt/không tìm thấy"), goalpointpack2–3 (`true` hai lần, chưa rõ Limit).
- eventendlesspoint, pack16, premiumpvppass, aircraft6discount1–2: Grant trả `true` nhưng `Player.data` không đổi (flag đã bật; Fighter 6 star 10 cao hơn pack star 7 và được giữ nguyên, đúng nhánh "không hạ star").
- pvp exp: state đổi cả hai lần, số exp chưa đối chiếu chi tiết.

## Ảnh hưởng lên save và cách khôi phục
Save dev bị đổi nhiều: gold ~14,5M → ~200M (tăng thêm sau lần chạy 7day thứ hai), gem 106.315 → ~277.000, cộng thêm vé, pieces, state AirDefense/Piggy/Fighters. Flag Banner/Salary đã được reset rồi Grant lại, kết thúc ở `true` như ban đầu.
Backup (trước lần chạy đầu): `/private/tmp/claude-501/-Users-mobione-wkspaces-Events/1f4cc14c-f77d-40db-b201-e1686755711a/scratchpad/save-backup/` gồm plist PlayerPrefs `unity.OneGame.1941 Air Attack.plist` và thư mục `appsupport`. Chưa khôi phục. Muốn khôi phục thì đóng Editor trước khi copy plist về `~/Library/Preferences/`.
Editor hiện đang mở scene `Assets/Project/Scenes/Menu.unity` (trước đó là scene untitled).

## Bổ sung 2026-10-23 (test lại toàn bộ trước khi build test)
Chạy lại 140 product đã wire (135 cũ + 5 Boss Tower) trong scene Menu (có UIManager). Kết quả:
- Không product nào ném exception. 28 `aa_7day*` chạy sạch, reward cộng cả hai lần (không guard).
- Boss Tower (TG14) đã xác minh đủ: `ticketdaily`, `ticket` cộng ticket mỗi lần (asset: +2 và +15, không phải +50/+5); bypass giới hạn đã kiểm bằng cách đặt bộ đếm chạm trần (daily 999/999, ticket 2/2), Grant vẫn cộng và bộ đếm không tăng; `booster`, `reroll`, `revive` reset flag → lần 1 `true`, lần 2 `false`.
- MilestoneProgression goalpointpack1–4: Limit 2/2/2/1, đã chạm trần nên chặn đúng (lần chạy trước pack2, 3 `true` hai lần là vì Limit=2).
- `milestoneboost1`, `milestoneboost2`: `true` hai lần, lần 2 state không đổi (idempotent).
- `aa_pvp_smallexp/mediumexp/largeexp` giờ `false/false`: Season Pass đã MAX do các lần test trước cộng exp nhiều lần (`PVP.AddSeasonPassExpByProduct` trả 0 khi `BattlePass.IsCompleted`). Config 3 gói hợp lệ (150/400/900 exp).
- `goalpointpack1` và `3dbasic` trả `false` nhưng `MilestoneProgressionProcess1` / `FirstPurchaseProcess` đổi ở lần gọi đầu, lần 2 không đổi. Nhiều khả năng là khởi tạo lười khi đọc process, chưa xác nhận bằng diff chi tiết.
- Vẫn chưa kiểm được đường cấp thưởng từ trạng thái sạch: WorldCup (4), eventcur1–5 (event đóng), các offer pack/_origin, 3d*, chippremiumpack, operationevent1, eventendlesspoint, pack16, premiumpvppass, aircraft6discount1–2. Nguyên nhân: save đã mua sẵn hoặc điều kiện chưa thỏa.
