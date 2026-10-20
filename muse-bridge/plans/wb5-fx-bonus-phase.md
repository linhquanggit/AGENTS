# WB5 P1/P2/P3 — Fx_bonus (điện + khói) bật sau anim chuyển phase
goal: Cả 3 boss EnemyWB5_P1_A, EnemyWB5_P2_A, EnemyWB5_P3_A: FX có sẵn (P1 không có Fx_bonus) `Fx_bonus` (điện) + `Fire_Engine_MB35` ×2 (khói); FX tắt lúc boss vào/chạy anim Appear (chuyển phase) và chỉ bật khi anim chạy xong (lúc boss sang PLAYING).
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md + dò trực tiếp ~/wkspaces/WB

## T1 — EnemyWorldBoss: list FX bật khi xong anim Appear
owner: muse
depends: -
context:
Luồng hiện tại: hết Path1 → `IE_ActionAppear` → `animationFlow.PlayFlow(flowAppear)` → chờ `delayActionAppear` → `ChangeToPlayingMode()` (= "chạy xong anim chuyển phase").
Sửa trong base `EnemyWorldBoss` (dùng chung mọi WB, nên null/empty-safe):
- Thêm `[SerializeField] protected List<GameObject> fxOnPlaying = new();` (rỗng = hành vi cũ).
- `Init(...)`: SetActive(false) mọi phần tử (boss từ pool/prefab phải tắt FX từ đầu).
- `ChangeToPlayingMode()`: SetActive(true) mọi phần tử (ParticleSystem con playOnAwake sẽ tự chạy; nếu có ParticleSystem thì gọi thêm `Play(true)` cho chắc).
- `OnDie()`: tắt lại (tuỳ chọn, nếu không có anim chết cần giữ FX — giữ nguyên nếu không chắc).
Không Odin, không đổi field có sẵn. Log (nếu cần) bằng DPDebug.
verified:
WorldBoss/Use/Scripts/Enemies/EnemyWorldBoss.cs:27 `flowAppear`, :28 `delayActionAppear`, :48 `public override void Init(Node _node = null, PathManager _newPath = null, List<EventNode> _lstEvents = null, float _speedMove = 0)`, :101 `OnDie()`, :145 `public override void ChangeToPlayingMode()`, :154 `OnEndPath1()` → :157 `StartCoroutine(nameof(IE_ActionAppear))`, :159-168 `IE_ActionAppear` (kết thúc bằng ChangeToPlayingMode, cả nhánh không có flow).
EnemyWB5_P1.cs / EnemyWB5_P3.cs: class rỗng `: EnemyWorldBoss`; EnemyWB5_P2.cs chỉ override `OnEndPath1` (log rồi gọi base) → sửa ở base là đủ cho cả 3.
refs: ~/AGENTS/AI/Unity/knowledge/INDEX.md (worldboss-*)
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/EnemyWorldBoss.cs
done_when: Unity compile 0 error; các WB khác (fxOnPlaying rỗng) không đổi hành vi.

## T2 — Để FX mặc định inactive (P1/P2/P3)
owner: claude
depends: -
context:
Theo Linh: `Fire_Engine_MB35` và `Fire_Engine_MB35 (1)` là lớp KHÓI; `Fx_bonus` là lớp ĐIỆN. Không tạo particle khói mới.
Làm qua Unity MCP (`PrefabUtility.LoadPrefabContents` → sửa → `SaveAsPrefabAsset` → `UnloadPrefabContents`).
- P1 không có `Fx_bonus` → KHÔNG thêm; prefab nào có object nào thì xử lý object đó.
- Đặt inactive những object có sẵn: P1 = 2 `Fire_Engine_MB35`; P2, P3 = `Fx_bonus` + 2 `Fire_Engine_MB35` (T1 bật khi xong anim chuyển phase).
verified:
Prefab: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P{1,2,3}_A.prefab.
`Fire_Engine_MB35` + `Fire_Engine_MB35 (1)`: nested prefab instance của Enemies/Addressable/Effect/Prefabs/MB35/VFX/Fire_Engine_MB35.prefab (guid e1f6b350fc8729943a0c78b1e4a64780), con trực tiếp của root, ĐÃ có ở cả 3 prefab (P1 dòng ~169833/169998, P2 ~23147/23312, P3 ~53588/53973), đang active.
`Fx_bonus`: P2 dòng 743, P3 dòng 19823 — con root, 1 ParticleSystem loop, active. P1 KHÔNG có.
refs: Project 1941 doc claude/worldboss-boss-library.md (WB5)
files: 3 prefab EnemyWB5_P{1,2,3}_A
done_when: các FX có sẵn đều inactive mặc định, lưu không lỗi.

## T3 — Gán fxOnPlaying + test 3 phase
owner: claude
depends: T1, T2
context:
Trên component EnemyWB5_P1/P2/P3 ở root mỗi prefab: `fxOnPlaying` = P1: [`Fire_Engine_MB35`, `Fire_Engine_MB35 (1)`]; P2, P3: [`Fx_bonus`, `Fire_Engine_MB35`, `Fire_Engine_MB35 (1)`]. Kiểm `flowAppear`/`delayActionAppear` đã có giá trị (ContextMenu `SyncDelayActionAppear` nếu = 0).
Test Play mode từng phase: lúc boss bay vào + anim Appear → không thấy FX; ngay khi xong anim (boss nhận damage/bắt đầu bắn) → điện + khói bật và loop.
verified:
`SyncDelayActionAppear` ContextMenu ở EnemyWorldBoss.cs:29-34; auto-wire flow "Appear"/"Action" ở :35-45.
refs: -
files: 3 prefab EnemyWB5_P{1,2,3}_A
done_when: Console 0 error; quan sát đúng thời điểm bật FX ở cả 3 phase.

## Câu hỏi mở
- Khi boss chết có tắt Fx_bonus không? Mặc định giữ nguyên.
