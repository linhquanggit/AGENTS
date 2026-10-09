# WB4 P2 Súng 5 — đạn 2 phase (bay lên + nháy sáng → rơi xuống)
goal: Đạn Súng 5 của WB4 (Tank) P2 tách thành 2 phase hiển thị: phase 1 bay lên dùng sprite P1 và nháy sáng trong lúc đứng lại, phase 2 rơi xuống dùng sprite P2. Dùng sprite có sẵn trong prefab, chỉ bật/tắt object theo phase; không đổi quỹ đạo/thông số bắn hiện tại.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 9 WB4 P2, 18.4, 15.4/15.6) + BR_WorldBoss/design/worldboss-design.txt R433–R436 + dò trực tiếp ~/wkspaces/WB

## T1 — Thêm visual theo phase vào BulletStopAndFall (tuỳ chọn, null-safe)
owner: muse
depends: -
context:
Design R433–R436: "Súng 5: 2 khẩu bắn thay phiên; bắn liên tục 15 viên ra phía sau khẩu súng 1 khoảng nhỏ… xong rồi rơi thẳng xuống dưới (na na Súng 3); delay giữa 2 khẩu 1.5s". Yêu cầu mới của Linh: phase bay lên có nháy sáng, phase rơi đổi hình.
Vòng đời hiện tại của BulletStopAndFall: bay theo hướng bắn `delayStop` giây → đứng yên `stopTime` giây → rơi thẳng xuống (gia tốc `fallGravity`, kẹp `fallMaxSpeed`), lúc bắt đầu rơi set rotation Z = 180 một lần.
Thêm (tất cả mặc định null/0 → hành vi cũ giữ nguyên, vì BulletWB5_P3_G7 cũng dùng class này):
- `[SerializeField] GameObject objRise;` (phase 1: bay + đứng), `[SerializeField] GameObject objFall;` (phase 2: rơi).
- Nháy sáng trong lúc đứng yên: `[SerializeField] SpriteRenderer flashRenderer; [SerializeField] Color flashColor = Color.white; [SerializeField] float flashInterval = 0.08f;` — đổi màu qua lại giữa màu gốc và flashColor (nhớ cache màu gốc ở Awake, trả lại màu gốc khi Init và khi chuyển sang rơi).
- Init(): objRise on, objFall off, reset màu. Lúc chuyển isFalling = true: objRise off, objFall on.
Quy ước code: biến local `_x`, private camelCase, không dùng Odin, không đổi tên field serialize có sẵn (`delayStop/stopTime/fallGravity/useFallMaxSpeed/fallMaxSpeed`), không thêm comment thừa.
verified:
Assets/Project/Bullets/Scripts/BulletStopAndFall.cs:4 `public class BulletStopAndFall : Bullet`
BulletStopAndFall.cs:6-10 field delayStop/stopTime/fallGravity/useFallMaxSpeed/fallMaxSpeed
BulletStopAndFall.cs:22 `public override void Init(GameObject _own, BigDouble _DMG, …)` — reset isStopped/isFalling/timer ở :31-36
BulletStopAndFall.cs:39 `protected override void OnMove()`; chuyển sang đứng ở :56-60; chuyển sang rơi ở :65-70 (`isFalling = true` :67, `transform.rotation = Quaternion.Euler(0f, 0f, 180f)` :69)
BulletStopAndFall.cs:14 `SetStopTime(float)` (FanFallShotWB5 dùng) — giữ nguyên
Script guid 97a821b6f38b14330b5c53ba9aeddc1d dùng trong 2 prefab: WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB4_P2_G2.prefab và BulletWB5_P3_G7.prefab
refs: ~/AGENTS/AI/Unity/knowledge/learnings/ubh-bullet-stopandfall.md, ~/AGENTS/AI/Unity/knowledge/snapshots/shoot-templates/00-overview.md
files: Assets/Project/Bullets/Scripts/BulletStopAndFall.cs
done_when: Unity compile 0 error; BulletWB5_P3_G7 (không gán field mới) chạy y như cũ.

## T2 — Gắn 2 phase vào prefab BulletWB4_P2_G2
owner: claude
depends: T1
context:
Prefab đạn của Súng 5 tên là `BulletWB4_P2_G2` (tên lệch số súng, giữ nguyên tên — đổi tên là việc khác).
Gán: `objRise = P1`, `objFall = P2`, `flashRenderer = SpriteRenderer của P1`. P2 để tắt mặc định trong prefab.
`objVFXList` hiện = [Trail, P1] → `Bullet.CheckVFX()` bật/tắt P1 theo `Settings.OnVFX` ở OnEnable, sẽ đè lên việc bật/tắt theo phase (tắt VFX là mất luôn thân đạn). Bỏ P1 khỏi objVFXList (giữ Trail; Fx tuỳ câu hỏi mở).
Lúc rơi script set rotation Z = 180 → kiểm hình P2 (Bullet5_30x42) quay đúng chiều rơi; lệch thì xoay local của P2, không sửa script.
Làm qua Unity MCP (execute_code codedom C# 6, SerializedObject), mở prefab bằng PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset.
verified:
Prefab BulletWB4_P2_G2 (guid df50eb5897fc748d59ff83f0ba52c096): root scale 0.5, CircleCollider2D + BulletStopAndFall; con: P1 (SpriteRenderer `ShurikenBullet3`, scale 1.59, order 105), Trail (TrailRenderer), Fx (ParticleSystem), P2 (SpriteRenderer `Bullet5_30x42`, lp y −0.05, scale 3.15, order 105). Cả P1 và P2 đang active → hiện tại 2 hình chồng nhau.
BulletStopAndFall trên prefab: delayStop 0.5, stopTime 1, fallGravity 60, useFallMaxSpeed true, fallMaxSpeed 30, objVFXList = [Trail, P1].
Assets/Project/Bullets/Scripts/Bullet.cs:21 `objVFXList`; Bullet.cs:53 OnEnable → :68 CheckVFX(); Bullet.cs:322-329 CheckVFX đặt mọi obj trong objVFXList active = Settings.OnVFX.
Sprite: Assets/Project/Bullets/Textures/Use/ShurikenBullet3.png, Assets/Project/Bullets/Textures/Use/Bullet5_30x42.png.
refs: Project 1941 doc claude/worldboss-boss-library.md mục 9 (WB4 P2), 18.4
files: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB4_P2_G2.prefab
done_when: Prefab lưu được; Inspector: objRise=P1, objFall=P2, flashRenderer=P1, P2 inactive, objVFXList không còn P1.

## T3 — Test Súng 5 trong MakeAll bằng WB-Shooter
owner: claude
depends: T2
context:
Súng: `EnemyWB4_P2_A` → `G5-RandomSpread-L` / `G5-RandomSpread-R` (ROTATE, PhaseRotation initAngle 180, 3 phase, loop, loopCooldown 2) + `RandomSpeedDirectionShotE` (waves 15, fireRate 0.1, speed 3–8, bullet BulletWB4_P2_G2).
Quy trình test (library 15.4/15.6): đặt tạm 1 instance `EnemyWB4_P2_A` dưới `WB-Shooter` trong Assets/Project/Scenes/MakeAll.unity, gán field `weapon` = G5-RandomSpread-L (LoadAssets chỉ lấy súng đầu).
Trước khi vào Play: `EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnlyAtlas`; sau Stop trả `AlwaysOnAtlas` (nếu không ~125 file .spriteatlas bị đánh dấu change). Vào Play → đọc Console ngay, có lỗi thì dừng Play sửa trước.
Đo bằng probe EditorApplication.update + Debug.Log mỗi 0.2s: với vài viên trong Pool, log phase (P1/P2 active), màu SpriteRenderer P1, vị trí y. Không chụp screenshot (nếu buộc phải chụp: output_folder "Temp" và xoá ngay).
Xong: xoá instance tạm, không lưu scene; kiểm `find Assets -name "*.spriteatlas" -newermt <giờ bắt đầu>` = 0.
verified:
EnemyWB4_P2_A.prefab: G5-RandomSpread-L lp (−1.34, −0.02), G5-RandomSpread-R lp (1.38, −0.04); RandomSpeedDirectionShotE useSelfCD false (cd lấy từ data), cooldown field 2.
EditorSettings.asset m_SpritePackerMode: 4 (AlwaysOnAtlas).
refs: Project 1941 doc claude/worldboss-boss-library.md mục 15.4, 15.6
files: (không đổi file nào; scene không lưu)
done_when: Console 0 error; log thấy mỗi viên: bay lên với P1, nháy màu trong ~1s đứng yên, đổi sang P2 lúc rơi, P2 hướng xuống; tắt VFX (Settings.OnVFX=false) thân đạn vẫn hiện.

## T4 — Kiểm hồi quy WB5 P3 Súng 7 + cập nhật library
owner: claude
depends: T3
context:
BulletWB5_P3_G7 dùng chung BulletStopAndFall (FanFallShotWB5 gọi SetStopTime để rơi lần lượt). Field mới để trống → phải chạy như cũ.
Cập nhật library bản chính BR_WorldBoss/library/worldboss-boss-library.md (mục 9 WB4 P2 + 18.4 dòng P2 S5) rồi đồng bộ project doc claude/worldboss-boss-library.md.
verified:
MakeAll.unity có `WB-Shooter/EnemyWB5_P3_A` với `G7-FanFall-L/R` (library mục 10 P3).
refs: Project 1941 doc claude/worldboss-boss-library.md mục 10 (P3 G7)
files: ~/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
done_when: G7 test Play: quạt 7 viên, đứng 0.5s, rơi lần lượt, Console sạch; library ghi cơ chế objRise/objFall/flash.

## Câu hỏi mở
- "Nháy sáng" kiểu nào: đổi màu sang trắng chớp tắt (đề xuất mặc định), hay nhấp nháy ẩn/hiện, hay phóng to nhẹ? Nháy suốt thời gian đứng yên (1s) hay chỉ cuối giai đoạn bay?
- Particle `Fx` thuộc phase nào (chỉ khi bay, chỉ khi rơi, hay cả hai)? Có cần đưa Fx vào objVFXList không?
- Trail có giữ trong phase rơi không?
- Có đổi tên prefab `BulletWB4_P2_G2` → `BulletWB4_P2_G5` cho khớp số súng không (đụng tham chiếu trong EnemyWB4_P2_A, cần làm riêng)?
