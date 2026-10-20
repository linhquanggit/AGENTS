# WB2 P3 — sửa sau test lần 3 (K1/K2 bắn xong mới move, lùi ra hết màn, anim bắn G5 mỗi viên, khôi phục FX puzzle G6)
goal: Sửa 3 lỗi Linh báo sau plan wb2-p3-fix-round3. Không tự đoán: log `[WB2_P3]` từng bước để chứng minh đã đúng.
project: ~/wkspaces/WB
source: Project 1941 — brief ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (Moveset 1, Moveset 2, S6 puzzle v2, Round 3), chat Linh 2026-10-20

## T1 — MS1: K1/K2 bắn HẾT mới đi; lùi ra HẾT màn mới teleport
owner: muse
depends: -
context:
- Linh: (a) **K1 (và K2) chưa bắn xong đã move** — đúng: bắn xong viên CUỐI → delay → mới move. K1: bắn xong → `postK1Delay` (TEMP 1s, Inspector) → vào góc đầu. K2: bắn xong → `postK2Delay` 1s → lùi ra. (b) **Chưa lùi ra hết màn đã teleport** — đúng: toàn bộ thân boss ra ngoài khung camera rồi mới đặt sang điểm ngoài màn của góc mới.
- Round 3 chờ `OnCycleDone` nhưng tín hiệu này bắn ra TRƯỚC khi viên cuối ra khỏi nòng (K1: 8 viên có delay theo từng FireDirection 0.12s; K2: 6 viên × 2 waves). Phải chờ viên cuối thật: đếm số viên spawn qua `DirectionShotE.onBeforeSpawn` (hoặc OnEndShot/ueEndShot của BaseShotE sau waitDelayEnd) = số FireDirection × waves rồi mới coi là xong. Log: "[WB2_P3] K1 spawned n/N, t=…".
- Lùi ra: kiểm `GetOffscreenPoint` bằng bounds THẬT lúc chạy (Renderer.bounds gộp của MeshRenderer Spine thân + súng, tính lại mỗi lần, không cache lúc Init khi Spine chưa render) so với rect camera; sau khi tới điểm lùi, ASSERT bounds không giao rect camera (log "[WB2_P3] exit bounds … cam …"); nếu còn giao thì lùi tiếp tới khi hết rồi mới teleport. Kiểm GamePlayController.CameraBouns trong MakeAll có phải stub sai không (round 2 có "stub bouns theo camera") — ưu tiên Camera.main ortho rect.
verified:
EnemyWB2_P3.cs: IE_PlayWeaponOnce(_weapon, _postDelay) chờ OnCycleDone (timeout weaponCycleTimeout) → `StopAttackOnly()`; :165 GetOffscreenPoint(_corner, _outDir) dùng CameraBouns rồi Camera.main; AutoDestroy() đã override no-op (round 3).
DirectionShotE.cs:44 `public event Action<FireDirection> onBeforeSpawn`, :80 invoke trước mỗi viên.
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error; log Play cho thấy move chỉ bắt đầu sau viên cuối + delay, và exit bounds ngoài camera trước teleport, đủ 4 góc.

## T2 — Anim bắn G5 chạy 1 lần cho MỖI viên
owner: claude
depends: -
context:
- Linh: chỉnh **override duration (NewDuration) của anim bắn G5 trong SpineAnimFlow** để mỗi lần bắn 1 viên thì anim bắn chạy 1 lần (hiện bắn mấy viên mới chạy 1 lần anim).
- Làm: flow Attack của SpineAnimFlow G5 (anim `Attack_Gun5`): đặt `NewDuration` ≤ khoảng cách giữa 2 viên (K1 0.12s, K2 theo delay FireDirection / fireRate hiện tại — đọc thực tế). Nếu K1 và K2 khoảng cách khác nhau → 2 flow riêng (vd `Attack_K1`, `Attack_K2`) trỏ cùng anim, NewDuration khác nhau, gán flowAttack tương ứng cho G5-K1 / G5-K2.
- Trigger mỗi viên: `GunSweepAimWB2` (round 2) đã nghe `onBeforeSpawn` để xoay nòng → thêm phát flow Attack ở đó (hoặc dùng ueOnShot nếu đã bắn mỗi viên). Nếu cần sửa code thì nhờ muse, giữ nhỏ.
verified:
Spine/SpineTools/SpineAnimFlow.cs:15 `AnimAction.NewDuration` (>0 thì TimeScale = Duration/NewDuration, :181-183), :57 SetNewDuration(flow, track, duration), :100 PlayFlow(name, onCompleted).
G5-K1 / G5-K2: 2 WorldBossWeapon dưới G5 dùng chung SpineAnimFlow G5; round 2: "anim Attack_Gun5 chạy".
files: EnemyWB2_P3_A.prefab (SpineAnimFlow G5, flowAttack K1/K2), (nếu cần) GunSweepAimWB2.cs
done_when: Play: đếm số lần anim bắn = số viên (K1 8, K2 6×2).

## T3 — Khôi phục FX puzzle G6: tách Wind, gắn Boss2 vào holder trong G6 (không spawn Boss2 trực tiếp)
owner: any
depends: -
context:
- Dò: `Boss2.prefab` (Assets/Effect_1945/Prefab/effect_WorldBoss/) bị thay bản mới 2026-10-19 ~17:55 (VN) — thêm child `Wind`, MẤT `SectorMaskFxWB2` (GUID 7ed1733d0d2e94240802c4ed41bfe78d), mất Mask Interaction Visible Inside trên PSR Warning_G/R, mất borderGap 0.3. Dữ liệu G6 trên EnemyWB2_P3_A.prefab còn nguyên; MakeAll không override G6.
- **Linh chốt cách làm:**
  1. **Tách `Wind`** ra khỏi `Boss2.prefab` thành prefab riêng `Boss2_Wind.prefab` (cùng thư mục, giữ nguyên transform/PS của Wind), xoá child Wind khỏi Boss2.prefab. Không gắn Wind vào G6 (chưa có yêu cầu) — ghi lại trong báo cáo.
  2. **G6 không dùng Boss2 trực tiếp** (bỏ cơ chế `fxPrefab` spawn Boss2 lúc chạy). Trong `EnemyWB2_P3_A.prefab`, dưới G6 tạo **GameObject mới** `G6-PuzzleFx` (holder) và **kéo Boss2.prefab vào làm con** (nested prefab instance). `SectorMaskFxWB2` đặt trên **holder** (không đặt lên Boss2 gốc), tham chiếu Warning_G / Warning_R / Smoke / Rada của Boss2 instance con; borderGap 0.3; holder tắt sẵn.
  3. Code (muse): `PuzzleZoneWB2` thay `fxPrefab` bằng field `fxHolder` (SectorMaskFxWB2, Inspector). Lúc bắt đầu puzzle: bật holder, đặt **world** position = tâm G6, **world rotation identity, lossyScale 1** (bù scale/rotation của cha; nếu cần thì tạm unparent rồi trả về cha khi xong), SetArea/Build/…; xong puzzle → HideAll + tắt holder. `SectorMaskFxWB2` hiện tìm child theo tên trong chính nó (FindChild) → cho phép gán ref tay hoặc tìm sâu trong con (Boss2 instance). Giữ cache mask theo N.
  4. Sandbox `PuzzleSandboxWB2` (MakeAll/WB2-PuzzleSandbox) đang dùng instance Boss2 có component cũ → cập nhật theo cùng mô hình holder (để còn test lại), không save MakeAll.
- Boss2.prefab gốc từ nay KHÔNG gắn script/override nào của puzzle (artist cập nhật thoải mái).
verified:
Boss2.prefab hiện: 0 MonoBehaviour, children Warning_R(Wave,Flash), Smoke(27 trang, 10 den, point), Rada, Warning_G(Wave,Flash), Wind. mtime 2026-10-19 10:55 UTC.
PuzzleZoneWB2.cs (SpecialCase) có field fxPrefab (=Boss2 trên prefab boss), flashTime 0.35, flashGap 0.1, delayBeforeInput 0.5, touchTimePerStep 1, redWarnTime 0.5, safeRadius 0.
SectorMaskFxWB2.cs (WorldBoss-2/Puzzle/): Awake FindChild("Warning_G"/"Warning_R"/"Smoke"/"Rada") nếu ref null; set PSR maskInteraction VisibleInsideMask; SetArea(worldCenter,size,on); Build(n).
files: Assets/Effect_1945/Prefab/effect_WorldBoss/Boss2.prefab (bỏ Wind), Boss2_Wind.prefab (mới), EnemyWB2_P3_A.prefab (G6-PuzzleFx + Boss2 nested), PuzzleZoneWB2.cs, SectorMaskFxWB2.cs (nhỏ), MakeAll sandbox (chưa save)
done_when: compile 0 error; Play MS2: quạt xanh/đỏ + khói N đường đúng như sandbox, FX không xoay/lệch theo boss; Boss2.prefab không còn Wind và không có component puzzle.

## T4 — Test tổng + báo cáo
owner: claude
depends: T1, T2, T3
context: Play WB-Shooter P3 ≥ 2 lượt. Không save MakeAll khi Linh chưa đồng ý; prefab EnemyWB2_P3_A + variant mới được save (Linh đã cho phép save prefab ở round 2).
done_when: Console 0 error; K1/K2 bắn đủ rồi mới move (log); lùi ra khuất hẳn rồi mới teleport (log bounds); anim G5 mỗi viên; puzzle G6 có FX đầy đủ; cập nhật brief (mục Round 4) + library + learning "không gắn script lên prefab FX của artist; dùng holder + nested prefab".

## Câu hỏi mở
- Wind tách ra prefab riêng: chưa gắn vào đâu — Linh chỉ định chỗ dùng sau.
