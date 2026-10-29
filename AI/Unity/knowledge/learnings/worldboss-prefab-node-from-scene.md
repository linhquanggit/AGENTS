# WB6: đóng gói prefab _A + Node test từ scene MakeAll — ref scene không theo được vào prefab

Scope: `Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB6_P{1,2,3}_A.prefab`, `Assets/Project/[GD-Khoa]/TestEnemyNode/WB/WB6_P{1,2,3}.prefab`, `Test-WB.asset`  |  Evidence: plan muse-bridge `wb6-prefab-node` T2/T3, 2026-10-29; GUID đã đối chiếu với `.meta` thật

Đóng gói 3 root `EnemyWB6_P{p}_A` trong `MakeAll.unity` (menu tạm `Tools/WB6/Make Prefabs + Nodes`, T1) thành prefab Addressable-folder + Node test, gắn vào Test-WB:
- | Phase | Prefab `_A` GUID | Node test GUID |
  | P1 | `a70c20bdacdeb4ac8b45de926ec660f7` | `bb7351cef767f4dfe9b3ca1389eafee5` |
  | P2 | `0aa12b4c4dd5b4588a4ddb6f7aea97be` | `3e4a4d14b06184d3fb069dbb6303c169` |
  | P3 | `da86eabfdd9034a86937997f10d11263` | `79d99910228a34bb4aeab32d57d9819f` |
- `Enemy.type = WB6 (50006)` trên cả 3 prefab `_A`; root prefab trong asset `localPosition = 0` (theo mẫu `EnemyWB2_P{p}_A`). T2: 0 missing script. T3: spawn test 3 phase đã xong.
- `Test-WB.asset` `data` = 6 phần tử: WB2_P1..P3 giữ nguyên (index 0–2), thêm WB6_P1..P3 (index 3–5). Append, không ghi đè: `GetItemsInPath()` ghi đè cả `data` nên không dùng. Spawn test qua `TestManager.SpawnEnemy(3/4/5)`.
- Node test = copy Node WB2_P{p}, đổi tên root thành `WB6_P{p}`, set `enemyPrefab` trực tiếp bằng SerializedObject, `useAddressable` 0, KHÔNG dùng `enemyReference`.
- Luật ref ngoài cây: trước khi save, quét mọi ObjectReference trong cây bằng SerializedObject + IsPersistent; ref trỏ tới object scene ngoài cây → ghi WARN, không FAIL nếu field đó null-safe. Trường hợp thật: P3 `targetDefault` (2 `LauncherShotE`) đã null vì prefab không lưu được ref scene `Fighter`; `LauncherShotE.cs:63` đã null-safe nên test vẫn chạy. Đây là WARN cho Release, không phải lỗi chặn test — nhưng Node chính thức không được để ref scene lọt vào prefab; phải gán target lúc runtime hoặc bằng ref trong prefab.
- Bẫy đóng gói riêng của WB6: P1_A có 6 gun `SkeletonAnimation` bị `skeletonDataAsset = null` lúc đóng gói (đã WARN từ trước: gun P1 null trong scene) → sau khi save prefab phải gán lại `Gun1/2/3_SkeletonData` và kiểm trên prefab đã save, không chỉ kiểm trong scene.
- Ranh giới của bước này: chỉ tới prefab `_A` + Node test. Node chính thức `Use/.../Nodes`, `WorldBossSO` (gán NodeWB, `Released`), nhóm Addressables là việc Release riêng, không làm trong plan đóng gói test này. Liên quan: library WB mục 11 + mục 14.
