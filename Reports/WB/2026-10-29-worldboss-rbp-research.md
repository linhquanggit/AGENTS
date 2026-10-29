# WorldBoss × RewardByProgression (RBP) — nghiên cứu chuẩn bị chuyển đổi

Trạng thái: ĐÃ TÍCH HỢP 2026-10-29 (code + data TEMP). Chi tiết: catalog `AI/Unity/knowledge/snapshots/reward-by-progression-catalog.md` mục 4. Đã chốt: nhân hệ số hạng vào gem; chốt group theo mùa; shop không chuyển; không migrate save (WB chưa publish).

Ngày: 2026-10-29 · Phạm vi: chỉ đọc code + data trong `~/wkspaces/WB` (không sửa gì).
Đường dẫn dưới đây tính từ `Assets/Project/`.

## 0. Kết luận nhanh
- WorldBoss **chưa dùng RBP** ở đâu cả: code không gọi `RewardByProgressionSystem` / `DPUtils.GetRewardByProgress`, data `Resources/Data/WorldBossSO.asset` không có Type ≥ 1.000.000.
- Có **2 nguồn thưởng nên chuyển**: thưởng xếp hạng cuối mùa (RewardsByLevel × hệ số hạng) và BattlePass (Free/Premium). Shop đổi, IAP/booster và currency sau trận **giữ nguyên** (lý do ở mục 3).
- Mẫu nên theo: **BattleTeam** (data trộn: Type `BP_*` thì quy đổi, Type thường thì cấp thẳng; chốt group + chapter 1 lần mỗi mùa) và **PVP BattlePass** (GOLD qua `GoldByChapter`).
- Không cần sửa server: thưởng do client cộng theo config ("server chỉ trả status").

## 1. Lõi RBP (tóm tắt để đối chiếu)
| Thành phần | Ý chính |
|---|---|
| `RewardByProgression/Scripts/RewardByProgressionSystem.cs` | Static entry. `GetRewardCount(id, gem[, group])`, `Resolve(...)`, `IsProgressionReward`, `TryGetRuntimeRewardType`, `CurrentGroup` |
| `RewardByProgressionResolver.cs` | `Quantity = floor(Gem / Price)`, price theo (RewardId, Group). Có `UsedGemValue`, `RemainderGemValue` |
| `GameProgressionLevel.cs` + `ProgressionLevel.cs` | Effective level = level thường; max rồi thì `cap + Abyss` |
| `Resources/Data/RewardByProgressionConfig.asset` | Groups: EARLY 1–150, MID 151–250, LATE 251+. Firebase key `reward_by_progression_groups` ghi đè mốc (áp ở `CC_Manager/Firebase_Analytics.cs:532`) |
| `DP/Scripts/DPUtils.cs:2418-2478` | Helper cũ: nhận Type **thường** + Count = gem, tự cộng offset; type không có alias thì trả nguyên Count. Mặc định gold id `SPIN_GOLD` |
| `Rewards/Scripts/RewardMultiplier.cs` | Type + Multiplier float, giữ `Count` legacy |

**Giá hiện có của các loại WorldBoss đang dùng** (EARLY / MID / LATE, đơn vị gem/1 món):

| Alias | Giá | Hệ quả khi quy đổi |
|---|---|---|
| BP_GOLD 1000001 | 5 / 5 / 5 | Quantity là **hệ số** cho `GoldByChapter` → scale theo chapter, không theo group |
| BP_GEM 1000002 | 1 / 1 / 1 | Không scale (gem = gem) |
| BP_ENERGY 1000006 | 1.5 / 1.5 / 1.5 | Không scale theo group |
| BP_BF_TICKET 1000008 | 1.5 / 0.5 / 0.1 | **LATE nhận gấp 15 lần EARLY** (vd 30 gem → 20 / 60 / 300 vé) |
| BP_FIGHTER/WINGMAN pieces, CHIP_TICKET, AWARD_* | có đủ 3 group | (chỉ liên quan nếu đưa shop vào) |
| WB_CURRENCY_1..6 (60001–6) | **không có alias** | Giữ cấp thẳng |

→ Thiết kế cần biết: với bảng giá hiện tại, chuyển RBP chỉ làm **BF_TICKET** và **GOLD** (qua chapter) thay đổi theo tiến độ; GEM/ENERGY sẽ y như cố định.

## 2. Bản đồ thưởng WorldBoss hiện tại
| # | Nguồn | Data | Tính ở | Cấp quà ở | Hiển thị ở |
|---|---|---|---|---|---|
| A | Thưởng xếp hạng cuối mùa | `RewardConfig.RewardsByLevel` (100 level, mỗi level: GOLD, GEM, WB_CURRENCY_1, BF_TICKET, ENERGY) — CSV `SetupData/FileCSV/WorldBoss/RewardsByLevel.csv` | `WorldBossSO.GetRankRewards` (WorldBossSO.cs:294-307): Clone rồi `Count *= MultiplyReward` theo hạng | `PopupPreviousWorldBossResult._OnTapToOpen` (:58) → `PopupWorldBossResultReward._Init` (:25) `Rewards.AddReward` | `UIWorldBoss.ShowCurrentReward` (:383-385), `UIWorldBossLeaderBoardReward.InitDict` (:52-63) + `SetBaseRewardPreview` (:121-122) |
| B | BattlePass | `BattlePassMilestones` (100 mốc, Free=GOLD 500, Premium=GEM 20) — CSV `BattlePass.csv` | `WorldBoss.BattlePassFree/PremiumGetRewardsClaim` (WorldBoss.cs:1189-1230), chỉ Clone | `BattlePassFreeClaim/PremiumClaim` (:1162-1186) | `ScrollerWorldBossPass` (:56) → `ItemWorldBossPassCellView.SetData`; `ScrollerWorldBossPassReward` (:18-19, cộng tổng) |
| C | Shop đổi (Daily/Special) | `ShopData.*Shops[].Reward` (pieces, chip ticket, award) mua bằng WB currency | — | `ItemWorldBossPackage` (:125-126) | `PopupWorldBossShopExchange` |
| D | IAP/booster | `PurchaseShopData` (`WORLD_BOSS_PURCHASE_TYPE` – không phải REWARD_TYPE) | — | `ItemWorldBossPurchase` (:141) | — |
| E | Currency sau trận | `DamageConversions` | `WorldBossSO` (damage % → multiplier) | `UIWorldBossFinished` (:45) | — |
| F | Bảng "loại quà theo khoảng level" | `RewardSprites[].RewardTypes` (sinh bởi `CastRewardType`, WorldBossSO.cs:558-584) | editor | — | `WorldBossRewardInfoScrollView` (:41-50) — chỉ icon theo Type |

**Luồng mùa (quan trọng cho việc chốt group):**
- Mùa = `start_time` server. `WorldBoss.CheckSeasonChanged` (WorldBoss.cs:172-205) wipe `Process` khi start_time mới (giữ `LastBattleResult`, `HasSeenInformation`).
- Thưởng hạng được nhận trong pha **REDEMPTION của chính mùa đó** (`UIWorldBoss.OnConnectNewSeasonCallback` → `ShowPreviousResult`, :226-237, :278-303) **trước** khi `SetEventStartTime` của mùa mới chạy → dữ liệu chốt lưu trong `Process` vẫn còn lúc claim.
- `rewardLevel` của thưởng hạng = mốc damage **toàn server** đạt được (`GetRewardLevel(bossLevel, phase)` = `(bossLevel-1)*PHASES_PER_LEVEL + phase + 1`) → đây là trục "tiến độ boss", khác trục "tiến độ người chơi" của RBP.

## 3. Đề xuất phạm vi
| Nguồn | Chuyển RBP? | Lý do |
|---|---|---|
| A. Thưởng hạng | ✅ | Giữ trục boss (RewardLevel) làm **ngân sách gem**, RBP quy đổi theo group người chơi → 2 trục bổ sung nhau |
| B. BattlePass | ✅ | Trùng mẫu PVP BattlePass |
| C. Shop đổi | ❌ (đề xuất) | Có giá bán bằng WB currency; scale số lượng = đổi tỷ giá currency theo group. Nếu muốn, làm sau như BlackMarket (`NewOfferIAPs/BlackMarket/Scripts/BlackMarket.cs:303-317`) |
| D. IAP/booster | ❌ | Không phải REWARD_TYPE |
| E. Currency sau trận | ❌ | Currency riêng module, không có alias |
| WB_CURRENCY trong thưởng hạng | ❌ cấp thẳng | Không có alias; giữ nguyên Count |

## 4. Thiết kế đề xuất

### 4.1 Data schema — giữ `RewardCount`, Type quyết định cách tính (mẫu BattleTeam)
- `Type` là alias `BP_*` → `Count` = **gem value**, quy đổi qua RBP.
- `Type` thường (vd `WB_CURRENCY_1`) → `Count` = số lượng cấp thẳng.
- Ưu điểm: **không đổi schema** `RewardByLevel.Rewards` / `BattlePassMilestoneData` (không mất data serialize), CSV giữ format, chỉ đổi header/cột Type sang số alias (1000001, 1000002, 1000008, 1000006). Rõ ràng hơn `DPUtils` (tự cộng offset ngầm: mọi GOLD đều bị coi là gem value).
- Phương án khác (nếu bố muốn giống BossTower/PVP): đổi field sang `RewardByProgressionEntry`/`RewardMultiplier` — phải migrate asset, sửa CsvToWorldBoss nhiều hơn. Lợi duy nhất: gem value float. Hiện các giá đều cho kết quả ổn với gem nguyên → không cần.

### 4.2 Một điểm resolve duy nhất cho module
Thêm (tên tạm) `WorldBossRewardResolver` (static, trong `WorldBoss/Use/Scripts/`):
```
RewardCount Resolve(RewardCount raw, int multiplier = 1)
  - raw null/NONE/Count<=0  → null
  - IsProgressionReward(raw.Type):
        gem = raw.Count * multiplier          // nhân hệ số hạng TRƯỚC khi floor
        r   = RewardByProgressionSystem.Resolve(raw.Type, gem, LockedGroup)
        type = runtime type; count = r.Quantity
        GOLD → GoldByChapter.GetGoldByChapterWithMultiply(id, count, LockedChapter, LockedAbyss, LockedGroup)
  - else → Clone, Count *= multiplier
List<RewardCount> ResolveList(...)
```
Mọi chỗ ở cột "Tính ở" + "Hiển thị ở" của mục 2 (A, B) **phải** đi qua hàm này, nếu không UI preview sẽ hiện gem value thay vì số quà:
1. `WorldBossSO.GetRankRewards` (grant hạng)
2. `UIWorldBoss.ShowCurrentReward` (scroll.Init)
3. `UIWorldBossLeaderBoardReward.InitDict` + `SetBaseRewardPreview`
4. `WorldBoss.AddBattlePassReward` (grant BP)
5. `ScrollerWorldBossPass` → `ItemWorldBossPassCellView.SetData`
6. `ScrollerWorldBossPassReward.Accumulate` (cộng tổng sau resolve)
7. `WorldBossSO.CastRewardType` (editor) → ghi **runtime type** vào `RewardTypes` để icon đúng (đề phòng `Rewards.GetData(BP_*)` không có sprite)

### 4.3 Chốt group + chapter theo mùa
- Thêm vào `WorldBoss.Process`: `PROGRESSION_GROUP RewardGroup`, `int RewardChapter`, `bool RewardAbyss`, `bool IsRewardLocked`.
- `LockRewardProgression()` gọi **sau** `SetEventStartTime` trong `UIWorldBoss.InitData` và `OnConnectNewSeasonCallback` (lúc mùa ACTIVE). Đã chốt thì bỏ qua. Mùa mới → `ResetAll` tạo Process mới → tự chốt lại.
- Copy logic chapter/abyss của `Events/BattleTeam/Scripts/BattleTeam.cs:74-97` (cùng điều kiện với `GoldByChapter.GetGoldBase`).
- Fallback khi chưa chốt (save cũ, hoặc user chỉ vào lúc REDEMPTION): chốt ngay theo hiện tại + log, giống migration `PVP/Scripts/PVP.cs:195-207`.
- Lý do chốt: (1) mùa kéo dài 1 tuần, user lên group giữa mùa thì preview ≠ grant; (2) Firebase đổi mốc group giữa mùa không làm đổi quà; (3) GOLD theo chapter cũng cố định trong mùa.

### 4.4 GOLD
- Thêm `GOLD_BY_CHAPTER_ID.WORLD_BOSS_RANK`, `WORLD_BOSS_BATTLE_PASS` **cuối enum** (`Scripts/GoldByChapterSO.cs`, sau `BOSS_TOWER`) + 2 dòng trong `Resources/Data/GoldByChapter.asset` (Multiply, LevelByChapter, LevelDifficulty — cần số từ design).
- Vì quantity GOLD là hệ số nhân, gem budget GOLD sẽ nhỏ (vd 25 gem / giá 5 = ×5 vàng chapter).

### 4.5 CSV / data
- `CsvToWorldBoss.cs`: không bắt buộc sửa (đã parse số enum, `Enum.IsDefined` nhận alias). Nên thêm cảnh báo khi gặp GOLD/GEM/BF_TICKET/ENERGY **không** alias (dễ nhập nhầm) và cập nhật comment header.
- `RewardsByLevel.csv`: header `Level,1000001,1000002,60001,1000008,1000006`; ô = gem value (WB_CURRENCY vẫn là số lượng).
- `BattlePass.csv`: cột Type 1000001/1000002; Count = gem value.
- Data hiện tại trông như placeholder (GOLD = Level×1000, BP 500/20 đều) → **cần bảng gem budget từ design**; chưa có thì đặt TEMP.

### 4.6 Kiểm chứng
- Script `execute_code` in bảng 3 group × (level 1/50/100 thưởng hạng, mốc BP 1/50/100) với group override, so preview vs grant.
- Hack: thêm vào `UI/Scripts/Setting/PopupWorldBossHack.cs` nút xem/đổi group đã chốt (tuỳ chọn).
- Console không có `[RewardByProgression] Cannot resolve`.
- Sau khi xong: thêm Case WorldBoss vào `AGENTS/AI/Unity/knowledge/snapshots/reward-by-progression-catalog.md` (living doc).

## 5. Thứ tự làm (dự kiến)
1. Resolver + lock group/chapter + enum gold id (code, không đụng data) — data cũ vẫn chạy y nguyên vì Type thường → cấp thẳng.
2. Chuyển BattlePass (data + 2 UI) — mẫu PVP, rủi ro thấp.
3. Chuyển thưởng hạng (data + 3 UI + CastRewardType).
4. Test 3 group, cập nhật catalog.

## 6. Câu hỏi cần bố chốt
1. Schema: giữ `RewardCount` + alias (đề xuất) hay đổi sang `RewardByProgressionEntry`/`RewardMultiplier`?
2. Hệ số hạng nhân vào **gem trước khi quy đổi** (đề xuất, ít hao do floor) hay nhân **số lượng sau quy đổi**?
3. Chốt group lúc vào mùa (đề xuất) hay lúc claim?
4. Shop đổi có đưa vào RBP không?
5. Bảng gem budget RewardsByLevel/BattlePass + 2 dòng GoldByChapter: design có chưa, hay đặt TEMP?
6. Với giá hiện tại GEM/ENERGY không scale theo group — có cần đề xuất giá riêng không? (Bảng giá dùng chung toàn game, đổi là ảnh hưởng mọi module.)

## 7. Phát hiện phụ (ngoài phạm vi, chưa sửa)
- Thưởng hạng luôn là `WB_CURRENCY_1` cho mọi boss (CSV header cứng 60001), trong khi currency sau trận dùng `GetCurrencyRewardType(bossID)` (WB_CURRENCY_1..6). Cần xác nhận có chủ ý không.
- `UIWorldBoss.ShowCurrentReward` và `UIWorldBossLeaderBoardReward` gọi `GetRewardBoxSprite(Phase)` nhưng hàm so sánh với `TargetLevel` (reward level) → có thể chọn sai sprite hộp.
- Catalog snapshot mục 0 còn ghi Groups mặc định cũ; asset hiện là 150/250.
