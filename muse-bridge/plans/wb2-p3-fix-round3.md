# WB2 P3 — sửa sau test lần 2 (MS1 nhịp bắn/teleport, G5 mở 1 lần, Fire_Engine, drone S7 hướng)
goal: Sửa 5 lỗi Linh phản hồi sau khi test plan wb2-p3-fix-round2 + verify bản sửa hướng drone S7 Claude đã tự sửa ngoài mission. Mọi thời gian mới là [SerializeField] chỉnh trên Inspector.
project: ~/wkspaces/WB
source: Project 1941 — brief ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (Moveset 1, Moveset 2), chat Linh 2026-10-19 (kèm ảnh 2 lửa đuôi boss phải chĩa theo 2 động cơ đuôi)

## T1 — MS1: bắn K2 xong hẳn → delay 1s → lùi ra → teleport; sửa lỗi dừng sau 1 góc
owner: muse
depends: -
context:
- Lỗi A (Linh): ở góc, K2 **chưa bắn xong đã move đi**. Đúng: K2 bắn HẾT (đủ 2 lần × 6 viên, kể cả delay per-FireDirection và waves) → chờ `postK2Delay` (1s, Inspector) → bay lùi ra khỏi màn → teleport sang góc mới.
  Nghi nguyên nhân: `IE_PlayWeaponOnce` thoát khi `OnCycleDone` bắn ra (có thể sớm hơn lúc viên cuối ra khỏi nòng) rồi gọi `_weapon.Stop()` ngay → cắt ngang. Cần chờ shot kết thúc thật (OnEndShot/ueEndShot của BaseShotE hoặc tính tổng thời gian waves × fireRate + delay) rồi mới đi tiếp; KHÔNG Stop weapon giữa các góc (xem T2).
- Lỗi B (Linh): teleport **mới ra 1 góc rồi dừng, boss biến mất luôn**. Phải đi đủ 4 góc rồi IE_ReplayPath1. Tìm nguyên nhân (log từng bước IE_MS1: corner index, vị trí GetOffscreenPoint, IsPlaying, exception) — nghi: điểm offscreen/enter tính sai sau lần exit đầu (boss kẹt ngoài màn), hoặc coroutine dừng do StopMaster/OnStopAttack bị gọi khi boss ra ngoài màn (kiểm cơ chế off-screen của base enemy: collider/visibility tắt → states đổi khỏi PLAYING ⇒ IsPlaying() false ⇒ IE_MS1 yield break). Sửa tận gốc, thêm log `[WB2_P3]` mỗi bước.
verified:
EnemyWB2_P3.cs (507 dòng) IE_MS1: K1 → foreach ShuffleCornerOrder: IE_EnterCorner → IE_PlayWeaponOnce(weaponK2) → IE_ExitCorner → WaitForSeconds(respawnDelay) → IE_ReplayPath1. IE_PlayWeaponOnce: Play → chờ OnCycleDone (timeout weaponCycleTimeout 12) → `_weapon.Stop()`. IE_ExitCorner lerp tới lastEnterOffscreen theo exitSpeed.
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/EnemyWB2_P3.cs
done_when: compile 0 error; log Play cho thấy đủ 4 góc mỗi lượt.

## T2 — G5 mở 1 lần / lượt MS1, giữ mở khi teleport, đóng khi sang MS2
owner: muse
depends: -
context:
- Linh: G5 chỉ chạy anim **mở 1 lần lúc bắt đầu MS1**, **giữ mở** suốt K1 + 4 góc K2 (kể cả lúc lùi ra/teleport) — hiện mỗi lần teleport lại mở lại. Sang MS2 thì **đóng G5** (đã có bước "G5 thu vào trước rồi G6 mở" — giữ, đảm bảo đóng thật bằng flowStop/Close).
- Lượt MS1 sau (sau MS2) mở lại 1 lần như đầu lượt.
- Cách gợi ý: không gọi weapon.Stop() (flowStop = anim đóng) giữa K1/K2 và giữa các góc; chỉ dừng shot (StopAttack của DirectionShotE) nếu cần; K2 không phát flowOpen (đã để rỗng); K1 phát flowOpen 1 lần đầu lượt; đóng = 1 lần Stop ở đầu MS2. Có thể thêm API nhỏ cho WorldBossWeapon (vd PlayAttackOnly / StopWithoutClose) nếu không phá súng khác — mặc định hành vi cũ giữ nguyên.
verified:
WorldBossWeapon.cs:39 flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop; :45 openOnce; :116 Play(); :253 Stop(). G5-K1 (openOnce=false) / G5-K2 (flowOpen rỗng) dùng chung SpineAnimFlow G5 (plan wb2-p3-ms1-ms2-apply T4). EnemyWB2_P3 có IE_StopWeaponAndWait (đóng G5 trước khi mở G6).
files: EnemyWB2_P3.cs, (nếu cần) Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBossWeapon.cs
done_when: compile 0 error; súng WB khác không đổi hành vi.

## T3 — Fire_Engine: bật khi bay vào màn, xoay theo 2 động cơ đuôi
owner: claude
depends: -
context:
- Linh (kèm ảnh): 2 lửa `Fire_Engine-L/-R` phải **chĩa đúng hướng 2 động cơ đuôi boss** và xoay theo khi boss xoay (FaceCenter ở góc, bay lùi…). Hiện không xoay.
- **Thời điểm bật (Linh chốt):** bật trong anim thân chuyển **`Idle2_to_3`** (Idle2 → Idle3, flow Appear của P3), **đúng lúc hình động cơ hiện ra trong anim**; bật xong **giữ nguyên, không tắt** suốt phase (không bật/tắt theo di chuyển/teleport). Cách: nếu `Idle2_to_3` có Spine event lúc engine hiện → bật theo event; không có → field `engineFxDelay` (giây tính từ lúc bắt đầu `Idle2_to_3`, Inspector) — tìm khung hình engine hiện ra để đặt giá trị mặc định. Lúc boss chưa tới đoạn đó (spline 1 lần đầu) thì tắt. Điều khiển từ EnemyWB2_P3 (list `engineFx`, Inspector).
- Phát lại khi replay spline 1 / sang MS2: KHÔNG chạy lại Idle2_to_3 → lửa vẫn giữ bật.
- Xoay: BoneFollower `followBoneRotation` = true (+ offset localRotation nếu art lửa lệch), particle simulation space Local để lửa không bị kéo lệch khi boss teleport; kiểm cả lúc rotZ ≠ 0.
verified:
Thân WB2 anim (library mục 20): Idle1 → Idle1_to_2 → Idle2 → Idle2_to_3 → Idle3; skeleton Assets/Project/WorldBoss/Addressable/Enemies/Spine/World_Boss_2/World_Boss2.skel.bytes (binary — kiểm event qua Spine API/Unity). Flow appear auto-wire: EnemyWorldBoss.cs:36 APPEAR_FLOW_NAMES {Appear, Action}, :27 flowAppear.
EnemyWB2_P3_A.prefab: `Fire_Engine-L` (line ~14, BoneFollower boneName "Dong co phan luc_5", followBoneRotation 0) và `Fire_Engine-R` (~18759, bone "Dong co phan luc_8", followBoneRotation 0). EnemyWorldBoss.fxOnPlaying = [] trên prefab. LƯU Ý: G5 (bone Gun... ) và các súng khác cũng followBoneRotation 0 → kiểm G5 có xoay theo thân khi boss quay mũi ở góc không (nếu K2 lệch hướng mũi thì bật followBoneRotation cho G5).
files: EnemyWB2_P3_A.prefab (instance trong MakeAll — chưa apply), EnemyWB2_P3.cs (field engineFx + bật/tắt)
done_when: Play: lửa bật đúng lúc engine hiện trong Idle2_to_3, sau đó luôn bật; luôn ra đúng 2 ống đuôi ở mọi góc/xoay.

## T4 — Verify bản sửa hướng drone S7 (Claude đã tự sửa ngoài mission 2026-10-19)
owner: claude
depends: -
context:
- Linh: hướng nhìn thẳng của drone = hướng laser bắn ra. Claude đã sửa trực tiếp `RotatingLinkFieldWB2.cs`: thêm `droneArtForwardAngle` (mặc định 90 vì art WB2_LinkDrone nhìn −Y), hàm `DroneRotation(spoke,isInner)` dùng khi dựng + mỗi frame, `Nose` bù cùng góc. Đã compile OK, CHƯA Play verify.
- Việc: review diff, Play tới S7, xác nhận mũi drone trùng tia laser (drone trong nhìn vào tâm, drone ngoài nhìn ra mép); turret lưng vẫn aim Fighter, 1 viên/1s. Chỉnh droneArtForwardAngle nếu lệch.
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/RotatingLinkFieldWB2.cs
done_when: screenshot S7 mũi drone đúng hướng laser.

## T5 — Test Play tổng + báo cáo
owner: claude
depends: T1, T2, T3, T4
context: Play WB-Shooter P3 ≥ 2 lượt MS1↔MS2. Không apply prefab / không save MakeAll khi Linh chưa đồng ý.
done_when: Console 0 error; (1) mỗi góc: K2 bắn đủ → nghỉ 1s → lùi ra khuất → góc mới; đủ 4 góc rồi replay spline 1 (2) G5 mở 1 lần đầu MS1, giữ mở 4 góc, đóng khi sang MS2, mở lại lượt sau (3) lửa đuôi bật từ Idle2_to_3, giữ nguyên, đúng hướng 2 ống đuôi (4) S7 drone đúng hướng. Báo cáo + cập nhật brief/library.

## Câu hỏi mở
- Không còn.
