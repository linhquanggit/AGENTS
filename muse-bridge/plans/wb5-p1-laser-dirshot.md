# WB5 P1 — Laser tuần tự, anim bắn G1, góc bắn G1
goal: Laser G2 bắn từng tia cách nhau 0.1s, mỗi tia thu nhỏ dần từ 80% duration đến hết; bật anim Attack cho 6 súng G1-DirShot; chuyển góc nghiêng z từ GO cha súng G1 sang DirectionShotE.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10, 15.6)

## T1 — LaserShotE bật từng tia, delay serialized
owner: muse
depends: -
context:
G2-DirS&LaserS (WB5Weapon2_3) SEQUENCE: DirectionShotE → LaserShotE (11 Laser_CL). Hiện 11 tia bật cùng lúc. Yêu cầu: bật lần lượt, mỗi tia cách 0.1s, giá trị serialized. Không đổi field cũ của LaserShotE (dùng chung nhiều boss).
Đã chốt: mỗi tia chạy timeline riêng, lệch nhau laserInterval: tia i bắt đầu ở i*laserInterval → hiện warning riêng (timeWarning) → bật laser → giữ đủ duration → tắt riêng. OnEndShot gọi sau khi tia cuối tắt (+ waitDelayEnd). PlayExplode/StopExplode cũng theo từng tia.
verified:
LaserE.cs:21 virtual SetShowWarning(bool); :22 PlayExplode(bool); :23 AutoAttack(); :24 StopAttack() — đủ để điều khiển từng tia
LaserShotE.cs:21 [SerializeField] protected LaserE[] lasers (LoadAssets:28 GetComponentsInChildren<LaserE>)
LaserShotE.cs:50 IE_SpawnBullet: warning → waitDelayShot → OnLaser() (bật tất cả) → wait duration → OffLaser → OnEndShot
LaserShotE.cs:130 virtual OnLaser() foreach lasers; :141 virtual OffLaser()
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: tạo Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-5/SequentialLaserShotWB5.cs (subclass LaserShotE, [SerializeField] float laserInterval = 0.1f; override IE_SpawnBullet: mỗi tia 1 coroutine con warning → AutoAttack → duration → StopAttack; OnEndShot sau tia cuối)
done_when: compile 0 error; log thời điểm warning/bật/tắt của 11 tia đều lệch nhau 0.1s

## T2 — Thay LaserShotE trong prefab G2 bằng SequentialLaserShotWB5
owner: claude
depends: T1
context:
Swap component giữ field (helper Swap qua SerializedObject, xem bài học 15.6); gọi LoadAssets; WorldBossWeapon.LoadAssets reset shotSteps → kiểm lại step 2 trỏ đúng shot mới.
verified:
EnemyWB5_P1_A.prefab: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P1_A.prefab, GO G2-DirS&LaserS dòng 60354
files: EnemyWB5_P1_A.prefab
done_when: Play scene test WB5_P1, probe log thời điểm bật 11 tia cách ~0.1s, Console 0 error

## T3 — Bật anim Attack cho 6 súng G1-DirShot
owner: claude
depends: -
context:
Anim bắn KHÔNG chạy: cả 6 súng G1 có flowAttack: Attack nhưng useSelfAnimAttack: 0 → weapon OPEN truyền null cho SetShotAction.
verified:
WorldBossWeapon.cs:40 useSelfAnimAttack=false mặc định; :130 (OPEN) _weapon.SetShotAction(useSelfAnimAttack ? _AnimShot : null, OnOpenShotDone)
EnemyWB5_P1_A.prefab: G1-DirShot-L1/L1-1/L1-2/R/R-1/R-2 đều useSelfAnimAttack 0, flowAttack Attack, flowOpen Open
files: EnemyWB5_P1_A.prefab
done_when: useSelfAnimAttack=1 cả 6; Play: Spine mỗi súng chạy flow Attack mỗi lượt bắn; Console 0 error

## T4 — Chuyển góc z từ GO súng G1 sang DirectionShotE
owner: claude
depends: T3
context:
GO cha đang xoay z → cả Spine anim bị nghiêng. Đã chốt: DirectionShotE là GO con riêng của mỗi súng (xoay nó không ảnh hưởng anim). Với mỗi súng: lấy z của GO cha → đặt vào localRotation z của GO con DirectionShotE (cộng vào z hiện có của con nếu khác 0) → đặt GO cha z=0. Không xoay từng FireDirection.
verified:
Góc z hiện tại (m_LocalRotation GO cha): L1 30 · L1-1 90 · L1-2 150 · R -30 · R-1 -90 · R-2 -150
files: EnemyWB5_P1_A.prefab
done_when: 6 GO cha rotation (0,0,0); hướng đạn world mỗi súng trước/sau giống nhau (log hướng FireDirection trước và sau); Spine súng không nghiêng; Console 0 error

## T5 — Laser thu nhỏ dần từ 80% duration
owner: muse
depends: T1
context:
Mỗi tia G2-DirS&LaserS: từ 80% duration bắt đầu nhỏ dần (theo bề rộng), về 0 đúng lúc hết duration rồi tắt. Mốc 0.8 để serialized (shrinkStartRatio). Tính riêng cho từng tia (các tia bật lệch 0.1s theo T1). Không sửa Laser_CL/LaserE dùng chung → làm trong SequentialLaserShotWB5.
⚠️ CHỈ thu nhỏ BỀ RỘNG (độ dày) tia, KHÔNG đổi chiều dài: không đụng transform.localScale của laser; không đổi positions LineRenderer; collider chỉ đổi size.y, giữ nguyên size.x và offset. LineRenderer.widthMultiplier chỉ ảnh hưởng độ dày nên dùng được.
Đã chốt: thu nhỏ CẢ collider (BoxCollider2D size.y cùng hệ số). Mỗi lần bắn tia phải bắt đầu từ kích thước gốc (hệ số = 1 khi AutoAttack) rồi mới thu nhỏ — KHÔNG lấy giá trị hiện tại lúc bắn làm gốc (tránh dính giá trị đã thu nhỏ của lượt trước). Kích thước gốc lấy 1 lần ở LoadAssets/Init (widthMultiplier gốc, lineWidth/size.y gốc, localScale đèn gốc), hoặc dùng hằng 1 cho widthMultiplier; reset về gốc khi StopAttack/OnEndShot.
verified:
Laser_CL.cs:22 LineRenderer lineRenderer (private, lấy ở Awake:28 GetComponent<LineRenderer>) → shot lấy LineRenderer bằng GetComponent trên từng lasers[i]
Laser_CL.cs:15 [SerializeField] float lineWidth = 0.15f — bề rộng collider; :40 colliderController.SetCollider(lineLength, lineWidth) (UpdateColliderWithLineRenderer)
Laser_CL.cs:10-11 startLight / hitLight (Transform) — FX đầu/cuối tia
Laser_CL.cs:53 (Update/UpdateLineRender) gọi colliderController.SetCollider(|length|, lineWidth) mỗi frame → ghi đè size collider; vì vậy coroutine của shot (chạy sau Update) phải set lại boxCollider.size.y = lineWidth*k mỗi frame trong lúc thu nhỏ
UpdateColliderWithLineRenderer.cs (Assets/DP/Scripts) :8 RequireComponent BoxCollider2D; :43 SetCollider(initX, initY) đặt offset + size
files: SequentialLaserShotWB5.cs ([SerializeField, Range(0,1)] float shrinkStartRatio = 0.8f; mỗi frame k = 1→0 từ shrinkStartRatio*duration tới duration; widthMultiplier = gốc*k, BoxCollider2D.size.y = lineWidth*k, đèn startLight/hitLight (FX điểm đầu/cuối, không phải thân tia) = scale gốc*k; reset gốc khi tắt)
done_when: compile 0 error; Play: log widthMultiplier từng tia = 1 tới 80% duration, giảm tuyến tính, ≈0 lúc tắt; collider size.y giảm cùng tỉ lệ, size.x + chiều dài tia không đổi; lượt bắn thứ 2, 3 mỗi tia bắt đầu lại ở width 1 rồi mới giảm; Console 0 error

## Câu hỏi mở
- (không còn)
