# Snapshot: Merge `main` → `/main/WorldBoss` lần 3 — capture conflict + cách resolve

**Ngày:** 2026-09-28. Workspace sạch tại **cs:8885** (`/main/WorldBoss`), source `br:/main` head **cs:8878**. Chưa merge thật — chỉ dry-run `cm merge br:/main` (output: scratchpad `merge-dryrun.txt`, 2818 dòng).
Bản 3 phía (base/src=main/dst=WB) của 53 file conflict lấy bằng `cm cat` → scratchpad `m3/norm/*.{base,src,dst}`, kết quả diff3 ở `m3/out3/` (scratchpad session `622de0bd…`, sẽ mất khi hết session — nội dung quyết định đã chép vào file này).
Checklist tổng / điểm chạm: [worldboss-merge-touchpoints.md](worldboss-merge-touchpoints.md). Nguyên văn code WB: [worldboss-premerge-enum-cache.md](worldboss-premerge-enum-cache.md), [worldboss-premerge-conflict-cache-2.md](worldboss-premerge-conflict-cache-2.md).

## 0. Tổng quan dry-run
- 1412 file main sửa → replace · 1259 item added · 54 moved · 8 deleted · **54 conflict** (Changed by both).
- **Main thêm module BossTower** → đụng **đúng** số của WB: PopupId 206-213, IAP 157-161, `EVENT_TYPE` 8, `GAMEPLAY_MODE` sau `AIR_DEFENSE`, `idProductIAP` Menu.unity.
- **Main xoá `Assets/Plugins/Sirenix`** (Odin) + thêm package `com.onegame.odin-removal-audit` (Editor window scan) → phần lớn conflict engine là do main gỡ Odin (src cs:8844). Sau merge, **~57 file WB + TabTestWorldBoss + PopupWorldBossHack còn `using Sirenix` → compile fail** — việc riêng, xem §4.
- **CRLF**: main đổi line ending nhiều file (Player.cs, Rewards.cs src = CRLF, dst = LF). Diff thô báo conflict nguyên file; sau khi bỏ `\r` thì **Player.cs, Rewards.cs, Manager.cs, UIGamePlay.cs, UIHome.cs, CC_Interface.cs tự merge sạch**. Trong Plastic merge tool: bật *Ignore EOL* (hoặc chọn "Ignore end of line and whitespace differences") để thấy đúng hunk.

## 1. File main replace (không conflict) nhưng ẢNH HƯỞNG WB
| File | Hệ quả | Việc sau merge |
|---|---|---|
| `Assets/AddressableAssetsData/AddressableAssetSettings.asset` (cs:8878) | vốn đã mất 3 group WB + label | add lại 3 group + label `WorldBoss` (guid ở touchpoints §2) |
| `Assets/Project/Resources/Data/Rewards.asset` (cs:8834) | vốn thiếu 7 entry WB | thêm 7 entry `Type 60000-60006` (Editor, cần icon) |
| `Assets/Project/UI/Resources/UI/UIGamePlay.prefab` (cs:8835) | vốn mất `popupWB` | kéo lại `Popup-WB-GamePlay`, gán `popupWB` |

## 2. ⚠️ Bẫy auto-merge "sạch" nhưng SAI (Plastic sẽ không hỏi)
1. **`Assets/CC_Manager/IAP_Product.cs`** — merge ra 2 khối trùng số: `aa_bosspathticket2..ticketdaily = 157-161` và `aa_worldboss1-5 = 157-161`. C# cho phép enum trùng giá trị → **compile OK nhưng sai lặng lẽ** (`WorldBossSO.asset` idProduct 157 sẽ hiện/tra thành `aa_bosspathticket2`). **Resolve:** WB → `aa_worldboss1 = 162 … aa_worldboss5 = 166` (byte, <255 OK). Sửa theo: `WorldBossSO.asset:46,66` (160→165, 161→166), `:7294,7314,7334` (157→162,158→163,159→164), `UIWorldBoss.prefab:20484` + `Item-WB-Purchase.prefab:1138` (157→162). Menu.unity key theo tên → không đổi.
2. **`Assets/Project/Scripts/EnumVariables.cs` `ITEM_TYPE`** — nhánh WB **đang** đặt `WB_TICKET` TRƯỚC `CAMPAIGN_REVIVE_TICKET`/`CAMPAIGN_EXTRATIME_TICKET` (implicit value) → 2 vé campaign bị lệch +1 so với main. Merge giữ nguyên thứ tự sai đó. **Resolve:** chuyển `WB_TICKET,` xuống **sau** `CAMPAIGN_EXTRATIME_TICKET,` (dòng cuối enum). Call site `WB_TICKET` symbolic duy nhất `PopupWorldBossBuyTicket.cs:265`; không serialize (enum-cache §3).

## 3. 21 file conflict thật (sau khi bỏ CRLF) — cách resolve từng file

### 3a. Enum / data (WB ↔ BossTower) — lấy CẢ HAI, WB nối đuôi
| File | Main (BossTower) | WB | Resolve |
|---|---|---|---|
| `Assets/DP/Scripts/UI/Popups.cs` | `BossTowerSelectPath..BossTowerLeaderboardClaim = 206-213` | 8 WB = 206-213 + `TimeUp = 214` | giữ khối BossTower 206-213 nguyên; WB renumber **214-221** (UIWorldBossInformation 214, RewardInformation 215, LeaderBoard 216, Shop 217, UIWorldBoss 218, TutorialWorldBoss 219, Finished 220, LeaderBoardReward 221), `TimeUp = 222` (thêm dấu `,`, sửa thụt lề) |
| `Assets/Project/Resources/Data/PopupsLazy.asset` | 8 entry `BossTower/UIBossTower*` Id 206-213 | 8 entry WB Id 206-213 | giữ 8 entry main; 8 entry WB đổi Id 214-221 (ResourcePath giữ nguyên) |
| 8 prefab `Assets/Project/WorldBoss/Use/Resources/*.prefab` `popupId` | — | 206-213 | đổi theo số mới 214-221 (dòng: enum-cache §1) |
| `Assets/Project/UI/Resources/UI/UITimeUp.prefab:319` | — | **`popupId: 209` — BUG CÓ SẴN** (enum TimeUp=214, popup do nhánh WB thêm cs:8370) | set `popupId: 222` |
| `Assets/Project/Scripts/EnumVariables.cs` `GAMEPLAY_MODE` | `…, AIR_DEFENSE, BOSS_TOWER` | `…, AIR_DEFENSE, WORLD_BOSS` | `…, AIR_DEFENSE, BOSS_TOWER, WORLD_BOSS` (BOSS_TOWER giữ 9 như main, WORLD_BOSS thành 10; không serialize — an toàn) |
| `Assets/Project/Scenes/Menu.unity` CC_IAP `idProductIAP` (~dòng 1900) | 5 entry `aa_bosspath*` | 5 entry `aa_worldboss1-5` | lấy cả 2 khối (main trước, WB sau); nội dung WB: enum-cache §2 |
| `Assets/Project/GamePlay/Scripts/GamePlayController.cs` (3 hunk: switch win ~648, lose ~1102, case ~2076) | `case BOSS_TOWER: OnBossTowerWin()/OnBossTowerLose()` | `case WORLD_BOSS: OnCompleteWorldBoss()` | giữ **cả 2 dòng case** ở mỗi hunk. Hunk 3 là `IsOneAirportOnly` (fall-through `PVP/TOURNAMENT/... → return true`) → 2 nhãn `case BOSS_TOWER:` + `case WORLD_BOSS:` liền nhau là đúng |
| `Assets/Project/Events/Scripts/Events.cs` `EVENT_TYPE` | `BOSS_PATH_EVENT = 8` | `WORLDBOSS_EVENT = 8` | 🔴 **CHẶN — cần backend**: giá trị gửi server (`DHS.ConnectEvent/ClaimEvent`). Không tự đánh số. Hỏi backend WB và BossTower mỗi bên dùng event_type nào |

### 3b. Engine dùng chung — main gỡ Odin, WB có field riêng
Luật: **lấy khối MAIN (không attribute Odin) + thêm field/khai báo riêng của WB dưới dạng `[SerializeField]` thường**, giữ `protected` nếu WB cần. Tên field giữ nguyên để không mất data serialize trên prefab boss.
| File | Field/khai báo WB phải giữ (thêm vào khối main) | Ghi chú |
|---|---|---|
| `Bullets/Scripts/Bullet.cs` ~170 | `useCrit` giữ **`protected`** (main để `private`) | subclass `DelayedBullet/HomingMissile/DamageArea` dùng |
| `Bullets/Scripts/BulletMultiStep.cs` ~9 | `[SerializeField] GameObject objExplodeNextStep;` | |
| `Enemies/.../BaseShot/BaseShotE.cs` ~14 | `isNotAttackByLoad`, `isShowGizmos` (+ giữ 2 field mới của main `useDmgPercentOfPlayerMaxHP/dmgPercentOfPlayerMaxHP`). Khối `Calculate()` Editor: main đã comment — theo main (hoặc đổi `Button` → `[ContextMenu("CALCULATE")]` nếu còn cần) | `isShowGizmos` dùng ở `LauncherRandomShotE`, `SummonDroneWB3` |
| `Enemies/.../BaseShot/DestinationShotE.cs` ~22 | `[SerializeField] protected bool useFireDirRotation = true;` | |
| `Enemies/.../BaseShot/LaserShotE.cs` ~17 | `public float WarningDuration => useWarning ? timeWarning : 0f;` | 2 event `ueStartWarning/ueStartAttacking` main đã chuyển lên dòng 13-14 — **đừng khai báo lại** (sẽ trùng) ; `WarningDuration` dùng ở `EnemyWorldBoss1_P3` |
| `Enemies/.../SpecialCase/LauncherRandomShotE.cs` ~7 | `#if UNITY_EDITOR [SerializeField] private float gizmoRadius = 2f; #endif` | |
| `Enemies/.../BaseShot/UbhShotE.cs` ~8 | `[SerializeField] protected bool triggerAnimOnShot = false;` | |
| `Enemies/.../EnemyBase/Enemy.cs` ~28 | `[SerializeField] protected bool isShowGizmos = false;` | |
| `Enemies/.../EnemyRotation/RotationRange.cs` ~8 | lấy main (có `isChild` mới) nhưng giữ modifier **`protected`** cho `counterclockwise/loopType/minAngle/maxAngle/ueCompletedLoop/delayAtMax/delayAtMin/ueYoYoPause/ueYoYoResume` | subclass `StepRotation` (WB) truy cập |
| `Enemies/.../EnemyRotation/StepRotation.cs` (2 hunk) | h1: bỏ `using Sirenix`; giữ `using UnityEngine.Events;` · h2: thêm `triggerStepOnPlay`, `ueStepReached` | |
| `Scripts/ItemMyRankBase.cs` ~14 | `protected EventLeaderboardEntry entry;` | hệ leaderboard WB (learning leaderboard-popup-event-entry-root) |
| `Scripts/ItemTopRankBase.cs` (2 hunk) | h1: bỏ `using Sirenix`, **giữ `using BreakInfinity;`** · h2: thêm `protected EventLeaderboardEntry entry;` + giữ `HideRankDecorationFields` của main | |
| `Scripts/LeaderboardPopupBase.cs` ~18 | `protected EventLeaderboardEntry myRank;` | dùng ở UIBTLeaderboard/UIAirDefenseLeaderBoard/UIPVPLeaderboard (nhánh WB) |
| `GamePlay/Scripts/TestManager.cs` ~2 | giữ `using DP.Utilities;`, bỏ `using Sirenix.OdinInspector;` | |
| `QL_Tools/TestingTools/TestingTools.cs` | lấy **main** (EditorWindow + `EnsureTabs()`/`DrawGUI`, đã bỏ Odin) | tab WorldBoss mất → phải port `TabTestWorldBoss` sang kiểu `DrawGUI` rồi thêm `_worldBossTab` vào `EnsureTabs` + mảng draw (§4) |

### 3c. 32 file còn lại — tự merge sạch (sau khi bỏ EOL), không cần tay
UISelectMode, PopupSelectAirport, ScrollViewReward, ItemRewardBanner, Fighters, UIWorldCupOfferDetail, 4 file MilestoneProgression leaderboard, ItemMyRankAirDefense, RotationTarget, Rotation2D, EnemyBoss, LinkLaser_CL, Laser_CL, LaserE, LauncherShotE, FireBreathShotE, DirectionShotE, AimingObject, EventEnemies, BulletDestination, UIManager, IAP_Product (⚠️ §2.1), Firebase_Analytics, Player, Rewards.cs, Manager, UIGamePlay.cs, UIHome, CC_Interface. `FR2_Cache.asset` → lấy bản main (cache).
Vẫn verify sau merge bằng grep WB (touchpoints §3) — file nào số hit giảm là mất.

## 4. Việc phát sinh sau merge (không phải conflict nhưng chặn compile)
- **Strip Odin khỏi module WB** (~57 file `Assets/Project/WorldBoss/Use/Scripts` + `LevelWorldBossManager` + `TabTestWorldBoss` + `PopupWorldBossHack`?): `[FoldoutGroup]/[TitleGroup]/[TabGroup]/[ShowIf]/[HideIf]/[ShowInInspector]/[ReadOnly]/[Button]` → bỏ / `[Header]` / `[ContextMenu]`. `FlowName(...)` là attribute project — giữ. Dùng window của `com.onegame.odin-removal-audit` để scan.
- Port `TabTestWorldBoss` sang kiểu tab `DrawGUI` của TestingTools mới.

## 5. Quyết định cần user
1. `EVENT_TYPE` 8 trùng → hỏi backend (chặn).
2. Duyệt renumber: PopupId WB 214-221 + TimeUp 222, IAP 162-166, GAMEPLAY_MODE WORLD_BOSS sau BOSS_TOWER, ITEM_TYPE WB_TICKET xuống cuối.
3. Strip Odin module WB: làm ngay sau merge (bắt buộc để compile).

## 6. ✅ Đã chốt (2026-09-28) + trạng thái chuẩn bị
- `EVENT_TYPE`: **`BOSS_PATH_EVENT = 8` (main giữ), `WORLDBOSS_EVENT = 9`** — luật chung "bên có sau lấy index tiếp" (memory `enum-index-later-comes-next`). Backend WB cần biết số 9.
- Renumber đã duyệt: PopupId WB 214-221 + `TimeUp` 222 · IAP `aa_worldboss1-5` 162-166 · `GAMEPLAY_MODE` `BOSS_TOWER, WORLD_BOSS` · `ITEM_TYPE` `WB_TICKET` xuống cuối.
- Odin: gỡ sạch như main (bỏ `using Sirenix`, bỏ attribute; `[Button]` method không tham số → `[ContextMenu]`; `MinValue`→`Min`, `PropertyRange` số → `Range`). 84 file keep-WB (bỏ `GridEvents.cs` vì main replace ở path mới `Events/GridEvent/`) + phần tự merge của 53 file conflict.
- **Phát hiện thêm khi resolve:** `ItemTopRankBase` — nhánh WB đổi `Show(int,string,float)` → `Show(EventLeaderboardEntry)`, nhưng BossTower của main (`ItemTopRankBossTower` override + `PopupPlayerRank.cs:50` gọi) dùng signature cũ → **giữ CẢ 2 overload**. `LaserShotE`: không khai lại 2 event (main đã dời lên dòng 13-14).
- `TabTestWorldBoss` port sang kiểu `DrawGUI` (TTGUI) + đăng ký tab "WorldBoss" vào `TestingTools.EnsureTabs()`. `AgentImporter.cs` (private): `OdinEditorWindow`→`EditorWindow`, `OnEnable` bỏ override/base, thêm `OnGUI` tay.
- Chuẩn bị ở scratchpad session `622de0bd…`: `m3/resolve.py` → `resolved_final/` (53 file) · `strip_odin.py` → `odinfree/` (84 file) · `apply_after_merge.py` (ghi 148 file + renumber 11 vị trí serialize; `--dry` đã pass toàn bộ assert trên workspace cs:8885). Scratchpad mất khi hết session → nếu mất, tái tạo theo §3-§6.

## 7. Đã thực thi (2026-09-28) — CHƯA check-in, CHƯA compile Unity
- User suýt dùng **Server side merge** trên GUI (check-in thẳng changeset, 0/54 conflict resolve) → đã Cancel. Bài học: merge cần sửa tay thì luôn merge vào **workspace**, không dùng server-side merge.
- Undo `.vscode/settings.json` (VS Code tự sửa; backup ở scratchpad `vscode-settings.backup.json`) để hết pending.
- `cm merge br:/main --merge --keepdestination` → workspace: 1462 Replaced · 1291 Copied · 61 Moved · 8 Removed · 63 Checked-out. `Assets/Plugins/Sirenix` đã xoá, `Assets/Project/BossTower` đã có.
- Verify trước khi ghi: 53/53 file conflict == bản WB (`.dst`). Chạy `apply_after_merge.py` → 148 file.
- Verify sau khi ghi: 0 marker conflict · 0 `Sirenix`/attribute Odin (ngoài comment) trong `Assets`+`Packages` · enum PopupId/IAP/EVENT_TYPE/GAMEPLAY_MODE/ITEM_TYPE/REWARD_TYPE/TUTORIAL không trùng số · 9 prefab popupId + 6 idProduct đúng số mới · hit WB trong 27 file dùng chung khớp baseline (TabTestWorldBoss +1 do DrawGUI) · Menu.unity đủ 5 `aa_worldboss` · PopupsLazy đủ 8 entry WB · `isWorldBossUnlock` đủ 4 chỗ.
- Còn lại: compile Unity; `FR2_Cache.asset` đang giữ bản WB (cache — Undo/để FR2 tự rebuild); việc Editor: 3 group Addressable + label `WorldBoss`, `popupWB` trong `UIGamePlay.prefab`, 7 entry `Rewards.asset`; báo backend `WORLDBOSS_EVENT = 9`.

## 8. ✅ Đã check-in cs:8889 (2026-09-28) — baseline cho lần merge sau
- Merge link thật là **main cs:8888** (main có thêm 8886/8888 sau dry-run 8878). Tập 54 conflict không đổi; 2 file conflict có src mới hơn: `Player.cs` (8874, chỉ đổi CRLF→LF) và `UIGamePlay.cs` (8886, `TextItem`→`ItemCollectView`) → đã merge lại với src mới + gỡ Odin trước khi check-in. **Bài học:** giữa dry-run và merge thật phải so `cm diff cs:<dryrun> cs:<merge link>` ∩ danh sách file mình ghi đè.
- `AgentImporter.cs` bị merge tự đánh dấu Added → đã undo về Private (không lên repo; bản đã gỡ Odin vẫn ở local).
- `packages-lock.json` check-in kèm (Unity resolve package embedded `com.onegame.odin-removal-audit`).
- Unity Editor.log lần compile cuối (sau khi áp file, trước fix UIGamePlay) không có `error CS`. Chưa verify compile sau fix UIGamePlay/Player (thay đổi nhỏ, code main).
- Baseline mới: PopupId WB 214-221, TimeUp 222 · IAP 162-166 · EVENT_TYPE WORLDBOSS_EVENT 9 · GAMEPLAY_MODE WORLD_BOSS 10 · ITEM_TYPE WB_TICKET 127 · REWARD_TYPE 60000-60006.
