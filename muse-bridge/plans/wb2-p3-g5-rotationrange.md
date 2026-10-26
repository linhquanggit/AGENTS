# WB2 P3 — G5: nòng mặc định hướng xuống, quét ±75° bằng StepRotation, bỏ GunSweepAimWB2
goal: G5 của EnemyWB2_P3_A nghỉ với nòng thẳng xuống (0°). Mỗi lần bắn (K1 8 viên, K2 6 viên): 0° → -75° (KHÔNG bắn) → bắt đầu bắn + quay -75° → +75° → quay về 0° (KHÔNG bắn) rồi dừng. Đạn bay thẳng theo nòng (1 FireDirection). Dùng StepRotation có sẵn (thêm 3 option opt-in, mặc định tắt). Bỏ GunSweepAimWB2 + SweepAimShotWB2 khỏi G5.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (18.2 WB2), claude/muse-handoff-lessons-wb2.md; Linh chốt trong chat 2026-10-20 (speed 50, 1 FireDirection, waves 8/6, tính fireRate, dùng StepRotation)

## T1 — StepRotation: thêm 3 option opt-in (mặc định tắt, không đổi hành vi cũ)
owner: muse
depends: -
context:
- Chỉ sửa Assets/Project/Enemies/Scripts/BaseEnemy/EnemyRotation/StepRotation.cs. KHÔNG sửa RotationRange.cs / Rotation2D.cs / DirectionShotE.cs / BaseShotE.cs / WorldBossWeapon.cs / EnemyWB2_P3.cs. StepRotation đang dùng ở 2 prefab khác → mọi option mới default false/rỗng, khi tắt hành vi phải y hệt bản cũ.
- Cấu hình G5 sẽ dùng (T3): stepAngle 75, speed -50, minAngle -75, maxAngle 75, loopType Yoyo, nextStepDelay 0, continuousRotation true, triggerStepOnPlay false, initAngle 0. Chạy như code hiện tại: Play() từ 0 → target -75 → chạm min (Yoyo đảo speed sang +) → 0 → +75 → chạm max (đảo speed sang -) → 0 → ... Lần Play sau vẫn từ 0, speed âm → lại đi -75 trước. ĐÚNG chu trình Linh muốn, chỉ thiếu 3 thứ dưới.
- Option 1 `[SerializeField] UnityEvent ueReachMin` (rỗng = không làm gì): invoke trong HandleLimitsAndLoop() khi hitMin (dòng 48–50), TRƯỚC khi đảo speed. T3 nối event này → DirectionShotE.Attack() → viên đầu ra đúng lúc nòng ở -75. (Không dùng ueStepReached/ueCompletedLoop vì 2 event đó phát ở cả -75, 0 và +75.)
- Option 2 `[SerializeField] bool stopAtInitAfterMax = false`: khi true, đánh dấu `passedMax = true` lúc hitMax; trong OnStepReached(), nếu stopAtInitAfterMax && passedMax && Mathf.Approximately(cacheAngle.z, initAngle) → passedMax = false; Pause(); return (KHÔNG StartCoroutine DelayNextStep). → 1 lần Play = đúng 1 chu trình 0 → -75 → 0 → +75 → 0 rồi đứng ở 0. Play() reset passedMax = false.
- Option 3 `[SerializeField] bool initLocal = false`: Rotation2D.Start() (private, Rotation2D.cs:16) gọi SetInitAngle() set `transform.eulerAngles` WORLD (Rotation2D.cs:32-33) — sai cho transform con (Rotate nằm dưới SkeletonUtility root, có parent xoay). Thêm `void Start()` trong StepRotation (Unity gọi Start của class con, che bản private của base): `if (initLocal) { cacheAngle = new Vector3(0,0,initAngle); ApplyRotation(cacheAngle); } else SetInitAngle();` — nhánh else giữ đúng hành vi cũ.
- Giữ nguyên mọi field/tên hiện có (private stepAngle, nextStepDelay, …) để 2 prefab cũ không mất giá trị serialize.
verified:
- StepRotation.cs:6 `public class StepRotation : RotationRange`; :8-11 stepAngle, nextStepDelay, continuousRotation, triggerStepOnPlay; :13 ueStepReached; :43-66 HandleLimitsAndLoop (hitMax :47, hitMin :48, Yoyo `speed = -speed` :57); :68-78 OnStepReached (ueStepReached :72, DelayNextStep :76); :80-88 DelayNextStep chỉ PrepareNextStep khi isRotating; :90-108 PrepareNextStep (clamp min/max :97-100); :121-139 Play() (`if (isRotating) return;`, cacheAngle = localEulerAngles, PrepareNextStep, isWaiting=false, base.Play()); :141-144 ApplyRotation set localEulerAngles.
- Rotation2D.cs:16 `private void Start(){ SetInitAngle(); }`; :37 `public virtual void Play()`; :43 `public void Pause()` (isRotating=false, uePause).
- StepRotation guid dùng trong 2 prefab.
refs: Project 1941 claude/muse-handoff-lessons-wb2.md (§3 giữ hành vi file dùng chung)
files: Assets/Project/Enemies/Scripts/BaseEnemy/EnemyRotation/StepRotation.cs
done_when: compile 0 error; diff chỉ thêm 3 option + Start() có nhánh else = SetInitAngle(); 2 prefab cũ dùng StepRotation không đổi giá trị serialize.

## T2 — Đo trục nòng G5 và căn FireDirection thẳng nòng
owner: claude
depends: -
context:
- Nòng G5 là bone Spine `Gun5`, bị override bởi transform `Rotate` (SkeletonUtilityBone mode Override, rotation=1). Xoay `Rotate` (local z) = xoay nòng.
- Mục tiêu: khi boss rotZ = 0 và Rotate local z = restAngle, nòng (art) chỉ thẳng xuống (-Y world). Tìm restAngle (kỳ vọng 0; nếu art lệch thì ghi giá trị thật — T3 đặt min/max = rest ∓ 75).
- FireDirection: Direction = transform.up. Mỗi FireDirection1 phải nằm ở ĐẦU NÒNG, trên trục nòng, và up trùng hướng nòng (ra ngoài). Hiện có 2 FireDirection1 đang pos (0.82, 0, 0), rot z 180 — sai (lệch sang phải nòng).
- Cách đo: mở prefab EnemyWB2_P3_A (Prefab Mode, Unity MCP), đặt Rotate local z = 0, chụp Scene view vùng G5; xác định trục bone Gun5 (Spine bone hướng theo local +X của bone) và vị trí đầu nòng; đổi tạm Rotate z = ±75 để chắc art quay đúng chiều (z dương = đầu nòng sang phải khi nòng xuống).
- Không đụng GunSweepAimWB2/SweepAimShotWB2 ở task này (T3 làm).
verified:
- EnemyWB2_P3_A.prefab: G5 [Transform 4492187944407180328] > Animation [4492187945331009165] (SkeletonAnimation + SpineAnimFlow) > SkeletonUtility-SkeletonRoot > Rotate [Transform 2418496383013767609, GO 3163420366468058426] có SkeletonUtilityBone (guid b238dfcd…, boneName Gun5, mode 1).
- Dưới Rotate: G5-K1 [4492187945180644584] (WorldBossWeapon K1) > DirectionShotE [4492187946445636376] > FireDirections > FireDirection1 [Transform 4492187945016847855, comp 4492187945016847854]; và DirectionShotE [4492187944617244807] (shot K2) > FireDirections > FireDirection1 [Transform 4492187945532845438, comp 4492187945532845433]. Cả hai FireDirection1: localPos (0.82,0,0), localRot quat (0,0,1,~0) = 180°.
- GunSweepAimWB2 đang đặt aimAngleOffset -180 (comment: "nong art chi xuong = -180") → art có thể lệch 180° so với up của FireDirection; cần đo, không suy.
refs: Project 1941 claude/worldboss-boss-library.md (mục 18.2 WB2)
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P3_A.prefab (chỉ 2 Transform FireDirection1)
done_when: ghi vào kết quả task: restAngle đo được, localPos/localRot mới của 2 FireDirection1; ảnh chụp Scene view G5 với gizmo FireDirection trùng nòng ở 3 góc Rotate z = -75 / 0 / +75; prefab đã save; Rotate để lại z = restAngle.

## T3 — Nối G5 vào StepRotation, bỏ GunSweepAimWB2 / SweepAimShotWB2, đặt waves + fireRate
owner: claude
depends: T1, T2
context:
- Trên GO `Rotate` (G5): Add 2 component StepRotation — `Step K1` và `Step K2` (cùng cấu hình, khác shot). Giá trị: initAngle = R (từ T2, kỳ vọng 0), minAngle = R-75, maxAngle = R+75, stepAngle 75, speed -50 (âm = đi về -75 trước; nếu T2 thấy chiều art ngược thì đổi dấu speed để vẫn trái→phải), loopType Yoyo, nextStepDelay 0, continuousRotation true, triggerStepOnPlay false, isRotating false, stopAtInitAfterMax true, initLocal true. ueReachMin: `Step K1` → DirectionShotE K1 (comp 4492187946445636379).Attack; `Step K2` → DirectionShotE K2 (comp 4492187944617244806).Attack.
- fireRate: quét -75 → +75 = 150° / 50°/s = 3.0s; fireRate = 3.0 / (N-1) (viên đầu ở -75 lúc ueReachMin, viên cuối ở +75).
  · K1 N=8: fireRate = 3/7 = 0.428571; góc: -75, -53.57, -32.14, -10.71, 10.71, 32.14, 53.57, 75.
  · K2 N=6: fireRate = 3/5 = 0.6; góc: -75, -45, -15, 15, 45, 75.
  · Thời gian 1 lượt: 1.5s (0 → -75, không bắn) + 3s (bắn) + 1.5s (về 0, không bắn) = 6s < weaponCycleTimeout 12s.
- DirectionShotE K1 (comp 4492187946445636379): waves 1 → 8; fireRate 0.1 → 0.428571; delayShot 0; delayStart 0; fireDirections = ĐÚNG 1 phần tử {delay 0, FireDirection1 4492187945016847854} (xoá 2 phần tử null); sweep = None.
- DirectionShotE K2 (comp 4492187944617244806): waves 1 → 6; fireRate 0.5 → 0.6; delayShot 0; delayStart 0; fireDirections = ĐÚNG 1 phần tử {delay 0, FireDirection1 4492187945532845433} (xoá 5 phần tử null); sweep = None.
- Xoá component GunSweepAimWB2 (5420494876065708818, 7180868648019078135) và SweepAimShotWB2 (6758631541287324116, 5702626041309333792) trên 2 GO DirectionShotE. KHÔNG xoá file .cs (DirectionShotE.cs:49 còn tham chiếu type SweepAimShotWB2).
- WorldBossWeapon G5-K1 (comp 4492187945180644587): attackType 1 → 0 (ATTACK_ON_EVENT); ueEvent → `Step K1`.Play; rotate để None. WorldBossWeapon G5 (comp 4492187944407180330, đang disabled — giữ nguyên, P3 gọi trực tiếp): attackType 1 → 0; ueEvent → `Step K2`.Play; rotate None.
- VÌ SAO rotate để None: StopAttackOnly() → StopWeapons() gọi `rotate?.Pause()` (WorldBossWeapon.cs:315). IE_PlayWeaponOnce gọi StopAttackOnly ngay sau viên cuối (lúc nòng ở +75) → nếu gán rotate thì nòng bị dừng ở +75, không về 0. Để None thì StepRotation tự chạy hết chu trình và tự dừng ở 0 (stopAtInitAfterMax).
- attackType 0 an toàn với flow mở G5: Play() chỉ phát flowOpen khi ON_OPEN/ON_SEQUENCE/forceOpenFlow → với ON_EVENT đi thẳng Attack() (ueAttack + ueEvent). Mở G5 do EnemyWB2_P3 gọi PlayOpenOnly (EnemyWB2_P3.cs:415). ueAnimPerWave → _AnimShot vẫn chạy theo từng viên.
- KHÔNG chạy ContextMenu LoadAssets của WorldBossWeapon/DirectionShotE. Sửa bằng SerializedObject sau khi Unity reload xong T1.
- Giữ nguyên: flow anim, bulletPrefab, bulletSpeed, cooldown, EnemyWB2_P3.cs.
verified:
- WorldBossWeapon.cs:116-131 Play(); :135-165 Attack(): ON_EVENT `ueEvent?.Invoke()`; :315 StopWeapons `rotate?.Pause()`; :273 StopAttackOnly → StopWeapons.
- Prefab: G5-K1 WorldBossWeapon shotEs=[4492187946445636379], rotate None, attackType 1, flowOpen rỗng; G5 WorldBossWeapon (m_Enabled 0) shotEs=[4492187944617244806], rotate None, attackType 1, flowOpen Open, openOnce 1.
- Prefab: DirectionShotE K1 waves 1, fireRate 0.1, fireDirections 3 phần tử (2 null), sweep → SweepAimShotWB2; K2 waves 1, fireRate 0.5, fireDirections 6 phần tử (5 null), sweep → SweepAimShotWB2.
- BaseShotE.cs:124 `public virtual void Attack()` (bắn 1 lượt, gọi được từ UnityEvent); :112-113 waitFireRate/waitDelayShot tạo lúc Init() → giá trị phải nằm sẵn trong prefab. DirectionShotE.cs:94-107 mỗi wave bắn hết fireDirections rồi chờ fireRate.
- EnemyWB2_P3.cs:590-640 IE_PlayWeaponOnce: N = Σ ExpectedBullets (1 FireDirection × waves → K1 8, K2 6), đếm onBeforeSpawn tới N → StopAttackOnly. Không cần sửa EnemyWB2_P3.cs.
- GunSweepAimWB2 (guid 64c1a990…) và SweepAimShotWB2 (guid 0f4abe22…) chỉ dùng trong EnemyWB2_P3_A.prefab.
refs: Project 1941 claude/muse-handoff-lessons-wb2.md (§3, §5, §6)
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P3_A.prefab
done_when: prefab save; grep prefab không còn guid 64c1a9901e36f47cb9d5b0d77a9bf4db và 0f4abe22f937443a9b4f7fe2cdce268c; Console 0 error.

## T4 — Kiểm chứng trong Play
owner: claude
depends: T3
context:
- Chạy scene test P3 (MakeAll hoặc scene WB2 đang dùng), để boss vào MS1. Log tạm (xoá sau khi đo, hoặc dùng execute_code đọc): mỗi viên K1/K2 → góc local z của Rotate và góc hướng đạn so với -Y world (trừ rotZ boss).
- Tiêu chí: (1) trước khi bắn nòng hướng xuống (Rotate z = rest); (2) pha 0 → -75 KHÔNG có đạn, viên đầu ra đúng frame nòng tới -75; (3) K1 đúng 8 viên, góc khớp bảng T3 sai số ≤ 3° (trễ frame của WaitForSeconds), trái→phải; (4) K2 đúng 6 viên mỗi góc, góc khớp ≤ 3° (tính theo hướng mũi boss); (5) viên cuối ở +75, sau đó nòng quay về rest KHÔNG bắn và đứng yên ở rest (không đi tiếp sang -75); (6) đạn sinh ở đầu nòng và bay thẳng theo nòng (chụp màn hình lúc K1 giữa chừng); (7) IE_PlayWeaponOnce log "K1 done (8/8, timeout=False)", "K2 done (6/6, timeout=False)"; (8) MS2 đóng G5 xong nòng ở rest.
verified:
- EnemyWB2_P3.cs:429 K1 qua IE_PlayWeaponOnce(weaponK1, postK1Delay); :446 K2 mỗi góc qua IE_PlayWeaponOnce(weaponK2, postK2Delay); log "[WB2_P3] {K1|K2} done t=… (n/N, timeout=…)".
refs: Project 1941 claude/worldboss-boss-library.md (18.2 WB2 — MS1)
files: (không sửa file; chỉ đo)
done_when: đủ 8 tiêu chí trên, kèm số đo góc từng viên và 2 ảnh chụp (K1 giữa chừng, nòng ở rest sau bắn).

## T5 — Cập nhật knowledge
owner: claude
depends: T4
context:
- Ghi vào Project 1941 claude/worldboss-boss-library.md mục WB2 P3: G5 dùng 2 StepRotation trên Rotate (rest 0 xuống, ±75, step 75, speed -50, Yoyo, stopAtInitAfterMax), ueReachMin → shot.Attack(), weapon ATTACK_ON_EVENT (ueEvent → StepRotation.Play, rotate None), fireRate = (150/|speed|)/(N-1); đã bỏ GunSweepAimWB2/SweepAimShotWB2 khỏi G5.
- Thêm learning ngắn: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-steprotation-sweep.md (StepRotation option ueReachMin/stopAtInitAfterMax/initLocal; vì sao weapon.rotate để None).
verified:
- Thư mục learnings có sẵn worldboss-wb2-onlyanim-gun.md.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/
files: claude/worldboss-boss-library.md (project), learnings/worldboss-steprotation-sweep.md
done_when: 2 tài liệu đã ghi, có số liệu đo ở T4.

## Câu hỏi mở
- Bước 0 → -75 và +75 → 0 chạy cùng speed 50 (StepRotation chỉ có 1 speed) → mỗi bước 1.5s, cả lượt 6s. Muốn nhanh hơn thì cần thêm option tốc độ riêng.
- Giữa lượt quét (-75 → +75) có khựng 1 frame ở 0 (2 step 75°, WaitForSeconds(nextStepDelay=0)). Chấp nhận được.
- K2 tooltip ghi "6 viên ×2 lần" nhưng prefab waves 1 → plan làm 1 lượt 6 viên mỗi góc. Cần 2 lượt/góc thì báo.
