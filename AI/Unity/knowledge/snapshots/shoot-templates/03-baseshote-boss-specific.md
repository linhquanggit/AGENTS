# 03 — Boss-specific one-off shot (SpecialCase)

Thư mục: `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/`. Đây là các subclass gắn cứng cho **đúng 1 boss/mini-boss**, đặt tên theo convention `<ShotType><BossId>` (vd `BombShotBoss14` = biến thể `BombShotE` chỉ dùng cho Boss14).

**Không tái sử dụng trực tiếp** — mỗi class ở đây đã tuỳ biến theo id/behavior riêng của 1 boss cụ thể. Khi cần 1 boss mới có hành vi tương tự, coi đây là *ví dụ cách tuỳ biến*, copy convention đặt tên + cấu trúc, không import thẳng class cũ vào boss mới.

| Class | Base suy ra từ tên | Ghi chú |
|---|---|---|
| `BombShotBoss14`, `BombShotBoss19`, `BombShotEM15`, `BombShotMB42` | `BombShotE` | Biến thể bomb riêng theo boss/elite-mob id. |
| `ControllerShotBoss16`, `ControllerShotBoss17` | `ControllerShotE` | Cấu hình luân phiên nhiều shot riêng cho Boss16/17. |
| `DirectionShotBoss20` | `DirectionShotE` | Bắn hướng riêng cho Boss20. |
| `EmptyShotE` | `BaseShotE` | No-op placeholder — dùng khi 1 slot shot cần tồn tại nhưng không bắn gì. |
| `LaserShotBoss20` | `LaserShotE` | Laser riêng Boss20. |
| `LauncherRandomShotE`, `LauncherShotMB18` | `LauncherShotE` | Biến thể launcher random / riêng MB18. |
| `LinkLaserShotWB1`, `LinkLaserWB1_CL` | `LinkLaserShotE` | Laser xích riêng cho World Boss 1 (WB1). |
| `MultiSummonShotE` | `SummonShotE` | Triệu hồi nhiều đợt/nhiều loại cùng lúc. |
| `PoisonousMistShotE` | `BaseShotE` | Hazard sương độc — không thuộc nhóm bomb/laser/summon chuẩn. |
| `RandomRotateShotE` | `DirectionShotE`/`SpiralShotE` | Xoay góc bắn ngẫu nhiên. |
| `SpawnFragmentWB1` | `BaseShotE` | Spawn mảnh vỡ riêng WB1. |
| `SummonB2`, `SummonB2AutoTarget`, `SummonB2_2` | `SummonShotE` | Triệu hồi riêng Boss2 (3 biến thể: thường / auto-target / phiên bản 2). |
| `SummonBoss20Weapon` | `SummonShotE` | Triệu hồi vũ khí riêng Boss20. |
| `SummonDroneWB1`, `SummonDroneWB3` | `SummonShotE` | Triệu hồi drone riêng World Boss 1/3. |
| `SummonShotMB23` | `SummonShotE` | Triệu hồi riêng MB23. |
| `SummonTT`, `SummonTTBoss21` | `SummonShotE` | Triệu hồi riêng "TT" / Boss21. |
| `WeaponClearBullet` | (utility) | Tiện ích xoá đạn trên màn hình — không phải shot pattern. |

## Building block dùng chung bởi các shot ở trên (không phải template độc lập)

Thư mục `BaseShot/ShotElement/`: `Flame_RP`, `LaserE`, `Laser_CL`, `Laser_CP`, `LinkLaser_CL`, `LinkLaser_DPS`, `PoisonousMist`, `SafeZoneBullet`, `HealthArmor`... — các component hazard được các shot ở [02-baseshote-core.md](02-baseshote-core.md) và file này gọi tới, tự chúng không phải là 1 shot pattern để chọn.

## Usage sites (không liệt kê full — xem [00-overview.md](00-overview.md))

Mỗi boss có 1 file consumer riêng (`EnemyBoss7.cs`, `EnemyMB4.cs`...) cấu hình/compose các template ở đây — không phải template, không cần catalog hoá từng file.
