# 02 — `BaseShotE` framework: template dùng thật cho enemy/boss (ưu tiên check đầu tiên)

Thư mục: `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/`. Đây là hệ tự viết của project — mọi enemy/boss thật đang chạy qua đây (không phải hệ UniBulletHell thuần ở [01-unibullethell-base.md](01-unibullethell-base.md)).

`BaseShotE` (abstract, `BaseShot/BaseShotE.cs`) — field chung `damageType/damageFactor/cooldown/delayStart/waves/fireRate/shotFX` + event `ueStartShot/ueOnShot/ueShotFinish/ueEndShot`; loop chuẩn `DelayStart → StartShot → DelayShot → Shot (spawn bullet, lặp FireRate) → DelayEnd → EndShot → Cooldown`. Class con chỉ cần implement `IE_SpawnBullet()`.

| Class | Hành vi |
|---|---|
| `StraightShotE` | Bắn thẳng về phía trước, có speed/accel/clamp min-max speed + cảnh báo tấn công tuỳ chọn. |
| `DirectionShotE` | Bắn theo danh sách `fireDirections` cấu hình sẵn (nhiều hướng nhắm), mỗi hướng có delay riêng qua `directionFireRate`. |
| `LinearDirectionShotE` | Biến thể `DirectionShotE`, thứ tự hướng bắn tuyến tính (không random). |
| `RandomDirectionShotE` | `DirectionShotE` + random hướng/số hướng bắn mỗi wave. |
| `RandomShotE` | Biến thể `DirectionShotE` bắn góc ngẫu nhiên trong khoảng min-max mỗi wave. |
| `SpiralShotE` | Biến thể `DirectionShotE` xoay góc bắn mỗi wave — pattern xoắn ốc. |
| `ZigZagShotE` | Đạn bay theo đường zig-zag (field speed/accel/lifetime + `IE_SpawnBullet` riêng). |
| `BezierShotE` | Đạn bay theo đường cong bezier hướng tới/tránh fighter target (`isTargetFighter`, accel). |
| `DestinationShotE` | Đạn bay tới 1 điểm đích cố định thay vì theo hướng/target. |
| `MultiplySpeedShotE` | Bắn thẳng nhưng tốc độ nhân/tăng dần trong khoảng min-max mỗi wave. |
| `LineBurstShotE` | Burst nhiều viên liên tiếp theo 1 đường thẳng. |
| `RandomSafeZoneShotE` | Cảnh báo (telegraph) rồi spawn vùng an toàn/nguy hiểm tại vị trí ngẫu nhiên trên màn hình (hỗ trợ padding/carrier prefab). |
| `BombShotE` | Spawn bomb, nổ sau `delayExplode`/`timeLive`. |
| `CircleLaserShotE` | Laser hình tròn, gây damage theo tick rate (`damageTickRate`). |
| `LaserShotE` | Laser thẳng, có telegraph cảnh báo (`useWarning`/`timeWarning`) + audio. |
| `LaserCreatorE` | Subclass `LaserShotE`, spawn/tạo instance laser thay vì tự bắn trực tiếp. |
| `LinkLaserShotE` | Laser nối/xích giữa các điểm, tuỳ chọn di chuyển root target (`useMoveRootTarget`, link lặp). |
| `LauncherShotE` | Bệ phóng xoay rồi bắn (`rotateSpeed` + speed/lifetime/accel đạn). |
| `LauncherCtrlBulletShotE` | Biến thể `LauncherShotE` có thêm hành vi điều khiển đạn sau khi bắn. |
| `FireBreathShotE` | Luồng lửa liên tục, có telegraph cảnh báo tuỳ chọn. |
| `FlameShot_RP` | Luồng lửa (biến thể render-pipeline riêng, tương tự FireBreath). |
| `CreateArmorShotE` | Spawn/cấp giáp/khiên thay vì đạn gây damage (dạng "shot" hỗ trợ, không tấn công). |
| `DropGiftShotE` | Spawn quà rơi (speed/lifetime/accel, tuỳ chọn load qua Addressables). |
| `DropRewardsShotE` | Spawn số lượng reward ngẫu nhiên (`countMin`-`countMax`). |
| `SummonShotE` | Triệu hồi enemy con tại vị trí cấu hình (enum `SUMMON_POS_TYPE`), có chỉnh khi tràn màn hình. |
| `SummonShotELockOn` | Biến thể `SummonShotE`, unit triệu hồi khoá vào target. |
| `UbhShotE` | **Cầu nối sang hệ UniBulletHell** — bọc `UbhShotCtrl` để expose API `Init/Attack/AutoAttack/ChangeCD` giống các `BaseShotE` khác. Dùng khi cần hình học ở [01-unibullethell-base.md](01-unibullethell-base.md) nhưng vẫn theo convention damage/cooldown của project. |
| `ControllerShotE` | Không tự là 1 shot — meta-controller giữ list `InfoShotE` (nhiều `BaseShotE`) + enum `SHOT_MODE` (`LINEAR/SHUFFLE/RANDOM/ALL`) để 1 enemy luân phiên/chọn giữa nhiều template. |
| `Shooter` | Wrapper mỏng: giữ tham chiếu 1 `BaseShotE` + damage/cooldown, trigger bắn. |

## Khi nào tái sử dụng trực tiếp vs khi nào cần class mới

- Khớp hành vi lõi (hướng bắn / hình học / hazard type) với 1 class ở trên → **kế thừa hoặc tham số hoá lại class đó**, đừng viết `BaseShotE` mới.
- Cần pattern hình học phức tạp hơn (N-way, spiral nhiều nhánh, waving, overtake...) mà không có ở đây → xem [01-unibullethell-base.md](01-unibullethell-base.md) trước, bọc qua `UbhShotE`.
- Chỉ nên viết `BaseShotE` con hoàn toàn mới khi hành vi *spawn bullet* khác biệt >50% so với tất cả class trên.
