# Bullet UbhBullet: không gắn Rigidbody2D Dynamic/Gravity trên prefab

Scope: bullet prefab dùng script kế thừa `UbhBullet` (Assets/Project/Bullets/Prefabs, Assets/Project/*/Enemies/Prefabs/Bullets)  |  Evidence: `BulletWB4_P2_G2.prefab` (từng có `Rigidbody2D` `m_BodyType: 0` + `m_GravityScale: 1`, gây quỹ đạo vòng cung thay vì thẳng) vs `UbhBullet_Normal.prefab` (không có `Rigidbody2D`, convention chuẩn)

`UbhBullet` di chuyển hoàn toàn qua script (`transform.position +=` mỗi frame trong `UpdateMove`). Nếu prefab gắn thêm `Rigidbody2D` kiểu Dynamic + `GravityScale > 0`, vật lý Unity (FixedUpdate) kéo object xuống song song với script set position trực tiếp → quỹ đạo cong ngoài ý muốn. Convention đúng: chỉ cần `Collider2D` (`m_IsTrigger: 1`) trên bullet — trigger detection vẫn hoạt động nhờ `Rigidbody2D` phía target (player/enemy); không cần Rigidbody2D trên bullet.
