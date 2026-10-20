# Template picker cho PathManager trong Inspector (QL_Tools)
goal: Chọn GameObject có PathManager → Inspector hiện panel lưới thumbnail template; click là apply hình lên waypoint (có Undo). Template gồm hình sinh bằng toán + hình chụp từ path có sẵn; có nút lưu path hiện tại thành template mới. Code trong Assets/QL_Tools, không sửa SWS. Mặt phẳng XY.
project: ~/wkspaces/WB
source: Dò trực tiếp project WB (SimpleWaypointSystem, QL_Tools, 331 file prefab/scene chứa PathManager)

## T1 — Dữ liệu template + thư viện ScriptableObject
owner: muse
depends: -
context:
PathTemplate (ScriptableObject): string displayName, string category, Vector2[] points (chuẩn hoá về khung [-0.5,0.5], tâm 0), bool closed, Vector2 defaultSize (world unit).
PathTemplateLibrary (ScriptableObject): List<PathTemplate>; 1 asset duy nhất, tìm bằng AssetDatabase.FindAssets("t:PathTemplateLibrary").
Hàm tĩnh Vector2[] Resample(points, count, closed) — chia lại đều theo độ dài cung để apply được với số waypoint bất kỳ.
Generator toán (procedural, category "Cơ bản"): Line dọc, Line ngang, Chéo, Circle/Oval (closed), Figure8H (lemniscate Gerono x=sin t, y=sin t·cos t, closed), U-turn dọc, U-turn ngang (rainbow), Arc-curveback, ZigZag dọc (n lần đổi hướng), Sine dọc, Sine ngang, Left↔Right ping-pong.
verified:
Thống kê 688 PathManager trong prefab game (script tự phân loại theo hình): line/chéo 75, line dọc 67, vòng kín tròn/oval 51, U/V-turn dọc 15, arc/curve-turn 14, line ngang 14, zigzag dọc 11+5, U-turn ngang (rainbow) 7, số 8/phức kín 4, sine ngang 4, còn lại hình phức tạp riêng lẻ.
Số waypoint hay gặp: 2 (81), 4 (50), 9 (33), 3 (29), 6 (20), 12 (19).
PathManager.cs:15 namespace SWS; :20 public Transform[] waypoints; :89 Create(Transform[] waypoints, bool makeChildren=false) tối thiểu 2 điểm
QL_Tools: file editor ở root bọc #if UNITY_EDITOR (FpsMonitorWindow.cs:1); SWS không có asmdef → cùng Assembly-CSharp
refs: -
files: Assets/QL_Tools/PathTemplates/PathTemplate.cs, PathTemplateLibrary.cs, PathTemplateShapes.cs (runtime-safe class ScriptableObject; phần editor bọc #if UNITY_EDITOR)
done_when: compile 0 error; Generate(Figure8H) + Resample(…,16,true) trả 16 điểm đối xứng qua tâm

## T2 — Panel thumbnail trong Inspector + apply + lưu template
owner: muse
depends: T1
context:
KHÔNG tạo [CustomEditor(typeof(PathManager))] mới (trùng PathEditor của SWS). Dùng [InitializeOnLoad] + Editor.finishedDefaultHeaderGUI: target có PathManager → foldout "Path Templates" (trạng thái lưu EditorPrefs).
Nội dung: tab theo category; lưới ô ~64px, thumbnail vẽ Handles.DrawAAPolyLine (điểm đầu chấm xanh, cuối chấm đỏ để thấy hướng); tên dưới ô.
Tuỳ chọn: Width, Height (mặc định = defaultSize của template), Point count (mặc định = waypoints.Length), Mirror X, Reverse, "Giữ vị trí Waypoint 0".
Click ô: Undo.RecordObjects transforms + PathManager; thêm child thiếu (Undo.RegisterCreatedObjectUndo), xoá child thừa (Undo.DestroyObjectImmediate); localPosition = point*size; đổi tên "Waypoint i"; pathManager.Create(); SetDirty; prefab instance → PrefabUtility.RecordPrefabInstancePropertyModifications.
Nút "Lưu path này thành template": nhập tên + category → chuẩn hoá waypoint local XY → tạo PathTemplate asset trong Assets/QL_Tools/PathTemplates/Library/ và add vào Library.
verified:
Editor/PathEditor.cs:15 [CustomEditor(typeof(PathManager))] đã chiếm inspector; :189 gọi (target as PathManager).Create()
PathManager.cs:73 Create(Transform parent=null) lấy child làm waypoint
QL_Tools/QLToolsWindow.cs:6 menu "Tools/QL Tools" đang bị comment toàn bộ — không gắn vào đó
refs: -
files: Assets/QL_Tools/PathTemplates/Editor/PathTemplateInspector.cs
done_when: chọn Path_WB5_P2_1 → thấy panel; click Figure8H → gizmo thành số 8, Path Length đổi; Ctrl+Z về 5 waypoint cũ; lưu template mới → xuất hiện ô mới; console 0 error

## T3 — Seed thư viện từ path có sẵn
owner: claude
depends: T2
context:
Tạo PathTemplateLibrary asset; thêm template procedural (T1) + chụp các path đặc trưng đang dùng nhiều (category "Từ game"), dùng chức năng lưu ở T2 hoặc Editor script tương đương. Không sửa prefab gốc.
verified:
Ứng viên (đã kiểm tồn tại):
Project/GamePlay/Prefabs/Paths/8L2R.prefab, 8R2L.prefab (số 8 trái→phải / phải→trái)
Project/GamePlay/Prefabs/Paths/sinpathmid.prefab (sine dọc 10 lần đổi hướng)
Project/GamePlay/Prefabs/Paths/rainbow0.4.prefab (U-turn ngang)
Project/GamePlay/Prefabs/Paths/TopR-curveback-out201.prefab (arc vào-quay ra)
Project/GamePlay/Prefabs/Paths/ZL2Rp.prefab (zigzag trái→phải)
Project/GamePlay/Prefabs/Paths/S12circleL.prefab (vào rồi lượn tròn)
Project/GamePlay/Prefabs/Paths/Path_EM2_1.prefab (zigzag dọc)
Project/WorldBoss/Use/Enemies/Prefabs/Paths/Path_WB3_P1_2.prefab (vòng kín boss)
refs: -
files: Assets/QL_Tools/PathTemplates/Library/PathTemplateLibrary.asset + các PathTemplate .asset
done_when: library có ≥12 procedural + ≥9 "Từ game"; thumbnail khớp hình gizmo của prefab gốc

## T4 — Kiểm thử trên path WB5
owner: claude
depends: T3
context:
Path mẫu Path_WB5_P2_1 (pos 0,11,0; 5 waypoint). Làm trên bản duplicate, không lưu đè.
verified:
splineMove.cs:25 public PathManager pathContainer; :256 GetPathPoints(localType != none)
EnemyWB3_P1.cs:9 Init(..., PathManager _newPath = null, ...)
refs: -
files: (không sửa code)
done_when: apply mọi template OK + Undo OK; Play mode enemy bay đúng số 8; screenshot Inspector + Scene

## Câu hỏi mở
- (không)
