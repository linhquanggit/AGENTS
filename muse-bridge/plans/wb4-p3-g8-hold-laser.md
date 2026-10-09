# WB4 P3 — G8-Dir: đạn laser tụ 2s ở đầu súng rồi bắn nhanh về mục tiêu
goal: Lượt 1 của G8-Dir: Hold_Energy tụ 2s ở đầu súng rồi bắn BulletWB4_P3_G2 nhanh về phía player. Chỉ chỉnh data trong prefab, không viết script mới.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (§9 WB4 P3 Súng 8, §15.6, §17) + dò trực tiếp prefab/script

## T1 — Cấu hình G8-Dir: tụ 2s + aim player + tăng tốc đạn
owner: claude
depends: -
context:
Dùng cơ chế có sẵn của WorldBossWeapon SEQUENCE, không cần code:
1) Tụ 2s: shotSteps[0].fxBefore = Hold_Energy, timeFxBefore = 2 (IE_Sequence: bật fxBefore → chờ timeFxBefore → tắt → mới Attack). Hold_Energy đặt m_IsActive 0 (Init đã gọi ActiveStepFX(false), để inactive cho chắc).
2) Hướng về player: thêm RotationAimFighter lên GO `DirectionShotE` (cha của FireDirection), speed cao (vd 20) để bám kịp; shotSteps[0].ueBeforeShot thêm persistent call → RotationAimFighter.Play() (WorldBossWeapon chỉ tự gọi rotate.Play() khi attackType = ROTATE, còn Stop() có gọi rotate.Pause() nên phải Play lại mỗi chu kỳ). Gán WorldBossWeapon.rotate = component này để Stop() pause được. Chỉnh offsetAngle sao cho FireDirection.up (đang xoay local 180°) chỉ vào player — kiểm bằng gizmo/Play.
3) Bắn nhanh: DirectionShotE.bulletSpeed 5 → 20.
KHÔNG gọi WorldBossWeapon.LoadAssets (reset shotSteps/delay). Làm trong 1 lần execute_code, in lại cấu trúc, save prefab.
verified:
Prefab: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P3_A.prefab
Path: EnemyWB4_P3_A/BODY-ROTATE/G8-Dir/{DirectionShotE, FireBreathShotE, Animation, Hold_Energy}
G8-Dir WorldBossWeapon fid 3808507788390356540: attackType 4 (SEQUENCE), rotate None, shotSteps[0]=DirectionShotE (fid 3808507787804866394) fxBefore None, timeFxBefore 0, delayAfterShot 0.2; [1]=FireBreathShotE; isLoopSequence 1, sequenceCoolDown 1
WorldBossWeapon.cs:170-176 IE_Sequence: ueBeforeShot → fxBefore.SetActive(true) → WaitForSeconds(timeFxBefore) → SetActive(false) → shot.Attack()
WorldBossWeapon.cs:146-147 rotate.Play() chỉ ở ATTACK_ON_ROTATE; :245 Stop() gọi rotate?.Pause()
DirectionShotE: bulletSpeed 5, 1 FireDirection fid 3808507788494362706 localRot z=180°; GO DirectionShotE localPos (0,-1.21), rot 0
FireDirection.cs:7-10 Position/Direction(=transform.up)/Rotation đọc live → bullet spawn theo hướng hiện tại, rotation đạn = FireDirection.Rotation nên đạn tự đúng hướng
Hold_Energy GO fid 6726585987539252717 localPos (0.01,-1.32) (cùng đầu súng), PS playOnAwake 1, lengthInSec 5
RotationAimFighter.cs: transform.AutoRotationTarget(fighter.transform, speed, offsetAngle), fighter = GamePlayController.Instance.FighterCurrent; Rotation2D.Start() SetInitAngle đặt eulerZ = initAngle (để 0)
refs: claude/worldboss-boss-library.md §15.6
files: EnemyWB4_P3_A.prefab
done_when: Inspector đúng các giá trị trên; Console 0 error.

## T2 — Test trong Play
owner: claude
depends: T1
context:
Theo §15.6: vào Play → đọc Console ngay; thả EnemyWB4_P3_A vào scene test (chưa có node P3), weapon.Init(dmg, cd) + weapon.Play() qua execute_code. Probe log mỗi 0.25s: Hold_Energy.activeSelf, góc FireDirection.up vs hướng tới player, vị trí đạn BulletWB4_P3_G2 trong Pool.
Kỳ vọng: t0 Hold_Energy bật 2s → t0+2 tắt, đạn bay hướng player (lệch < 5°), speed ≈ 20 → +0.2s FireBreathShotE như cũ. ≥2 chu kỳ, đổi vị trí player giữa 2 lần.
verified:
Node test chỉ có WB4_P1, WB4_P2 ([GD-Khoa]/TestEnemyNode/WB/)
refs: claude/worldboss-boss-library.md §15.6
files: - (chỉ test)
done_when: log khớp kỳ vọng 2 chu kỳ; Console 0 error; báo Linh.

## Câu hỏi mở
- "Bắn nhanh": tạm bulletSpeed 20 (cũ 5).
- Trong 2s tụ chỉ thấy FX Hold_Energy, viên đạn chỉ xuất hiện lúc bắn. Nếu muốn thấy chính viên đạn đứng ở đầu súng 2s thì mới cần code (shot giữ đạn).
- RotationAimFighter xoay cả FireBreathShotE? Không — chỉ GO DirectionShotE; FireBreath giữ hướng cũ.
- Lượt 1 dài thêm 2s → cột lửa muộn hơn 2s; có giảm sequenceCoolDown không?
