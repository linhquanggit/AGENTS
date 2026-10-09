# WB4 P3 — G8-Dir: đạn xuyên player + đường lửa xoay theo hướng đạn, dài kín màn
goal: BulletWB4_P3_G2 trúng player vẫn gây damage nhưng KHÔNG bị huỷ (bay tiếp tới mép màn). 2 đường lửa Gun_Wave_R / Gun_Wave_R-1 xoay theo hướng đạn, độ dài cố định đủ kín màn từ đầu súng — không tính độ dài lúc runtime.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (§9 WB4 P3 Súng 8, §15.6) + plan wb4-p3-g8-hold-laser (đã done) + dò trực tiếp prefab/script

## T1 — Script đạn xuyên cho enemy: BulletPenetrationE
owner: muse
depends: -
context:
`BulletPenetrationE : Bullet` — override OnTriggerEnter2D giống Bullet nhưng KHÔNG gọi DestroyDefault: check request → OnTakeDamage → SpawnHitSound → ExplosionVFX(collision.ClosestPoint(transform.position)) (giữ hiệu ứng trúng). Đạn tiếp tục bay, tự huỷ ở mép màn nhờ isDestroyOutCamera (Bullet.OnUpdateDestroy) hoặc timeToLive.
Damage 1 lần mỗi lần chạm (OnTriggerEnter2D chỉ gọi khi mới vào) — không thêm damage theo thời gian.
Không dùng lại BulletPenetration có sẵn: nó kế thừa BulletFighter (đạn phía player, implement ObjectClear).
verified:
Bullet.cs:266-282 OnTriggerEnter2D: request.CheckIsTrue → OnTakeDamage → SpawnHitSound → ExplosionVFX(ClosestPoint) → DestroyDefault()
Bullet.cs:225-240 OnUpdateDestroy: ra ngoài Bouns → DestroyDefault
BulletPenetration.cs (Bullets/Scripts/): `class BulletPenetration : BulletFighter`, override OnTriggerEnter2D bỏ DestroyDefault — mẫu để copy; dùng ở Bullet_Fighter5_PassiveSkill2, Bullet_Wingman7 (đạn player)
BulletFighter.cs:3 `class BulletFighter : Bullet, ObjectClear`
refs: claude/worldboss-boss-library.md §15.6
files: Assets/Project/Bullets/Scripts/BulletPenetrationE.cs (mới)
done_when: compile 0 error; giữ CRLF như Bullet.cs.

## T2 — Đổi script đạn BulletWB4_P3_G2
owner: claude
depends: T1
context:
Trên root prefab BulletWB4_P3_G2: swap component Bullet → BulletPenetrationE giữ toàn bộ field (EditorJsonUtility.ToJson/FromJsonOverwrite, §15.6). Kiểm lại tham chiếu tới prefab (DirectionShotE.bulletPrefab của G8-Dir trỏ fileID component Bullet 2417109459301976594) — sau swap fileID component đổi → gán lại bulletPrefab = component mới trên EnemyWB4_P3_A.
verified:
Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB4_P3_G2.prefab: script Bullet (guid 9a42da3357c0bae458d938e2ab2c37cc), isDestroyOutCamera 1, offsetBouns (0.1,0.1), timeToLive 20, prefabHitVFX có (guid 3b4beba3…)
Prefab đạn chỉ được tham chiếu trong EnemyWB4_P3_A.prefab:15212 (grep toàn Assets, ngoài FR2_Cache)
refs: claude/worldboss-boss-library.md §15.6
files: BulletWB4_P3_G2.prefab, EnemyWB4_P3_A.prefab
done_when: đạn dùng BulletPenetrationE; G8-Dir DirectionShotE.bulletPrefab không None; Console 0 error.

## T3 — Đường lửa xoay theo đầu súng, dài kín màn, khoá hướng lúc bắn
owner: claude
depends: -
context:
Nguyên nhân lửa luôn thẳng: RotationAimFighter (plan trước) nằm trên GO `DirectionShotE`; `FireBreathShotE` là anh em trong G8-Dir nên không xoay.
1) Reparent GO `FireBreathShotE` làm con của GO `DirectionShotE` (worldPositionStays = true).
2) DirectionShotE.ueOnShot thêm persistent call → RotationAimFighter (fid 2734432001113378891).Pause() → hướng lửa = hướng đạn, không xoay tiếp khi lửa phun. Chu kỳ sau ueBeforeShot → Play() đã có.
3) Độ dài cố định, chỉnh 1 lần trong prefab: độ dài chỉ đổi được bằng ShapeModule.radius (Linh đã thử; shape scale/position không ăn). Shape Cone (type 4): đường lửa đối xứng quanh tâm transform, dài 2·radius·k (k = localScale của chính PS ≈1.7386; scalingMode Local). Đặt cho CẢ 2 PS:
   - radius sao cho 2·radius·k ≥ đường chéo màn chơi (lấy từ GamePlayController CameraBouns: 2·|CameraBouns| — đo trong Play) + dư ~10%.
   - PIVOT của FX đường lửa nằm ở GIỮA đoạn (không phải ở đầu) → radius giãn đều 2 phía quanh tâm. Phải dời vị trí để MÉP TRÊN của đường lửa bắt đầu đúng độ cao Y của súng:
     · Làm trong hệ local của GO `DirectionShotE` (lúc góc xoay = 0, lửa chĩa thẳng xuống): mép trên y = y của FireDirection (đầu súng); tâm PS y = y_đầu_súng − halfLen_local, với halfLen_local = radius·k / scale của các cha giữa DirectionShotE và PS (FireBreathShotE sau reparent có scale ≠ 1 do worldPositionStays — tính bằng InverseTransformPoint/lossyScale cho chắc).
     · x của tâm = x của FireDirection (trục lửa trùng trục đạn; hiện lệch ~0.35 → căn lại).
     · Cách đo chắc nhất: Play 1 lần với góc 0, GetParticles của từng PS → lấy y lớn nhất (mép trên) so với FireDirection.position.y, rồi bù hiệu số vào localPosition. Làm cho cả Gun_Wave_R và Gun_Wave_R-1.
     Vì cả cụm là con của DirectionShotE nên khi xoay, mép trên vẫn dính ở đầu súng theo mọi hướng.
   Như vậy bắn hướng nào lửa cũng chạm mép màn; phần thừa ngoài màn không ảnh hưởng.
KHÔNG gọi WorldBossWeapon.LoadAssets / FireBreathShotE.LoadAssets; fireBreaths[] (fid 3808507789215222983, 3808507787821101421) còn nguyên. 1 lần execute_code, in lại cây + radius/position, save prefab.
verified:
Prefab: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P3_A.prefab
Cây: BODY-ROTATE/G8-Dir/{DirectionShotE[FireDirection], FireBreathShotE[Gun_Wave_R, Gun_Wave_R-1], Animation, Hold_Energy}
DirectionShotE localPos (0,−1.21) scale 1.7386, RotationAimFighter fid 2734432001113378891 (speed 20, offset 90), G8-Dir.rotate = nó, shotSteps[0].ueBeforeShot → Rotation2D.Play
FireBreathShotE Transform fid 3808507787976421924 localPos (−0.37,−2.96); component fid 3808507787976421925: fireBreaths 2 phần tử, duration 3, useTracking 0
Gun_Wave_R (copy trong boss, Transform fid 7969218487892922387, PS fid 7969218487892922384): localPos (0.02,−8.76), euler (−180,90,−90), scale (1.7386,1.7386,−1.7386), scalingMode 1 (Local), shape type 4, radius 4
Gun_Wave_R-1: nested prefab instance fid 3808507787821101417 (Assets/Effect_1945/Prefab/Effect_WorldBoss4/Gun_Wave_R.prefab), localPos (0.02,−8.79), override ShapeModule.radius.value tại EnemyWB4_P3_A.prefab:72073
FireBreath.cs:34 owner = transform.parent (vẫn là FireBreathShotE); BaseShotE.cs:161-165 OnBeingShot → ueOnShot, trước SpawnBullet (DirectionShotE.cs:43-53)
refs: claude/worldboss-boss-library.md §15.6
files: EnemyWB4_P3_A.prefab
done_when: cây mới DirectionShotE/{FireDirection, FireBreathShotE/{Gun_Wave_R, Gun_Wave_R-1}}; ueOnShot → Pause; ở góc 0 mép trên đường lửa cùng Y với đầu súng (sai số ≤ 0.1), đầu dưới vượt mép màn; Console 0 error.

## T4 — Test trong Play
owner: claude
depends: T2, T3
context:
GamePlayFake như plan trước (thêm pool tạm nếu thiếu MyPoolManager; không lưu scene). Probe: hướng đạn lúc SHOT, góc + 2 đầu đoạn hạt lửa (GetParticles chiếu lên hướng đạn → min/max), HP player, trạng thái đạn sau khi chạm.
Case A — player đứng yên (0,−3): đạn trúng → player mất máu 1 lần, đạn KHÔNG mất, bay tiếp tới mép màn rồi về pool; có hit VFX.
Case B — bắn chéo (player (3,−3)) và gần ngang: lửa trùng hướng đạn (< 2°), mép trên/đầu gần lửa dính ở đầu súng (≤ 0.1), đầu xa vượt mép màn.
Case C — player đứng trong đường lửa: FireBreath vẫn gây damage như cũ.
≥ 2 chu kỳ; Console 0 error.
verified:
Plan wb4-p3-g8-hold-laser T2: aimErr 0°, GamePlayFake thiếu MyPoolManager.
refs: claude/worldboss-boss-library.md §15.6
files: - (chỉ test)
done_when: 3 case khớp; báo Linh.

## Câu hỏi mở
- Player trúng đạn sẽ chịu damage đạn + damage lửa (lửa phủ cả đường bay). Có giữ cả 2 hay giảm damage đạn?
- Hạt con của Gun_Wave_R (Explosive/Smoke/Spark/Screen_Fx) nằm ở tâm PS — sau khi dời tâm chúng ra giữa đoạn dài; có cần giữ ở đầu súng không?
