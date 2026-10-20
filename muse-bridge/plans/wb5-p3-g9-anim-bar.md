# WB5 P3 G9 — orbit 2 giữ mở anim, đạn orbit 1 không bị kill giữa màn
goal: 2 đường link G9 dùng chung điểm tâm thanh (không tách); G9-Orbit-S2 thấy súng mở (và giữ mở) ngay khi vào trận; thanh điện của G9-Orbit-S1 không còn tự bị kill sau khi bay 1 đoạn — chỉ hủy khi cặp rơi khỏi đáy màn / chạm player như logic shot.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 P3 Súng 9); plan trước wb5-p2p3-fix, wb5-g4-track-g8-hold (cùng lỗi Idle = tư thế đóng như G8)

## T0 — Prefab: 2 link G9 cùng điểm tâm thanh, không tách
owner: claude
depends: -
context:
Mỗi cặp vẽ 2 đường link (laser + laser2 clone), neo lệch ±linkOffset vuông góc trục A-B → 2 dây tách nhau. Linh chốt: cả 2 dây nối đúng tâm thanh. Sửa chỉ prefab: linkOffset = 0 cho G9-Orbit-S1 và G9-Orbit-S2 (không đổi code; hiệu ứng grow A→B / B→A giữ nguyên).
verified:
ElectricOrbitShotWB5.cs:42 l1a/l1b/l2a/l2b neo lệch ±linkOffset; :62 [SerializeField, Min(0f)] float linkOffset = 0.15f; :193-198 UpdateLinkAnchors _n = vuông góc × linkOffset
EnemyWB5_P3_A.prefab:15557 linkOffset 0.15 (S1), :17130 linkOffset 0.15 (S2)
files: EnemyWB5_P3_A.prefab
done_when: Play P3_A: S1 và S2 cả 2 dây trùng nhau, đi qua tâm 2 thanh suốt lúc giãn/quay/đan sao; Console 0 error

## T1 — Prefab: flow Gun9 giữ tư thế mở (S1 + S2)
owner: claude
depends: -
context:
Nguyên nhân S2 "chưa mở": WorldBossWeapon Play() phát flow Open lúc vào trận, nhưng flow Open = [Open, Idle(loop)] và Idle = tư thế đóng (CloseInit cũng = Idle) → súng mở rồi đóng ngay. S2 có delayStart 5 nên suốt 5s đầu đứng ở Idle (đóng) → nhìn như chưa mở; S1 phóng ngay (launchCooldown 2) nên anim Attack che đi. Giống hệt lỗi G8 (plan wb5-g4-track-g8-hold T3).
Sửa (chỉ prefab, không code): SpineAnimFlow của G9-Orbit-S1/Gun9-L/Animation và G9-Orbit-S2/Gun9-L/Animation: flow Open bỏ action Idle phía sau (chỉ Open, Loop 0 → giữ frame cuối = mở); flow Attack bỏ action Idle phía sau (giữ frame cuối Attack). Gun9-R tự theo L qua SpineAnimSync. Không sửa Idle/CloseInit.
Nếu khi Play thấy frame cuối Attack là tư thế đóng → đặt useSelfAnimAttack = 0 cho G9-Orbit-S1/S2 (như G8) và ghi lại.
verified:
EnemyWB5_P3_A.prefab (Assets/Project/WorldBoss/Addressable/Enemies/Prefabs): SpineAnimFlow 6905402582495643768 (G9-Orbit-S2/Gun9-L/Animation) và 6905402582426459021 (G9-Orbit-S1/Gun9-L/Animation) flows: Idle[Idle L], CloseInit[Idle L], Open[Open, Idle L], Attack[Attack, Idle L]
WorldBossWeapon G9-Orbit-S1/S2: attackType 1, useSelfAnimAttack 1, openOnce 0, flowOpen Open, flowAttack Attack; S2 shot delayStart 5 (dòng ~17076), S1 launchCooldown 2 (dòng ~15564)
SpineAnimSync Gun9-R → source Gun9-L cùng weapon (dòng 16144 S2, 16908 S1)
SpineAnimFlow.cs:125-170 ExecuteFlow: action Loop 0 chờ complete rồi sang action kế; hết action không clear track (giữ pose cuối)
Gun9.skel.bytes anim: Open, Attack, Idle
files: EnemyWB5_P3_A.prefab
done_when: Play P3_A: ngay khi boss vào PLAYING, cả súng S1 và S2 (L+R) mở và giữ mở; S2 trong 5s đầu vẫn mở; mỗi lần phóng cặp anim Attack chạy xong vẫn ở tư thế mở; Console 0 error

## T2 — S1: phóng cặp mới không được hủy cặp cũ đang bay
owner: muse
depends: -
context:
Nguyên nhân (Linh xác nhận bị kill ngay giữa màn, không phải ở rìa): PERSISTENT_ORBIT chỉ có 2 Pair cố định; mỗi launchCooldown 2s lấy pairs[_launchIndex % 2] rồi gọi DespawnPair(_p) + LaunchPair(_p) → mỗi cặp bị hủy và phóng lại sau 4s, trong khi cặp rơi fallSpeedPx 60 px/s cần lâu hơn nhiều mới ra khỏi đáy màn → cặp biến mất giữa màn.
Sửa: tách cấu hình và trạng thái chạy. pairs[] chỉ là template (launchA/B, laser, centerOffset, radius…). Mỗi lần phóng tạo 1 instance runtime mới (copy field cấu hình từ template, bars mới từ pool, laser/laser2 clone riêng — tái dùng clone cũ đã rảnh trong 1 pool nhỏ) và thêm vào List active. Tick mọi instance active; instance chỉ despawn khi TickPair trả false (rơi khỏi đáy / chạm player / bị phá) → trả laser clone về pool rảnh. Vẫn xoay vòng template theo _launchIndex để đổi nửa màn như cũ. StopAttack/Init despawn hết instance active. WEAVE_ONCE (S2) giữ nguyên logic 1 instance/template.
Bonus an toàn: SpawnBar tắt tự hủy ngoài camera của thanh (thêm Bullet.SetDestroyOutCamera(bool), lưu giá trị prefab ở OnEnable, khôi phục khi DestroyDefault/OnDisable như offsetBounsPrefab) — vòng đời thanh do shot quản.
verified:
ElectricOrbitShotWB5.cs (Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase): IE_SpawnBullet PERSISTENT ~:563-585: _nextLaunchAt = _clock + launchCooldown; _p = pairs[_launchIndex % pairs.Length]; DespawnPair(_p); LaunchPair(_p); ~:448 IsBroken; ~:453 DespawnPair (DestroyDefault 2 bar); Pair class :19-46 (laser2 + neo l1a… là NonSerialized runtime); :62 linkOffset; ~:503 IsPairOutOfScreen theo đáy camera
EnemyWB5_P3_A.prefab G9-Orbit-S1 (~dòng 15523): mode 0, pairs 2 phần tử, approachTime 1, chargeTime 0.5, orbitRadiusPx 165, fallSpeedPx 60, launchCooldown 2
Bullet.cs (Assets/Project/Bullets/Scripts):13 isDestroyOutCamera; :41-42 offsetBounsPrefab; :61 SetOffsetBounds / ResetOffsetBounds; BulletWB5_P3_G9_S1.prefab:24307 isDestroyOutCamera 1
BulletDestinationWB5.cs:17-29 OnTriggerEnter2D không DestroyDefault (không phải nguyên nhân)
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: ElectricOrbitShotWB5.cs, Bullet.cs
done_when: compile 0 error; Play P3_A ≥ 30s: log mỗi instance S1 (id, lý do despawn, y lúc despawn) — mọi cặp chỉ despawn khi "out bottom" (y < đáy camera) hoặc "touch player"; cùng lúc có >2 cặp trên màn khi cần; dây đúng cặp; S2 không đổi; P2 link không đổi; Console 0 error

## T3 — Test
owner: claude
depends: T0, T1, T2
context:
Play MakeAll.unity, EnemyWB5_P3_A (và lướt P2_A G6 link vì cùng sửa Bullet.cs). Gỡ log tạm sau khi xác nhận.
verified:
Prefabs: EnemyWB5_P3_A.prefab, EnemyWB5_P2_A.prefab
files: -
done_when: 2 lỗi hết; Console 0 error/exception

## Câu hỏi mở
- (không có — nếu frame cuối Attack của Gun9 là tư thế đóng, T1 tự chuyển sang useSelfAnimAttack = 0)
