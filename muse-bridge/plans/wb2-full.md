# WB2 PHANTOM ORACLE — code + dựng đủ 3 phase (anim flow, súng, shot)
goal: Hoàn thiện EnemyWB2_P1_A / P2_A / P3_A trong MakeAll.unity: flow anim theo quy ước Swap/In/Out, script boss, toàn bộ WorldBossWeapon + ShotE + class mới cho Súng 1–7 và tên lửa đặc biệt, test bằng WB-Shooter. Không tạo prefab boss/Node/SO/Addressables.
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library.md (mục 7, 12, 15, 17, 18.2), BR_WorldBoss/briefs/WB2_PLAN.md + WB2_P1..P3.md, design R231–R316 (Sheet 2026-10-15), ghi chú Linh 2026-10-15

## Quy ước anim WB2 (Linh, 2026-10-15) — áp cho mọi task
- Anim mở = `In_...`, trạng thái đóng = `Out_...` / close. **Anim close đang chờ art sửa** → dùng tạm anim hiện có, ghi TODO, không chỉnh timing.
- 3 skeleton dùng chung nhiều phase: `Gun1_Swap2`, `Gun3_Swap5`, `Gun4_Swap7`.
- Súng swap khi vào phase mới: trạng thái init = **trạng thái mở của phase cũ** → rồi chạy anim mix sang súng mới. VD Gun1_Swap2 ở P2: init `Idle_Gun1` → `Gun1_Mix_Gun2` → `Idle_Gun2`.
- Súng `(OnlyAnim)` (vị trí không còn dùng): vào phase chạy anim Out 1 lần, **không loop**, giữ frame cuối (Spine giữ pose cuối khi entry non-loop hết). Có thể làm 1 flow: Out (loop=false) rồi action cuối là pose close loop.
  - P2: 2 `G1-(OnlyAnim)` chạy `Out_gun1`.
  - P3: 4 `G2-(OnlyAnim)` chạy `Out_Gun2`.
- Không scene save khi Linh chưa đồng ý (MakeAll dùng chung). Không tạo prefab boss/Node/SO.


## Luật chung (BR_WorldBoss/AGENT.md)
- Class mới đặt `Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/`, hậu tố `WB2`; gizmo gọi `WorldBossGizmos.Label(transform,pos,color)`.
- Script boss: `Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/` (mẫu `WorldBoss-5/EnemyWB5_P2.cs` : EnemyWorldBoss).
- Bẫy chain (library mục 12): weapon EVENT/ROTATE/SEQUENCE loop/CHARGE loop/summon không bao giờ bắn OnCycleDone → không cho vào chain.
- Số design thiếu → giá trị tạm `// TEMP` + ghi bảng "Số tạm" trong báo cáo. Không lưu MakeAll khi chưa hỏi Linh.

## T1 — Flow anim súng + thân cả 3 phase
owner: claude
depends: -
context:
Gun1_Swap2: In_Gun1(2.00) Idle_Gun1 Attack_Gun1(0.40) Out_gun1(3.00) Gun1_Mix_Gun2(3.87) Idle_Gun2 Attack_Gun2(0.40) Out_Gun2(2.00).
Gun3_Swap5: In_Gun3(4.17) Idle_Gun3(8.00) Attack_Gun3(0.60) Gun3_mix_Gun5(6.83) Idle_Gun5 Attack_Gun5(0.43).
Gun4_Swap7: In_Gun4(2.37) Idle_Gun4 Attack_Gun4(1.00) Gun4_Mix_Gun7(3.00) Idle_Gun7 Attack_Gun7(4.00). Gun6: In(3.33) Idle Attack(1.00). Rocket: Idle.
Thân World_Boss2: Idle1, Idle1_to_2(4.00), Idle2, Idle2_to_3(4.00), Idle3, Dead.
verified:
MakeAll.unity — P1: G1- ×4 (bone Gun1_1..4), Rocket-L(Gun1_1)/R(Gun1_4). P2: G2-L(Gun1_1), G2-R(Gun1_4), G1-(OnlyAnim)×2 (Gun1_2/3), G3, G4(Gun4). P3: G2-(OnlyAnim)×4, G5, G6(Gun6), G7(Gun4). Súng chỉ có SkeletonAnimation, CHƯA có SpineAnimFlow/WorldBossWeapon.
WorldBossWeapon.cs:38-41 anim(SpineAnimFlow) + flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop/flowCharge; :93 Init() PlayFlow(flowCloseInit).
SpineAnimFlow.cs:32 (Assets/Spine/SpineTools) AnimFlow{Name, Actions}, AnimAction{AnimationName, Loop, Delay, TrackIndex, NewDuration}.
EnemyWorldBoss.cs:39-45 AutoWireFlowAppear: flow tên `Appear` → flowAppear + delayActionAppear.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_PLAN.md
files: MakeAll.unity (EnemyWB2_P1_A/P2_A/P3_A)
done_when:
- Súng swap: CloseInit = pose mở phase cũ → Open = Mix → Idle mới loop; Attack = Attack_GunN → Idle. (P2 G2: Idle_Gun1→Gun1_Mix_Gun2; P3 G5: Idle_Gun3→Gun3_mix_Gun5; G7: Idle_Gun4→Gun4_Mix_Gun7.)
- Súng mới: Open = In_* → Idle; Attack. (P1 G1, P2 G3/G4, P3 G6.)
- OnlyAnim: chỉ SkeletonAnimation start `Out_gun1` (P2) / `Out_Gun2` (P3), loop=false.
- Thân: P2 flow Appear = Idle1_to_2→Idle2; P3 Appear = Idle2_to_3→Idle3. Console sạch.

## T2 — Script boss + class P1 (tên lửa đặc biệt)
owner: muse
depends: -
context:
3 script rỗng `EnemyWB2_P1/P2/P3 : EnemyWorldBoss` (P3 sẽ thêm logic ở T6).
Tên lửa (R244–R249): phóng 3 quả hình mũi tên tới vị trí random ⅓, ¾, ½ màn; HP = 2s bắn liên tục (uniqueHP = số phát trúng, chỉnh theo fire rate MBC); bị bắn nổ → tách 3 mũi tên rơi thẳng; CD 1s. "Giống súng 1 Boss22".
verified:
Boss22 Súng 1 = BombShotMB42 (SpecialCase/BombShotMB42.cs:8, : BaseShotE): bulletPrefab BombMB42 (Bullets/Scripts/BombMB42.cs:8 : MonoBehaviour), targetPositions, List<Boss22BombData{DirectionShotE, Target}>, onBombReachTarget; dùng trong Enemy_Boss22_201_A.prefab.
EnemyWB5_P2.cs:3 mẫu script boss.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P1.md
files: WorldBoss-2/EnemyWB2_P1.cs, EnemyWB2_P2.cs, EnemyWB2_P3.cs; nếu BombShotMB42 không đáp ứng 3 quả mũi tên + tách 3 khi chết: SpecialCase/SpecialRocketShotWB2.cs (+ bullet/enemy tên lửa)
done_when: compile 0 error; tái dùng BombShotMB42 nếu đủ, chỉ tạo class WB2 cho phần thiếu (ghi lý do).

## T3 — Dựng + test P1
owner: claude
depends: T1, T2
context:
Súng 1 (R251–R255): 4 khẩu, trái xong mới phải; 2 khẩu/bên bắn lượt, mỗi khẩu 2 lần; như Súng 3 (20 viên tròn) nhưng nửa → 10 viên/khẩu (TEMP, chờ Linh); CD 3s sau khi cả 2 bên xong.
verified:
DirectionShotE.cs:7; WorldBossWeapon ATTACK_ON_SEQUENCE (isLoopSequence) / chain (useChain, chainGroup, chainDelayAfter) — library mục 12.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P1.md
files: MakeAll.unity (EnemyWB2_P1_A)
done_when: G1 ×4 = WorldBossWeapon + DirectionShotE, chain thứ tự L1→L2→R1→R2, CD 3s; Rocket-L/R bắn tên lửa theo T2; test WB-Shooter đúng nhịp, Console sạch.

## T4 — Class P2: drone S4
owner: muse
depends: T2
context:
S4 anten (R272–R279): gọi 2 drone (portal VFX) ở giữa 2 hông màn mỗi 3s, max 6; drone đứng im, bắn 1 tia liên tục tới player, xoay chậm hơn player 1 chút, có HP riêng.
verified:
WorldBossSummon.cs:6 (: SummonShotE.cs:8) spawn transforms×waves qua MyPoolManager; RotationAimFighter.cs:4; LaserShotE.cs:8; EnemyWB1_P3_Drone.cs:6 (: Enemy) mẫu enemy drone WB; Spine `Drone` (Idle).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P2.md
files: SpecialCase/SummonDroneWB2.cs (giới hạn max 6, vị trí 2 hông), WorldBoss-2/EnemyWB2_Drone.cs (: Enemy, tia + RotationAimFighter tốc độ chậm)
done_when: compile 0 error; drone prefab (enemy) dùng Spine Drone, có HP, chết thì slot trống để gọi lại.

## T5 — Dựng + test P2
owner: claude
depends: T1, T4
context:
Flow (R281): xuất hiện → S4 & S2 cùng active → S2 bắn đủ 3 lần → S3 bắn (5s) → lại S4&S2. Khi S2 bắn các súng khác dừng (R265).
S2 (R258–R262) 2 cánh: mỗi lần 2 lượt cách 0.2s tới player, 3 viên mũi tên vòng cung; CD 1s; 3 lần.
S3 (R264–R270) ụ giữa: 20 viên × 2 vòng cùng lúc, mỗi lần xoay 10° phải, delay 0.2s, đạn hơi chậm, 5s.
verified:
StepRotation.cs:6; DestinationShotE.cs:7; DirectionShotE.cs:7.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P2.md
files: MakeAll.unity (EnemyWB2_P2_A)
done_when: G2-L/R (DestinationShotE waves 2 / 0.2s, 3 dir), G3 (DirectionShotE 20 dir ×2 + StepRotation 10°), G4 (SummonDroneWB2) chạy đúng flow trên — S4 ngoài chain; S2→S3 bằng chain/SEQUENCE không loop. Test Console sạch.

## T6 — Code P3: Teleport + Memory
owner: muse
depends: T2
context:
MS1 (R286–R291): bắn thẳng xuống lần lượt 8 viên trái→phải quạt 30° → delay 1s → teleport (anim 1–1.5s, chưa có anim → FX/fade) random 4 góc không lặp → hướng mũi vào tâm → S5 bắn 2 lần, 6 viên quạt 30°.
MS2 (R293–R312): ra giữa màn → 6 đường KHÓI chia 6 phần → S6 (mũi) chiếu XANH lần lượt random (lặp tối đa 1 lần); player phải chạm đủ; sai → ô thành ĐỎ (cảnh báo) → S6 bắn laser từ mũi tới hết màn vào ô đỏ, chết luôn. Lần 2+: 8 phần. S7 (cọc) sau 1 vòng S6: 16 drone nhỏ + 8 đường điện chia màn, quay CW, drone trượt lên xuống, khe hở giữa 2 drone, drone bất tử, bắn player CD 2s; 8s → về MS1. S8 đã bỏ.
verified:
LinkLaser_CL.cs:8; LaserShotE.cs:8; mẫu điều khiển cọc + LinkLaser: ElectricOrbitShotWB5 / DestinationLinkShotWB5 (library mục 12).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md
files: WorldBoss-2/EnemyWB2_P3.cs (state machine MS1↔MS2, teleport), SpecialCase/PuzzleZoneWB2.cs, KillZoneLaserWB2.cs, RotatingLinkFieldWB2.cs
done_when: compile 0 error; gizmo vẽ sector/đường điện; số chưa có ghi TEMP.

## T7 — Dựng + test P3, báo cáo
owner: claude
depends: T1, T6
context:
G5 (Gun3_Swap5) = S5; G6 (Gun6, mũi) = S6 Puzzle + laser; G7 (Gun4_Swap7) = S7.
verified:
—
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md (mục 3–5)
files: MakeAll.unity (EnemyWB2_P3_A); BR_WorldBoss/library/worldboss-boss-library.md (mục 7, 17, 18.2); briefs/WB2_P*.md (Trạng thái)
done_when: MS1→MS2→MS1 chạy vòng; Puzzle đúng/sai → laser; S7 8s; báo cáo bảng theo AGENT.md mục 5; hỏi Linh trước khi save MakeAll.

## Quyết định (Linh duyệt 2026-10-15)
- Rocket giữ bone hiện tại. G3/G5 bone đã gán. Thân P3 dùng Idle3 + appear Idle2_to_3.
- Súng OnlyAnim chỉ là hình: không WorldBossWeapon (tránh bẫy chain/OnCycleDone, không chiếm slot weapon của EnemyWorldBoss), không code mới — SkeletonAnimation start anim `Out_*` loop=false.
- Anim `null/*` (thư mục nháp của art) — KHÔNG dùng:
  - Gun1_Swap2 `null/In_Gun2`: bản nháp, đã có `Gun1_Mix_Gun2`.
  - Gun4_Swap7 `null/In_Gun7`: đã có `Gun4_Mix_Gun7`; `null/Attack_Gun8`: thuộc Súng 8 (đã bỏ); `null/Out` + `null/vfx`: key slot S4.x (đóng/vfx anten S4) — để ý nếu art chuyển ra ngoài thư mục null thì dùng cho S4.
  - World_Boss2 `null/null…3`: key bone g1.x/s6.3, nháp.


## Câu hỏi mở
- Súng 1 "nửa đạn Súng 3" = 10 viên/khẩu? (đang TEMP)
- Thời gian mỗi lần chiếu xanh / thời gian chờ player chạm / laser bao lâu (MS2).
- Tốc độ đạn S3 "hơi chậm", tốc độ xoay drone S4, tốc độ quay 8 đường điện S7.
- Anim close (Out_*) chờ art sửa → giữ tên, thay file khi art giao. Anim teleport chưa có.
