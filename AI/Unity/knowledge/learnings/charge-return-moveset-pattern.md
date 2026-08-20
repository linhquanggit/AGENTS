# Pattern moveset lao: bay hẳn ra ngoài màn rồi về thẳng (dash-out & return)
Scope: Enemies moveset (movement) — chung  |  Evidence: EnemyWorldBoss1_P3.cs IE_MoveSet1 / BuildDashPath / UpdateOffScreenCollider (làm mẫu)

Khi làm moveset "lao vào Fighter rồi về", cách gọn & không xấu = cho bay hẳn ra ngoài màn, KHÔNG cong/không warp:
1. **Lấy đà**: `DOMove` lùi ngược hướng (`-dir`) một đoạn nhỏ, `Ease.OutQuad`, chạy TRƯỚC vòng dash và KHÔNG gọi `FaceVelocity` → mũi vẫn hướng target ("co người lấy đà").
2. **Lao ra**: `DOMove` thẳng tới `pOut = origin + dir * (GamePlayController.CameraBouns.magnitude + margin)` → ra hẳn ngoài màn; `Ease.Linear`; bắn trong đoạn này.
3. **Về**: từ `pOut` `DOMove` **thẳng** về origin (không cong, không teleport). Cú quay đầu 180° xảy ra khi đang ngoài màn nên không thấy giật; đoạn về là đường thẳng nên luôn đẹp → khỏi lo "quay lại có ổn không".
4. **Bẫy off-screen**: rìa màn có volume `ColliderDestroyEnemies` → `OnTriggerDestroy` → `AutoDestroy`/ReturnToPool (Enemy.cs:311). Phải **tắt collider khi ra ngoài `CameraBouns`, bật lại khi vào trong** (có margin ~0.3 tránh re-trigger) — xem `UpdateOffScreenCollider`.
5. **Mũi**: `FaceVelocity` dùng `Quaternion.RotateTowards` (damping theo `faceTurnSpeed`), KHÔNG hard-set rotation → hết rung/giật.
6. **Tách geometry** vào `BuildDashPath()` dùng chung runtime + `OnDrawGizmos` (cần `previewTarget` Transform để xem ở edit-time; `previewBound` khi chưa có `GamePlayController`).

Đừng dùng ease đơn (InBack/CatmullRom một-nét) để "lao–lượn–về": biên độ InBack tỉ lệ quãng đường (đích off-screen → lùi quá xa); tách nhịp lùi riêng. Ease "lùi rồi lao" chuẩn là `Ease.InBack` nhưng chỉ hợp move quãng ngắn.
