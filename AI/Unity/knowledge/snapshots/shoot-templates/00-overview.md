# Snapshot: Catalog mẫu Shoot/Bullet Pattern trong project

**Mục đích:** liệt kê toàn bộ template bắn đạn (shot pattern) đã có trong project — cả hệ built-in của asset UniBulletHell lẫn hệ tự viết `BaseShotE` — để trước khi tạo 1 loại đạn/shoot mới, check qua catalog này xem có tái sử dụng/kế thừa được từ template có sẵn không, tránh viết trùng.

**Snapshot ngày:** 2026-08-03. Nguồn: khảo sát `Assets/UniBulletHell/Script/ShotPattern/`, `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/`, `Assets/Project/GamePlay/Weapons/Scripts/`, `Assets/Project/Bullets/Scripts/` (Explore agent, đọc field + method signature, không đọc full body từng file).

**Không phải deep-read từng dòng** — mỗi class chỉ được skim để tóm tắt hành vi; trước khi copy field/logic vào code thật, luôn mở lại file gốc để xác nhận.

## Cấu trúc catalog (đọc theo thứ tự khi cần)

| File | Nội dung | Khi nào mở |
|---|---|---|
| [01-unibullethell-base.md](01-unibullethell-base.md) | 28 class `Ubh*Shot` built-in của asset UniBulletHell (N-way, circle, spiral, random, homing...) | Cần pattern hình học chuẩn (fan, ring, spiral, sóng...), không gắn damage/cooldown riêng của project |
| [02-baseshote-core.md](02-baseshote-core.md) | ~29 class kế thừa `BaseShotE` — hệ tự viết của project, có damage/cooldown/wave/event, đang dùng thật cho enemy/boss | **Ưu tiên check đầu tiên** — đây là hệ enemy/boss thực sự đang dùng |
| [03-baseshote-boss-specific.md](03-baseshote-boss-specific.md) | Subclass one-off gắn cứng cho 1 boss/mini-boss cụ thể (`BombShotBoss14`, `SummonB2`, `LinkLaserShotWB1`...) | Chỉ tham khảo cách 1 boss cụ thể tuỳ biến template — **không copy trực tiếp**, luôn phải sửa lại theo id boss mới |
| [04-weapons-legacy.md](04-weapons-legacy.md) | Hệ `Weapons/` cũ (không kế thừa `BaseShotE`) — laser family, weapon direction/burst/flamethrower | Chỉ khi enemy/vũ khí xác nhận đang dùng hệ Weapons cũ, không phải BaseShotE |
| [05-ubh-bullet-subclasses.md](05-ubh-bullet-subclasses.md) | Class bullet (không phải shot pattern) kế thừa `UbhBullet`/`UbhBaseShot` trực tiếp | Cần tuỳ biến hành vi *sau khi bắn* (đường bay/trail/homing của viên đạn), không phải cách bắn |

## Cơ chế lõi (bắt buộc biết trước khi chọn template)

- **2 hệ song song, không thay thế nhau:**
  - `UbhBaseShot` (asset UniBulletHell, `Assets/UniBulletHell/Script/ShotPattern/UbhBaseShot.cs`) — lo hình học bắn (góc, số lượng, tốc độ), KHÔNG có damage/cooldown/wave theo convention project.
  - `BaseShotE` (`Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/BaseShotE.cs`) — hệ riêng của project: field `damageType/damageFactor/cooldown/delayStart/waves/fireRate/shotFX` + Unity events (`ueStartShot/ueOnShot/ueShotFinish/ueEndShot`); loop chuẩn `DelayStart → StartShot → DelayShot → Shot (spawn bullet, lặp theo FireRate) → DelayEnd → EndShot → Cooldown`. Mọi class con implement `IE_SpawnBullet()`.
  - **Cầu nối 2 hệ:** `UbhShotE` (`Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/UbhShotE.cs`) — wrap 1 `UbhShotCtrl` (UniBulletHell) để expose API `Init/Attack/AutoAttack/ChangeCD` giống các `BaseShotE` khác. Nếu cần 1 pattern hình học đã có sẵn ở UniBulletHell (mục 01) nhưng vẫn phải theo damage/cooldown convention của project → dùng `UbhShotE`, KHÔNG viết lại bằng tay.
- **Meta-controller ghép nhiều template trên 1 enemy:** `ControllerShotE` (`BaseShot/ControllerShotE.cs`) giữ list `InfoShotE` + enum `SHOT_MODE` (`LINEAR/SHUFFLE/RANDOM/ALL`) để enemy luân phiên/chọn ngẫu nhiên giữa nhiều shot đã cấu hình sẵn — dùng khi 1 boss cần nhiều pattern thay phiên, thay vì viết 1 class mới gộp hết.
- Doc gốc trong code gần như trống (`UbhBaseShot`/subclass chỉ có `/// <summary></summary>` rỗng; `BaseShotE.cs` có 1 đoạn comment mô tả loop, tác giả ghi "PLEASE CHANGE HERE! - AKI") — catalog này là nguồn tóm tắt hành vi duy nhất; asset chỉ có `Assets/UniBulletHell/readme.txt` dạng quick-start, không có taxonomy.
- **Usage sites (không phải template):** mỗi boss/enemy có 1 file consumer riêng (vd `Assets/Project/Enemies/Scripts/EnemyBoss7.cs`, `EnemyMB4.cs`..., mirror ở `Assets/Project/PVP/Scripts/Enemy/`, `Assets/Project/Tournament/Scripts/Enemy/`, `Assets/Project/Endless/Scripts/`, `Assets/Project/Events/AirDefense/Scripts/Enemies/`) chỉ cấu hình/compose các template ở mục 02-04, không tự định nghĩa pattern mới. Khi cần ví dụ "1 boss thật đang dùng template X như thế nào", tìm theo tên boss trong các folder này thay vì đọc lại catalog.

## Recipe: khi được yêu cầu "làm 1 loại đạn/shoot mới"

1. Xác định cần gì: **hình học bắn mới** (fan/ring/spiral/random/sóng...) hay **hành vi sau bắn mới** (đường bay đạn: zigzag/bezier/homing) hay **hazard mới** (laser/flame/bomb/summon)?
2. Hình học bắn → quét bảng ở [01-unibullethell-base.md](01-unibullethell-base.md) trước (28 pattern có sẵn: N-way, overtake, waving, spiral multi-arm, paint, random...). Nếu khớp ≥80% → dùng thẳng qua `UbhShotE` (mục cơ chế lõi) thay vì viết `BaseShotE` mới.
3. Nếu cần gắn damage/cooldown/wave theo chuẩn enemy project mà hình học đã có ở UniBulletHell → bọc qua `UbhShotE`, đừng viết `BaseShotE` con mới trùng lặp.
4. Nếu hình học KHÔNG có sẵn ở UniBulletHell nhưng tương tự 1 class trong [02-baseshote-core.md](02-baseshote-core.md) (vd cần thêm biến thể zigzag/bezier/laser) → kế thừa/sửa từ class gần nhất trong core, không viết từ đầu.
5. Nếu là tuỳ biến one-off cho đúng 1 boss (không ai khác dùng lại) → đặt trong `BaseShot/SpecialCase/` theo đúng convention đặt tên `<ShotType><BossId>` đã thấy ở [03-baseshote-boss-specific.md](03-baseshote-boss-specific.md), KHÔNG cần tổng quát hoá.
6. Chỉ khi không có template nào ở mục 01/02 khớp hành vi lõi (>50% logic khác) mới viết `BaseShotE` mới hoàn toàn — implement đủ hook `IE_SpawnBullet()`, bắn qua vòng loop chuẩn, không tự chế loop riêng.
7. Trước khi hoàn tất, xác nhận lại field/method thật trong file gốc — catalog chỉ tóm tắt, không thay cho việc đọc source khi code thật.
