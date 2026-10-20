# WB2 — collider 3 phase + G1 (OnlyAnim) P2 chưa chạy
goal: Thêm collider thân cho WB2 P1/P2/P3 để nhận hit đúng; ở P2 cho 2 anim G1-(OnlyAnim) chạy [Out Gun 1 + Close] theo quy ước WB2.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 20 quy ước anim WB2; mục 18.2 WB2); đối chiếu Unity ngày 2026-10-16

## T1 — Tạo collider thân cho 3 phase WB2
owner: claude
depends: -
context:
- Thân boss WB2 có 3 prefab phase: EnemyWB2_P1_A, EnemyWB2_P2_A, EnemyWB2_P3_A.
- Hiện không phase nào có Collider2D ở thân (chỉ có Rigidbody2D ở root P2). Drone có sẵn CircleCollider2D.
- Collider phải nằm trên thân, không đè lên súng. Súng là BoneFollower con, không có collider riêng.
- Design vùng nhận hit theo ảnh/Figma WB2 (chưa có số liệu trong project).
verified:
- EnemyWB2_P1_A.prefab: không có Collider2D nào; có G1-L1, G1-L2, G1-R1, G1-R2 (WorldBossWeapon).
- EnemyWB2_P2_A.prefab: không có Collider2D nào; root có Rigidbody2D + splineMove + EnemyWB2_P2.
- EnemyWB2_P3_A.prefab: chỉ có BoxCollider2D trên LaserShotE/Laser2D_CL (trigger, là tia laser), không có collider thân.
refs: claude/worldboss-boss-library.md (mục 18.2 WB2)
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P1_A.prefab, EnemyWB2_P2_A.prefab, EnemyWB2_P3_A.prefab
done_when: Mỗi prefab có ít nhất 1 collider thân (không trigger, layer enemy đúng như WB khác); test trong TestEverything: player bắn trúng thân boss thì nhận damage; console không có lỗi; Scene view xác nhận collider khớp thân boss.

## T2 — P2: chạy 2 anim G1-(OnlyAnim) [Out Gun 1 + Close]
owner: claude
depends: T1
context:
- Quy ước WB2 (mục 20): súng (OnlyAnim) không có WorldBossWeapon/SpineAnimFlow; SkeletonAnimation start anim Out_* loop=false, giữ frame cuối.
- P2 có 2× Out_gun1. Yêu cầu: khi vào P2 cả hai chạy "Out Gun 1" rồi "Close" (theo tên anim trong skeleton).
- Cần xác định trigger chạy anim (script phase P2 hoặc event) trước khi gán; nếu chưa rõ thì để Câu hỏi mở.
verified:
- EnemyWB2_P2_A.prefab có 2 GO trùng tên "G1-(OnlyAnim)" (BoneFollower, không WorldBossWeapon). Không có SpineAnimFlow để gọi flow.
- Mục 20 (worldboss-boss-library.md): OnlyAnim P2 = 2× Out_gun1.
refs: claude/worldboss-boss-library.md (mục 20)
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P2_A.prefab (2 GO G1-(OnlyAnim)); script phase P2 nếu cần thêm trigger
done_when: Vào P2 trong Play, cả 2 súng chạy Out Gun 1 rồi Close đúng thứ tự; không trùng tên GO (đặt tên khác nhau); screenshot xác nhận pose cuối đúng.

## Câu hỏi mở
- Collider thân: kích thước/vị trí lấy từ art nào, và layer/tag dùng chung với WB nào?
- G1-(OnlyAnim): trigger chạy anim là event nào (đổi phase, hết HP phase, hay Init P2)? Có cần Close lặp lại sau Out không?
- Hai GO G1-(OnlyAnim) trùng tên: giữ hay đổi tên để tránh nhầm khi gán anim?
