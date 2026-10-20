# WB4 P3 — G8-Dir: bật Hold_Energy + đường line ngắm bám theo xoay thân
goal: Trong EnemyWB4_P3_A, G8-Dir bật Hold_Energy khi tụ lực; đường line ngắm (Charge_Arrow/Arrow_Line) xoay theo BODY-ROTATE giống G6-DamageZone của EnemyWB4_P2_A bám BodyRotate. Chỉ sửa data prefab, không viết script mới.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (§9 WB4) + plan cũ wb4-p3-g8-hold-laser, wb4-p2-g6-track + dò trực tiếp prefab/script

## T1 — Bật Hold_Energy trong chu kỳ tụ của G8
owner: claude
depends: -
context:
Plan wb4-p3-g8-hold-laser từng đặt shotSteps[0].fxBefore = Hold_Energy, nhưng hiện fxBefore đã đổi sang Charge_Arrow → Hold_Energy không còn được bật. Cần bật CẢ HAI trong 2s tụ: giữ Charge_Arrow là fxBefore, bật Hold_Energy bằng ueBeforeShot (GameObject.SetActive(true) + ParticleSystem.Play) và tắt sau timeFxBefore (vd thêm vào ueAttack/step kế, hoặc Hold_Energy để looping=0 duration ≈2s và chỉ cần Play).
Không gọi WorldBossWeapon.LoadAssets (reset shotSteps).
verified:
EnemyWB4_P3_A.prefab path: EnemyWB4_P3_A/BODY-ROTATE/G8-Dir/{Animation, DirectionShotE/FireDirection/Charge_Arrow/Arrow_Line, DirectionShotE/FireBreathShotE, Hold_Energy}
G8-Dir WorldBossWeapon (guid 929b49aa…, fid 3808507788390356540): attackType 4 (SEQUENCE), rotate = Rotation2D fid 2734432001113378891 (RotationAimFighter trên DirectionShotE)
shotSteps[0]: shot DirectionShotE (fid 3808507787804866394), fxBefore = Charge_Arrow (GO 4588729092804096907, m_IsActive 0), timeFxBefore 2, delayAfterShot 0.5, ueBeforeShot → RotationAimFighter.Play
Hold_Energy GO 6726585987539252717: m_IsActive 1, ParticleSystem fid 5920107378469859134 playOnAwake 1, looping 0 → chỉ nổ 1 lần lúc spawn; không có persistent call nào trỏ tới nó
WorldBossWeapon.cs:194-197 ueBeforeShot.Invoke → fxBefore.SetActive(true) → WaitForSeconds(timeFxBefore) → SetActive(false); :234 ActiveStepFX chỉ đụng fxBefore
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb4-p3-g8-hold-laser.md; /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P3_A.prefab
done_when: Hold_Energy đặt inactive/không playOnAwake; mỗi chu kỳ G8 thấy Hold_Energy + Charge_Arrow cùng hiện 2s rồi tắt khi bắn; save prefab, console 0 error.

## T2 — Line ngắm G8 bám xoay BODY-ROTATE (như G6 ở P2)
owner: claude
depends: T1
context:
Mẫu P2: G6-DamageZone là con trực tiếp BodyRotate, WorldBossWeapon.rotate = None, không có Rotation2D riêng → mọi thứ (kể cả tâm ngắm) xoay theo thân. BodyRotate P2 có RotationAimFighter (offsetAngle 90) + RotationAimBarrel (barrelLocalDir (0,-1), maxDegreesPerSecond 360).
P3 hiện tại: DirectionShotE có RotationAimFighter riêng (speed 20) → line ngắm tự aim độc lập, không theo thân.
Việc: bỏ aim riêng ở DirectionShotE (disable component, gỡ persistent call Play ở ueBeforeShot, G8-Dir.rotate → aim của BODY-ROTATE hoặc None) và để BODY-ROTATE xoay aim player theo cách P2 (thêm RotationAimBarrel giống P2 nếu cần, chỉnh barrelLocalDir khớp hướng FireDirection — FireDirection localRot z=180°). Kết quả: Charge_Arrow/Arrow_Line + đạn bắn ra theo đúng hướng thân đang xoay.
verified:
P2: G6-DamageZone ← BodyRotate ← EnemyWB4_P2_A; G6 WorldBossWeapon rotate {fileID: 0}; BodyRotate comps: Spine + RotationAimFighter(guid c36601…, speed 5, offsetAngle 90) + RotationAimBarrel(guid 4ce7671e…, barrelLocalDir (0,-1), maxDeg 360)
P3: G8-Dir ← BODY-ROTATE ← EnemyWB4_P3_A; BODY-ROTATE comps: Spine + RotationAimFighter (speed 10, isRotating 0), KHÔNG có RotationAimBarrel
P3: DirectionShotE RotationAimFighter fid 2734432001113378891 speed 20; Charge_Arrow ← FireDirection(rot z 180) ← DirectionShotE
Rotation2D.cs:4 abstract; :39 Play() isRotating=true; :43 Pause()
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb4-p2-g6-track.md, wb4-p3-g8-fire-rotate.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P3_A.prefab
done_when: Play scene test WB4 P3: trong 2s tụ, line ngắm xoay cùng thân về phía player; đạn G8 bay đúng theo line; các súng khác trên BODY-ROTATE không bị lệch; console 0 error.

## T3 — Kiểm thử & ghi lại
owner: claude
depends: T2
context:
So sánh trực quan với G6 ở P2. Ghi kết quả vào status.
verified:
-
refs: claude/worldboss-boss-library.md
files: -
done_when: Video/screenshot hoặc ghi chú xác nhận cả 2 yêu cầu; prefab đã save.

## Câu hỏi mở
- BodyRotate P2 được Play() từ đâu (không có persistent call trong prefab) — có thể do code EnemyWB4_P2/EnemyWorldBoss; cần xác nhận khi làm T2 để P3 gọi tương tự.
- BODY-ROTATE P3 xoay theo player có ảnh hưởng súng khác gắn trên nó (vd G2) không — nếu có, chỉ xoay khi G8 đang tụ/bắn?
- Hold_Energy tắt đúng lúc bắn hay giữ đến hết delayAfterShot?
