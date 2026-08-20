# Snapshot: WorldBoss — Addressable audit + plan (CHỜ THỰC THI)

**Trạng thái:** plan đã chốt nội dung, **chưa code dòng nào**. User tự làm, chờ user.
**Quyết định Remote/Local: ĐÃ CHỐT `Remote` (CDN) — 2026-07-28, user duyệt.** Cả 3 group `worldboss-*` dùng path Remote (`m_BuildPath: ec634c1d…`, `m_LoadPath: 030cfeb3…`).
**Ngày chụp:** 2026-07-28. Số liệu là **source bytes**, KHÔNG phải build size (texture nén ASTC/ETC2 khi build).
Chạy `BuildReportTool` (có sẵn trong project) lấy baseline trước khi bắt đầu.

> Lưu ngoài git repo WorldBoss ⇒ merge trong repo không đụng tới.
> Phương pháp verify tổng quát ở [../addressables.md](../addressables.md). File này chỉ chứa kết quả áp lên WorldBoss.

---

## 1. Kết quả audit

### WorldBoss chưa addressable-hoá chút nào
`Assets/Project/WorldBoss/Addressable/` có **306 file / 51 MB** nhưng thiếu **cả 5 mắt xích**:
1. Không group `worldboss-*` nào trong `AssetGroups/`.
2. Không entry nào — GUID của `Popup-WB-GamePlay.prefab` (`2f4437b3…`), `EnemyWB1_P1_A.prefab` (`e7ba2fd7…`) không có trong `AssetGroups/*.asset`.
3. Không label `WorldBoss` (`m_LabelTable` chỉ có: default, Endless, Campaign, Tournament, PVP, Noel, AirDefense, Abyss).
4. 6 Node prefab đều `useAddressable: 0` + `enemyPrefab` hard ref.
5. `grep CheckAndDownload|ReleaseAllHandle` trong `WorldBoss/Scripts/` = **0 kết quả**.

Tên folder `Addressable/` chỉ là quy ước, **không có hiệu lực kỹ thuật**.

### Hai đường asset vào build
- `WorldBoss.unity` trong Build Settings (`EditorBuildSettings.asset:1058-1060`, `enabled: 1`). Scene ref **0** texture UI — chỉ gameplay.
- `Assets/Project/Resources/Data/WorldBossSO.asset` → hard-ref `NodeWB` → enemy → spine → texture. Resources = ship vô điều kiện.

### Hạ tầng project (chung, không riêng WB)
- Addressables `1.19.19`, active builder = `BuildScriptPackedMode` (đúng).
- Remote.LoadPath = **`https://onegamecdn.b-cdn.net/1941/14.9/[BuildTarget]`** (đo lại 2026-07-28 — CDN đã đổi host *và* version, giá trị cũ `cdn.onegamestudio.net/1941/14.8` trong snapshot này SAI). Remote.BuildPath = `ServerData/[BuildTarget]` (id `ec634c1d…`), Remote.LoadPath id `030cfeb3…`, Local.BuildPath id `ccbbede0…`.
- Phân loại lại **23/32 group dùng Remote** — kể cả `Default Local Group` (tên gây nhầm, schema giống hệt `noel-prefab`). 8 group `abyss-*`/`airdefense-*` trỏ id `969b806178e6bb544bd2ffaea0e5609c` (**không phải** Local.BuildPath, là biến profile khác — cần resolve nếu bàn tới 2 module đó).
- `m_BuildAddressablesWithPlayerBuild: 2` = **DoNotBuildWithPlayer** ⇒ bundle KHÔNG tự build cùng player, phải `Addressables > Build > New Build` bằng tay trước mỗi lần build app. Bỏ bước này = bundle cũ/thiếu.
- `AssetGroupTemplates/Packed Assets.asset` có schema **trùng khớp** `noel-prefab` mọi field, chỉ trống `m_BuildPath`/`m_LoadPath` ⇒ tạo group từ template này rồi chỉ cần set 2 path Remote, không phải set tay 20 field.
- Convention entry: **per-file**, `m_Address` = full asset path, label gán từng entry (campaign-texture có 402 entry). Project **không** dùng folder-entry.
- Đã có tool verify sẵn: `Assets/Editor/AddressableApkDuplicateTextureChecker.cs:62` → menu `Tools/Addressables/Check textures duplicated in APK` (đúng detector double-wire case (a)).
- ⚠️ `m_BuildRemoteCatalog: 1` nhưng `AddressableAssetsData/Android|iOS/` **rỗng** → không có `addressables_content_state.bin` → **không chạy được "Update a Previous Build"**.
- 6 file rác `(name_conflict*)_link.xml` trong `AddressableAssetsData/`.

---

## 2. Bản đồ UI (phần tốn nhiều công đào nhất — đừng đào lại)

```
WorldBoss/Resources/*.prefab  (8 popup, 2.6 MB)   ← Resources: LUÔN vào build
  ├─ m_Sprite ref THẲNG   44/106 texture
  └─ nested prefab instance  12/19 item prefab      (m_SourcePrefab — KHÔNG phải field spawn)
         └─ ref thêm  33/106 texture
Project/UI/Resources/UI/UIGamePlay.prefab  → Popup-WB-GamePlay.prefab (HUD)
WorldBoss.unity  → 0 texture UI
```

- Texture `Use/` + `Atlas/` = 106 file / 8.6 MB. 67 file (4.4 MB) reachable từ popup+item; ~32 file nữa reachable từ nơi khác (chủ yếu `WorldBossSO.asset`); ~7 file là ứng viên chết **chưa xác nhận**.
- **Trần tối ưu UI**: ~9.2 / 11.8 MB (≈78%) gỡ được mà KHÔNG cần đụng `Resources/`, `PopupsLazy`, hay `UIManager`. Phần kẹt chỉ là 2.6 MB YAML của 8 popup prefab.

### Bẫy đã kiểm chứng — đừng lặp lại phân tích sai
- `m_SourcePrefab` là field nội bộ Unity của `PrefabInstance` = **nested instance**, không phải field `Instantiate`. 12 item prefab thuộc loại này.
- Field spawn-source **thật** chỉ có 9 field / 8 prefab / **278 KB** — quá nhỏ, và `cellViewPrefab` là của **EnhancedScroller** (`GetCellView()` tiêu thụ **đồng bộ**, đổi sang `AssetReference` phải preload trước). **Kết luận: KHÔNG làm.**
  | Popup | Dòng | Field | Target |
  |---|---|---|---|
  | UIWorldBossShop | 1255 / 1257 | `cellViewPrefab` / `lastCellViewPrefab` | Item-WB-WLY-CellView / -Special-CellView |
  | UIWorldBossShop | 3413, 3477 | `cellPrefab` | Item-WB-Panel-Package |
  | UIWorldBossShop | 3690 | `itemTabBossPrefab` | Item-WB-Shop-BossTab |
  | UIWorldBossShop | 4117 | `itemPrefab` | Item-WB-Purchase |
  | UIWorldBossRewardInfor | 53 | `cellViewPrefab` | Item-WB-RewardInfo-CellView |
  | UIWorldBossLeaderBoard | 26461 | `cellViewPrefab` | Item-Normal-Rank-WorldBoss |
  | UIWorldBossLeaderBoardReward | 633 | `itemPrefab` | Item-WB-LeaderBoard-NormalReward |

### PopupsLazy không giảm build size
`PopupsLazy.asset` chỉ giữ chuỗi `ResourcePath` (0 ref prefab) — đúng như thiết kế. Nhưng popup nằm trong `Resources/` nên **vẫn ship vô điều kiện**. PopupsLazy giải quyết **RAM / load time**, KHÔNG giải quyết dung lượng app.
`Popups.asset` **kiểu cũ vẫn còn** trong `Assets/Project/Resources/Data/`, giữ **199 ref prefab** (có 6/8 popup WB), loader đã comment ở `Assets/DP/Scripts/UI/Popups.cs:12` → asset chết, nên xoá.

### Top texture nặng (KB)
```
bg_ui_WB     732    Boss3_Phase3 495    Boss1_Phase3 454
infor_boss   655    Boss3_Phase2 493    frame_boss   412
Boss1_Phase1 548    Boss3_Phase1 489    banner_bosstime 354
Boss1_Phase2 517
```
6 file `Boss*_Phase*` (**3.0 MB**) vào build qua `WorldBossSO.asset`, và **slot addressable đã có sẵn**: `SprBossShow`.

---

## 3. Plan

### P0 — Dọn rác · đo lại 2026-07-28 · **~6.2 MB rời build** (+4.1 MB dọn repo)
> ⚠️ Con số cũ "10.7 MB rời build" **SAI** — đã đo lại bằng GUID sweep. Chi tiết dưới.

**A. 14 ảnh mockup ở `Addressable/UI/Textures/` (13 `demo_*` + `infor_box.png` = 10.3 MB)**
> 🛑 **HOÃN tới release (user quyết 2026-07-28).** Các GameObject `Demo`/`DEMO`/`BG-1`/`Background-1` đang được dùng để **đối chiếu khi test** — KHÔNG xoá bây giờ, kể cả 5 file orphan. Đưa vào checklist pre-release. Số liệu dưới giữ nguyên để lúc đó dùng lại.

Chia 2 nhóm:
- **9 file / 6.2 MB THỰC SỰ ship** (có hard ref từ popup trong `WorldBoss/Resources/`). Tất cả 9 ref đều nằm trên GameObject **`m_IsActive: 0`** (`Demo` / `DEMO` / `BG-1` / nested `Image-BG` rename `Background-1`) ⇒ overlay canh design, không phải UI thật. Verified an toàn xoá.
  | File | KB | Popup | GameObject (active=0) |
  |---|---|---|---|
  | demo.png | 1147 | UIWorldBoss | `Background-1` (nested `Image-BG`, override `m_Sprite`, alpha=1) |
  | demo_weekly_pass_2.png | 1134 | UIWorldBossShop | `DEMO` |
  | demo_levelcomplate.png | 1095 | UIWorldBossFinished | `DEMO` |
  | infor_box.png | 987 | UIWorldBossRewardInfor | `BG-1` (sibling `BG` active=1 mới là BG thật) |
  | demo_leaderboard.png | 831 | UIWorldBossLeaderBoard | `Demo` |
  | demo_endgame.png | 718 | UIWorldBoss | `Demo` |
  | demo_infor.png | 169 | UIWorldBossInformation | `DEMO` |
  | demo_ranking_Reward.png | 164 | UIWorldBossLeaderBoardReward | `Demo` |
  | demo_ticket.png | 102 | UIWorldBoss | `Demo` |
- **5 file / 4.1 MB đã là orphan** — ref duy nhất là `Assets/FR2_Cache.asset` (cache của Find Reference 2, editor-only): `demo_gameplay_WB` 992, `demo_puchase` 1308, `demo_weekly_pass` 999, `demo_home_mode` 694, `demo_shop_WB.jpg` 173. **Xoá KHÔNG giảm build** (chúng chưa từng vào build) — chỉ gọn repo.

**B. 6 file `(name_conflict*)_link.xml` (138 KB, `AddressableAssetsData/`)** — không phải rác trơ: mỗi file `preserve="all"` cho **227–421 type** `Assembly-CSharp`, và `managedStrippingLevel.Android = 1` (Low) đang bật ⇒ chúng **chặn IL2CPP strip code**. Xoá ⇒ giảm binary (chưa đo được, cần 1 build so sánh). `Assets/link.xml` (2 type) là file thật của project, **giữ**. Addressables tự sinh link.xml lúc build nên 6 file này là leftover. **Rủi ro: KHÔNG phải 0** — cần 1 build + smoke test.

**C. `Assets/Project/Resources/Data/Popups.asset` (22 KB)** — 197 GUID ref, **195/196 prefab đã nằm trong `Resources/`** ⇒ xoá tiết kiệm **~0 MB build size**, chỉ là dọn asset chết (loader đã comment ở `Assets/DP/Scripts/UI/Popups.cs:12`). Đừng tính vào con số tiết kiệm.

- **Verify**: mở 8 popup WB (PopupId 200–207) + `WorldBoss.unity`, Console không missing-sprite; build Android so `BuildReportTool` trước/sau.

### P1 — Hạ tầng · không đổi hành vi
- Thêm label `WorldBoss`.
- Tạo 3 group, schema copy từ `noel-prefab_BundledAssetGroupSchema` (`m_Compression: 1`, `m_BundleMode: 0`), path **Remote** (`m_BuildPath: ec634c1d…`, `m_LoadPath: 030cfeb3…`):
  | Group | Nội dung | File | Source (đo lại 2026-07-28) |
  |---|---|---|---|
  | `worldboss-prefab` | `Enemies/Prefabs/**` (40 prefab) + `Effects/**` (35: 14 png, 14 mat, 5 prefab, 1 shader, 1 fbx) | 75 | 27.8 MB (25.0 + 2.8) |
  | `worldboss-spine` | `Enemies/Textures/World_Boss_1` (36) + `World_Boss_3` (54), đã trừ `.DS_Store` | 90 | 5.5 MB (3.4 + 2.1) |
  | `worldboss-texture` | `UI/Textures/Use` (30) + `Atlas` (76) | 106 | 8.2 MB (7.1 + 1.1) |
  | **Tổng** | | **271** | **41.5 MB** |
- `Addressable/UI/Prefabs/` (19 file, 0.5 MB) **cố ý KHÔNG vào group**: 8 cell prefab EnhancedScroller (đã chốt không làm) + 12 nested instance trong popup `Resources/` + `Popup-WB-GamePlay.prefab` (ref từ `UIGamePlay.prefab`). Đừng đọc là bỏ sót.
- Cách làm: **1 Editor script one-shot idempotent** (`Assets/Editor/WorldBossAddressableSetup.cs`, menu `Tools/Addressables/…` theo mẫu tool sẵn có) — 271 entry per-file + label đồng loạt, kéo tay dễ sinh folder-entry lệch convention. API đã verify trên 1.19.19: `settings.CreateGroup(name,false,false,false,template.SchemaObjects)` (schemasToCopy → `Object.Instantiate` ra schema riêng, tự set `Group`), `schema.BuildPath/LoadPath.SetVariableByName(settings,"Remote.BuildPath"/"Remote.LoadPath")`, `settings.AddLabel`, `settings.CreateOrMoveEntry`, `entry.SetLabel`.
- **Verify**: đúng **271** entry (75 / 90 / 106), đều label `WorldBoss`. Đã xác nhận **0 file** trong `WorldBoss/Addressable/` là entry của group khác ⇒ không có xung đột move. Tổng 304 asset trong folder = 271 (3 group) + 19 (`UI/Prefabs`, cố ý loại) + 14 (mockup `demo_*`, hoãn tới release). Analyze → *Check Duplicate Bundle Dependencies*; dup thì gom vào group `asset-duplication` (đã có sẵn).
- Bước này asset **vẫn** vào build (hard ref còn) — chủ ý: build được bundle rồi mới cắt ref.

### P2 — Gameplay · ~32 MB
- 6 Node prefab ở `Addressable/Enemies/Prefabs/Nodes/`: `useAddressable: 1`, xoá `enemyPrefab`, gán `enemyReference`. Logic sẵn ở `Assets/Project/GamePlay/Scripts/Node.cs:75-96` — **không sửa code**.
- `CheckAndDownload("WorldBoss", …)` trước `Manager.LoadWorldBoss` trong `UIWorldBoss._OnPlayClick` (copy mẫu `Events/AirDefense/Scripts/UIAirDefenseIntro.cs:73`).
- `ReleaseAllHandle()` ở `WorldBoss/Scripts/LevelWorldBossManager.cs:92` (`OnMatchCompleted`).
- **Verify**: (1) log `Dữ liệu cần tải label[WorldBoss]` + progress; (2) 2 boss × 3 phase spawn đúng, không NullRef; (3) thoát trận RAM nhả; (4) `Caching.ClearCache()` rồi vào lại phải tải được từ CDN.

### P3 — Boss banner · ~3.0 MB
- Gán `SprBossShow` = 6 sprite `Boss1|3_Phase1-3`, xoá `FakeSprBossShow` (`WorldBoss/Scripts/WorldBossSO.cs:566-567`).
- Sửa `WorldBoss/Scripts/UI/UIWorldBoss.cs:278` và `:286` → `Addressables.LoadAssetAsync<Sprite>` + `AddressableManager.instance.AddLoadedHandle(handle)`, mẫu `Assets/Project/Scripts/AddressableMapSprite.cs:36-56`.
- ⚠️ **`UIWorldBossFinished` và `UIWorldBossShop` cũng ref `691c547a…` (= `Boss1_Phase1`) qua `m_Sprite`** — phải cắt cả 2 chỗ, không thì 3 MB vẫn ở lại build.
- **Verify**: banner đúng từng phase; `grep FakeSprBossShow` = 0.

### P4 — Texture UI nặng còn lại · ~2.2 MB
- Thêm `AddressableUISprite` — clone `AddressableMapSprite` nhưng cho `UnityEngine.UI.Image` (~40 dòng).
- Áp cho `bg_ui_WB`, `infor_boss`, `frame_boss`, `banner_bosstime` → **xoá `m_Sprite`** tương ứng.
- `Atlas/` (76 file nhỏ, 1.4 MB): **để nguyên** — async load icon nhỏ gây nháy UI.
- **Verify**: mở đủ 8 popup (PopupId 200–207), không ô trắng/tím.

### P5 — Content update (độc lập, làm song song được)
- Sau build Addressables chính thức, commit `addressables_content_state.bin` vào `AddressableAssetsData/Android|iOS/`.
- **Verify**: `Addressables > Build > Update a Previous Build` không báo thiếu state file.

### KHÔNG làm
- **8 cell prefab / 278 KB** — EnhancedScroller đồng bộ, chi phí > lợi ích.
- **UI-B** (đưa popup ra khỏi `Resources/` + `UIManager.GetPopup` async): đụng `Assets/DP/Scripts/UI/UIManager.cs` (shared base toàn game); `GetPopup` hiện đồng bộ → đổi chữ ký lan ra **mọi** module; đang chờ merge branch `main` refactor Popups. Chỉ lợi thêm ~2.6 MB. Làm sau merge, và làm cho cả project.

---

## 4. Rủi ro

1. **First-play cần mạng** — sau P2 người chơi lần đầu bắt buộc tải bundle. `AddressableManager` hiện chỉ log khi fail, **không retry, không báo user**. Các mode khác cũng vậy nên không phải regression, nhưng WB là event có hạn giờ → fail = mất lượt.
2. **Bundle phải lên CDN trước release** — path hardcode version `14.8`. Xác nhận với người quản CDN.
3. **Merge branch `main`** đang treo: P0/P1/P5 an toàn; P2/P3/P4 đụng file trong `WorldBoss/` nên conflict thấp; UI-B thì cao.
4. Trước khi xoá `m_Sprite` bất kỳ: xác minh runtime load chạy được (component wired, GameObject active vì load ở `OnEnable`, có `AddressableManager` trong scene — `AddressableMapSprite` gọi `AddressableManager.instance` **không null-guard**).

## 5. Tổng

| Bước | Source rời build |
|---|---|
| P0 | **~6.2 MB** (đo lại 2026-07-28; +4.1 MB orphan chỉ gọn repo, +binary do bỏ 6 link.xml) |
| P2 | ~32 MB |
| P3 | ~3.0 MB |
| P4 | ~2.2 MB |
| **Tổng** | **~48 MB source** |
---

## 6. Tiến độ thực thi — cập nhật 2026-07-29

Từ đây trở xuống là **trạng thái thật đo trên disk**, ưu tiên hơn mục 1-5 ở trên khi hai bên xung đột.

### P3 — Boss banner: CODE XONG, DATA + BUILD CÒN TREO (2026-07-29, cuối phiên)
- `WorldBossPhaseData.FakeSprBossShow` **đã xoá**, chỉ còn `SprBossShow` kiểu `AssetReferenceSprite` (`WorldBossSO.cs:569`). ⚠️ User từng tự đổi field này sang `Sprite`; để vậy mà Unity re-serialize là **6 `m_AssetGUID` trong asset bị wipe** và **không có git để lùi** (repo không phải git). Đã trả về đúng type, verify 6 GUID còn nguyên trên disk.
- Hàm mới `UIWorldBoss.ShowBannerBoss()` (`UIWorldBoss.cs:292-311`): guard `RuntimeKeyIsValid` → `LoadAssetAsync<Sprite>` → `AddressableManager.instance.AddLoadedHandle` → set `imgBannerBossCurrent.sprite`; cache `bannerLoadedKey` để không load lại cùng phase; fail = log đỏ + **giữ placeholder**. 2 call site cũ `:279`/`:287` đã thay bằng nó (`:282`, `:290`). +2 using + 2 field (`:4-5`, `:18-19`). IDE sạch, **chưa compile/Play**.
- **`worldboss-texture` giờ có 6 entry** (`Boss1|3_Phase1-3`) + label `WorldBoss`, `SprBossShow` wire đủ 6 ô ⇒ dòng "UI/Textures/Use chưa tick, có chủ ý" ở dưới **đã lỗi thời với 6 file này**.
- 🔴 **Bundle texture chưa từng build**: 6 entry thêm SAU build #2 (lúc đó group rỗng) ⇒ phải `Build > New Build` + upload bundle/catalog/hash + APK mới, nếu không cả 6 phase rơi vào log `banner load failed` và đứng ở placeholder.
- **Sửa số của P3 ở mục 3**: (a) `Boss1_Phase1` bị **4** prefab hard-ref, không phải 2 — `UISelectMode.prefab:11010` (`Image-Banner-Boss`, hiện TRƯỚC mọi download, module khác), `UIWorldBossShop.prefab:270,4809` (`Image-Boss` ×2), `UIWorldBossFinished.prefab:432` (`Image-Banner`), `UIWorldBoss.prefab:38267` (`Image-Banner` = `imgBannerBossCurrent`, chỉ là placeholder). Shop/Finished là **static**, không code nào set (`UIWorldBossFinished.cs:46-47` chỉ set currency + phase icon). (b) "3.0 MB" là **source bytes** — 6 png đều 669×360 RGBA, `maxTextureSize: 2048` ⇒ ~235 KB/file trong build ⇒ P3 thật ≈ **1.4 MB**.
- **Việc data user tự làm, chưa xong**: duplicate `Boss_Banner_Default.png` ra **ngoài** `Addressable/` với `maxTextureSize: 512` (~60 KB), **không tick addressable**, rồi đổi `m_Sprite` của 4 chỗ trên sang nó ⇒ 6 png banner rời APK hết, hết double-ship, và placeholder luôn có sẵn khi chưa tải.

### Đã xong
- **3 group đã tồn tại** (`worldboss-prefab` / `worldboss-spine` / `worldboss-texture`) + label `WorldBoss` đã có trong `m_LabelTable`. Mục 1 nói "không group nào" — **đã lỗi thời**.
- **`worldboss-spine`: 90/90 entry**, đủ label, `m_Address` = full asset path. Folder đã rename `Enemies/Textures` → `Enemies/Spine`; 6 entry cũ trỏ `/Textures/` đã resync. Verify YAML: 90 entry · 90 label · **0** address còn `/Textures/`.
- **`worldboss-prefab`: 10 entry** (6 `EnemyWB*_A` + 4 `EnemyDrone_*_A`). Còn thiếu `Effects/**` (35) + `Enemies/Prefabs/{Bullets 10, Nodes 6, Paths 12, VFX 4}` — chưa là entry.
- **P2 mắt xích 4 XONG: 6/6 Node** (`Enemies/Prefabs/Nodes/`) → `useAddressable: 1`, `enemyPrefab` trống, `enemyReference` trỏ đúng Enemy prefab từng phase, cả 6 target là entry + label.
  ⇒ Chuỗi `Resources/Data/WorldBossSO.asset → Node → Enemy → Spine` **ĐÃ CẮT**. 90 file Spine giờ chỉ còn được ref bởi 10 enemy prefab (đều là entry). Rời base build: Spine 5.9 MB + 10 prefab YAML 17.8 MB; ứng viên đi kèm `Bullets` 4.0 + `VFX` 5.2 + `Paths` 0.14 MB (chưa xác nhận closure).
- **Tool verify mới: `Assets/QL_Tools/Editor/QLAddressableSync.cs`**, menu `QL/Addressable Sync`. Dropdown group + label lấy động từ settings, ô input folder (+Browse, kéo-thả), 5 preset folder WB, nút: `Verify` (chỉ đọc) · `Apply` · `Bỏ tick tất cả` · `Bỏ label` · `Fix Paths → Remote`. Verify báo `MISSING ENTRY` · `WRONG GROUP` · `MISSING LABEL` · `STALE ADDRESS` · `ORPHAN`, tách riêng khung SYNC và khung PATHS (Local/Remote + includeInBuild). **Đừng viết lại `WorldBossAddressableSetup.cs` như mục P1 đề xuất** — tool này thay thế, generic theo folder.

### Quyết định mới
- ✅ **`CheckAndDownload("WorldBoss")` — ĐÃ APPLY 2026-07-29** (user đảo quyết định "bỏ qua" trước đó trong cùng ngày). Vị trí thật sau khi sửa: `const string ADDRESSABLE_LABEL = "WorldBoss";` ở `UIWorldBoss.cs:16` (cạnh `SCENE_NAME`), thân mới `_OnPlayClick()` ở `UIWorldBoss.cs:399-448`. `CheckAndDownload` chèn **trước** `WorldBoss.TryPlay()` (TryPlay trừ vé + `SaveData()` ngay tại `WorldBoss.cs:574-578` ⇒ tải fail sau đó = mất vé) — `TryPlay()` + `LoadWorldBoss()` giờ nằm trong `_OnDownloadCompleted`. Khuôn 3 local function copy `UISelectMode.cs:93-134`; dùng `ScriptLocalization.Please_try_again_later_` (`Assets/ScriptLocalization.cs:9`, namespace `I2.Loc`, `using` đã có ở `UIWorldBoss.cs:6`) để **không** phải thêm `[SerializeField] LocalizedString` vào prefab nằm trong `Resources/`. Compile sạch (IDE chỉ báo 1 hint "Name can be simplified" cho `this.Hide()` — code cũ giữ nguyên). **Chưa test runtime.**

  Code đã apply (log theo style của file này, `<color=…>[UIWorldBoss]</color>`, không theo style plain của `UIEvent`/`UIHome`):

  ```csharp
  public void _OnPlayClick()
  {
      if (!IsShow) return;
      SoundUI.OnButtonClick();
      if (WorldBoss.CurrentEventStatus != Games.Events.EVENT_STATUS.ACTIVE)
      {
          DPDebug.Log($"<color=#ffd900>[UIWorldBoss]</color> play blocked, event status {WorldBoss.CurrentEventStatus}");
          return;
      }
      if (!WorldBoss.CanPlay())
      {
          // no free plays and not enough tickets --> show buy ticket popup
          popupBuyTicket.Show(ShowTicket, (int)WorldBoss.TicketCurrent, WorldBossSO.DailyTicket);
          return;
      }
      bool _isNeedToDownload = false;
      AddressableManager.instance.CheckAndDownload(ADDRESSABLE_LABEL, _OnStarDownload, _OnLoading, _OnDownloadCompleted);

      void _OnStarDownload()
      {
          DPDebug.Log($"<color=#4aff21>[UIWorldBoss]</color> start downloading label {ADDRESSABLE_LABEL}");
          _isNeedToDownload = true;
          UIManager.Instance.GetPopup(PopupId.DownloadProgress).Show();
      }
      void _OnLoading(float _percent)
      {
          if (UIManager.Instance.IsHasPopup(PopupId.DownloadProgress, out UIDownloadProgress _popup))
              _popup.SetProgress(_percent);
      }
      void _OnDownloadCompleted(bool _isDownloaded)
      {
          DPDebug.Log($"<color=#4aff21>[UIWorldBoss]</color> download completed {_isDownloaded}, needed {_isNeedToDownload}");
          if (!_isDownloaded)
          {
              UIManager.Instance.GetPopup<UIDownloadProgress>(PopupId.DownloadProgress)
                  .ShowError(ScriptLocalization.Please_try_again_later_);
              return;
          }
          if (_isNeedToDownload && !UIManager.Instance.IsHasPopup(PopupId.DownloadProgress, out _)) return;

          if (UIManager.Instance.IsHasPopup(PopupId.DownloadProgress, out UIDownloadProgress _popup))
              _popup.Hide();

          if (!WorldBoss.TryPlay()) return;
          Manager.Instance.LoadWorldBoss(SCENE_NAME, _onCompleted: () =>
          {
              this.Hide();
          });
      }
  }
  ```

  Hành vi kỳ vọng: Local ⇒ `GetDownloadSize` = 0 ⇒ `_OnStarDownload` không chạy, vào trận như cũ, không popup. Remote chưa tải ⇒ popup progress → xong → **rồi mới** trừ vé. Fail ⇒ `ShowError`, **vé còn nguyên**. User đóng popup giữa lúc tải ⇒ `return` sớm, không trừ vé.
  Verify: (1) compile sạch, bấm Play vào trận bình thường; (2) Console `[UIWorldBoss] download completed True, needed False`; (3) vé trừ đúng 1 lần, đối chiếu `textTicket`; (4) sau khi đổi Remote — chặn CDN → phải thấy `ShowError` và vé không giảm.
- **Sửa P2: KHÔNG cần thêm `ReleaseAllHandle()` cho WB.** `GamePlayController` đã gọi ở 8 chỗ (689, 735, 1096, 1255, 1438, 1508, 1539, 1607) + `UIPause.cs:143,145`; WB dùng chung `GamePlayController` nên tự có. Câu "thêm ở `LevelWorldBossManager.cs:92`" trong P2 là **sai/không cần**.
- **3 group vẫn Local** (`969b8061…` / `18c706ea…`), chưa đổi Remote: user **chưa build remote lần nào**, nên hoãn. Paths chỉ có hiệu lực ở lần `Build > New Build`, đổi sớm không lợi gì.
- **`UI/Textures/Use` (30) + `Atlas` (76) chưa tick, CÓ CHỦ Ý.** Không có component nào load Sprite vào `UnityEngine.UI.Image` qua Addressables (`grep AssetReferenceSprite` toàn `Project/` = chỉ `AddressableMapSprite.cs` cho `SpriteRenderer`, + slot **rỗng** `WorldBossSO.SprBossShow`). Consumer thật = `Image.m_Sprite` hard-ref trong popup ở `WorldBoss/Resources/`. Tick khi group còn Local ⇒ **+8.6 MB vào APK, ship 2 lần**. Điều kiện tick: làm P4 trước, HOẶC đổi Remote cùng lúc tick. `Atlas/` chỉ là tên folder — 0 `SpriteAtlas` asset, `spritePackingTag` rỗng cả 95 file.

### Phát hiện mới
- 🔴 **12 file orphan bị tick oan**: `Enemies/Spine/World_Boss_3/Gun2/*` (216 KB) + `Gun_Rada/*` (108 KB) — grep GUID từng file: **không ai trong toàn `Assets/` ref tới**. Trước khi tick chúng không vào build; giờ là entry của group Local nên sẽ vào. Nên bỏ tick hoặc xoá.
- **`AddressableManager.instance` sống trong scene WB** — đã xác minh: nó chỉ nằm trên `Project/Prefabs/GameManagers.prefab`, prefab đó chỉ có ở `Menu.unity` (+MenuTest, GamePlayFake), **không** ở `GamePlay.unity` hay `WorldBoss.unity`. Sống sót nhờ `Manager : Singleton<Manager>` → `DP/Scripts/Singleton.cs:23` `DontDestroyOnLoad(gameObject)`. Flow: `Manager.cs:1006` `LoadSceneAsync(GamePlay)` **single** → `WorldBoss.unity` **additive**. ⇒ `Node.cs:84` không null. (Rủi ro #4 mục 4 coi như đã kiểm cho WB.)
- ⚠️ **Race chưa test**: `Node.OnLoadAddressable()` async trong `OnEnable`, còn `SpawnEnemy()` guard `if (enemyPrefab != null)` (`Node.cs:124`) ⇒ load chưa xong = **bỏ qua spawn, không log, không lỗi**. Cả 6 Node đang `showOnLoadComplete: 0`. Đây là rủi ro chức năng số 1 sau khi cắt hard-ref.
- `AddressableApkDuplicateTextureChecker` (`Tools/Addressables/Check textures duplicated in APK`) chỉ lọc `Texture` + `SpriteAtlas` (`:504-507`) ⇒ với Spine chỉ thấy 15 png / 4.6 MB, **bỏ sót** 15 `.skel.bytes` + 15 `.atlas.txt` + 15 `.mat` + 30 `_SkeletonData/_Atlas` (~1.3 MB). Đừng lấy số của nó làm số cuối.
- Pre-existing, **chưa sửa**: `AddressableManager.Awake()` gán `instance = this` vô điều kiện (không guard trùng) trong khi `Singleton.Awake` destroy bản trùng ⇒ nếu Awake của nó chạy trước trên bản trùng, `instance` trỏ object sắp bị destroy.

### Build #2 — 2026-07-29 15:30 (catalog `2026.07.29.08.30.02`) — ĐÃ REMOTE, ĐÃ BỎ TICK ORPHAN
Trạng thái **mới nhất**, ưu tiên hơn khối "Build #1" ngay dưới.

- ✅ **Bỏ tick 12 orphan** (`Gun2` + `Gun_Rada`) qua tool `QL/Addressable Sync`. Catalog: entry asset WB **100 → 88** (78 spine + 10 prefab), orphan **0**. `worldboss-spine` bundle 1,207,756 → **1,147,410 B** (−60,346 B, −5%; ít hơn 324 KB source vì png đã nén). `worldboss-prefab` giữ nguyên 1,477,357 B, hash `e235e1d6` không đổi. Trước khi bỏ tick đã verify GUID: 12 file **0 reference thật**, hit duy nhất là `Assets/FR2_Cache.asset` (DB của plugin Find Reference 2, không phải consumer).
- ✅ **Cả 3 group → Remote**: `m_BuildPath: ec634c1d…` (Remote.BuildPath = `ServerData/[BuildTarget]`) · `m_LoadPath: 030cfeb3…` (Remote.LoadPath = `https://onegamecdn.b-cdn.net/1941/LinhTest/[BuildTarget]`). Catalog xác nhận 2 bundle WB đã ra URL CDN.
- 📉 **`aa/Android/Android/` (đường vào APK) 5.1 MB → 2.5 MB** (chỉ còn abyss + airdefense). `ServerData/` 52 → 55 MB. **APK giảm 2,624,767 B = 2.50 MB.** Label `WorldBoss` vẫn trong key table ⇒ `CheckAndDownload` resolve đúng.
- ⚠️ **Phải upload CDN, nếu không WB chết**: 2 bundle WB + `catalog_2026.07.29.08.30.02.json` + `.hash` lên `1941/LinhTest/Android/`. 23 bundle module khác giữ nguyên hash, không cần upload lại. App live KHÔNG bị phá (catalog versioned theo timestamp, `settings.json` trong APK cũ trỏ catalog cũ) — nhưng muốn test thì **phải build APK mới**.
- 🔴 **Race ở `Node` vừa tăng hạng rủi ro**: bundle giờ phải chờ mạng, `SpawnEnemy()` guard `if (enemyPrefab != null)` (`Node.cs:124`) ⇒ load chưa xong = bỏ qua spawn, **không log, không lỗi**. Gate `_OnPlayClick` chỉ đảm bảo bundle đã tải, KHÔNG đảm bảo `LoadAssetAsync` xong khi `OnEnable` chạy. Cả 6 Node `showOnLoadComplete: 0`. Test: cài app mới (persistentDataPath sạch) → popup progress **giờ mới chạy lần đầu** → 6 phase → chặn CDN (`ShowError` + vé không giảm) → throttle 3G.
- ⚠️ `LinhTest` trong URL CDN nhìn như folder test, chưa xác nhận profile release.

### Build #1 — 2026-07-29 15:14 (catalog `2026.07.29.08.13.56`) — lúc còn Local, còn orphan
Đã chạy `Build > New Build`. Số dưới đây **thay thế** mọi con số source-bytes ở mục 1-5 khi nói về tác động build.

- `worldboss-spine_assets_all_*.bundle` = **1,207,756 B** (1.15 MB) — nén từ 5.9 MB source.
- `worldboss-prefab_assets_all_*.bundle` = **1,477,357 B** (1.41 MB).
- **WB tổng 2.56 MB.** `worldboss-texture` **không sinh bundle** (group rỗng, đúng chủ ý).
- Catalog có **100 entry WB** (90 spine + 10 prefab) + label `WorldBoss` **có trong key table** ⇒ `CheckAndDownload("WorldBoss")` resolve được, không throw.
- 🔴 **WB là module DUY NHẤT còn Local.** Catalog: WB → `{Addressables.RuntimePath}/Android/…`; campaign/endless/pvp/noel/tournament → `https://onegamecdn.b-cdn.net/1941/LinhTest/Android/…`. `ServerData/` = **52 MB, 0 byte WB**; `aa/Android/Android/` (đường vào APK) = **5.1 MB, WB chiếm 2.56 MB ≈ 50%**.
- ⚠️ **Tới giờ APK chưa giảm byte nào.** Cắt hard-ref chỉ đổi *cách* asset vào build (scene-dependency → local bundle), không đổi *việc* nó vào build. Phần thắng thật = đổi Remote ⇒ **−2.56 MB APK**, con số này giờ đã chắc.
- ⚠️ Vì bundle local, `GetDownloadSizeAsync("WorldBoss")` = **0** ⇒ gate download ở `_OnPlayClick` chạy nhưng **không bao giờ hiện popup**. Nhánh progress + `ShowError` **chưa test được** cho tới khi đổi Remote.
- 🔴 **12 orphan `Gun2`/`Gun_Rada` đã vào bundle thật** (đếm trong catalog). Trước khi tick chúng không vào build.
- Không phải lỗi: catalog có `Project/WorldBoss/Scenes/WorldBoss` do Built In Data group bật `m_IncludeBuildSettingsScenes: 1` (+ `m_IncludeResourcesFolders: 1` — xác nhận lại `Resources/` ship vô điều kiện), **không** phải entry của group WB. Scene vẫn ship qua Build Settings, `Manager.LoadWorldBoss()` không ảnh hưởng.
- Chưa có `.apk`/`.aab` ⇒ chưa so baseline bằng `BuildReportTool`.

### Việc còn treo (thứ tự rẻ → đắt)
1. Bỏ tick 12 file orphan `Gun2` + `Gun_Rada` (324 KB).
2. Test 6 phase spawn boss (race ở trên) — chạy `New Build` trước, vì `m_BuildAddressablesWithPlayerBuild: 2`.
3. Tick nốt `worldboss-prefab`: `Effects/**` + `Bullets`/`VFX`/`Paths` (preset có sẵn trong tool).
4. Đổi 3 group → Remote, trước lần build remote đầu tiên.
5. ~~P3~~ **code xong 2026-07-29** (xem khối P3 đầu mục 6) — còn: bản `Boss_Banner_Default` nhẹ + đổi `m_Sprite` 4 prefab + `New Build` & upload. · P4 (`AddressableUISprite` cho `Image`) · P0 (dọn mockup, hoãn tới release) · P5 (`content_state.bin`).
