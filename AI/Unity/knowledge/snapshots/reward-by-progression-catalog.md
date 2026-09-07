# Snapshot: Catalog các module dùng RewardByProgression — dữ liệu so sánh khi làm module mới

**Mục đích:** trước khi cắm RewardByProgression vào 1 module mới, đọc catalog này để so sánh cách các module trước đã làm (data schema, alias id, cách xử lý GOLD, ràng buộc giá dùng chung) thay vì đọc lại từ đầu.
**Snapshot bắt đầu:** 2026-08-24. Nguồn lõi: `Assets/Project/RewardByProgression/Scripts/*`.
**Living doc** — thêm 1 case mới mỗi khi 1 module hoàn thành tích hợp. KHÔNG xoá case cũ khi thêm case mới.

---

## 0. Cơ chế lõi (bắt buộc biết trước khi so sánh case)

- Entry point: `RewardByProgressionSystem.GetRewardCount(rewardId, gemValue)` / `.GetQuantity(...)` — [RewardByProgressionSystem.cs:35-65](Assets/Project/RewardByProgression/Scripts/RewardByProgressionSystem.cs#L35-L65). Tự đọc `GameProgressionLevel.Current` (effective level = gộp level thường + Abyss, xem [ProgressionLevel.cs](Assets/Project/RewardByProgression/Scripts/ProgressionLevel.cs)), không cần truyền level thủ công.
- `Quantity = floor(GemValue / PricePerUnit)`, giá lấy theo `PROGRESSION_GROUP` (EARLY/MID/LATE) ứng với effective level — [RewardByProgressionResolver.cs:39-43](Assets/Project/RewardByProgression/Scripts/RewardByProgressionResolver.cs#L39-L43).
- **`BP_` = "By Progress"**, KHÔNG phải viết tắt của BattlePass — xem comment gốc "DANH SÁCH REWARD BY PROGRESS mới. tất cả các reward mới sẽ + offset: 1_000_000" ở [Rewards.cs:699](Assets/Project/Rewards/Scripts/Rewards.cs#L699). Đây là một **quy ước đặt tên chung cho toàn bộ enum `REWARD_TYPE`**, không thuộc riêng module nào: alias = `runtime REWARD_TYPE + 1,000,000` (vd `GEM=2 → BP_GEM=1000002`). Danh sách đã có sẵn ~20 alias — [Rewards.cs:700-721](Assets/Project/Rewards/Scripts/Rewards.cs#L700-L721): `BP_GOLD, BP_GEM, BP_FIGHTER_SPECIAL_PIECES, BP_FIGHTER_ULTRA_PIECES, BP_WINGMAN_SPECIAL_PIECES, BP_WINGMAN_ULTRA_PIECES, BP_ENERGY, BP_BF_TICKET, BP_PROMOTE_MEDAL, BP_REWARD_CONTAINER_FIGHTER, BP_REWARD_CONTAINER_WINGMAN, BP_CHIP_TICKET_NORMAL, BP_CHIP_TICKET_SPECIAL, BP_AWARD_*_RANDOM (COMMON/RARE/EPIC/LEGEND/MYTHIC/LEGEND_EVENT/MYTHIC_EVENT), BP_CHIP_RANDOM`.
- **Bảng giá `RewardByProgressionConfig` (asset `Resources/Data/RewardByProgressionConfig`) là MỘT NGUỒN CHÂN LÝ DÙNG CHUNG CHO TOÀN GAME theo thiết kế** — giống vai trò của `GoldByChapter` cho gold. Mỗi RewardId chỉ có 1 dòng giá × mỗi group (EARLY/MID/LATE); mọi module cần "quy đổi gem theo tiến độ" cho cùng 1 loại reward thì DÙNG CHUNG dòng giá đó, đây là chủ đích chứ không phải rủi ro trùng lặp cần né. Chỉ tạo `REWARD_TYPE` mới (và alias mới) khi thật sự cần một loại reward CHƯA có trong danh sách trên, không phải để "tách giá" cho riêng module.
- `RewardByProgressionConfig.Validate()` chạy lúc `Service` khởi tạo lần đầu (static, dùng chung toàn app) — thiếu giá 1 group cho 1 RewardId sẽ throw ngay từ module ĐẦU TIÊN gọi tới, không riêng module đang sửa.
- Muốn số lẻ (không tròn) hoặc migrate êm từ Count cũ → dùng struct có sẵn [`RewardMultiplier`](Assets/Project/Rewards/Scripts/RewardMultiplier.cs) (Type + Multiplier float, giữ field `Count` cũ để đọc data legacy).

## 1. Bảng so sánh nhanh

| Module | Data schema | Alias RewardId dùng | Nơi resolve | Xử lý GOLD |
|---|---|---|---|---|
| PVP BattlePass | `RewardMultiplier` (Type+Multiplier) trong `BattlePassLevelData.FreeReward/PremiumReward` | `BP_GOLD`, `BP_GEM`, ... (dùng chung) | `BattlePassData.ResolveReward()` | Quantity resolve → làm **multiplier** cho `GoldByChapter.GetGoldByChapterWithMultiply(PVP_SEASON_BATTLE_PASS, qty)`, KHÔNG dùng thẳng làm Count |
| MilestoneProgression | *(chưa làm — TODO)* | | | |

## 2. Case 1 — PVP BattlePass (`Assets/Project/BattlePass/`)

Luồng đầy đủ ở [BattlePassData.cs:126-163](Assets/Project/BattlePass/Scripts/BattlePassData.cs#L126-L163):

1. Field data đổi từ `RewardCount` cố định → `RewardMultiplier` (Type + Multiplier float) — [BattlePassData.cs:126-129](Assets/Project/BattlePass/Scripts/BattlePassData.cs#L126-L129).
2. Guard: `RewardByProgressionSystem.IsProgressionReward(_reward.Type)` phải true (bắt buộc là alias BP_\*), sai thì log lỗi + trả `REWARD_TYPE.NONE` — [BattlePassData.cs:147-151](Assets/Project/BattlePass/Scripts/BattlePassData.cs#L147-L151).
3. `GemValue = Multiplier` (hoặc `Count` cũ nếu >0, để đọc data legacy) đưa thẳng vào `RewardByProgressionSystem.GetRewardCount(type, gemValue)` — [BattlePassData.cs:153-154](Assets/Project/BattlePass/Scripts/BattlePassData.cs#L153-L154).
4. Nếu resolve ra `GOLD`, Quantity đó KHÔNG phải Count cuối — nó là multiplier cho bảng vàng riêng `GoldByChapter.GetGoldByChapterWithMultiply(GOLD_BY_CHAPTER_ID.PVP_SEASON_BATTLE_PASS, qty)` — [BattlePassData.cs:155-159](Assets/Project/BattlePass/Scripts/BattlePassData.cs#L155-L159). Lý do: gold đã có nguồn chân lý riêng (xem `GoldByChapter.cs`), tránh 2 bảng giá gold song song.
5. Không cache — `GetFreeReward()/GetPremiumReward()` resolve lại mỗi lần gọi, luôn theo progression hiện tại của người chơi tại thời điểm gọi.

## 3. Case 2 — MilestoneProgression

*(TODO: điền đầy đủ luồng code sau khi hoàn thành tích hợp cho `Assets/Project/Events/MilestoneProgression/`. Phần dưới là audit data đã làm trước khi code.)*

Điểm khác biệt đã biết trước:
- Milestone hiện có khái niệm **Phase theo EventIndex** ([MilestoneProgressionSO.cs:36-42](Assets/Project/Events/MilestoneProgression/Scripts/MilestoneProgressionSO.cs#L36-L42)) — KHÔNG dùng trục này để chọn PROGRESSION_GROUP; đã chốt dùng effective level toàn cục (giống PVP BattlePass) thay vì tạo trục group riêng theo event.
- GOLD hiện xử lý bằng switch-case thủ công theo Id mốc gọi `GoldByChapter.GetGoldByChapter(...)` trực tiếp ([MilestoneProgression.cs:156-171](Assets/Project/Events/MilestoneProgression/Scripts/MilestoneProgression.cs#L156-L171)) — chưa qua RewardByProgression, là ứng viên thay bằng pattern ở mục 2.4.

**Audit data thật** (`Assets/Project/Resources/Data/MilestoneProgressions1.asset`, đếm field `Type:` của mọi `MilestoneData.Reward`) — đối chiếu với danh sách alias `BP_*` đã có sẵn ở mục 0:

| REWARD_TYPE dùng trong data | Giá trị số | Đã có alias `BP_*` sẵn? |
|---|---|---|
| GOLD | 1 | ✅ `BP_GOLD` |
| GEM | 2 | ✅ `BP_GEM` |
| CHIP_TICKET_NORMAL | 57 | ✅ `BP_CHIP_TICKET_NORMAL` |
| CHIP_TICKET_SPECIAL | 58 | ✅ `BP_CHIP_TICKET_SPECIAL` |
| FIGHTER_SPECIAL_PIECES | 30001 | ✅ `BP_FIGHTER_SPECIAL_PIECES` |
| FIGHTER_ULTRA_PIECES | 30002 | ✅ `BP_FIGHTER_ULTRA_PIECES` |
| WINGMAN_SPECIAL_PIECES | 40001 | ✅ `BP_WINGMAN_SPECIAL_PIECES` |
| WINGMAN_ULTRA_PIECES | 40002 | ✅ `BP_WINGMAN_ULTRA_PIECES` |
| AWARD_*_RANDOM (COMMON/RARE/EPIC/LEGEND/MYTHIC/LEGEND_EVENT) | 62–67 | ✅ `BP_AWARD_*_RANDOM` |
| FIGHTER_13, FIGHTER_14 | 81, 82 | ❌ không có alias — **KHÔNG cần**, xem lý do dưới |

→ **Toàn bộ loại reward Milestone cần scale theo tiến độ đã có sẵn alias `BP_*` dùng chung** — không cần tạo `REWARD_TYPE` mới, chỉ cần đổi `Type` trong data từ giá trị gốc (vd `1`) sang alias (vd `1000001`) và đổi `Count` cố định thành `GemValue`.

⚠️ `FIGHTER_13`/`FIGHTER_14` KHÔNG thuộc phạm vi convert — đây không phải reward dạng "số lượng quy đổi theo giá gem". Trong data thật ([MilestoneProgressions1.asset:305-312, 602-609](Assets/Project/Resources/Data/MilestoneProgressions1.asset#L305-L312)) chúng chỉ xuất hiện ở mốc cuối cùng (grand prize) với `Count=1` cố định — cấp thẳng 1 phi công cụ thể ([Rewards.cs:211-216](Assets/Project/Rewards/Scripts/Rewards.cs#L211-L216)), không có khái niệm "giá/đơn vị" để scale. Loại reward dạng "cấp thẳng 1 unit cố định, không đổi theo level" thì giữ nguyên `RewardCount`, không đưa vào RewardByProgression.
