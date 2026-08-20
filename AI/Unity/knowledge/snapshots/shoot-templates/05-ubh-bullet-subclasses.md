# 05 — Bullet subclass trực tiếp từ UniBulletHell (không phải shot pattern)

Các class này định nghĩa **hành vi của viên đạn sau khi bắn** (đường bay, trail, homing...), không phải cách bắn — dùng khi shot pattern đã chọn ở [01](01-unibullethell-base.md)/[02](02-baseshote-core.md) đủ, chỉ cần đổi loại đạn.

Thư mục `Assets/Project/Bullets/Scripts/`, tất cả kế thừa `UbhBullet` (base ở `Assets/UniBulletHell/Script/Bullet/UbhBullet.cs`) trừ khi ghi chú khác:

| Class | Ghi chú |
|---|---|
| `UbhBulletBoss7_1` | Đạn riêng cho Boss7 (biến thể 1). |
| `UbhBulletFighter` | Đạn dùng cho fighter/player-side (nếu áp dụng). |
| `UbhBulletMultiStep` | Đạn có nhiều "step"/giai đoạn bay khác nhau. |
| `UbhBulletSimple` | Đạn cơ bản, hành vi tối giản. |
| `UbhBulletSimpleFighter` | Biến thể đơn giản riêng cho fighter. |
| `UbhBulletTrail` | Đạn có hiệu ứng trail. |
| `UbhHomingBullet` | Đạn tự lái về target sau khi bắn (khác `UbhHomingShot` ở mục 01 — đây là hành vi đạn, không phải cách bắn). |
| `BulletThreeBallRotation` | Đạn đi kèm `WeaponThreeBallRotation` ([04-weapons-legacy.md](04-weapons-legacy.md)) — quỹ đạo xoay quanh 1 tâm. |
| `UbhBulletStopAndFall` | 2 phase: bay như base shot bình thường → sau `delayStop` giây thì freeze tại chỗ `stopTime` giây → free-fall thẳng xuống (`Vector3.down`, tăng tốc theo `fallGravity`). Xem [learning](../../learnings/ubh-bullet-stopandfall.md). |

Không liệt kê lại `UbhBulletSimpleSprite2d`/`UbhBulletSimpleModel3d`/`UbhBulletStoppable`/`UbhTentacleBullet`/`BulletWeapon10_1` — các class built-in gốc của asset, xem [01-unibullethell-base.md](01-unibullethell-base.md) mục "Thư mục liên quan".
