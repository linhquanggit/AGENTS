# WB5 P2 — G6 nối dây đúng tâm 2 viên đạn + G4 bật FX Explosive khi tách đạn
goal: (1) Súng 6 `G6-ElectricLink`: dây điện nối đúng tâm 2 thanh tụ điện, cả 2 lớp tia (LinkLaser2D_CL và con `LinkLaser2D_CL (1)`) đều bám theo. (2) Tên lửa Súng 4 `BulletWB5_P2_G4`: đúng lúc bắn nextShot (tách 10 viên) thì bật FX con `Explosive`.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 WB5 P2, 12 class WB-specific, 15.4/15.6) + dò trực tiếp ~/wkspaces/WB

## T1 — DestinationLinkShotWB5: neo dây vào tâm đạn + điều khiển mọi lớp tia
owner: muse
depends: -
context:
Shot bắn 2 thanh (mỗi FireDirection 1 thanh), sau `linkDelay` (0.5) nối dây giữa 2 thanh đang bay trong `linkDuration` (1) rồi thu đạn. Laser được tách khỏi cây boss ở `Start` (`DetachLaser`, bật lại `Rigidbody2D.simulated`) vì Rigidbody2D kinematic lồng trong Rigidbody2D dynamic của boss làm boss trôi.
Sửa:
- `BulletDestinationWB5`: thêm `[SerializeField] Transform linkPoint;` + `public Transform LinkPoint => linkPoint != null ? linkPoint : transform;` (null = tâm transform, hành vi cũ).
- `DestinationLinkShotWB5`: `SetPoint` dùng `_alive[i].LinkPoint` thay cho `.transform`.
- Gom mọi `LinkLaser_CL` dưới `linkLaser` (gồm chính nó, `GetComponentsInChildren<LinkLaser_CL>(true)`) vào 1 list, cache 1 lần; mọi chỗ đang gọi `linkLaser.Init / SetPoint / AutoAttack / StopAttack` gọi cho cả list; `DetachLaser` bật `Rigidbody2D.simulated` cho mọi laser trong list.
- Giữ nguyên tên field serialize có sẵn (`linkLaser`, `linkRequest`, `linkDelay`, `linkDuration`, `detachLaserOnInit`…). Không Odin. Log dùng DPDebug theo quy ước nếu cần.
verified:
Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/DestinationLinkShotWB5.cs:11 `public class DestinationLinkShotWB5 : DestinationShotE`
DestinationLinkShotWB5.cs:22 `[SerializeField] protected LinkLaser_CL linkLaser;`
DestinationLinkShotWB5.cs:37 `DetachLaser()`, :44 `_rb.simulated = true`, :48 `Start()`, :58 `OnDisable()` (:61 StopAttack), :65 `OnDestroy()`, :76-77 Init laser + StopAttack, :153-157 `linkLaser.Init / SetPoint(_alive[0].transform, _alive[1].transform) / AutoAttack / StopAttack`, :175 `DespawnVolley()`, :223-225 `OnEndShot()` StopAttack.
Bullets/Scripts/BulletDestinationWB5.cs:9 `public class BulletDestinationWB5 : BulletDestination`; :28 Init (đặt `stopAtTarget = false`, đạn bay thẳng không dừng).
Script BulletDestinationWB5 (guid f72c852c862ae4ced8937c8e1bcbd245) dùng ở BulletWB5_P2_G6, BulletWB5_P3_G9_S1, BulletWB5_P3_G9_S2 → field mới phải null-safe.
DestinationLinkShotWB5 (guid 3cf9376bdc4684611b4e7e71ecb7b904) chỉ dùng trong WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P2_A.prefab.
EnemyWB5_P2_A: `G6-ElectricLink/LinkLaser2D_CL` (LinkLaser_DPS, LineRenderer world, width 2, mat plasma_beam_red, UVTime 3, Rigidbody2D kinematic simulated=false) có con `LinkLaser2D_CL (1)` (LinkLaser_DPS, UVTime −2, Rigidbody2D kinematic simulated=false, LineRenderer lưu sẵn (0,0,0)→(0,0,1)) — con này hiện KHÔNG được shot gọi Init/SetPoint nên không bao giờ bám theo đạn.
BulletWB5_P2_G6: pivot root, visual `Ball_Electric_2` (ParticleSystem, simulationSpace Local, lp 0) → tâm hình trùng pivot.
FireDir-L lp (−0.65, −0.38) rz 180; FireDir-R lp (0.72, −0.37) rz 180 (trong prefab EnemyWB5_P2_A).
refs: ~/AGENTS/AI/Unity/knowledge/snapshots/shoot-templates/00-overview.md
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/DestinationLinkShotWB5.cs, Assets/Project/Bullets/Scripts/BulletDestinationWB5.cs
done_when: Unity compile 0 error; WB5 P3 G9 (dùng chung BulletDestinationWB5) không bị ảnh hưởng.

## T2 — HomingMissileSplitWB5: bật FX khi tách đạn
owner: muse
depends: -
context:
`Split()` chạy 1 lần khi hết `splitDelay` (G4 = 1s): `nextShot.Attack()` (DirectionShotE con 10 hướng), tên lửa bay tiếp (`destroyOnSplit = false`).
Sửa: thêm `[SerializeField] protected ParticleSystem splitFx;` (null = không làm gì).
- `Init()`: nếu có splitFx → `Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` (đạn lấy từ pool phải sạch FX cũ).
- `Split()`: nếu có splitFx → `gameObject.SetActive(true)` + `Play(true)`, ngay cạnh `nextShot.Attack()`.
Null-safe vì class dùng chung cho 4 prefab.
verified:
Bullets/Scripts/HomingMissileSplitWB5.cs:11 `public class HomingMissileSplitWB5 : HomingMissile`; :14 `nextShot`; :15 `splitDelay`; :16 `destroyOnSplit`; :28 `public override void Init(BulletHomingMissileData _data)`; :94 `protected virtual void Split()` (:97 `nextShot.Attack()`); :109 `Despawner()` (StopAllCoroutines).
Script guid 0f13b046d073f40dba8ba5355d35c0d4 dùng ở BulletWB5_P2_G4, BulletWB5_P2_G5_1, BulletWB5_P3_G8, BulletWB5_P3_G8_Split.
refs: Project 1941 doc claude/worldboss-boss-library.md mục 12 (HomingMissileSplitWB5)
files: Assets/Project/Bullets/Scripts/HomingMissileSplitWB5.cs
done_when: Unity compile 0 error.

## T3 — Gắn field mới vào prefab
owner: claude
depends: T1, T2
context:
Làm qua Unity MCP (`execute_code` codedom C# 6; `PrefabUtility.LoadPrefabContents` → sửa SerializedObject → `SaveAsPrefabAsset` → `UnloadPrefabContents`).
- `BulletWB5_P2_G4`: `splitFx` = con `Explosive`.
- `BulletWB5_P2_G6`: `linkPoint` để trống (tâm hình trùng pivot). Chỉ thêm con `LinkPoint` nếu test T4 cho thấy lệch.
- `EnemyWB5_P2_A`: không cần sửa nếu T1 tự gom laser con; kiểm `linkLaser` vẫn trỏ `G6-ElectricLink/LinkLaser2D_CL`.
verified:
BulletWB5_P2_G4 (guid 71ce7aa8f644340f8b3eb549b6876650): con `Explosive` lp (0, −0.24), scale 0.5, ParticleSystem playOnAwake=false, loop=false, simulationSpace Local, 2 con `Particle System-3/-4` (playOnAwake=false). Con khác: `Root` (SpriteRenderer), `HommingParticle (1)` (World), `WeaponDirectionShot` (DirectionShotE, 10 FireDirection).
HomingMissileSplitWB5 trên G4: splitDelay 1, destroyOnSplit false, prefabExplosion = Explosive_3_very_small, isDestroyOutCamera true.
BulletWB5_P2_G6 (guid 1da71c8ae06f448c99f2eaab85daf2dc): root CircleCollider2D offset (0,0) + BulletDestinationWB5; con `Ball_Electric_2` (lp 0) + 4 ParticleSystem con, tất cả Local.
refs: Project 1941 doc claude/worldboss-boss-library.md mục 10 (WB5 P2 bảng weapon)
files: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB5_P2_G4.prefab
done_when: Inspector G4: `splitFx` = Explosive; prefab lưu không lỗi.

## T4 — Test trong MakeAll
owner: claude
depends: T3
context:
MakeAll.unity hiện chỉ còn `WB-Shooter` (không còn object EnemyWB5_P2_A trong scene) → đặt tạm 1 instance prefab `EnemyWB5_P2_A` dưới `WB-Shooter`, test xong xoá, KHÔNG lưu scene.
Quy trình (library 15.4/15.6): trước Play `EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnlyAtlas`, sau Stop trả `AlwaysOnAtlas`; vào Play đọc Console ngay, có lỗi dừng Play sửa trước; chạy súng bằng `weapon.Init(dmg, cd); weapon.Play();` qua execute_code; đo bằng probe `EditorApplication.update` + `Debug.Log`, không screenshot (buộc phải chụp: `output_folder: "Temp"` rồi xoá).
Probe G6: khi dây bật, với từng LinkLaser_CL: `positionCount == 2` và 2 đầu `LineRenderer.GetPosition` cách tâm 2 viên ≤ 0.01; theo dõi suốt `linkDuration` (đạn vẫn bay). Vị trí boss không đổi (boss trôi = va chạm vật lý trong cây boss).
Probe G4: `Explosive.isPlaying` = false trước khi tách, = true ngay frame tách (≈1s sau phóng), số viên con tăng 10 cùng lúc.
Cuối: `find Assets -name "*.spriteatlas" -newermt <giờ bắt đầu>` = 0.
verified:
WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P2_A.prefab có `G4-Rocket-L/R`, `G5-ArrowSpin`, `G6-ElectricLink` (library mục 10).
ProjectSettings/EditorSettings.asset `m_SpritePackerMode: 4`.
refs: Project 1941 doc claude/worldboss-boss-library.md mục 15.4, 15.6
files: (không đổi file; scene không lưu)
done_when: Console 0 error; probe G6 cả 2 lớp tia đúng tâm 2 viên suốt thời gian nối; probe G4 Explosive chỉ chạy đúng lúc tách; boss đứng yên; 0 file .spriteatlas bị sửa.

## T5 — Hồi quy P3 + cập nhật library
owner: claude
depends: T4
context:
Class dùng chung với P3: BulletDestinationWB5 (G9_S1/S2), HomingMissileSplitWB5 (G8, G8_Split) → chạy nhanh `EnemyWB5_P3_A` G8 + G9 trong MakeAll (instance tạm, không lưu), Console sạch.
Cập nhật library bản chính `~/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md` (mục 10 P2 + bảng class mục 12) rồi đồng bộ project doc `claude/worldboss-boss-library.md`.
verified:
WorldBoss/Addressable/Enemies/Prefabs/ có EnemyWB5_P3_A (library mục 10 P3).
refs: Project 1941 doc claude/worldboss-boss-library.md mục 10 (P3)
files: ~/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
done_when: P3 G8/G9 chạy không lỗi; library ghi field `linkPoint`, gom nhiều lớp tia, `splitFx`.

## Câu hỏi mở
- "Điểm link ở tâm của 2 viên đạn": hiểu là mỗi đầu dây nằm đúng tâm từng viên (plan đang theo hướng này). Nếu ý là điểm giữa của 2 viên (vd hiệu ứng tại trung điểm) thì báo lại.
- Con `LinkLaser2D_CL (1)` (UVTime −2) là lớp tia thứ 2 cố ý thêm để tia dày/đẹp hơn? Plan cho nó bám theo cùng 2 điểm; nếu là thừa thì xoá thay vì gom.
- `Explosive` đang Local và là con của tên lửa → tên lửa bay tiếp thì vụ nổ chạy theo tên lửa. Có muốn nổ đứng yên tại điểm tách (đổi simulationSpace sang World hoặc tách ra pool riêng) không?
