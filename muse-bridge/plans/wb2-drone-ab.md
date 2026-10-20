# WB2 Drone – xuất hiện từ xương A (mép màn) bay về xương B (điểm spawn)
goal: Drone WB2 chạy anim In xuất hiện ở mép màn (bone A) bay theo path tới điểm spawn (bone B), rồi Idle tại B. Không đổi skeleton data, chỉ điều khiển bone A/B qua SkeletonUtilityBone Override.
project: ~/wkspaces/WB
source: Project 1941 — claude/wb2-drone-bone-AB-retarget.md

## T1 — EnemyWB2_Drone: đặt bone A ra mép màn khi Setup
owner: muse
depends: -
context:
Drone_SkeletonData (Spine 4.1.24): root → A (setup 5.57,0.08 Unity units), B (~0,0). Path constraint A2: slot Oatch@A, bone "Than Drone2", Percent, Tangent, offsetRot 180. Path weight: đầu=100% A, cuối=100% B. Anim In 2.267s (path 0→1 từ 0.333s→2.0s), Idle chỉ animate nhánh Base (con của B).
Prefab đã có SkeletonUtility + SkeletonUtilityBone A và B, mode Override (Position) → code chỉ cần set transform.position của GameObject A.
Yêu cầu: thêm [SerializeField] Transform boneA; [SerializeField, Range(0,1.5f)] float appearEdgeX = 1.1f (TEMP). Trong Setup() sau transform.position = _position: side = _position.x < 0 ? -1 : 1; boneA.position = new Vector3(side * CameraBouns.x * appearEdgeX, _position.y, boneA.position.z). Null-safe (boneA null hoặc GamePlayController.Instance null → bỏ qua).
Hướng thân drone theo tangent path: nếu A nằm bên trái B (side < 0) thì drone có thể lộn → set skeletonAnim.Skeleton.ScaleX = side>0 ? 1 : -1 TRƯỚC khi đặt boneA (SkeletonUtility tự tính theo flip). Kết quả cần kiểm ở T3.
verified:
EnemyWB2_Drone.cs:13 public class EnemyWB2_Drone : Enemy
EnemyWB2_Drone.cs:41 public virtual void Setup(Vector3 _position, BigDouble _damage, Action<EnemyWB2_Drone> _onDie)
EnemyWB2_Drone.cs:61-66 AppearThenFire: SetAnimation(0,"In",false) + AddAnimation(0,"Idle",true,0); laser bắn sau In
SummonDroneWB2.cs:85-87 GetFromPool(dronePrefab, _point.position) rồi _drone.Setup(_point.position, ...)
SummonDroneWB2.cs:52-57 spawnPoints mặc định x = ±CameraBouns.x*boundSpawnX(0.9), y=0
GamePlayController.Instance.CameraBouns (dùng .x) — ví dụ Bullet.cs:54
EnemyWB2_Drone.prefab (Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/): SkeletonUtilityBone boneName A mode 1 (line ~79), B mode 1 (line ~5547), root mode 0; SkeletonAnimation _animationName Idle loop 1
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_Drone.cs
done_when: compile 0 error; không đổi tên field/serialize có sẵn

## T2 — Gán boneA trên prefab
owner: claude
depends: T1
context:
Kéo GameObject Animation/SkeletonUtility-SkeletonRoot/root/A vào field boneA của EnemyWB2_Drone trên prefab, save prefab.
verified:
Hierarchy prefab: EnemyWB2_Drone/Animation/SkeletonUtility-SkeletonRoot/root/{A,B}, LaserPivot
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_Drone.prefab
done_when: field boneA khác null trong prefab, prefab save không lỗi

## T3 — Test trong Play mode, chụp màn hình
owner: claude
depends: T2
context:
Chạy WB2 tới phase có SummonDroneWB2 (P2 S4) hoặc đặt tạm drone vào scene test gọi Setup ở x<0 và x>0. Chụp 3 thời điểm: t≈0.2s (drone ở mép màn), t≈1.2s (đang bay), t≈2.5s (Idle tại spawn point). Kiểm cả drone trái và phải: không bị lộn ngược, đích đúng spawn point, laser bắn sau khi In xong.
files: (không sửa file; nếu chỉnh appearEdgeX thì sửa trên prefab)
done_when: 2 bên đều bay từ mép vào đúng điểm spawn, console 0 error, ảnh chụp gửi Linh

## Câu hỏi mở
- Trong lúc In (≈2s) collider drone đang bật (Setup:47) → player bắn trúng drone lúc đang bay vào được. Có cần tắt collider tới khi In xong không?
- appearEdgeX = 1.1 (ngoài mép màn) là TEMP — design muốn xuất hiện trong hay ngoài màn?
- RotationAimFighter có đặt trên root drone không? Nếu nó xoay root thì vị trí A cũng bị xoay theo → cần kiểm ở T3 (chưa dò được GameObject chứa nó).
- Khoảng cách A–B thực tế (~CameraBouns.x*0.2 nếu spawn ở 0.9) khác setup 5.57 → đường cong có thể bị bẹt, cần animator xem ở T3.
