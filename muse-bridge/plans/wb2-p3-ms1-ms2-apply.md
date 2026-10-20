# WB2 P3 — MS1 4 góc + MS2 puzzle S6 v2 (apply vào boss)
goal: Sửa EnemyWB2_P3 theo spec Linh chốt 2026-10-19: MS1 (G5 có K1/K2, bỏ spline 2, đủ 4 góc bay vào/bay lùi ra, rồi bay lại spline 1) và MS2 (G5 thu → G6 mở, puzzle v2 đã test sandbox, vạch đỏ cảnh báo + laser kill chắc chắn, S7 8s, về MS1). Dùng lại code sandbox `SectorPuzzleWB2` + `SectorMaskFxWB2` (đã gắn trong Boss2.prefab).
project: ~/wkspaces/WB
source: Project 1941 — brief ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (mục Moveset 1, Moveset 2, S6 puzzle v2), library mục 7, design R283–R316 (sheet 2026-10-19), plan wb2-p3-g6-puzzle-sandbox (done)

## T1 — EnemyWB2_P3: Moveset 1 mới
owner: muse
depends: -
context:
SPEC MS1 (Linh chốt, brief WB2_P3 "Moveset 1"):
1. Vào phase: spline 1 (appear) như base → Action (appear flow) → PLAYING.
2. KHÔNG chạy spline 2 (loop) ở P3 → bắn **K1** (G5, 8 viên trái→phải, delay giữa viên trong FireDirection).
3. K1 xong → random index góc (4 góc; trong 1 lượt MS1 mỗi góc đúng 1 lần, thứ tự random).
4. Có index: đặt boss tức thì tại điểm NGOÀI màn phía góc đó (dọc hướng tâm→góc, cách mép `enterOffscreenDistance`) → bay vào ĐÚNG góc (+ `cornerOffset` Vector2 tuỳ chỉnh, Inspector) trong `enterTime` → mũi hướng thẳng tâm màn (FaceCenter, noseOffsetAngle 90 ⇒ rotZ 0 = mũi xuống) → bắn **K2** (G5, 6 viên, 2 lần, theo hướng mũi).
4b. Rời góc (kiểu B): bay LÙI (giữ hướng mũi, đi ngược hướng lúc vào) NHANH ra ngoài màn trong `exitTime` → chờ `respawnDelay` (nhỏ) → bước 4 cho góc tiếp theo.
5. Lặp tới đủ 4 góc (góc cuối cũng bay lùi ra).
6. Đủ 4 góc → MS2: đặt boss về điểm đầu spline 1, rotZ 0, chạy lại spline 1 như lần đầu. Khi spline 1 kết thúc lần này KHÔNG được chạy lại appear/ChangeToPlayingMode/OnAttack (cờ `isReplayPath1`) → chỉ báo cho state machine đi tiếp MS2.
- K1 chỉ bắn 1 lần ở đầu mỗi lượt MS1. Hết MS2 quay lại MS1: bắn K1 TẠI CHỖ đang đứng rồi vào 4 góc.
- Mọi thời gian/khoảng cách là [SerializeField] TEMP: enterOffscreenDistance 3, enterTime 0.6, exitTime 0.35, respawnDelay 0.15, cornerOffset (0,0), giữ cornerX 0.75 / cornerYTop 0.75 / cornerYBottom −0.2 / centerY 0.1.
- Bỏ teleportFx fade cũ (giữ field nếu cần tránh vỡ serialize, không dùng). Bỏ IE_MoveToCenter khỏi MS2 (thay bằng bước 6).
- K1/K2: tách thành 2 WorldBossWeapon `weaponK1`, `weaponK2` (thay `weaponMS1` + `weaponS5`; giữ tên field cũ bằng FormerlySerializedAs nếu đổi tên). Dựng node ở T4.
verified:
EnemyWorldBoss.cs:162-170 `public override void ChangeToPlayingMode()` → SetFxOnPlaying, colliders, PLAYING, PlayingWorldBoss(), OnAttack(), **StartPath(2)** ⇒ P3 override để bỏ StartPath(2).
EnemyWorldBoss.cs:171 `protected override void OnEndPath1()` → StopPath(1) + IE_ActionAppear → ChangeToPlayingMode ⇒ khi chạy lại spline 1 ở bước 6 phải chặn nhánh này.
EnemyBoss.cs:93 `protected void StartPath(int index)` (mySplineMoves[index-1].StartMove()), :99 StopPath, :125 `SetStartPosition()` đặt vị trí = mySplineMoves[0].pathContainer.position, :133 SetupSplineMoves gắn movementEndEvent += OnEndPath1.
EnemyWB2_P3.cs (WorldBoss/Use/Scripts/Enemies/WorldBoss-2/) hiện có: IE_Master MS1↔MS2, IE_MS1 (weaponMS1 → postVolleyDelay → IE_Teleport 1 góc → weaponS5), PickCorner (chỉ tránh góc trước), TryGetCorner từ CameraBouns, FaceCenter, IE_PlayWeaponOnce(weapon) chờ OnCycleDone/timeout, IE_MoveToCenter.
Prefab EnemyWB2_P3_A: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P3_A.prefab (guid 7c06b8bb3c2d2468c9d6b2197567fb92); giá trị: postVolleyDelay 1, teleportTime 1.2, noseOffsetAngle 90, cornerX 0.75, cornerYTop 0.75, cornerYBottom −0.2, centerY 0.1, teleportFx null, corners rỗng. MakeAll có instance dưới WB-Shooter (đang tắt).
refs: ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error; logic đúng 6 bước trên; không còn StartPath(2) ở P3.

## T2 — PuzzleZoneWB2 v2 + KillZoneLaserWB2 FireAtPlayer
owner: muse
depends: -
context:
PUZZLE (brief "Moveset 2" bước 3–8, logic đã test sandbox):
- Viết lại BÊN TRONG `PuzzleZoneWB2` (GIỮ tên class/file/GUID, G6 trên prefab đang tham chiếu). Dùng `SectorPuzzleWB2` (BuildSequence(n, n, rng), Begin, Feed, GetRedZones, GetSector) và `SectorMaskFxWB2` (Build, FlashBlue, SetBlueHeld, ShowRed, HideAll, ShowSmoke, SetArea). Chép vòng chạy từ `PuzzleSandboxWB2.Run` (nháy từng ô → delayBeforeInput → input mỗi frame → Correct giữ xanh DistinctHead / Completed / Wrong-hoặc-timeout).
- Tâm = vị trí G6 (transform PuzzleZoneWB2) CHỐT tại lúc bắt đầu puzzle. Player = GamePlayController.Instance.FighterCurrent.
- FX: field `fxPrefab` (Boss2.prefab, có SectorMaskFxWB2). Spawn/kích hoạt 1 instance KHÔNG làm con boss (parent null hoặc cùng root scene), position = tâm, rotation identity, scale 1; cache instance, dùng lại các lượt. Cache mask theo N (chỉ build lại khi N đổi — sửa SectorMaskFxWB2 nếu cần: Build(n) bỏ qua khi n == builtN và area không đổi). Xong puzzle: HideAll + ShowSmoke(false) + tắt instance.
- Box hiển thị FX: `areaOffset` (Vector2, so với tâm) + `areaSize` (Vector2) + `limitFxToArea` — Linh TỰ setting trên Inspector; mặc định lấy từ sandbox: size (4.01, 7.51), offset (0,0), limit true. KHÔNG tự lấy theo camera.
- Thông số [SerializeField] TEMP: flashTime 0.35, flashGap 0.1, delayBeforeInput 0.5, touchTimePerStep 1.0, redWarnTime 0.5, safeRadius 0.
- `ZoneCount` vẫn do EnemyWB2_P3 gán (6 lượt MS2 đầu, 8 từ lượt 2). Event `OnPuzzleComplete(List<int> reds)` giữ nguyên: rỗng = PASS. Khi fail: ShowRed(reds) NGAY rồi mới invoke (EnemyWB2_P3 gọi laser).
- Bỏ smokeLinePrefab / ueZoneBlue / ueZoneRed / blueTime / blueGap / touchTime / redTime cũ (FormerlySerializedAs nếu đổi nghĩa; hoặc xoá field — không có tham chiếu ngoài).
LASER (brief bước 7, Linh chốt): `KillZoneLaserWB2.FireAtPlayer(Action onComplete)`:
- Dùng `LaserShotE` con của G6 với useWarning = true, timeWarning = redWarnTime: trong lúc warning, mỗi frame xoay laser từ G6 hướng Fighter (vạch đỏ BÁM theo Fighter). Hết warning → laser bắn dọc hướng hiện tại trong laserDuration (0.6) → gây `DamageData(1, DAMAGE_TYPE.ONE_SHOT)` lên Fighter MỘT lần ngay lúc bắn (chắc chắn chết, không check va chạm). Nếu warning của LaserShotE không bám được → tự vẽ LineRenderer đỏ trong redWarnTime rồi Attack laser với useWarning false.
- Bỏ FireZones/CheckKill theo ô (hoặc để [Obsolete]).
verified:
LaserShotE.cs:10 `useWarning`, :11 `timeWarning`, :17 `WarningDuration`, :52-60 IE warning → ShowWarning/HideWarning (Laser_CL.SetShowWarning), :68 `duration < 0` = không tự tắt.
KillZoneLaserWB2.cs (SpecialCase) hiện: FireZones(List<int>, Action), StopFire, AimLaser(angleDeg) = rotation z = angle − 90 + aimOffsetAngle, CheckKill gọi ITakeDamage.TakeDamage(gameObject, new DamageData(1, DAMAGE_TYPE.ONE_SHOT)).
PuzzleZoneWB2.cs (SpecialCase, 164 dòng): IE_SpawnBullet → IE_RunPuzzle; ZoneCount property; event OnPuzzleComplete(List<int>).
Sandbox code: WorldBoss/Use/Scripts/Enemies/WorldBoss-2/Puzzle/{SectorPuzzleWB2,SectorMaskFxWB2,PuzzleSandboxWB2}.cs; SectorMaskFxWB2.SetArea(worldCenter, size, on) rebuild mask khi đổi.
Boss2.prefab (Assets/Effect_1945/Prefab/effect_WorldBoss/, guid 97e9b0e7599e70a408f5126790f420b2) đã có SectorMaskFxWB2, borderGap trên prefab 0.4 (Linh chỉnh 0.3 trên scene).
G6 hierarchy (EnemyWB2_P3_A/G6): WorldBossWeapon + BoneFollower; con Animation (Spine), PuzzleZoneWB2, KillZoneLaserWB2, LaserShotE, Laser2D_CL, Line_Laser, HitLaser_Red_Start/End.
refs: ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/PuzzleZoneWB2.cs, KillZoneLaserWB2.cs; (nhỏ) WorldBoss-2/Puzzle/SectorMaskFxWB2.cs (cache theo N)
done_when: compile 0 error; PuzzleSandboxWB2 vẫn chạy được (không phá API SectorPuzzleWB2/SectorMaskFxWB2).

## T3 — EnemyWB2_P3: Moveset 2 nối lại
owner: muse
depends: T1, T2
context:
MS2 (brief bước 1–10): sau bước 6 MS1 (spline 1 xong, rotZ 0) →
1. **G5 thu vào TRƯỚC** (weapon G5/K2 Stop với flowStop/Close, CHỜ anim xong) → rồi mới **G6 Play (Open)**. Tuần tự, không song song.
2. puzzleZone.ZoneCount = round>=2 ? 8 : 6 → chờ OnPuzzleComplete (timeout như cũ).
3. reds rỗng → bỏ qua laser. reds có → killZoneLaser.FireAtPlayer → chờ xong.
4. G6 Stop → S7 (weaponS7 + RotatingLinkFieldWB2, chờ OnFieldComplete 8s) → round++ → về MS1 (K1 tại chỗ).
- StopMaster/OnDie/OnStopAttack: dừng laser, puzzle, tắt FX instance.
verified:
EnemyWB2_P3.IE_MS2 hiện: IE_MoveToCenter → ZoneCount → OnPuzzleComplete → weaponS6.Play/Stop → killZoneLaser.FireZones → linkField.OnFieldComplete → weaponS7 → round++.
WorldBossWeapon.cs:39 flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop; :116 Play(); :253 Stop(); :71 event OnCycleDone; :72 OnOpenStart.
RotatingLinkFieldWB2.cs:57 event OnFieldComplete; :10 đã có khe hở giữa 2 drone mỗi đường (innerR1..outerR0).
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error.

## T4 — Dựng node + wiring trên EnemyWB2_P3_A (MakeAll)
owner: claude
depends: T1, T2, T3
context:
- Dưới G5 tạo 2 WorldBossWeapon `G5-K1` (DirectionShotE 8 dir quạt 30°, lấy từ `MS1-Opening`, delay 0.12/viên) và `G5-K2` (DirectionShotE 6 dir quạt 30°, waves 2, fireRate 0.5 — lấy từ G5 hiện tại), dùng chung SpineAnimFlow của G5. Tắt `MS1-Opening` (không xoá). Gán weaponK1/weaponK2.
- G6/PuzzleZoneWB2: gán fxPrefab = Boss2.prefab, areaSize (4.01, 7.51), areaOffset (0,0). Boss2.prefab: borderGap = 0.3.
- LaserShotE dưới G6: useWarning true, timeWarning theo redWarnTime.
- Kiểm 4× G2-(OnlyAnim) vẫn sync Open với G5 (syncOpenWith) sau khi tách K1/K2.
- LUẬT: sửa trên instance MakeAll; KHÔNG apply prefab EnemyWB2_P3_A / không save MakeAll khi Linh chưa đồng ý. Không sửa WorldBossSO/Addressables.
verified:
MakeAll: WB-Shooter/EnemyWB2_P3_A (prefab instance, đang tắt) có G2-(OnlyAnim)×4, G5 (BoneFollower+WorldBossWeapon, lpos (0,0.56)), G6 (lpos (0.01,−4.88)), G7, MS1-Opening (WorldBossWeapon, lpos 0), Fire_Engine. Root WB-Shooter + 8 root khác đang tắt bởi sandbox (PuzzleSandboxWB2.disabledRoots, context menu "Restore disabled roots"). MakeAll đã save 2026-10-19 07:13 có WB2-PuzzleSandbox.
files: Assets/Project/Scenes/MakeAll.unity (chưa save), Assets/Effect_1945/Prefab/effect_WorldBoss/Boss2.prefab
done_when: không Missing reference; Inspector đủ field mới.

## T5 — Test Play WB-Shooter
owner: claude
depends: T4
context:
Restore disabled roots (sandbox), tắt WB2-PuzzleSandbox, bật WB-Shooter + EnemyWB2_P3_A (tắt P1/P2). Fighter dummy làm mục tiêu; để test puzzle cho phép kéo Fighter (dùng PuzzleProxyPlayerWB2 tạm gắn lên Fighter hoặc execute_code đặt vị trí).
done_when: Console 0 error và quan sát đúng:
(1) spline 1 → appear → K1 → 4 góc khác nhau: vào từ ngoài màn, đứng đúng góc (+offset), mũi hướng tâm, K2 2 lần, bay lùi ra; không chạy spline 2.
(2) đủ 4 góc → bay lại spline 1 → rotZ 0 → G5 thu xong mới G6 mở.
(3) puzzle 6 ô, tâm = G6, FX trong box; PASS → không laser → S7 8s; FAIL → ô đỏ + vạch đỏ bám Fighter 0.5s → laser → ONE_SHOT.
(4) về MS1: K1 tại chỗ → 4 góc → MS2 lượt 2 = 8 ô.
(5) Warning_G/R phủ đủ box khi tâm ở mũi (ghi lại nếu thiếu). S7: drone mỗi đường có khe hở giữa (R309).
Screenshot các mốc.

## T6 — Báo cáo + knowledge
owner: claude
depends: T5
files: ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (trạng thái + cột Hiện trạng), library mục 7 + 17 (class mới SectorPuzzleWB2/SectorMaskFxWB2) + 15 (bài học), sync project doc claude/worldboss-boss-library.md
done_when: Linh đọc báo cáo, quyết save MakeAll / apply prefab EnemyWB2_P3_A.

## Câu hỏi mở
- Không còn câu chặn. Thông số TEMP (enter/exit time, offscreen distance, box FX) Linh tự chỉnh sau test.
