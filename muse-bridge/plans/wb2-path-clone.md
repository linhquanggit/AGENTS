# WB2 — tách path spline riêng (clone từ Path_WB5_P1)
goal: 3 thân EnemyWB2_P1/P2/P3_A trong MakeAll đang dùng thẳng Path_WB5_P1_1/_2 của WB5. Clone ra 6 path riêng của WB2 (giữ nguyên điểm) và trỏ splineMove sang bản clone, để sau chỉnh đường bay WB2 không ảnh hưởng WB5.
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library (mục 12 EnemyWorldBoss path 1/2, mục 14/15 MakeAll); quét YAML MakeAll.unity 2026-10-16

## T1 — Clone 6 prefab path cho WB2
owner: claude
depends: -
context:
Clone bằng `AssetDatabase.CopyAsset` (GUID mới, không copy file .meta tay). Giữ nguyên điểm/thông số — chỉnh đường bay làm sau.
Tên theo quy ước `Path_WB{n}_P{p}_{path}`:
- Path_WB5_P1_1 → Path_WB2_P1_1, Path_WB2_P2_1, Path_WB2_P3_1
- Path_WB5_P1_2 → Path_WB2_P1_2, Path_WB2_P2_2, Path_WB2_P3_2
verified:
- Folder: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/ — chưa có Path_WB2_* (đã có WB1, WB3, WB4 P1–P2, WB5 P1–P3).
- Path_WB5_P1_1.prefab guid 9040a20b501264f89a494ad19a6017d5; Path_WB5_P1_2.prefab guid adbadbeb880b841a4b4a2e25bccde99b; PathManager fileID trong prefab = 7646840350953909207.
- 2 path này còn được EnemyWB5_P1_A.prefab dùng → KHÔNG sửa/move/rename bản gốc.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 12, 15)
files: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/Path_WB2_P{1,2,3}_{1,2}.prefab (mới)
done_when: 6 prefab mới tồn tại, mỗi cái GUID khác bản gốc, điểm path trùng bản gốc; Path_WB5_P1_1/_2 không đổi (git/plastic diff rỗng).

## T2 — Trỏ splineMove của 3 thân WB2 sang path clone
owner: claude
depends: T1
context:
Mỗi thân có 2 splineMove (path 1 = appear, path 2 = loop sau ChangeToPlayingMode → StartPath(2)). Thay `pathContainer` theo đúng số: path đang trỏ _1 → Path_WB2_P{p}_1, đang trỏ _2 → Path_WB2_P{p}_2. Làm qua SerializedObject trong Editor (không sửa YAML tay).
verified:
MakeAll.unity (Assets/Project/Scenes/MakeAll.unity):
- EnemyWB2_P1_A (GO 6415632611665152897): splineMove 6415632611665152899 → WB5_P1_2; 6415632611665152900 → WB5_P1_1
- EnemyWB2_P2_A (GO 2051267650): splineMove 2051267652 → WB5_P1_2; 2051267653 → WB5_P1_1
- EnemyWB2_P3_A (GO 871018710): splineMove 871018712 → WB5_P1_2; 871018713 → WB5_P1_1
- Sau thay: grep 2 guid WB5_P1 trong MakeAll phải = 0 (hiện mỗi guid 3 lần, chỉ do WB2).
- splineMove: Assets/SimpleWaypointSystem/Scripts/Movement/splineMove.cs
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: Assets/Project/Scenes/MakeAll.unity (6 splineMove trên 3 thân WB2)
done_when: 6 pathContainer trỏ đúng Path_WB2_P{p}_{1|2}; grep guid 9040a20b… và adbadbeb… trong MakeAll = 0; EnemyWB5_P1_A.prefab vẫn trỏ path WB5.

## T3 — Test chạy path 3 phase + lưu
owner: claude
depends: T2
context:
Path clone giống hệt bản gốc → hành vi phải không đổi. Test từng phase bằng WB-Shooter (bật phase cần test, tắt phase khác rồi trả lại như cũ). Trước Play đổi spritePackerMode → BuildTimeOnlyAtlas, sau Stop trả AlwaysOnAtlas (library 15.4). Đọc Console ngay khi vào Play.
verified:
- EnemyWorldBoss.cs:169 StartPath(2) trong ChangeToPlayingMode; :171 OnEndPath1 → StopPath(1) → appear.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 15.4)
files: library mục 7/18.2 (ghi WB2 dùng Path_WB2_P{p}_{1,2}, clone từ WB5_P1)
done_when: mỗi phase P1/P2/P3 chạy hết path 1 → appear → path 2 loop như trước; Console sạch; 0 file .spriteatlas bị ghi; hỏi Linh trước khi save MakeAll.

## Câu hỏi mở
- Linh có muốn 3 phase dùng chung 1 cặp path (Path_WB2_1/_2) thay vì 6 file riêng? Plan đang làm 6 file riêng theo quy ước WB1/WB3/WB5 (mỗi phase 1 cặp).
