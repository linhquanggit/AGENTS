# Snapshot: WorldBoss — điểm chạm file dùng chung + checklist merge `main` (lần 3)

**Ngày chụp:** 2026-09-28, trước merge (chưa merge). Project: `/Users/mobione/wkspaces/WorldBoss` (Plastic SCM, không git).
**Vì sao file này nằm ở đây, không nằm trong repo:** user chốt checklist merge KHÔNG track trong Plastic (không đưa lên `main`). Doc module chính thức vẫn ở `Assets/Project/WorldBoss/WORLDBOSS_OVERVIEW.md` (đã cập nhật 09-28).

Nội dung nguyên văn để dán lại khi mất nằm ở 2 snapshot cũ — file này là **mục lục + trạng thái verify**, không lặp lại code:
- Enum + vị trí serialize: [worldboss-premerge-enum-cache.md](worldboss-premerge-enum-cache.md)
- Code WB xen trong 7 file dùng chung: [worldboss-premerge-conflict-cache-2.md](worldboss-premerge-conflict-cache-2.md)

---

## 0. 🔴 Rủi ro lớn nhất lần merge này — Odin

Branch này **vẫn còn** `Assets/Plugins/Sirenix/Odin Inspector` và **~57 file trong `Assets/Project/WorldBoss/Use/Scripts`** + `UIGamePlay.cs:49` + `TabTestWorldBoss.cs` + `LevelWorldBossManager.cs:8,14-15` dùng `using Sirenix.OdinInspector` / `[FoldoutGroup]` / `[ShowInInspector]` / `[ReadOnly]`.
Rule OneGame: Odin đã bị gỡ vì bản quyền. Nếu `main` đã xoá `Assets/Plugins/Sirenix` → sau merge **compile fail hàng loạt**.
- Sau merge: kiểm tra `ls Assets/Plugins/Sirenix`. Còn → compile bình thường (nhưng vẫn nợ việc gỡ). Mất → phải strip Odin khỏi toàn bộ file WB (thay bằng `[Header]`/`[SerializeField]`/`[ContextMenu]`), lưu ý field `[ShowInInspector]` không serialize và `FlowName(...)` là attribute project, không phải Odin.
- Chưa sửa gì — ngoài phạm vi, chờ user quyết.

## 1. Enum dùng chung — verify 2026-09-28: KHỚP baseline 100%

| Enum | File | WB entry | Loại số | Verify 09-28 |
|---|---|---|---|---|
| `PopupId` | `Assets/DP/Scripts/UI/Popups.cs:267-274` | 8 entry | tuần tự → renumber max+1 | 206-213 ✅ |
| `IAP_Product : byte` | `Assets/CC_Manager/IAP_Product.cs:195-199` | `aa_worldboss1-5` | tuần tự → renumber max+1 | 157-161 ✅ |
| `ITEM_TYPE` / `GAMEPLAY_MODE` | `Assets/Project/Scripts/EnumVariables.cs:176,216` | `WB_TICKET`, `WORLD_BOSS` | implicit, append cuối | ✅ |
| `REWARD_TYPE` | `Assets/Project/Rewards/Scripts/Rewards.cs:748-754` | `WB_TICKET`, `WB_CURRENCY_1-6` | range riêng 60000-60006 | ✅ |
| **MỚI** `Events.EVENT_TYPE` | `Assets/Project/Events/Scripts/Events.cs:23` | `WORLDBOSS_EVENT = 8` | **hợp đồng server** — KHÔNG renumber | ✅ |
| **MỚI** `TUTORIAL` | `Assets/Project/Tutorials/Scripts/Tutorials.cs:18` | `UNLOCK_WORLD_BOSS = 8` | explicit; save key dùng `ToString()` (`Tutorials.cs:79,224`) → renumber không mất save | ✅ |
| **MỚI 09-28** `ENEMY_TYPE` | `Assets/Project/Enemies/Scripts/BaseEnemy/Enemies.cs:199` | `WB1…WB6 = 50001-50006` | range riêng; **serialize INT** ở `Resources/Data/Enemies.asset` (`Type: 5000x`, 6 entry) + `type:` trên 12 prefab `WorldBoss/Addressable/Enemies/Prefabs` | fixed 09-28 (xem learning worldboss-enemy-type-renumber-data-loss) |

2 enum MỚI chưa có trong enum-cache:
- `EVENT_TYPE.WORLDBOSS_EVENT = 8` được gửi server qua `DHS.ConnectEvent(..., EVENT_TYPE.WORLDBOSS_EVENT)` (`UISelectMode.cs:281`, `UIWorldBoss.cs`). Nếu `main` cũng thêm event mới dùng số 8 → **conflict giá trị, không phải chỉ conflict dòng**: WB phải giữ 8 (backend đã biết), báo lại để bên kia đổi — không tự tịnh tiến.
- `TUTORIAL.UNLOCK_WORLD_BOSS = 8`: nếu `main` chiếm 8 → đổi WB sang số trống bất kỳ được (key theo tên). Call site: `UIHome.cs:1416`, `UITutorialWorldBoss.cs:29`, `TabTestWorldBoss.cs:273`.

## 2. Asset/scene serialize — verify 2026-09-28

| Vị trí | Nội dung WB | Verify 09-28 |
|---|---|---|
| 8 prefab `Assets/Project/WorldBoss/Use/Resources/*.prefab` field `popupId` | 206-213 | ✅ khớp (cùng số dòng baseline) |
| `Assets/Project/Resources/Data/PopupsLazy.asset:408-423` | 8 entry Id/ResourcePath | ✅ |
| `Assets/Project/Resources/Data/WorldBossSO.asset:46,66,7294,7314,7334` | `idProduct` 160,161,157,158,159 | ✅ |
| `UIWorldBoss.prefab:20484`, `Item-WB-Purchase.prefab:1138` | `LocalizePrice.idProduct: 157` | ✅ |
| `WorldBossSO.asset` `Type: 60001` | 100 dòng | ✅ |
| `WorldBossSO.asset:4342-4500` `Released` | WB1=1, WB3=1, còn lại 0 | (mới ghi) |
| `Assets/Project/Scenes/Menu.unity:1900-1928` CC_IAP `idProductIAP` | 5 entry `aa_worldboss1-5` (key theo tên) | ✅ |
| `Assets/Project/Resources/Data/Rewards.asset` | 7 entry `Type: 6000x` | 🔴 **VẪN THIẾU** (0 entry) — tồn từ trước, NPE `ItemWorldBossPackage.cs:83` |
| `Assets/Project/UI/Resources/UI/UIGamePlay.prefab` | field `popupWB` + nested `Popup-WB-GamePlay` | 🔴 **VẪN MẤT** — cần Editor, NPE `UIGamePlay.cs:180,620-636` |
| `Assets/AddressableAssetsData/AddressableAssetSettings.asset` `m_GroupAssets` + `m_LabelTable` | 3 group WB + label `WorldBoss` | 🔴 **ĐÃ MẤT từ merge lần 2** (file sửa 2026-09-14 09:04): 0/3 group guid còn trong `m_GroupAssets`, label `WorldBoss` không còn trong `m_LabelNames` (chỉ còn default/Endless/Campaign/Tournament/PVP/Noel/AirDefense/Abyss). File group vẫn còn trên disk + entry vẫn gắn label → Addressables bỏ qua 3 group ⇒ `CheckAndDownload("WorldBoss")` + Node `enemyReference` fail. Fix trong Editor: Groups window kéo lại 3 asset group (hoặc Create→ lại thì mất GUID — KHÔNG), thêm lại label `WorldBoss`, rồi `QL/Addressable Sync` → Verify |
| 3 group asset | guid prefab `41b15d4d0df5c4fb18c47879efb701af` · spine `b9d7c913e8e4b42168073aceaf995e8b` · texture `c4e2128a1b36a4782b1b93ebdc27463c` | cần dán lại vào `m_GroupAssets` dạng `- {fileID: 11400000, guid: <guid>, type: 2}`. Entry có label: prefab 10 · spine **78** (snapshot addressable 07-29 ghi 90 → kiểm tra có rớt 12 entry không) · texture 6 |
| Banner `Boss1_Phase1.png` (guid `691c547a…`) | hard-ref từ prefab | ✅ 0 prefab còn hard-ref (4 chỗ ghi ở addressable-plan §6 P3 đã được đổi) |

## 3. Code WB xen trong file dùng chung (ngoài `Assets/Project/WorldBoss/`)

Verify 09-28 cho 4 file đã cache nguyên văn (so từng dòng code trong cache-2): `CC_Interface.cs` 0/4 mất · `GamePlayController.cs` 0/19 · `UIHome.cs` 0/7 · `UIGamePlay.cs` 0/13 · `Firebase_Analytics.cs` `isWorldBossUnlock` đủ 4 vị trí (75,195,335,387).

Danh sách đầy đủ file dùng chung có tham chiếu WB (grep `WorldBoss|WORLD_BOSS|WB_TICKET|WB_CURRENCY|aa_worldboss`, 09-28) — sau merge chạy lại grep này, file nào số hit giảm là nghi mất:

| Hit | File | Ghi chú |
|---|---|---|
| 81 | `Assets/QL_Tools/TestingTools/Tabs/TabTestWorldBoss.cs` | tool test (Odin) |
| 34 | `Assets/Project/SetupData/CsvToWorldBoss.cs` | import CSV → WorldBossSO |
| 26 | `Assets/Project/UI/Scripts/Setting/PopupWorldBossHack.cs` | hack menu (fake-time, Released filter :131) |
| 21 | `Assets/Project/GamePlay/Scripts/GamePlayController.cs` | cache-2 §4 |
| 12 | `Assets/Project/UI/Scripts/SelectMode/UISelectMode.cs` | entry + connect (:45,255-305) |
| 11 | `Assets/CC_Manager/CC_Interface.cs` | cache-2 §1 (IAP switch) |
| 9 | `Assets/Project/Home/Scripts/UIHome.cs` | cache-2 §5 (tutorial) |
| 8 | `Assets/DP/Scripts/UI/Popups.cs` | enum §1 |
| 7 | `Assets/Project/UI/Scripts/SelectAirport/PopupSelectAirport.cs` | |
| 7 | `Assets/Project/UI/Scripts/GamePlay/UIGamePlay.cs` | cache-2 §7 |
| 7 | `Assets/Project/Rewards/Scripts/Rewards.cs` | enum §1 |
| 6 | `Assets/Project/Scripts/Player.cs` | `WorldBossProcess` save (:86,251,310-315,1551) — mất = mất save |
| 5 | `Assets/CC_Manager/IAP_Product.cs` | enum §1 |
| 4 | `Assets/Project/Scripts/Manager.cs` | `LoadWorldBoss` (:1096-1106) |
| 4 | `Assets/Project/Fighters/Scripts/Fighters.cs` | |
| 4 | `Assets/CC_Manager/Firebase_Analytics.cs` | cache-2 §2 |
| 3 | `Assets/QL_Tools/TestingTools/TestingTools.cs` | |
| 3 | `Assets/QL_Tools/Editor/QLAddressableSync.cs` | preset folder WB |
| 3 | `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/ShooterWorldBoss.cs` | |
| 2 | `Assets/Project/Scripts/EnumVariables.cs` | enum §1 |
| 2 | `Assets/Project/Scripts/CharacterStats.cs` | |
| 2 | `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/SummonDroneWB1.cs` | |
| 1 | `Assets/Project/UI/Scripts/SelectMode/WorldBossButtonSelectMode.cs` | |
| 1 | `Assets/Project/Tutorials/Scripts/Tutorials.cs` | enum §1 |
| 1 | `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/ShotElement/SafeZoneBullet.cs` | |
| 1 | `Assets/DP/Scripts/UI/UIManager.cs:39` | `TutorialPopups` HashSet |
| 1 | `Assets/DP/EventDispatcher/EventName.cs:107` | `OnWorldBossAddedDamage` (implicit enum — append cuối nếu mất) |
| — | `Assets/Project/Events/Scripts/Events.cs:23` | `EVENT_TYPE.WORLDBOSS_EVENT` (grep không bắt vì tên `WORLDBOSS`) |

⚠️ `Assets/DP/Scripts` là shared — user cấm sửa (kể cả private→public); chỉ được **dán lại** entry WB bị mất trong `Popups.cs`/`UIManager.cs`/`EventName.cs`, không refactor.

## 4. Checklist sau khi merge xong
1. `ls Assets/Plugins/Sirenix` → quyết Odin (mục 0) trước mọi thứ khác, vì compile fail che hết lỗi còn lại.
2. Enum (mục 1): làm theo enum-cache "Checklist restore"; thêm 2 enum mới `EVENT_TYPE`/`TUTORIAL` theo luật ở mục 1.
3. Serialize (mục 2): grep lại từng dòng; renumber PopupId/IAP thì sửa prefab/PopupsLazy/WorldBossSO/LocalizePrice.
4. Code (mục 3): chạy lại grep, so số hit với bảng; file nào giảm → so với cache-2 và dán lại nguyên văn.
5. Addressable: đăng ký lại 3 group + label `WorldBoss` (mục 2 — đang mất sẵn TRƯỚC merge lần 3, làm được luôn không cần chờ) → `QL/Addressable Sync` Verify (0 MISSING/WRONG GROUP/MISSING LABEL). `AddressableAssetSettings.asset` là file hay bị `main` đè → luôn check lại sau merge.
6. Editor: gán lại `popupWB` trong `UIGamePlay.prefab`; thêm 7 entry `Rewards.asset` (cần icon thật).
7. Play test: mở UIWorldBoss → Play 1 trận WB1 (HUD `popupWB` hiện, không NPE) → kết thúc về hub → Shop mua thử 1 gói `aa_worldboss*` (hiện giá) → Leaderboard.
8. Sau khi xong: cập nhật lại file này + 2 cache làm baseline cho lần merge sau.
