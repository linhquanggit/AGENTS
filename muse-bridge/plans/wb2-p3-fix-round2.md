# WB2 P3 — sửa sau test lần 1 (MS1 bay lùi, Súng 5, nghỉ S6→S7, tốc độ, Súng 7 drone mới)
goal: Sửa 5 điểm Linh phản hồi sau khi test plan wb2-p3-ms1-ms2-apply. Mọi khoảng cách/thời gian mới phải là [SerializeField] để Linh tự chỉnh trên Inspector.
project: ~/wkspaces/WB
source: Project 1941 — brief ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (Moveset 1, Moveset 2 bước 9), chat Linh 2026-10-19, mô phỏng S7 mode A (/mnt/user-data/outputs/wb2_g7_drone_field.html — Linh chốt)

## T1 — MS1: bay lùi ra HẾT màn + tốc độ
owner: muse
depends: -
context:
- Lỗi: `IE_ExitCorner` lùi tới `lastEnterOffscreen` = góc + dir·enterOffscreenDistance(3) → thân boss to hơn 3 nên còn hiện trên màn khi đã chuyển góc (sai visual).
- Sửa: tính điểm ngoài màn sao cho TOÀN BỘ bounds boss (Renderer/Collider2D bounds gộp, tính 1 lần lúc Init hoặc khi cần) nằm ngoài khung camera (GamePlayController.CameraBouns hoặc Camera.main ortho) + `offscreenMargin` (TEMP 0.5, Inspector). Dùng cho cả điểm bắt đầu bay vào (IE_EnterCorner) và điểm bay lùi tới (IE_ExitCorner). Chỉ khi đã ra hết mới respawn sang góc mới. Giữ field enterOffscreenDistance làm khoảng cộng thêm tối thiểu (hoặc bỏ, ghi lại).
- `enterTime` mặc định 0.6 → **0.3** (bay vào nhanh gấp đôi). exitTime giữ nhanh (tốc độ cũng nên giữ: quãng dài hơn → có thể chuyển exit sang theo tốc độ `exitSpeed` thay vì thời gian — chọn 1 cách, Inspector chỉnh được).
- Spline 1 nhanh gấp đôi: ×2 `speed` của mySplineMoves[0] trên EnemyWB2_P3_A (làm ở T5 trên scene; áp cho cả appear lẫn replay vì dùng chung).
verified:
EnemyWB2_P3.cs:338 IE_EnterCorner: lastEnterOffscreen = _target + _outDir * enterOffscreenDistance; :367 IE_ExitCorner lerp tới lastEnterOffscreen trong exitTime; :74 enterOffscreenDistance 3, :76 enterTime 0.6, :78 exitTime 0.35, :80 respawnDelay 0.15.
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error.

## T2 — Súng 5 (K1/K2): xoay nòng theo đạn + điểm bắn ở đầu nòng
owner: muse
depends: -
context:
- Linh chốt: bắn lần lượt trái→phải, **nòng G5 xoay về đúng hướng từng viên lúc bắn** (quét theo đạn); bắn xong xoay về hướng mũi. Đạn sinh ở **đầu nòng** (xoay cùng nòng).
- Cách làm gợi ý: script nhỏ mới (vd `GunSweepAimWB2` trong SpecialCase/ hoặc WorldBoss-2/) gắn trên G5: nhận góc của FireDirection sắp bắn (hook trước SpawnBullet của DirectionShotE — thêm UnityEvent/Action `onBeforeSpawn(FireDirection)` vào DirectionShotE là chấp nhận được nếu không đổi hành vi mặc định) → xoay transform nòng (visual Spine của G5 hoặc node pivot) tới góc đó (tức thì hoặc xoay nhanh `aimTurnSpeed`, Inspector). Muzzle: node `Muzzle` con của pivot, ở đầu nòng; đạn spawn tại Muzzle với hướng của FireDirection.
- KHÔNG đổi API cũ của DirectionShotE cho súng khác (chỉ thêm hook optional).
verified:
DirectionShotE.cs:39 `List<FireDirectionData> fireDirections` (lấy từ GetComponentsInChildren<FireDirection>), :44 `SpawnBullet(FireDirection fireDir)` (vị trí/hướng đạn lấy từ transform FireDirection), :31 bulletSpeed. FireDirectionData (GamePlay/Scripts/FireDirection.cs:31) có delay + FireDirection.
G5-K1 / G5-K2 (T4 plan trước): 2 WorldBossWeapon dưới G5, copy DirectionShotE, dùng chung SpineAnimFlow G5; K2 flowOpen rỗng, K1 openOnce=false.
files: DirectionShotE.cs (hook optional), script xoay nòng mới
done_when: compile 0 error; súng khác dùng DirectionShotE không đổi hành vi.

## T3 — Nghỉ giữa Súng 6 và Súng 7
owner: muse
depends: -
context: Thêm `delayS6ToS7` (TEMP 1s, Inspector) trong IE_MS2: sau puzzle PASS hoặc sau laser xong → G6 Stop → chờ delayS6ToS7 → S7.
files: EnemyWB2_P3.cs
done_when: compile 0 error.

## T4 — Súng 7: cặp drone quay lưng, laser từ mũi, quay quanh tâm vùng
owner: muse
depends: -
context:
SPEC (Linh chốt theo mô phỏng mode A):
- 8 cặp (16 drone). Mỗi cặp = 1 "thanh" nằm trên 1 đường xuyên tâm vùng (tâm = centerPoint hiện tại của RotatingLinkFieldWB2 = tâm màn runtime). 2 drone **quay lưng vào nhau**: drone trong mũi hướng vào tâm, drone ngoài mũi hướng ra mép màn.
- Mỗi drone bắn **laser từ mũi** dọc đường: tia trong chạy tới tâm vùng (dừng cách tâm `innerLaserStop`), tia ngoài chạy ra hết màn. Thân 2 drone + khoảng hở là chỗ không có laser.
- **`pairGap`** (khoảng hở giữa 2 drone, world) — Inspector, Linh chỉnh. Thêm cả `pairRadius` (vị trí tâm cặp trên đường) + biên trượt.
- Cả thanh (drone + anim + laser) **quay CW quanh tâm vùng** (`rotateSpeed`, giữ 30°/s) → laser quét; drone xoay theo hướng đường (anim drone vẫn chạy).
- Cặp **trượt ra/vào dọc đường** (R310) giữa `slideMin`/`slideMax` (Inspector), `slideSpeed`.
- **Súng trên lưng drone** (R311, Linh nhắc lại): mỗi drone có 1 súng/turret trên lưng **xoay độc lập với thân drone, luôn hướng về Fighter** (thân drone vẫn xoay theo đường/laser). Đạn sinh tại đầu nòng turret (`turretMuzzle`), bay thẳng tới vị trí Fighter lúc bắn. Drone bất tử.
- **Nhịp bắn súng lưng (Linh chốt, thay cho "CD 2s"):** mỗi súng lưng bắn **1s/lần, mỗi lần 1 viên** → cấu hình bằng BaseShotE chuẩn trên turret: `useSelfCD = true`, `cooldown = 1`, `waves = 1`. Ưu tiên tái dùng: turret = node có `RotationAimFighter` (xoay theo Fighter) + `DirectionShotE` với 1 `FireDirection` đặt ở `Muzzle`, `linkedRotation` = RotationAimFighter đó → chỉnh được hoàn toàn trên Inspector, không hard-code trong RotatingLinkFieldWB2. RotatingLinkFieldWB2 chỉ Init/Attack/StopAttack các shot này khi dựng/dỡ trường (bỏ DroneShoot + droneShootCooldown tự viết, giữ field cũ không dùng nếu cần serialize). "Lần lượt": lệch `delayStart` mỗi drone = index × (cooldown / số drone) để không bắn cùng lúc (có bool bật/tắt).
- Turret: nếu prefab drone (WB2_LinkDrone) chưa có node turret/muzzle thì tạo node con `Turret` (+ `Muzzle`) trong code hoặc T5 dựng trên prefab; visual tạm nếu art chưa có (ghi lại cho artist).
- Laser gây damage qua `linkRequest`/TakeDamageRequest như hiện tại (mất máu, không ONE_SHOT) — giữ cơ chế damage cũ của link.
- 8s → OnFieldComplete (giữ).
- Laser: tái dùng `LinkLaser_CL` (2 đầu = mũi drone ↔ điểm cuối) hoặc laser 1 đầu có sẵn — chọn cái bám được 2 điểm động; tia ngoài kéo tới điểm ngoài màn (bán kính ≥ đường chéo màn).
verified:
RotationAimFighter: Assets/Project/Enemies/Scripts/BaseEnemy/EnemyRotation/RotationAimFighter.cs; DirectionShotE.cs:10 `linkedRotation` (RotationAimFighter); BaseShotE.cs:37 useSelfCD, :38 cooldown, :40 delayStart, :41 waves.
RotatingLinkFieldWB2.cs:150 shootT random, :195 DroneShoot(_pos …) từ tâm drone, :200 DroneShoot.
RotatingLinkFieldWB2.cs (SpecialCase): spokeCount 8, dronesPerSpoke 2, fieldRadius 12, rotateSpeed 30, fieldDuration 8, slideSpeed 1.2, innerR0/innerR1/outerR0/outerR1 (khe hở giữa innerR1–outerR0), dronePrefab (WB2_LinkDrone), linkPrefab LinkLaser_CL (WB2_LinkLaser), linkRequest, bulletPrefab, bulletSpeed 8, droneShootCooldown 2; event OnFieldComplete; SetCenterPoint(Transform).
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/RotatingLinkFieldWB2.cs (viết lại bên trong, giữ tên/GUID/event/SetCenterPoint)
done_when: compile 0 error.

## T5 — Chỉnh scene/prefab instance + test Play
owner: claude
depends: T1, T2, T3, T4
context:
- Spline 1: speed ×2 trên splineMove thứ nhất của EnemyWB2_P3_A.
- G5-K1/K2: góc giữa các FireDirection ×2 so với hiện tại (quạt giãn gấp đôi, đối xứng quanh hướng mũi); bulletSpeed ×2; đặt FireDirection/Muzzle ở đầu nòng; gán `flowAttack` = anim bắn của G5 cho K1 và K2 (hiện anim bắn KHÔNG chạy) và kiểm anim chạy thật.
- Gắn script xoay nòng (T2) lên G5, gán pivot + Muzzle.
- S7: kiểm drone/laser visual, chỉnh pairGap mặc định cho thấy rõ khe.
- LUẬT: không apply prefab EnemyWB2_P3_A, không save MakeAll khi Linh chưa đồng ý.
done_when: Play WB-Shooter, Console 0 error, quan sát: (1) bay lùi ra khuất hẳn rồi mới hiện ở góc mới; bay vào 0.3s; spline 1 nhanh ×2 (2) G5 nòng xoay theo từng viên, đạn ra ở đầu nòng, quạt giãn ×2, đạn nhanh ×2, anim bắn chạy (3) có nghỉ ~1s giữa G6 và G7 (4) S7: 8 thanh drone quay lưng, laser từ mũi vào tâm + ra mép, quay CW, trượt, khe chỉnh được bằng pairGap; turret trên lưng mỗi drone luôn hướng Fighter, mỗi turret bắn 1 viên/1s (useSelfCD 1, waves 1), lệch nhịp giữa các drone, đạn ra từ nòng turret. Screenshot từng mục.

## T6 — Báo cáo + doc
owner: claude
depends: T5
files: brief WB2_P3.md (MS1 + MS2 bước 9 + số liệu mới), library mục 7/17
done_when: Linh đọc báo cáo.

## Câu hỏi mở
- (Mặc định đã áp, Linh "ok chốt") S7 laser = mất máu theo linkRequest cũ; drone có turret trên lưng aim Fighter, mỗi turret 1 viên/1s (useSelfCD=true, cooldown 1, waves 1), lệch nhịp; có tia trong vào tâm; có trượt. G5 xoay theo từng viên.
