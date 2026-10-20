# WB2 — thay anim đóng (Close) art vừa giao + drone In
goal: Art đã giao anim `Close` cho Gun1_Swap2 / Gun3_Swap5 / Gun4_Swap7 / Gun6 và `In` cho Drone. Thay các CloseInit tạm (Out_*/In_* đóng băng NewDuration=99999) bằng `Close`, thêm anim xuất hiện cho drone S4, test lại 3 phase. Không đổi logic/shot.
project: ~/wkspaces/WB
source: Project 1941 — library mục 20 (quy ước anim WB2), plan wb2-full (status T1–T7 done 2026-10-15), quét Unity MCP 2026-10-15

## Quy ước (library mục 20)
- `flowCloseInit` chạy lúc `WorldBossWeapon.Init()` (WorldBossWeapon.cs:93) = trạng thái lúc boss vừa vào phase.
- Súng MỚI xuất hiện ở phase → CloseInit = `Close` (đóng) → Open = `In_*` → Idle.
- Súng SWAP → CloseInit = pose mở phase cũ (giữ nguyên, KHÔNG đổi sang Close) → Open = Mix → Idle.
- Súng (OnlyAnim) → giữ `Out_*` loop=false (đã test giữ pose cuối) — không đổi.
- Không lưu MakeAll khi chưa hỏi Linh.

## T1 — Thay CloseInit bằng Close
owner: claude
depends: -
context:
Anim mới: Gun1_Swap2 `Close`(0.00), Gun3_Swap5 `Close`(0.13), Gun4_Swap7 `Close`(0.00), Gun6 `Close`(0.13). Duration 0 = 1 pose tĩnh → đặt Loop=true, bỏ NewDuration.
verified:
Quét SpineAnimFlow trong MakeAll.unity 2026-10-15:
- EnemyWB2_P1_A/G1-L1, L2, R1, R2: CloseInit = `Out_gun1` (tạm) → đổi `Close*`.
- EnemyWB2_P2_A/G3: CloseInit = `In_Gun3*` frame0 (NewDuration 99999) → `Close*`.
- EnemyWB2_P2_A/G4: CloseInit = `In_Gun4*` frame0 → `Close*`.
- EnemyWB2_P3_A/G6: CloseInit = `In*` frame0 → `Close*`.
- GIỮ NGUYÊN: P2 G2-L/R CloseInit `Idle_Gun1*`; P3 G5 `Idle_Gun3*`, G7 `Idle_Gun4*` (swap); Rocket-L/R `Idle*`; OnlyAnim G1×2 (P2) `Out_gun1`, G2×4 (P3) `Out_Gun2`.
- Thân: P1 Appear `Idle1*`, P2 Appear `Idle1_to_2 → Idle2*`, P3 Appear `Idle2_to_3 → Idle3*` — đúng, không đổi.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 20)
files: Assets/Project/Scenes/MakeAll.unity (SpineAnimFlow của 7 súng trên)
done_when: 7 flow CloseInit = `Close` loop, không còn NewDuration 99999; Open/Idle/Attack giữ nguyên; Console sạch.

## T2 — Drone S4 xuất hiện bằng In
owner: claude
depends: -
context:
Drone Spine mới có `In`(0.33) + `Idle`(0.13). Drone gọi bằng SummonDroneWB2 (T4 wb2-full), prefab EnemyWB2_Drone.prefab (T5 wb2-full).
verified:
SpecialCase/SummonDroneWB2.cs, WorldBoss-2/EnemyWB2_Drone.cs (: Enemy) tồn tại; prefab EnemyWB2_Drone.prefab tạo ở T5 wb2-full.
refs: /Users/mobione/AGENTS/muse-bridge/plans/wb2-full.md (T4, T5)
files: EnemyWB2_Drone.prefab (SkeletonAnimation start = In, hoặc SpineAnimFlow "Appear" In → Idle loop chạy khi spawn từ pool)
done_when: mỗi lần drone spawn (kể cả lấy lại từ pool) chạy In rồi Idle loop; drone chỉ bắt đầu bắn sau In (≥0.33s); Console sạch.

## T3 — Test lại 3 phase + báo cáo
owner: claude
depends: T1, T2
context:
Test từng phase bằng WB-Shooter (bật phase cần test, tắt phase khác rồi bật lại như cũ).
verified:
wb2-full T3/T5 còn chưa verify: rocket bị bắn nổ → tách 3 (MakeAll thiếu SoundManager → HealthArmor.DeActive NRE); drone bị bắn chết / laser aim player (Fighter dummy không có FighterBase).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md (mục 3–5)
files: library mục 20 (bỏ dòng "chờ art sửa", ghi Close); briefs/WB2_PLAN.md (art còn thiếu)
done_when:
- P1: 4 G1 đứng Close → In_Gun1 → Idle; P2: G3/G4 Close → In → Idle, G2 swap đúng; P3: G6 Close → In → Idle, G5/G7 swap đúng. Không giật pose ở frame đầu.
- Ghi kết quả + ảnh/ghi chú nếu pose Close lệch vị trí so với frame đầu của In_*.
- Hỏi Linh trước khi save MakeAll.

## Quyết định
- Súng swap không dùng Close (đã có pose mở phase cũ làm init).
- OnlyAnim giữ Out_* (đã đóng đúng); chỉ đổi nếu Linh muốn thêm Close loop sau Out.

## Câu hỏi mở
- 2 điểm chưa verify từ wb2-full (rocket tách 3, drone chết/laser) — cần scene có SoundManager + Fighter thật; có test trong scene gameplay không?
