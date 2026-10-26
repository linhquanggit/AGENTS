# StepRotation quét nòng ±N° + bắn theo góc (WB2 P3 G5)

Dùng `StepRotation` (có sẵn) để quét nòng rest → min → max → rest rồi dừng, bắn theo từng viên. Thay `GunSweepAimWB2` + `SweepAimShotWB2`.

## Option opt-in thêm vào `StepRotation` (mặc định tắt, 2 prefab cũ không đổi hành vi)
- `ueReachMin` (UnityEvent): phát khi chạm min, trước khi Yoyo đảo speed. Nối tới `DirectionShotE.Attack` → viên đầu ra đúng lúc nòng ở min. Không dùng `ueStepReached`/`ueCompletedLoop` (phát cả ở điểm giữa).
- `stopAtInitAfterMax`: sau khi chạm max thì dừng khi về `initAngle` (1 lần Play = 1 chu trình). `PrepareNextStep` clamp target về init để dừng đúng kể cả khi start lệch lưới `stepAngle`.
- `initLocal`: `Start()` đặt `initAngle` theo **local** (base `SetInitAngle` set world, sai cho transform con). `Play()` chuẩn hoá góc hiện tại về cùng miền với `initAngle` (`initAngle + DeltaAngle(initAngle, localEuler.z)`).

## Bẫy
- `Play()` cũ lấy `transform.localEulerAngles` (0..360). Nếu init/min/max là số âm (vd -90/-165/-15) thì chênh 360° → nòng quay ~435° (≈8.7s ở 50°/s) mới chạm min, viên đầu trễ ~8.8s. Luôn chuẩn hoá (đã làm khi `initLocal`).
- `WorldBossWeapon.rotate` phải để **None** với kiểu này: `StopAttackOnly → StopWeapons → rotate?.Pause()` chạy ngay sau viên cuối (lúc nòng ở max), nòng sẽ đứng ở max thay vì về rest.
- `BaseShotE.waitFireRate` chỉ tạo trong `Init()` (và chỉ khi `fireRate>0 && waves>1`). Weapon không nằm trong `EnemyWorldBoss.weapons` thì `Init` không chạy → `waitFireRate` null → `yield return null` mỗi wave (bắn mỗi frame). Thêm weapon vào list `weapons` của prefab. `P3.OnAttack` không gọi `base.OnAttack` nên list này không tự Play weapon.
- Công thức: `fireRate = (khoảng quét / |speed|) / (N-1)`; viên đầu ở min, viên cuối ở max.
- Nòng art Spine: đo trục bằng cách xoay `Rotate` và chụp Scene view; `FireDirection.up` phải trùng hướng nòng (local rot z = -90 khi nòng là +X local) và nằm ở đầu nòng.

## Test nhanh WB trong Play
Menu → chờ → đóng popup → `Manager.Instance.LoadTestScene()` (nạp GamePlay + TestEverything additive + `GamePlayController.OnStartGame`) → `TestManager.SpawnEnemy(2, 0)` (WB2_P3). KHÔNG `SceneManager.LoadScene("TestEverything")` trực tiếp: thiếu `GamePlayController`/camera/`MyPoolManager` → NullReference mỗi lần pool Instantiate (`Characters.OnScaleInit`), hàng trăm lỗi.
