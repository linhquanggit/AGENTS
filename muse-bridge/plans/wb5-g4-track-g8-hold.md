# WB5 — G4 rocket xoay ngắm player, G8 portal mở 1 lần giữ nguyên
goal: P2_A: G4-Rocket-L/R xoay theo player (track) khi ngắm/bắn. P3_A: G8-Portal mở 1 lần rồi giữ trạng thái mở, không còn open→close→open mỗi lần bắn.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 P2 bảng G4, mục 18.5 P3 S8); plan trước wb5-p2p3-fix (T4 openOnce đã done)

## T1 — WorldBossWeapon: chạy rotate cho kiểu ATTACK_ON_OPEN
owner: muse
depends: -
context:
Hiện chỉ ATTACK_ON_ROTATE (case :158) và SEQUENCE (IE_Sequence :197) gọi rotate?.Play(); ATTACK_ON_OPEN không gọi → súng OPEN có Rotation2D cũng không xoay. Stop()/StopWeapons gọi rotate?.Pause() (:232/:278) — giữ nguyên.
Sửa: thêm [SerializeField] bool rotateOnOpenAttack = false; trong Attack() case ATTACK_ON_OPEN, nếu rotateOnOpenAttack thì rotate?.Play(). Mặc định false → không ảnh hưởng boss/súng khác.
verified:
WorldBossWeapon.cs (Assets/Project/WorldBoss/Use/Scripts/Enemies):37 [SerializeField] protected Rotation2D rotate; :75 LoadAssets rotate = GetComponentInChildren<Rotation2D>(); :124-131 case ATTACK_ON_OPEN SetShotAction + AutoAttack (không Play rotate); :158 ROTATE rotate?.Play(); :197 IE_Sequence rotate?.Play()
Rotation2D.cs (Assets/Project/Enemies/Scripts/BaseEnemy/EnemyRotation):7 isRotating, :8 speed 5, :11 initAngle, :17 Start → SetInitAngle, :19-23 Update chỉ Rotating() khi isRotating, :37 Play(), :43 Pause()
RotationAimFighter.cs: Rotating() → transform.AutoRotationTarget(fighter.transform, speed, offsetAngle); fighter = GamePlayController.Instance.FighterCurrent
files: WorldBossWeapon.cs
done_when: compile 0 error; weapon khác (rotateOnOpenAttack=false) hành vi không đổi

## T2 — Prefab P2_A: gắn RotationAimFighter cho G4-Rocket-L/R
owner: claude
depends: T1
context:
Mỗi G4 có: root (WorldBossWeapon + BoneFollower bám bone, followBoneRotation 0) → Animation (Spine Gun4) + RocketShot (LauncherShotE) → FireDirection (Direction = transform.up). Xoay cần ăn cả visual súng lẫn hướng bắn → thêm RotationAimFighter lên root G4-Rocket-L và G4-Rocket-R (BoneFollower không ghi rotation nên không đè). Đặt offsetAngle sao cho nòng (FireDirection.up) chỉ vào player — đo trong Editor (WB4 dùng 0 và 90 tuỳ hướng sprite); speed 5 (giống WB4). Gán WorldBossWeapon.rotate = component vừa thêm, rotateOnOpenAttack = 1. Giữ nguyên tên lửa (aimOnLaunch, maxTrackAngle 20°).
verified:
EnemyWB5_P2_A.prefab (Assets/Project/WorldBoss/Addressable/Enemies/Prefabs): G4-Rocket-L GO dòng ~22630, G4-Rocket-R ~20993; WorldBossWeapon G4-L fileID 8496502240492062633: rotate {fileID: 0}, attackType 1, flowOpen Open, flowAttack Attack, useSelfAnimAttack 1, useChain 0; BoneFollower 8496502240492062632 boneName Engine26, followBoneRotation 0
Ví dụ dùng: EnemyWB4_P2_A.prefab RotationAimFighter (speed 5, offsetAngle 0 / 90); EnemyWB4_P3_A G8-Dir dùng RotationAimFighter
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings (worldboss-*)
files: EnemyWB5_P2_A.prefab
done_when: Play P2_A: 2 súng G4 xoay theo player khi player di chuyển, nòng chỉ đúng player (log góc nòng vs góc tới player lệch < 5° sau khi ổn định), tên lửa bay ra đúng hướng nòng; Console 0 error

## T3 — Prefab P3_A: G8 giữ trạng thái mở
owner: claude
depends: -
context:
openOnce đã có và đang bật (G8 không Open lại) — nhưng vẫn thấy open→close→open vì: (1) flow "Open" = Open → Idle(loop), flow "Attack" = Attack → Idle(loop), mà Idle = tư thế đóng (CloseInit cũng là Idle); (2) useSelfAnimAttack 1 → mỗi lần shot bắt đầu (OnStartShot → actStartShot = _AnimShot) đều chạy flow Attack → về Idle = đóng. Skeleton Gun_8 chỉ có 3 anim: Open, Attack, Idle (không có anim "giữ mở").
Sửa (chỉ prefab, không code): trong SpineAnimFlow của G8-Portal/Animation: flow Open bỏ action Idle phía sau (chỉ Open, Loop 0 → Spine giữ frame cuối = mở). WorldBossWeapon G8: useSelfAnimAttack = 0 (không chạy Attack mỗi lần bắn). Không sửa flow Idle/CloseInit (dùng lúc Init).
verified:
EnemyWB5_P3_A.prefab: WorldBossWeapon G8-Portal (~dòng 18018) openOnce 1 (dòng 18048), useSelfAnimAttack 1, flowAttack Attack, useChain 0; SpineAnimFlow 6905402582196838494 (G8-Portal/Animation) flows: Idle[Idle L], CloseInit[Idle L], Open[Open, Idle L], Attack[Attack, Idle L]
WorldBossWeapon.cs:84 Init opened=false + :92 PlayFlow(flowCloseInit); :98-100 _AnimShot → PlayFlow(flowAttack); :112-126 Play() openOnce bỏ qua Open lần 2+
BaseShotE.cs:155-158 OnStartShot → actStartShot?.Invoke()
SpineAnimFlow.cs:125-170 ExecuteFlow: action Loop 0 chờ WaitForSpineAnimationComplete rồi sang action kế; hết action thì không clear track (giữ pose cuối)
Gun_8.skel.bytes anim: Attack, Idle, Open
files: EnemyWB5_P3_A.prefab
done_when: Play P3_A qua ≥4 lần spawn portal: súng G8 mở 1 lần và giữ nguyên tư thế mở suốt (quan sát + log flow), portal vẫn spawn/bắn bình thường; Console 0 error

## T4 — Test tổng
owner: claude
depends: T1, T2, T3
context:
Play scene test (MakeAll.unity, WB-Shooter/EnemyWB5_P2_A và P3_A). Kiểm thêm G6/G5 P2 và G9 P3 không bị ảnh hưởng.
verified:
Prefabs: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P2_A.prefab, EnemyWB5_P3_A.prefab
files: -
done_when: 2 yêu cầu đạt; Console 0 error/exception

## Câu hỏi mở
- G8: bỏ hẳn anim Attack khi bắn (plan hiện tại). Nếu muốn giữ anim Attack thì cần anim Attack kết thúc ở tư thế mở — file Spine Gun_8 hiện không có, phải nhờ artist.
