# WB2 P3 — sửa sau test lần 4 (vị trí góc thấy G5, G6 giữa màn ở MS2, lùi ra trước mỗi teleport, puzzle chạy lại, đóng súng không dùng)
goal: Sửa 5 lỗi Linh báo 2026-10-20 khi test (round 4 đang chạy dở T1–T3). Plan này GỘP và THAY phần còn lại của wb2-p3-fix-round4: bắt đầu bằng kiểm lại code round 4 đã sửa, rồi làm tiếp. Mỗi mục phải Play verify bằng log `[WB2_P3]` + screenshot, không đánh done khi chưa nhìn thấy kết quả.
project: ~/wkspaces/WB
source: Project 1941 — brief ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md, library mục 7 + mục 20 (luật OnlyAnim/đóng súng WB2), design R283–R316 (R294: MS2 boss ra giữa màn), plan wb2-p3-fix-round4 (status T1–T3 doing)

## T0 — Chốt trạng thái round 4
owner: claude
depends: -
context: Round 4 đang dở: T1 (K1/K2 bắn hết viên cuối mới đi + lùi ra hết màn mới teleport) Muse code xong chưa verify; T2 (anim bắn G5 mỗi viên, NewDuration) Claude đang làm; T3 (tách Boss2_Wind, holder `G6-PuzzleFx` + Boss2 nested, PuzzleZoneWB2.fxHolder) code + prefab xong chưa verify. Đọc diff/code hiện tại, đánh dấu round4 T1–T3 theo kết quả thật, ghi vào status plan round4 ("superseded by round5" phần còn lại). Không revert gì khi chưa rõ.
files: ~/AGENTS/muse-bridge/plans/wb2-p3-fix-round4.status.json
done_when: biết rõ cái gì của round 4 đã chạy đúng/sai.

## T1 — MS1: vị trí đứng ở góc phải THẤY G5 (không chỉ thò mũi/G6)
owner: muse
depends: T0
context:
- Linh: vào góc chỉ thò mỗi Gun6 (mũi) vào màn → không thấy G5 bắn K2. Đúng: boss bay vào **sâu trong màn** đủ để **G5 (giữa thân) nằm trong màn và nhìn rõ** rồi mới bắn K2.
- Đổi cách tính điểm đứng: điểm góc là vị trí mong muốn của **G5 (world)**, không phải pivot boss: boss.position = cornerTarget − (G5.position − boss.position) sau khi đã FaceCenter (rotation) → G5 đứng đúng góc. Góc = rect camera thu vào `cornerInset` (Vector2, Inspector, TEMP (1.5, 2.0) world) — thay cho hệ số cornerX/cornerYTop/cornerYBottom (giữ field cũ, ghi chú không dùng). `cornerOffset` giữ.
- Verify: screenshot 4 góc thấy rõ G5 + đạn K2 trong màn.
verified:
EnemyWB2_P3_A.prefab (save 2026-10-19 20:24 UTC): cornerX 0.75, cornerYTop 0.75, cornerYBottom −0.75, centerY 0.1, cornerOffset (0,0), enterOffscreenDistance 3, enterTime 0.3, offscreenMargin 0.5, exitSpeed 9, respawnDelay 0.15, postK1Delay 1, postK2Delay 1, noseOffsetAngle 90. G5 lpos (0, 0.56), G6 lpos (0.01, −4.88) (mũi).
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error; screenshot 4 góc.

## T2 — Mọi lần teleport đều LÙI RA HẾT MÀN trước (kể cả từ MS2 → MS1 và sau K1)
owner: muse
depends: T0
context:
- Linh: từ MS2 về MS1 phải **đi lùi ra khỏi màn rồi mới chuyển** (teleport). Luật chung: boss KHÔNG bao giờ nhảy vị trí khi còn thấy trên màn — mọi lần đổi chỗ (sau K1 tại chỗ → góc đầu; giữa các góc; hết MS2/S7 → MS1) = bắn xong → delay → **bay lùi** (ngược hướng mũi) ra tới khi bounds ra hết camera → mới teleport.
- Trình tự MS2 → MS1 (Linh chốt 2026-10-20): hết S7 (G7 đóng, xem T4) → boss **lùi từ vị trí "G6 ở giữa màn" về vị trí kết thúc spline 1** (điểm cuối spline 1 / vị trí appear, rotZ 0; di chuyển mượt `returnToAppearTime`, Inspector) → **bắn K1** tại đó (G5 mở lại 1 lần đầu lượt) → `postK1Delay` → **lùi ra hết màn** → teleport góc 1 → 4 góc như lượt đầu. Lưu vị trí cuối spline 1 lúc appear lần đầu (hoặc lấy điểm cuối path) để dùng lại.
- Lượt đầu: spline 1 → appear → K1 tại chỗ → delay postK1Delay → **lùi ra hết màn** → teleport góc 1.
files: EnemyWB2_P3.cs
done_when: log mỗi lần teleport có "exit bounds outside camera = true".

## T3 — MS2: boss di chuyển để G6 ở GIỮA màn, puzzle chạy lại
owner: muse
depends: T0
context:
- Linh: ở MS2 boss phải **move sao cho G6 (mũi) nằm ở giữa màn** (design R294 "boss đi từ từ ra giữa màn"). Sau spline 1 replay (rotZ 0) → di chuyển mượt (`moveToCenterTime`, Inspector) sao cho G6.position = tâm màn (Camera.main center, + `g6CenterOffset` Inspector) → rồi mới G5 đóng / G6 mở / puzzle. Tâm puzzle = vị trí G6 lúc đó.
- Linh: **puzzle chưa work** — kiểm toàn chuỗi: G6 Play (Open) → PuzzleZoneWB2 chạy (log từng pha: flash seq, input step, Wrong/Completed), holder `G6-PuzzleFx` bật đúng chỗ (world rot 0, scale 1), SectorMaskFxWB2 tìm được Warning_G/R/Smoke trong Boss2 nested (log số mask/PSR), box area hiển thị, FAIL → vạch đỏ + laser ONE_SHOT. Sửa mọi chỗ gãy (nghi round 4 đổi fxPrefab → fxHolder chưa nối, hoặc Boss2 bản mới thiếu ref). So với sandbox `PuzzleSandboxWB2` (vẫn phải chạy được).
verified:
PuzzleZoneWB2 trên prefab (trước round 4): fxPrefab=Boss2, flashTime 0.35, flashGap 0.1, delayBeforeInput 0.5, touchTimePerStep 1, redWarnTime 0.5; KillZoneLaserWB2 laserShot=LaserShotE, laserDuration 0.25, aimOffsetAngle 180. Boss2.prefab bị artist thay 2026-10-19 ~17:55 VN (mất SectorMaskFxWB2).
files: EnemyWB2_P3.cs, PuzzleZoneWB2.cs, SectorMaskFxWB2.cs, EnemyWB2_P3_A.prefab
done_when: Play MS2: G6 ở giữa màn; 1 lần PASS (điều khiển proxy/Fighter theo đúng chuỗi — dùng PuzzleProxyPlayerWB2 gắn tạm lên Fighter hoặc execute_code đặt vị trí theo seq) + 1 lần FAIL → laser kill; screenshot quạt xanh/đỏ.

## T4 — Đóng các súng không dùng (luật WB2 library mục 20)
owner: claude
depends: T0
context:
- Linh: Brain đã quy định đóng súng không dùng, chưa làm. Áp cho P3:
  (a) 4× `G2-(OnlyAnim)` (`WorldBossOnlyAnimGun`): CloseInit = `Idle_Gun2` → `Open` = `Out_Gun2` → `Close` loop, phát CÙNG FRAME với flowOpen của G5 (syncOpenWith, event `WorldBossWeapon.OnOpenStart`). Round 4 đổi G5 sang `PlayOpenOnly()`/K1 → KIỂM event OnOpenStart vẫn bắn (syncOpenWith hiện = K1) → nếu không, sửa để Out_Gun2 chạy 1 lần lúc G5 mở lần đầu.
  (b) G6: ở MS1/S7 phải ở trạng thái đóng (`Close` loop làm CloseInit), chỉ Open ở MS2 puzzle, xong puzzle/laser → Stop (đóng).
  (c) G7 (`Gun4_Swap7`): CloseInit cho tới khi S7 bắt đầu → Open (Mix Gun4→Gun7 lần đầu) → hết S7 → đóng/Idle đóng; không để mở suốt.
  (d) G5: đóng khi sang MS2 (đã có), mở lại đầu MS1 lượt sau.
  (e) Mọi súng phase P1/P2 còn sót trên P3 (nếu có) → đóng/ẩn theo luật.
verified:
library mục 20 (WB2, Linh 2026-10-15/16): súng swap `flowCloseInit` = pose mở phase cũ → `flowOpen` = Mix (Gun3_mix_Gun5 6.83s, Gun4_Mix_Gun7 3.00s); OnlyAnim CloseInit → Open(Out_*) → Close, sync với súng swap qua `syncOpenWith`; P3: 4× G2-(OnlyAnim) ↔ G5. Library mục 7: súng MỚI trong phase (G6 ở P3) dùng `Close` loop làm CloseInit. Round 4 note: "G2 syncOpenWith=K1".
files: EnemyWB2_P3_A.prefab (flow fields), EnemyWB2_P3.cs (gọi Stop/Close đúng lúc), WorldBossWeapon.cs (nếu PlayOpenOnly không phát OnOpenStart)
done_when: Play 2 lượt: screenshot lúc MS1 (G6, G7 đóng; G2 OnlyAnim đã Out+Close), lúc MS2 (G5 đóng, G6 mở), lúc S7 (G7 mở, G6 đóng).

## T5 — Test tổng + báo cáo
owner: claude
depends: T1, T2, T3, T4
context: Play WB-Shooter P3 ≥ 2 lượt MS1↔MS2. Không save MakeAll khi Linh chưa đồng ý. Báo cáo bảng từng mục 1–5 của Linh: đã sửa gì, bằng chứng (log/screenshot). Cập nhật brief WB2_P3 (mục Round 5) + library mục 7.
done_when: Linh xem báo cáo.

## Câu hỏi mở
- Không còn.
