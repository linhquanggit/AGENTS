# Test tự tạo FX muzzle — 2 phiên bản (plan thử khả năng Claude + Muse)
goal: Tạo 2 FX chớp nòng súng độc lập (không gắn vào boss/enemy nào) và đặt cạnh nhau trong scene MakeAll để Linh so sánh. Ver A tự nghĩ từ đầu, ver B học từ FX có sẵn. Làm xong báo DONE, KHÔNG tự Play — Linh tự test.
project: ~/wkspaces/WB
source: Yêu cầu trực tiếp của Linh (2026-10-12); dò project

## T1 — Ver A: tự thiết kế FX muzzle từ đầu
owner: claude
depends: -
context:
KHÔNG mở/copy/tham khảo prefab FX nào có sẵn trong project. Tự dựng bằng ParticleSystem mới (hoặc SpriteRenderer + animation): chớp sáng ngắn ở nòng, vài tia/đốm lửa bắn theo hướng bắn (trục +Y), mỗi phát ~0.1–0.2s, Loop + Play On Awake để xem liên tục khi Play. Chỉ dùng material/texture mặc định của Unity hoặc tự tạo material/texture mới. Game 2D, camera orthographic. Ghi lý do chọn thông số vào báo cáo.
verified:
Scene: Assets/Project/Scenes/MakeAll.unity
refs: -
files: Assets/Project/FX_Test/Muzzle/FX_Muzzle_A.prefab (+ material/texture mới cùng thư mục)
done_when: Prefab tồn tại, không missing reference, compile 0 error

## T2 — Ver B: học từ FX có sẵn rồi tạo FX muzzle mới
owner: claude
depends: -
context:
Đọc cấu trúc các FX nòng có sẵn (module particle, material, texture, thời lượng, scale, sorting) rồi tạo prefab MỚI kết hợp điểm hay của chúng — không chỉ duplicate 1 cái. Được dùng lại texture/material có sẵn. Cùng quy ước với ver A: hướng +Y, Loop + Play On Awake. Báo cáo ghi đã học gì từ file nào.
verified:
Assets/Effect_1945/Prefab/GunShoot_5.prefab
Assets/Effect_1945/Prefab/Gunshoot/gunshoot_1.prefab, gunshoot_2.prefab, gunshoot_3.prefab
Assets/Effect_1945/Texture/Muzzle2.png
Assets/FORGE3D/Sci-Fi Effects/Effects/Plasma Beam/plasma_beam_muzzle_blue.prefab (+ _red, _green)
refs: -
files: Assets/Project/FX_Test/Muzzle/FX_Muzzle_B.prefab
done_when: Prefab tồn tại, không missing reference, compile 0 error

## T3 — Đặt 2 FX vào MakeAll
owner: claude
depends: T1, T2
context:
Tạo GameObject gốc "FX_Test_Muzzle" ở root scene MakeAll, trong đó 2 instance FX_Muzzle_A (bên trái) và FX_Muzzle_B (bên phải) cách nhau đủ xa, nằm trong vùng camera, có label/tên rõ. Không sửa object nào khác trong scene. Save scene.
verified:
Assets/Project/Scenes/MakeAll.unity tồn tại
refs: -
files: Assets/Project/Scenes/MakeAll.unity
done_when: Scene có FX_Test_Muzzle với 2 con A/B, scene đã save, console 0 error. KHÔNG Play.

## T4 — Báo DONE
owner: claude
depends: T3
context: Không Play test. Báo: đường dẫn 2 prefab, vị trí trong scene, ver A — ý tưởng + thông số chính, ver B — học từ file nào, cái gì.
verified: -
refs: -
files: -
done_when: Báo cáo DONE gửi Linh

## Câu hỏi mở
- Thư mục FX_Test đặt tạm ở Assets/Project/FX_Test/ — đổi nếu Linh có chỗ quy ước khác.
