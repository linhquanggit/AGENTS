# WB6 — tạo prefab _A + Node test 3 phase, gắn vào Test-WB
goal: Đóng gói 3 root EnemyWB6_P1_A/P2_A/P3_A đang dựng trong MakeAll.unity thành prefab Addressable-folder, tạo 3 Node test WB6_P1..P3 và thêm vào Test-WB.asset; spawn thử được cả 3 phase trong TestEverything.
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library.md (mục 5 mô hình prefab/Node, mục 14 config, mục 15 — Linh yêu cầu trực tiếp nên được tạo prefab/Node); BR_WorldBoss/briefs/WB6_PLAN.md; plan wb6-full, wb6-art-p2-p3-anim, wb6-p2-gun6-split, wb6-p3-gun9-split (đều done)
approvals: Linh yêu cầu trực tiếp 2026-10-29. Được tạo 3 prefab Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB6_P{1,2,3}_A.prefab; 3 Node Assets/Project/[GD-Khoa]/TestEnemyNode/WB/WB6_P{1,2,3}.prefab; sửa Test-WB.asset (THÊM 3 node, giữ 3 node WB2); lưu MakeAll.unity (3 root thành prefab instance). KHÔNG sửa WorldBossSO, nhóm Addressables, Node chính thức Use/.../Nodes. Được xoá: Editor script menu tạm + .meta, ~/.claude/logs/wb6-prefab-node*.
test: Mở Assets/Project/Scenes/TestEverything.unity → Play → TestManager.SpawnEnemy(indexEnemy=3/4/5, indexData=vị trí Test-WB trong TestManager.testData — menu tự tìm theo tên) mỗi phase ~10s, đọc log → DestroyEnemy → thoát Play bằng EditorApplication.isPlaying=false.

## T1 — Muse: viết MenuItem tạo prefab + node + gắn Test-WB
owner: muse
depends: -
context:
- Mô hình: prefab boss EnemyWB{n}_P{p}_A (script boss + child WorldBossWeapon); Node giữ ref tới prefab. Node test dùng `enemyPrefab` trực tiếp (useAddressable 0), KHÔNG dùng enemyReference.
- Node test mẫu = WB2_P{p}.prefab: root layer 7, pos (0.03,-4.2,0), scale (0.35,0.35,1), CircleCollider2D trigger r0.5, Node: type 0, speedMove 2, path {fileID 7695132575833699494, guid 6dd5b8ac17825664ca65a038486cb2f6}, isEvent 1, lstEvents [eventType 2, index 0].
- Menu `Tools/WB6/Make Prefabs + Nodes` (Editor script tạm, vd Assets/Project/WorldBoss/Editor/WB6MakePrefabNode.cs), idempotent (chạy lại không nhân đôi), làm lần lượt P1→P3:
  1. Mở MakeAll.unity, tìm root `EnemyWB6_P{p}_A`. Quét mọi ObjectReference trong cây: ref trỏ tới object scene NGOÀI cây → ghi log WARN (đường dẫn component.field → target); nếu có ref ngoài cây bắt buộc (không null được) → bỏ phase đó, ghi FAIL.
  2. PrefabUtility.SaveAsPrefabAssetAndConnect → .../Addressable/Enemies/Prefabs/EnemyWB6_P{p}_A.prefab. Trong asset: root localPosition = 0 (theo EnemyWB2_P{p}_A), giữ scale/rotation/children. Không đổi tên field/enum.
  3. AssetDatabase.CopyAsset WB2_P{p}.prefab → [GD-Khoa]/TestEnemyNode/WB/WB6_P{p}.prefab (GUID mới), đổi tên root GameObject = WB6_P{p}, set `enemyPrefab` (SerializedObject) = component EnemyWB6_P{p} trên root prefab mới.
  4. Thêm 3 Node vào `data` của Test-WB.asset (sau WB2_P1..P3 → index 3/4/5); không chạy GetItemsInPath.
  5. Lưu scene + AssetDatabase.SaveAssets. Ghi ~/.claude/logs/wb6-prefab-node.log: mỗi phase GUID prefab, GUID node, số WorldBossWeapon, số Missing Script/ref null mới, WARN ref ngoài cây; cuối: thứ tự Test-WB.data.
- Thêm menu `Tools/WB6/Test Spawn Nodes`: vào TestEverything + Play, tìm index Test-WB trong TestManager.testData theo tên, SpawnEnemy(3/4/5) lần lượt mỗi cái 10s, log tên enemy spawn, state (đạt PLAYING chưa), số Exception/Error console trong 10s, rồi DestroyEnemy; xong tự thoát Play. Log ~/.claude/logs/wb6-prefab-node-test.log.
verified:
- Node.cs: Assets/Project/GamePlay/Scripts/Node.cs:27 `[SerializeField] protected Enemy enemyPrefab;` (guid script e7af0be798a3cc047bdd41062c60b660)
- TestEnemyData.cs:7-8 `string path; List<Node> data;` GetItemsInPath() ghi đè cả data (không dùng). Test-WB.asset hiện data = WB2_P1/P2/P3 (guid 8f45cee7…, 263012bc…, 100ffa32…, fileID 233064654644583307)
- TestManager.cs:87 `List<TestEnemyData> testData`; :90 `SpawnEnemy(int indexEnemy, int indexData)`; :98 DestroyEnemy(); scene dùng: Assets/Project/Scenes/TestEverything.unity
- MakeAll.unity: root EnemyWB6_P3_A (dòng ~39123), P2_A (~94097), P1_A (~105574), m_PrefabInstance 0 (chưa là prefab)
- Script boss: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-6/EnemyWB6_P1/P2/P3.cs : EnemyWorldBoss
- Chưa có EnemyWB6_* trong Addressable/Enemies/Prefabs, chưa có WB6_* trong TestEnemyNode/WB
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/ (worldboss-*), BR_WorldBoss/briefs/WB6_PLAN.md
files: Assets/Project/WorldBoss/Editor/WB6MakePrefabNode.cs (tạm)
done_when: unity-errors = 0 error CS; script chỉ dùng API có thật (PrefabUtility/AssetDatabase/SerializedObject), liệt kê file đã tạo.

## T2 — Claude: review, chạy menu tạo, kiểm file
owner: claude
depends: T1
context: Review T1 theo context/verified (đúng path, append không ghi đè, enemyPrefab = component EnemyWB6_P{p}). PASS mới chạy `Tools/WB6/Make Prefabs + Nodes` (1 lệnh execute_menu_item), đọc log.
files: 3 prefab _A, 3 node WB6_P*, Test-WB.asset, MakeAll.unity
done_when: tồn tại 6 file .prefab + .meta; Test-WB.asset data = 6 phần tử (WB2_P1..P3, WB6_P1..P3); mỗi node enemyPrefab guid = guid EnemyWB6_P{p}_A tương ứng (grep); 0 Missing Script trong 3 prefab; log không có FAIL; unity-errors 0 error CS.

## T3 — Claude: test spawn 3 phase
owner: claude
depends: T2
context: Chạy `Tools/WB6/Test Spawn Nodes`, đọc ~/.claude/logs/wb6-prefab-node-test.log. Lỗi → muse-ask --bug kèm dòng log.
done_when: 3/3 node spawn đúng EnemyWB6_P{p}_A, cả 3 đạt PLAYING trong 10s, 0 Exception/Error mới trong console; đã thoát Play.

## T4 — Muse: cập nhật library/learnings
owner: muse
depends: T3
context: Ghi kết quả: WB6 có prefab _A P1–P3 + Node test WB6_P1..P3 trong Test-WB (index 3–5); GUID; WARN ref ngoài cây (nếu có); việc còn lại để Release (Node chính thức Use/.../Nodes, WorldBossSO, Addressables).
files: BR_WorldBoss/library (bảng Config entry points + mục WB6), /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-prefab-node-from-scene.md
done_when: 2 file cập nhật, có GUID và đường dẫn đúng như log T2.

## T5 — Claude: nghiệm thu + dọn
owner: claude
depends: T4
context: Sau PASS tự xoá (không hỏi): Assets/Project/WorldBoss/Editor/WB6MakePrefabNode.cs + .meta (và thư mục Editor nếu do T1 tạo mới và rỗng), ~/.claude/logs/wb6-prefab-node*.log, ~/AGENTS/muse-bridge/.agent/shots/* của mission này. GIỮ: 3 prefab EnemyWB6_P*_A, 3 node WB6_P*, Test-WB.asset, MakeAll.unity, script EnemyWB6_P*.cs.
done_when: unity-errors 0 error CS sau khi xoá; không còn file tạm trong project/logs/shots; báo Linh dòng `Đã dọn: …`.

## Câu hỏi mở
- Test-WB: mặc định THÊM WB6 sau WB2 (giữ WB2). Nếu Linh muốn thay hẳn WB2 → sửa bước 4 T1 và index test thành 0/1/2.
- Release thật (Node_WB6_P*_A chính thức + WorldBossSO + Addressables) chưa nằm trong plan này.
