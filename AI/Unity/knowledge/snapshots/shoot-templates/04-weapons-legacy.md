# 04 — Hệ `Weapons/` cũ (không kế thừa `BaseShotE`)

Thư mục: `Assets/Project/GamePlay/Weapons/Scripts/`. Hệ song song thứ 2, phần lớn là `MonoBehaviour` trần (không kế thừa `BaseShotE` ở [02-baseshote-core.md](02-baseshote-core.md)) — có vẻ cũ hơn/gắn với player hoặc weapon dùng chung. **Chỉ dùng làm tham khảo khi xác nhận enemy/vũ khí đang tuỳ biến thực sự nằm trong hệ này**, không trộn lẫn với `BaseShotE`.

| Class | Hành vi |
|---|---|
| `WeaponDirectionShot` (base) | Bắn theo hướng cấu hình, base cho 2 biến thể dưới. |
| `WeaponDirectionShotExplosive` | `WeaponDirectionShot` + nổ khi trúng. |
| `WeaponDirectionShotSlowdown` | `WeaponDirectionShot` + hiệu ứng làm chậm khi trúng. |
| `WeaponDirectionPause` | Bắn theo hướng, có pause/resume timing riêng. |
| `WeaponBurstClosestTarget` | Bắn burst tự khoá vào target gần nhất. |
| `WeaponMultipleTarget` / `ItemMultipleTarget` / `ClosestTargetDetector` | Helper chọn/nhắm nhiều target (không tự bắn). |
| `WeaponLaser`, `WeaponLaserAim`, `WeaponLaserParticle`, `WeaponLaserTarget (: Enemy)`, `Laser`, `LaserAim`, `LaserParticle`, `LaserRotation`, `LaserTarget`, `LaserWarning`, `CircleLaser`, `FighterLaser_DPS` | Cả 1 họ laser: aim/particle/rotation/warning-telegraph — nhiều biến thể chồng chéo, cần đọc kỹ trước khi chọn cái nào. |
| `WeaponFlamethrower` | Luồng lửa liên tục (tương tự `FireBreathShotE`/`FlameShot_RP` ở hệ BaseShotE nhưng không chung base). |
| `WeaponTT` | Vũ khí đặc biệt riêng "TT" (khả năng gắn boss cụ thể). |
| `WeaponThreeBallRotation : UbhBaseShot` | **Duy nhất trong folder này subclass thẳng UniBulletHell** — bắn 3 quả bóng xoay quỹ đạo, đi kèm bullet `BulletThreeBallRotation.cs`. |
| `DamageArea`, `BulletPauseAndResume` | Component hazard/tiện ích hỗ trợ, không phải shot pattern độc lập. |

## Lưu ý khi cân nhắc tái sử dụng

- Trước khi copy 1 class ở đây, kiểm tra xem enemy/boss mục tiêu đã dùng `BaseShotE` (mục 02) hay chưa — nếu enemy đó đã chuẩn hoá theo `BaseShotE`, ưu tiên tương đương bên [02-baseshote-core.md](02-baseshote-core.md) (vd `LaserShotE` thay vì `WeaponLaser`) để không lẫn 2 convention trên cùng 1 enemy.
