# WB4 P3 — Dây xích G7 chạy như LinkLaser
goal: LineRenderer của giáp G7 (texture xích bánh xe) phải chạy texture dọc theo dây giống LinkLaser_CL. Mắt xích giữ đúng tỉ lệ, không bị kéo giãn khi dây dài ra.
project: ~/wkspaces/WB
source: Project 1941 — dò trực tiếp prefab/script/material

## T1 — Component cuộn UV cho LineRenderer
owner: muse
depends: -
context:
Làm giống LinkLaser_CL: mỗi frame gọi `lineRender.material.SetTextureOffset("_MainTex", new Vector2(Time.time * UVTime + lineOffset, 0f))`. Trong đó lineOffset = Random.Range(0, 5f) lấy khi OnEnable, để các dây không chạy cùng pha.
Viết component riêng `LineUVScroll` (RequireComponent LineRenderer), field: `float UVTime = -1f`, `bool randomOffset = true`. Không sửa DashReturnShotE/DashHoldShakeReturnShotE (đang skip plan offset). Chỉ cuộn khi lineRenderer.enabled, vì DashHoldShakeReturnShotE tự bật/tắt linkLine. Lấy material instance một lần ở Awake (`.material`) và cache lại, đừng gọi `.material` mỗi frame.
verified:
LinkLaser_CL.cs:13-14 `bool isAnimateUV = true; float UVTime = -1f`; :32 lineOffset = Random.Range(0, 5f); :120-121 SetTextureOffset("_MainTex", Time.time * UVTime + lineOffset)
DashHoldShakeReturnShotE.cs:17 linkLine.enabled = true khi dash, :36 enabled = false khi xong
refs: -
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-4/LineUVScroll.cs (mới)
done_when: compile 0 error.

## T2 — Gắn vào G7-L/M/R và đổi LineRenderer sang Tile
owner: claude
depends: T1
context:
LineRenderer đang để textureMode Stretch (0) nên xích bị giãn theo độ dài và cuộn trông không ra mắt xích. Đổi textureMode = Tile (1) cho cả 3 dây rồi gắn LineUVScroll. Chỉnh `textureScale.x` (hoặc độ rộng dây) sao cho mắt xích đúng tỉ lệ. Chỉnh dấu/độ lớn UVTime để xích chạy đúng chiều mong muốn (mặc định -1, như LinkLaser).
verified:
EnemyWB4_P3_A.prefab LineRenderer: G7-M fid 3808507789531353224, G7-L fid 3808507788804616467, G7-R fid 3808507787623170436. Cả 3 textureMode 0, useWorldSpace 1, material guid 3872c425b35aa49fcb61f462398c49fb
Material: Assets/FORGE3D/Sci-Fi Effects/Effects/Plasma Beam/Materials/Trail_rail_WB4.mat, shader Assets/Effect_1945/Shader/Mobile-Particle-Alpha.shader, _MainTex = Chain_Trail_Tex.png (import wrapU 0 = Repeat, cuộn được). Material này chỉ dùng trong EnemyWB4_P3_A.prefab.
G7-L có sẵn LinkLaser_CL (isAnimateUV 1, UVTime -1) nhưng không phải nó vẽ dây giáp. linkLine do DashHoldShakeReturnShotE điều khiển.
refs: -
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P3_A.prefab
done_when: Play scene test WB4 P3 (Assets/Project/[GD-Khoa]/TestEnemyNode/WB/WB4_P3.prefab): khi giáp đẩy xuống, giữ, rung, kéo về, xích chạy liên tục dọc dây, mắt xích không giãn. Console 0 error.

## Đã chốt (Linh)
- Xích chạy 1 chiều cố định, không đổi chiều khi kéo về.
- Áp dụng cả 3 dây G7-L, G7-M, G7-R.
