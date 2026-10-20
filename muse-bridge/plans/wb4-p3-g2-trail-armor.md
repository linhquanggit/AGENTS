# WB4 P3 — Đạn G2 để vệt + tụ Arrow_Line, súng aim player; giáp G7 đẩy/giữ/rung/kéo về
goal: EnemyWB4_P3_A: (1) BulletWB4_P3_G2 để lại vệt khi bay giống Arrow_Line của EnemyDrone_P3_WB4_A; lúc tụ hiện Arrow_Line ở đầu súng scale to dần rồi mới bắn; súng G8-Dir xoay theo player. (2) Giáp G7-RandomShot (G7-L/M/R): đẩy xuống nhanh → giữ 2s → rung nhẹ → kéo về; G7-M phải chạy anim như L/R.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md; plans cũ wb4-p3-g8-hold-laser, wb4-p3-g8-fire-rotate

## T1 — Vệt bay của BulletWB4_P3_G2 giống drone
owner: claude
depends: -
context:
Mẫu đúng: Arrow_Line trong EnemyDrone_P3_WB4_A (đang để lại vệt khi di chuyển). Copy cấu hình ParticleSystem/Renderer của Arrow_Line drone sang Arrow_Line của đạn (emission rateOverDistance, simulation space World, renderer/material, sorting), giữ nguyên tên GO.
Khi đạn lấy từ pool phải Clear()+Play() Arrow_Line để không kéo vệt từ vị trí cũ (nếu cần thêm script nhỏ OnEnable trên Arrow_Line — giao Muse ở T2).
verified:
Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB4_P3_G2.prefab: root BulletWB4_P3_G2 (BoxCollider2D? comp 58 + BulletPenetrationE guid 846bd212…), con Arrow_Line (line 4836) có ParticleSystem + ParticleSystemRenderer, active 1
Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyDrone_P3_WB4_A.prefab: Arrow_Line (line 19552) cũng PS + Renderer
Đã so: cả hai looping 1, prewarm 0, moveWithTransform 1, startLifetime 2, rateOverDistance 2 → khác biệt nằm ở module khác (renderer/material/shape/scale) — diff toàn bộ khi làm
BulletWB4_P3_G2 bắn từ EnemyWB4_P3_A/BODY-ROTATE/G8-Dir/DirectionShotE (DirectionShotE.cs:60 MyPoolManager.GetFromPool → pooled)
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb4-p3-g8-fire-rotate.md
files: BulletWB4_P3_G2.prefab
done_when: Play scene test WB4 P3: đạn G2 bay để lại vệt giống drone, không có vệt nối từ vị trí pool cũ; Console 0 error.

## T2 — Script reset vệt khi spawn từ pool (nếu T1 cần)
owner: muse
depends: T1
context:
Component nhỏ gắn lên Arrow_Line: OnEnable → ps.Clear(true); ps.Play(true). Không sửa Bullet/BulletPenetrationE.
verified:
BulletPenetrationE.cs (17 dòng) : Bullet, chỉ override OnTriggerEnter2D
refs: -
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-4/ResetTrailOnEnable.cs (mới)
done_when: compile 0 error; gắn lên Arrow_Line của đạn, bắn liên tiếp nhiều viên không có vệt lỗi.

## T3 — Tụ đạn: Arrow_Line ở đầu súng scale to dần rồi mới bắn
owner: claude
depends: T1
context:
Hiện tụ dùng shotSteps[0].fxBefore = Hold_Energy, timeFxBefore = 2 (WorldBossWeapon.IE_Sequence: bật fxBefore → chờ → tắt → Attack). Đạn chỉ spawn sau tụ nên lúc tụ không thấy đạn.
Làm: tạo GO "Charge_Arrow" dưới G8-Dir/DirectionShotE tại vị trí FireDirection, chứa bản sao visual Arrow_Line (đứng yên, không có collider), scale từ ~0 → bằng scale đạn trong 2s (DOTween DOScale khi OnEnable, hoặc Animation clip); gán làm fxBefore (giữ hoặc gộp Hold_Energy theo ý Linh). Khi fxBefore tắt thì đạn thật spawn đúng chỗ → liền mạch.
Nếu cần script scale-on-enable → Muse viết (nhỏ, OnEnable reset scale 0 rồi tween tới target trong duration).
verified:
EnemyWB4_P3_A.prefab line 16263-16264: shotSteps fxBefore {fileID: 6726585987539252717}(=Hold_Energy, line 18345), timeFxBefore 2
G8-Dir WorldBossWeapon attackType 4 (SEQUENCE); DirectionShotE.bulletScale field (DirectionShotE.cs:20) → scale đích lấy theo bulletScale nếu ≠0
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb4-p3-g8-hold-laser.md
files: EnemyWB4_P3_A.prefab (+ script scale nếu cần)
done_when: Play: trong 2s tụ thấy Arrow_Line lớn dần ở đầu súng, hết tụ đạn bay ra ngay tại đó; Console 0 error.

## T4 — G8-Dir aim vào player
owner: claude
depends: -
context:
Theo plan hold-laser: RotationAimFighter trên GO DirectionShotE (cha FireDirection), WorldBossWeapon.rotate = component đó, shotSteps[0].ueBeforeShot gọi RotationAimFighter.Play() (Stop() gọi rotate.Pause() nên mỗi chu kỳ phải Play lại). offsetAngle sao cho FireDirection.up (local z=180°) chỉ vào player. Hiện súng chưa focus → kiểm 3 điểm trên trong Inspector, sửa chỗ thiếu; xoay phải giữ trong suốt thời gian tụ (Play trước fxBefore).
verified:
RotationAimFighter guid c36601224b347b9419566360b2faa8c6 xuất hiện 3 lần trong EnemyWB4_P3_A.prefab (đã gắn ở đâu đó — xác nhận đúng GO)
WorldBossWeapon.cs:37 `rotate` (Rotation2D), :146-147 rotate.Play() chỉ ở ATTACK_ON_ROTATE; :245 Stop() → rotate?.Pause()
G8-Dir nằm dưới BODY-ROTATE → nếu BODY-ROTATE cũng xoay thì offset phải tính theo world
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb4-p3-g8-hold-laser.md
files: EnemyWB4_P3_A.prefab
done_when: Play: di chuyển player, đầu súng G8 bám theo trong lúc tụ, đạn bay trúng vị trí player lúc bắn.

## T5 — Giáp G7: đẩy nhanh xuống, giữ 2s, rung, kéo về
owner: claude
depends: -
context:
G7-RandomShot là WorldBossWeapon (attackType 1, flowIdle "Idle", flowAttack "Attack", anim = SpineAnimFlow). Chuyển động giáp nằm trong Spine animation "Attack" (hoặc flow của SpineAnimFlow). Sửa timing: đoạn đẩy xuống ngắn (~0.15–0.2s, ease out), giữ nguyên 2s, rung nhẹ (~0.2–0.3s, biên độ nhỏ), kéo về. Nếu anim Spine không sửa được trong Unity → chỉnh qua SpineAnimFlow (chuỗi anim + time scale) hoặc báo Linh cần sửa ở Spine.
verified:
EnemyWB4_P3_A.prefab: G7-RandomShot (line 15395) WorldBossWeapon guid 929b49aa…; con G7-L (16989), G7-M (17872), G7-R (14800), G7-Straight (15669)
G7-L/M/R đều có BoneFollower (Spine, guid a1fd8daa…) + Renderer; G7-L thêm LinkLaser_CL
WorldBossWeapon.cs:38-39 SpineAnimFlow anim, flow names; :73 anim = GetComponentInChildren<SpineAnimFlow>()
refs: claude/worldboss-boss-library.md
files: EnemyWB4_P3_A.prefab / SpineAnimFlow data của G7 (hoặc skeleton asset)
done_when: Play: mỗi lượt G7 giáp đẩy nhanh xuống, đứng yên 2s, rung nhẹ, rút về; không giật.

## T6 — G7-M chạy anim
owner: claude
depends: T5
context:
G7-L và G7-R có anim, G7-M thì không. So BoneFollower của G7-M với L/R (skeletonRenderer, boneName, followBoneRotation, followXY) và track/slot Spine tương ứng; sửa cho giống.
verified:
G7-M (line 17872) comps: Transform, Renderer(120), BoneFollower — cùng cấu trúc G7-R
refs: -
files: EnemyWB4_P3_A.prefab
done_when: Play: G7-M chuyển động cùng nhịp L/R.

## T7 — Test tổng P3
owner: claude
depends: T1, T2, T3, T4, T5, T6
context: Chạy scene test WB4 P3 (Assets/Project/[GD-Khoa]/TestEnemyNode/WB/WB4_P3.prefab), quan sát 2–3 chu kỳ G8 và G7.
verified: WB4_P3.prefab tồn tại
refs: -
files: -
done_when: Mọi done_when T1–T6 đạt; Console 0 error/0 exception.

## Câu hỏi mở
- Linh gọi "BulletWB4_P3_G2" nhưng đạn này do G8-Dir bắn (DirectionShotE); G2-Closed trong boss không có weapon. Xác nhận đúng là súng G8-Dir.
- Lúc tụ: thay hẳn Hold_Energy bằng Charge_Arrow hay hiện cả hai?
- G7 "đẩy" nằm trong Spine anim "Attack" hay tween riêng? Nếu phải sửa file Spine thì cần artist.
- G7-L có LinkLaser_CL nối giữa giáp — giữ 2s có cần laser bật suốt không?
