# 01 — UniBulletHell built-in ShotPattern (28 class)

Thư mục: `Assets/UniBulletHell/Script/ShotPattern/`. Tất cả kế thừa `UbhBaseShot` (cùng thư mục) — base lo chung: số lượng đạn (`m_bulletNum`), tốc độ (`m_bulletSpeed`), lifetime (`bulletTimeToLive`), gia tốc (`m_accelerationSpeed`), clamp min/max speed (`m_useMinSpeed/m_useMaxSpeed`), event `OnFiredBulletEvent`, và vòng lặp `Fire()`/coroutine mà mọi subclass override.

**Không có damage/cooldown/wave** theo convention project — chỉ lo hình học bắn. Muốn dùng cho enemy/boss thật phải bọc qua `UbhShotE` (xem [00-overview.md](00-overview.md) mục cơ chế lõi).

| Class | Hành vi |
|---|---|
| `UbhLinearShot` | Bắn từng viên một theo đường thẳng, góc `m_angle` cố định, cách nhau `m_betweenDelay`. |
| `UbhLinearLockOnShot` | `UbhLinearShot` + tự khoá góc bắn vào target (tag/gần nhất/ngẫu nhiên). |
| `UbhNwayShot` | Fan N-way kinh điển: `m_wayNum` viên trải trên `m_betweenAngle` quanh `m_centerAngle`. |
| `UbhNwayLockOnShot` | N-way fan có tâm khoá vào target. |
| `UbhNwayLockOnAngleShot` | N-way lock-on nhưng cả fan liên tục re-aim (không chỉ khoá 1 lần). |
| `UbhSpreadNwayShot` | N-way fan, mỗi viên trong fan có tốc độ khác nhau (`m_diffSpeed`) → đạn giãn ra theo thời gian/khoảng cách. |
| `UbhSpreadNwayLockOnShot` | Spread N-way + lock-on. |
| `UbhOverTakeNwayShot` | N-way fan, viên bắn sau nhanh hơn viên trước (`m_diffSpeed`, `m_shiftAngle`) → các line "vượt" nhau. |
| `UbhOverTakeNwayLockOnShot` | Overtake N-way + lock-on. |
| `UbhWavingNwayShot` | N-way fan có tâm dao động hình sin (`m_waveRangeSize`, `m_waveSpeed`) — fan quét qua lại. |
| `UbhWavingNwayLockOnShot` | Waving N-way + lock-on. |
| `UbhSinWaveBulletNwayShot` | N-way fan, mỗi viên đạn tự bay theo đường sin (`m_waveRangeSize/m_waveSpeed/m_waveInverse`). |
| `UbhSinWaveBulletNwayLockOnShot` | Sin-wave-bullet N-way + lock-on. |
| `UbhCircleShot` | Bắn 1 vòng tròn 360° cùng lúc. |
| `UbhHoleCircleShot` | Vòng tròn đầy nhưng có 1 khoảng hở (`m_holeSize` tại `m_holeCenterAngle`). |
| `UbhHoleCircleLockOnShot` | Hole-circle có khoảng hở hướng vào/tránh target. |
| `UbhSpiralShot` | Xoắn ốc 1 line: bắn 1 viên mỗi tick, góc xoay liên tục (`m_startAngle`, `m_shiftAngle`, `m_betweenDelay`). |
| `UbhSpiralNwayShot` | Fan N-way xoay mỗi tick → xoắn ốc nhiều nhánh. |
| `UbhSpiralMultiShot` | Nhiều nhánh xoắn ốc đơn (`m_spiralWayNum`) xoay đồng thời. |
| `UbhSpiralMultiNwayShot` | Multi-arm spiral × N-way mỗi nhánh (`m_spiralWayNum` × `m_wayNum`) — hoa văn xoay dày đặc. |
| `UbhRandomShot` | Bắn góc ngẫu nhiên trong khoảng (`m_randomCenterAngle`, `m_randomRangeSize`), tốc độ/delay ngẫu nhiên mỗi viên. |
| `UbhRandomLockOnShot` | Random-angle nhưng tâm khoá vào target. |
| `UbhRandomSpiralShot` | Spiral có thêm nhiễu ngẫu nhiên góc (`m_randomRangeSize`) + tốc độ/delay ngẫu nhiên. |
| `UbhRandomSpiralMultiShot` | Multi-arm của random spiral (`m_spiralWayNum` nhánh có nhiễu). |
| `UbhPaintShot` | "Tô" 1 cung góc rộng theo từng line (`m_paintCenterAngle`, `m_betweenAngle`, `m_nextLineDelay`) — lấp đầy cung theo thời gian. |
| `UbhPaintLockOnShot` | Paint sweep có tâm khoá vào target. |
| `UbhHomingShot` | Đạn tự lái liên tục hướng về target sau khi bắn (`m_homingAngleSpeed`) — khác lock-on (chỉ nhắm 1 lần lúc bắn). |

## Thư mục liên quan (không phải shot pattern, nhưng hay dùng kèm)

- `Assets/UniBulletHell/Script/Bullet/` — hành vi *viên đạn sau khi bắn* (không phải cách bắn): `UbhBullet` (base), `UbhBulletSimpleSprite2d`, `UbhBulletSimpleModel3d`, `UbhBulletStoppable`, `UbhTentacleBullet`, `BulletWeapon10_1`.
- `Assets/UniBulletHell/Script/Controller/UbhShotCtrl.cs` — component driver enemy gắn vào: giữ list shot-pattern, fire rate/loop/delay, start/finish `UnityEvent`. Được compose vào project qua `UbhShotE` (xem [00-overview.md](00-overview.md)), không subclass trực tiếp.
- `Assets/UniBulletHell/readme.txt` — quick-start duy nhất của asset (thêm `Shot Controller`, chọn prefab trong `[UniBulletHell]>[Example]>[Prefab]>[ShotPattern]`); 4 scene ví dụ `UBH_ShotShowcase`, `UBH_ShotShowcase3D` (demo cả 56 biến thể tham số của các class trên), `UBH_GameExample`, `UBH_Tutorial`.
