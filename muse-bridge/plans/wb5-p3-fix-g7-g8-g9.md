# WB5 P3 EnemyWB5_P3_A — sửa Gun7, Gun8, Gun9
goal: Sửa đạn Gun7 theo hình WB4 P1 Gun3 (giữ tốc độ); Gun8 mở liên tục, đạn bắn ra tại các điểm random trong hình elipse (lõi đen của portal), số điểm = số wave đạn, tốc độ đạn x1.5, khoảng cách giữa các lần như cũ; Gun9 (S1, S2) vẽ 2 đường link cách đều tâm.
project: /Users/mobione/wkspaces/WB
source: Linh (chat 2026-10-14) — yêu cầu trực tiếp trên EnemyWB5_P3_A; library claude/worldboss-boss-library.md mục 10 P3, 18.5

## T1 — Gun7 (FanFall-L/R): đổi hình đạn theo WB4 P1 Gun3, giữ tốc độ
owner: claude
depends: -
context:
- G7-FanFall-L và G7-FanFall-R dùng FanFallShotWB5, bắn BulletWB5_P3_G7 (BulletStopAndFall).
- Yêu cầu: chỉ clone visual từ Gun3 WB4 P1. Giữ nguyên tốc độ hiện tại (không đổi speed, gravity, max).
- Giữ prefab đạn riêng cho P3.
verified:
- Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB5_P3_G7.prefab (đạn đang dùng)
- Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB4_P1_G3.prefab (hình nguồn)
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/FanFallShotWB5.cs:21 IE_SpawnBullet, :33 SpawnBullet(cacheDir.FireDirection)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10 P3, G7)
files: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB5_P3_G7.prefab
done_when: Đạn G7 trong Play có hình giống WB4 P1 G3, tốc độ rơi giữ như cũ, không lỗi Console.

## T2 — Gun8 (Portal): mở liên tục, đạn bắn từ điểm random trong elipse, tốc độ x1.5
owner: muse
depends: -
context:
- G8-Portal dùng PortalShotWB5. Hiện: cd 3s, mỗi portal bắn 12 mũi tên × 0.4s, speed 5, sống 5s, tối đa 2 portal.
- Mở portal liên tục (không chờ đóng hết).
- Vùng spawn đạn: hình elipse lấy từ FX Portal_War_1941 (vùng lõi đen của effect, ref trực tiếp từ prefab này trong G8-Portal), đạn bắn ra tại các điểm random trong elipse. Vẽ gizmo elipse theo đúng vùng đó. Số điểm random = số wave đạn (hiện là 12 mũi tên/lần, tức 12 điểm).
- Tốc độ đạn = 1.5 × tốc độ hiện tại (5 → 7.5).
- Khoảng cách giữa các lần bắn: giữ như cũ (0.4s giữa các wave).
- Bán kính/trục elipse: để Linh chỉnh trong Inspector, tạm đặt bằng kích thước lõi đen; ghi là số tạm.
verified:
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/PortalShotWB5.cs:87 Init, :96 StopAttack, :119 StopPortalShots, :125 IE_SpawnBullet, :178 IE_PortalLife, :228 OnDrawGizmosSelected (có sẵn gizmo, thêm elipse vào đây)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10 P3, G8)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/PortalShotWB5.cs
refs_fx: Assets/Effect_1945/Prefab/effect_WorldBoss/Portal_War_1941.prefab (ref trong G8-Portal, vùng elipse lấy từ đây)
done_when: Trong Play portal mở liên tục, mỗi lần bắn 12 điểm random trong elipse, đạn nhanh gấp 1.5, nhịp 0.4s giữa các wave, gizmo hiện elipse, không lỗi Console.

## T3 — Portal_War_1941: scale dần rồi spawn portal, tắt sau khi spawn
owner: claude
depends: T2
context:
- Portal_War_1941 là object con của G8-Portal => Animation.
- Luồng đúng:
  1. Lần đầu vào: mở Animation clip open của Portal_War_1941 một lần, rồi giữ trạng thái open (keep open), không mở lại.
  2. Mỗi lần cần spawn portal: scale dần object Portal_War_1941 từ 0 lên đúng size hiện tại. Khi đạt size thì spawn portal.
  3. Sau khi spawn portal thì tắt object Portal_War_1941.
  4. Lần sau cần spawn portal: lặp lại bước 2 và 3 (scale dần, spawn, tắt).
- Không đổi thời lượng anim hiện có, không tự thêm số mới. Thời lượng scale dần là số tạm, Linh chỉnh sau.
verified:
- Assets/Effect_1945/Prefab/effect_WorldBoss/Portal_War_1941.prefab (m_Name Portal_War_1941 tại dòng 4827)
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/PortalShotWB5.cs:178 IE_PortalLife (điểm spawn portal, nơi thêm bước scale và tắt)
files: Assets/Effect_1945/Prefab/effect_WorldBoss/Portal_War_1941.prefab; Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/PortalShotWB5.cs; AnimationClip open của effect (nếu có)
done_when: Trong Play: lần đầu open một lần rồi giữ; mỗi lần spawn portal thì Portal_War_1941 scale từ 0 lên size, spawn portal xong thì tắt; không lỗi Console.

## T4 — Gun9 (S1, S2): 2 đường link cách đều tâm, laser chạy dần cho thấy việc nối
owner: muse
depends: -
context:
- Laser hiện tại nối tức thì, không thấy quá trình. Yêu cầu: làm chậm để nhìn thấy laser chạy từ quả này tới quả kia (animate đầu laser chạy dần). Tạm 0.6s mỗi đường, Linh chỉnh sau.
- G9-Orbit-S1 (Kiểu 1) và G9-Orbit-S2 (Kiểu 2) dùng ElectricOrbitShotWB5, mỗi cặp có 2 quả và LinkLaser2D_CL.
- Mỗi cặp vẽ 2 đường link (A→B và B→A), cùng nối 2 quả đối diện.
- Hai đường cách đều tâm của cặp để nhìn hợp lý.
- Áp dụng cho cả S1 và S2.
verified:
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/ElectricOrbitShotWB5.cs:14 enum MODE, :42 mode, :204 LaunchPair, :228 DespawnPair, :269 IE_SpawnBullet
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/ElectricOrbitShotWB5.cs
done_when: Trong Play mỗi cặp có 2 đường link đối xứng qua tâm, luôn nối đúng 2 quả, không lỗi Console.

## T5 — Test tổng thể EnemyWB5_P3_A trong Play
owner: claude
depends: T1, T2, T3, T4
context:
- Vào Play ở WB-Shooter. Không lưu MakeAll.unity nếu Linh chưa đồng ý.
- Đọc Console sau khi test, không còn lỗi.
files: MakeAll.unity (chỉ đọc)
done_when: Console 0 error; báo kết quả từng Gun7, Gun8, Gun9.

## Câu hỏi mở
- Gun8: khoảng cách giữa các lần mở portal khi mở liên tục (chưa có số; đang giữ cd 3s làm tạm).
- Gun9: tốc độ chạy của laser (0.6s mỗi đường là số tạm, Linh chốt sau khi xem).
