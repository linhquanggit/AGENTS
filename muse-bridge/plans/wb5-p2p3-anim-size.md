# WB5 P2 + P3 — cập nhật size animation theo concept art
goal: Chỉnh scale/vị trí Spine thân và Spine súng của EnemyWB5_P2_A và EnemyWB5_P3_A cho khớp concept art 3 phase (tỉ lệ súng so với thân, vị trí từng súng).
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library.md (mục 10 WB5), BR_WorldBoss/design/WB5_concept_P1-P3_guns.jpg

## T1 — Đo tỉ lệ tham chiếu từ concept + P1
owner: claude
depends: -
context:
Concept (ảnh 1280×691, 3 thân cạnh nhau, thân giữ nguyên qua 3 phase). P2: súng 4 = 2 ụ hai cánh, súng 6 = 2 cột điện phía trên, súng 5 = đĩa gai ở giữa thân. P3: súng 7 = 2 ụ cam ngoài cánh, súng 8 = tháp tròn trên giữa, súng 9 = 4 cột điện hàng ngang (chú thích chỉ 4 cột). Đo trên ảnh: bề rộng mỗi súng / bề rộng thân → tỉ lệ đích.
verified:
EnemyWB5_P1_A.prefab:389 Spine súng Animation scale 0.35 (Gun_1, guid dabd58ae) — mốc đã duyệt ở P1
EnemyWB5_P2_A.prefab:109 root EnemyWB5_P2_A scale 1.2; Spine thân WorldBoss_5 (guid 067ee807) scale 1
EnemyWB5_P3_A.prefab:708 root EnemyWB5_P3_A scale 1.2; Spine thân WorldBoss_5 scale 1
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/design/WB5_concept_P1-P3_guns.jpg, /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: (không sửa) — ghi bảng tỉ lệ đích vào báo cáo task
done_when: Có bảng: súng → tỉ lệ rộng súng/thân trên concept, tỉ lệ hiện tại trong Unity (đo bounds renderer), scale đề xuất

## T2 — Chỉnh scale + vị trí Spine súng P2
owner: claude
depends: T1
context:
Súng bám bone thân bằng BoneFollower; chỉ đổi localScale của node "Animation" (Spine) và offset nếu lệch vị trí so với concept. Không đổi tên node, không đổi bone đang follow trừ khi lệch hẳn chỗ.
verified:
EnemyWB5_P2_A.prefab: 5 Spine súng đều scale 0.35 — Gun4 ×2, Gun_6 ×2 (Gun6-R/L), Gun5 ×1
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P2_A.prefab
done_when: Screenshot Game view P2 cạnh concept: tỉ lệ + vị trí súng 4/5/6 khớp (sai lệch < ~10%); FireDirection/điểm bắn vẫn nằm đúng miệng súng; Play 1 lượt không lỗi console

## T3 — Chỉnh scale + vị trí Spine súng P3
owner: claude
depends: T1
context:
Như T2. Súng 9 = 4 khẩu Gun9 (Kiểu 1 bone20/bone19, Kiểu 2 Engine26/bone21 theo library).
verified:
EnemyWB5_P3_A.prefab: Spine súng scale 0.35 — Gun_9 ×4, Gun_7 ×2, Gun_8 ×1 (Gun_8/Gun_8/Gun_8_SkeletonData, guid 3c32cb01)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P3_A.prefab
done_when: Screenshot P3 cạnh concept khớp tỉ lệ/vị trí súng 7/8/9; điểm phóng cọc điện G9 và portal G8 vẫn đúng; Play 1 lượt không lỗi

## T4 — Kiểm thân + collider sau khi đổi
owner: claude
depends: T2, T3
context:
Thân phải chiếm ½ màn ngang × 1 màn dọc (library mục 10). Nếu đổi scale root/thân thì cập nhật 3 capsule collider + spline path theo.
verified:
P2/P3 root scale 1.2; P3 thân clone từ P2 (Rigidbody2D, 3 capsule, 2 splineMove)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: EnemyWB5_P2_A.prefab, EnemyWB5_P3_A.prefab
done_when: Thân P1/P2/P3 cùng kích thước trên màn; collider phủ thân; boss không trôi; test trong MakeAll.unity OK

## T5 — Ghi lại vào library
owner: claude
depends: T4
context: Ghi bảng scale cuối cùng của từng súng P2/P3 vào mục 10 WB5.
verified: -
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: BR_WorldBoss/library/worldboss-boss-library.md
done_when: Library có bảng scale mới, ngày cập nhật

## Câu hỏi mở
- Concept là tỉ lệ cuối hay chỉ để chỉ vị trí? Nếu chỉ vị trí thì T2/T3 chỉ chỉnh offset.
- Súng 3 (P1) chú thích chưa rõ vị trí — không thuộc task này.
- "Size animation" có bao gồm tốc độ anim (timeScale) không? Plan hiện chỉ xử lý kích thước.
