# World Boss súng (OnlyAnim) — WorldBossOnlyAnimGun: Out → Close đồng bộ với súng swap Open

Scope: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBossOnlyAnimGun.cs (script chung, trước đây OnlyAnimGunWB2 — giữ GUID 172305a9…), EnemyWB2_P2_A / EnemyWB2_P3_A.prefab  |  Evidence: Linh chốt 2026-10-16; plan muse-bridge `wb2-p2-onlyanim-flow`

Súng (OnlyAnim) là vị trí súng của phase trước bị bỏ khi sang phase mới (không có `WorldBossWeapon`, không trong chain). Luật:
- `CloseInit` = Idle của súng phase trước, loop (P2 G1 → `Idle_Gun1`; P3 G2 → `Idle_Gun2`). Đây là pose lúc boss vừa vào phase.
- `Open` = `Out_*` (loop=false) → `Close` (loop=true).
- Phát `Open` CÙNG FRAME với lúc súng swap được gán (`syncOpenWith`) bắt đầu `flowOpen` (anim Mix). Súng swap có `openOnce`, nên OnlyAnim cũng chỉ Out 1 lần/fight; `Init()` của súng swap trả OnlyAnim về CloseInit.
- Độ dài `Out_*` và anim Mix không cần khớp nhau (chạy độc lập).
- Sai cũ: chạy Out ngay ở `OnEnable` → Out lúc boss xuất hiện, lệch với súng swap.
Gán sync: P2 G1-L/R ↔ G2-L/R; P3 4× G2 ↔ G5. Liên quan: library WB mục 20 (quy ước anim WB2), plan `wb2-close`.
