# WB5 P2 G6 — cọc điện giãn ra rồi bay song song hết màn, giữ link
goal: 2 cọc G6-ElectricLink bay thẳng qua hết màn hình không dừng/biến mất giữa chừng, dây link nối suốt tới khi cọc ra khỏi màn; 2 cọc bay song song theo hướng chụp vị trí player 1 lần, giãn khoảng cách trong linkDelay rồi giữ cố định.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 P2, Súng 6)

## T1 — Quỹ đạo mới: chụp player 1 lần → giãn trong linkDelay → bay song song
owner: muse
depends: -
context:
Đã chốt với Linh:
- Vị trí player chỉ chụp 1 lần lúc bắn → hướng chung mainDir = normalize(playerPos − tâm 2 bệ). Không bám player sau đó.
- 2 cọc bay theo mainDir; trong khoảng linkDelay khoảng cách ngang (vuông góc mainDir) giữa 2 cọc giãn từ khoảng cách 2 bệ → linkWidth. Hết linkDelay: cả 2 bay song song đúng mainDir, khoảng cách giữ cố định = linkWidth.
- Link bật sau linkDelay (đúng lúc giãn xong) và giữ tới khi 1 cọc không còn active (ra khỏi màn, isDestroyOutCamera tự hủy) → StopAttack → DespawnVolley → wave sau/OnShotFinish. Không cắt bằng linkDuration nữa.
- Thêm [SerializeField] float linkWidth (khoảng cách cuối, world unit; default 3.5 (Linh chốt; cũ ≈ 2.8); màn rộng ~5.625 unit (ortho 5, 9:16)), [SerializeField] bool linkUntilOutScreen = true. targetOffsetScreenX / clampInScreen / BuildTargets không còn dùng cho hướng bay (giữ field, không xoá — tránh mất serialize; có thể dùng clamp để dời tâm cụm khỏi mép nếu cần).
Gợi ý cách làm đơn giản: lúc bắn cho mỗi cọc hướng lệch ra ngoài: dir_i = normalize(mainDir*speed*linkDelay + side_i*(linkWidth − gap0)/2) (side_i = ±perp(mainDir)); sau linkDelay đặt lại direction = mainDir cho cả 2. Bullet có thể quá linkDelay chút do frame → sau linkDelay snap vị trí ngang về đúng ±linkWidth/2 quanh trục chung.
verified:
Bullet.cs:37 protected Vector3 direction; :153 Init gán direction = _dir; :248 di chuyển newPos = direction*speed*Time.deltaTime → cần thêm public setter (vd SetDirection(Vector3)) trong BulletDestinationWB5 để đổi hướng giữa chừng
BulletDestinationWB5.cs (Assets/Project/Bullets/Scripts):34 stopAtTarget=false (bay thẳng, ra màn tự hủy)
DestinationLinkShotWB5.cs:24 linkDelay 0.5, :25 linkDuration 1; :167 IE_SpawnBullet hiện: spawn → wait linkDelay → link → wait linkDuration → StopAttack → DespawnVolley (hủy cọc giữa màn — nguyên nhân "dừng lại"); :226 SpawnLinkBullet(_targetPos, _fireDir) tính _dir = target − FireDirection.Position
EnemyWB5_P2_A.prefab:20941 bulletSpeed 8, bulletTimeLive 10; :20971 linkDelay 0.5; :20958 targetOffsetScreenX ±0.25
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: DestinationLinkShotWB5.cs, BulletDestinationWB5.cs
done_when: compile 0 error; Play + log mỗi 0.1s khoảng cách 2 cọc: tăng dần trong linkDelay, ≈ linkWidth sau đó và không đổi; 2 cọc song song, bay hết màn; link bật lúc giãn xong và tắt khi cọc ra màn; player di chuyển sau khi bắn không làm đổi hướng; Console 0 error

## T2 — Chỉnh thông số trong prefab
owner: claude
depends: T1
context:
Đặt linkWidth = 3.5 (Linh chốt), linkUntilOutScreen = true trên DestinationLinkShotWB5 của G6-ElectricLink. Kiểm bulletTimeLive 10 đủ để cọc speed 8 ra hết màn.
verified:
EnemyWB5_P2_A.prefab (Assets/Project/WorldBoss/Addressable/Enemies/Prefabs) chứa DestinationLinkShotWB5 (dòng ~20941–20973)
files: EnemyWB5_P2_A.prefab
done_when: prefab lưu giá trị mới; Play scene test thấy 2 cọc giãn rồi song song, dây nối tới mép màn; Console 0 error

## Câu hỏi mở
- Tâm hướng bắn: tâm 2 bệ hay tâm boss? Mặc định tâm 2 bệ.
