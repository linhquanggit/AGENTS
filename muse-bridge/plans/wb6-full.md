# WB6 "Leviathan Citadel" — code-first 3 phase (placeholder, chưa art)
goal: Viết các class pattern mới + script boss WB6 và dựng/test 9 súng P1–P3 trong MakeAll.unity dưới WB-Shooter bằng placeholder; kích thước/hình dạng/vị trí để field TEMP, chốt khi có anim.
project: ~/wkspaces/WB
source: Project 1941 — BR_WorldBoss/briefs/WB6_PLAN.md, WB6_P1.md, WB6_P2.md, WB6_P3.md, WB6_P1_S1_video_check.md (mục "Chốt 2026-10-23"); library worldboss-boss-library.md mục 11, 12, 15, 16, 17, 19; design R533–R608


## Luật điều phối (Linh chốt 2026-10-23 — áp dụng cho cả plan, ưu tiên hơn skill nếu khác)
- Claude = PM/Leader, làm ÍT NHẤT có thể, chịu trách nhiệm chất lượng. Claude chỉ tự làm: thao tác Unity Editor (chạy menu dựng, Play), review, nghiệm thu, báo Linh. MỌI việc viết/sửa code (kể cả nhỏ) giao Muse; Claude tự viết code chỉ khi Muse BLOCKED/TIMEOUT hoặc >3 vòng review cùng lỗi (ghi `--log "[self-code] lý do"` gắn vào cùng lệnh Bash).
- Cổng review code trước mọi Play: đọc đúng file Muse liệt kê, đối chiếu brief/signature trong `verified:`, lỗi → `muse-ask --bug` chỉ rõ file:line + cách đúng. Chỉ khi PASS mới dựng/Play.
- Hạn chế Unity MCP (rất tốn token): ưu tiên Read/Grep/Bash; lỗi compile → grep ~/Library/Logs/Unity/Editor.log (`error CS`); kết quả test → đọc ~/.claude/logs/flow-capture.log; dựng bằng 1 lệnh menu do Muse viết (T8–T10); không dùng MCP để review/xem thử; gom thao tác.
- Chỉ số: Play/tự-viết-code ghi log gắn vào lệnh đang chạy; không tự dựng bảng chỉ số.
- CHIA VIỆC SONG SONG (tự động, không chờ Linh): lấy task sẵn sàng (chưa done, depends đã done; chạy `muse-ask --plan <id> --ready` nếu có lệnh này, không thì tự đọc `depends:`). Các task `owner: muse` sẵn sàng mà `files:` KHÔNG giao nhau → giao CÙNG LÚC, mỗi task 1 lệnh `muse-ask --plan <id> --task Tn "..."` chạy nền (mỗi task tự có chat riêng), tối đa 4 chat cùng lúc. Trong lúc chờ: review task đã DONE, làm task owner:claude đã sẵn sàng. Mỗi khi 1 task xong + qua review → lấy lại danh sách sẵn sàng và giao tiếp. Nếu lệnh không tự tạo chat riêng được → chạy tuần tự (vẫn đúng). Nhóm đầu của plan này: T1, T2, T4, T6 song song; sau đó T8/T9/T10; rồi T3/T5/T7.
- Không lưu scene, không sửa prefab/scene có sẵn, không đụng WorldBossSO/Addressables/prefab boss. Class pattern dùng lại đặt tên chung (không hậu tố WB6), đúng danh sách đã duyệt trong brief WB6_PLAN.md.
- Editor script đặt ở Assets/Editor/WB6/ (project không dùng asmdef cho code Project/ → Assembly-CSharp-Editor tự thấy runtime class; xác nhận bằng compile).
- KHÔNG LÀM RÁC project: chỉ tạo/sửa đúng file trong `files:` của task. Cổng review phải so danh sách file Muse báo (`| files: …`) với `files:` — file thừa (backup, file thử, script phụ, log) → bắt Muse xoá/dọn trước khi PASS. Không để object/prefab thử trong scene hay thư mục project; file tạm ngoài project để ở ~/.claude/logs. Mọi thứ tạm phải có chỗ dọn (T11); không tạo thêm công cụ/tính năng ngoài plan.

## T1 — Bullet pattern chung: BulletDelayedCurveToTarget + BulletGrowOverTime
owner: muse
depends: -
context:
- Luật mới (AGENT.md 2026-10-23): class pattern tái dùng đặt tên chung, KHÔNG hậu tố boss; bullet → `Assets/Project/Bullets/Scripts/`.
- `BulletDelayedCurveToTarget : Bullet` (P1·S1): bay thẳng `delayBeforeCurve` (1s) → chụp đích 1 lần (`targetMode` Player/Point; không có Fighter thì fallback điểm) → cong dần hướng bay trong `curveDuration` (~1.5s) → bay thẳng, KHÔNG bám; tốc độ nhân `speedMultiplierAfterCurve` (1.2, TEMP). Khác `UbhHomingShot`/`ChasingBullet` (bám liên tục).
- `BulletGrowOverTime` (component gắn bullet bất kỳ, P1·S3 + P2·S4): scale từ `startScale` tới `maxScale` trong `growDuration`, collider scale theo; size TEMP (chốt khi có anim). Reset khi OnEnable (pool).
- Số TEMP đánh dấu `// TEMP`.
verified:
- Bullet.cs:195 `public virtual void Init(GameObject _own, BigDouble _DMG, DAMAGE_TYPE _damageType = DAMAGE_TYPE.CONST, …)`
- Bullet.cs:293 `protected virtual void OnMove()`; Bullet.cs:91 `protected virtual void OnEnable()`; Bullet.cs:143 `protected virtual void Update()`
- Tham khảo: ChasingBullet.cs (Project/Bullets/Scripts), HomingMissileSplitWB5.cs:54 `AimAt(Vector3)`, :63 `UpdateMoveTarget()`
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_PLAN.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1_S1_video_check.md
files: Assets/Project/Bullets/Scripts/BulletDelayedCurveToTarget.cs, Assets/Project/Bullets/Scripts/BulletGrowOverTime.cs
done_when: compile 0 error; field có Tooltip; không sửa Bullet.cs

## T2 — Script boss EnemyWB6_P1/P2/P3 + RingPulseSpiralShot (P1·S2)
owner: muse
depends: -
context:
- `EnemyWB6_P1/P2/P3 : EnemyWorldBoss` đặt cạnh WorldBoss-5 (thư mục `WorldBoss-6/`), rỗng; di chuyển quanh 1/3 trên màn (design R542).
- P2: boss xoay ngang (tween transform, TEMP thay bằng anim) TRƯỚC khi S4 bắn lần đầu và GIỮ NGANG hết Phase 2.
- `RingPulseSpiralShot : BaseShotE` (shot → `BaseShot/`): 8 viên vòng tròn; nhịp bán kính mảng `[×1 MBC, co, ×1.5, co, ×1.5]` → lần 3 toả hẳn theo xoắn ốc, tốc nhanh rồi chậm dần; CD 2.5s, bắn ngay; 2 khẩu bắn CÙNG LÚC. Thời gian mỗi nhịp, tốc xoắn = TEMP field.
verified:
- EnemyWorldBoss.cs:49 `public override void Init(Node _node = null, PathManager _newPath = null, List<EventNode> _lstEvents = null, float _speedMove = 0)`; :89 OnAttack; :109 `IE_PlayChain(WorldBossWeaponChain)`
- Mẫu: Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-5/EnemyWB5_P1.cs, EnemyWB5_P2.cs, EnemyWB5_P3.cs
- BaseShotE.cs:187 `protected abstract IEnumerator IE_SpawnBullet()`; :84 Init; :175 OnEndShot
- Tham khảo 1 nhịp hội tụ: Project/Bullets/Scripts/UbhRingConvergeShot.cs (ringRadius, turn3Delay, burstWaves)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 12, 16)
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-6/EnemyWB6_P1.cs, EnemyWB6_P2.cs, EnemyWB6_P3.cs; Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/RingPulseSpiralShot.cs
done_when: compile 0 error; gizmo dùng `WorldBossGizmos.Label` (WorldBossGizmos.cs:27)

## T3 — Dựng + test Phase 1 (S1, S2, S3)
owner: claude
depends: T1, T2, T8
context:
- HẠN CHẾ MCP: MenuItem `Tools/WB6/Build P1` do Muse viết ở task T8 (Editor script, dựng root + súng + gán field + prefab đạn, ghi tóm tắt ra file) và log debug runtime ghi `~/.claude/logs/flow-capture.log`. Claude chỉ: chạy 1 lệnh menu → Play 1 lần → đọc file log. Compile check bằng grep Editor.log.
- BƯỚC 0 — REVIEW CODE trước khi test (bắt buộc): đọc code Muse giao ở T1, T2 (chỉ file trong `files:`), đối chiếu brief/spec + base class signature; refresh_unity + read_console (compile 0 error). Code sai spec/thiết kế kém → `muse-ask --bug` kèm điểm cần sửa cụ thể (file:line + cách đúng), Muse sửa lại; lặp tới khi ổn (tối đa 3 vòng, quá thì báo Linh). CHỈ khi code ổn mới vào Play/test.
- Trước khi dựng: chạy `BR_WorldBoss/tools/scan_spine_wb.cs.txt` qua Unity MCP execute_code (codedom C# 6) xác nhận chưa có art WB6 → code-first.
- Root `EnemyWB6_P1_A` (placeholder) dưới `WB-Shooter` trong Assets/Project/Scenes/MakeAll.unity; súng = Transform rỗng theo sketch (vị trí TEMP).
- `G1-CurveToPlayer-L/R`: `DirectionShotE` 11 dir 360°, waves 4, fireRate 0.1, pha nạp `chargeDelay` 0.6s (chỗ cho flowCharge), 2 khẩu thay phiên delay 2s → bullet `BulletDelayedCurveToTarget`.
- `G2-RingPulseSpiral-L/R`: `RingPulseSpiralShot`, 2 khẩu cùng lúc, CD 2.5s.
- `G3-GrowOrb-L/R`: mỗi khẩu 2 viên chữ V ngược 15–20° bay tới player (`DestinationShotE`/`DirectionShotE` aim ±8–10°) + `BulletGrowOverTime` tới ×3 MBC (TEMP); CD 3s.
- ATTACK_TYPE không phụ thuộc Spine (OPEN/SEQUENCE flow trống). Không lưu scene khi Linh chưa đồng ý.
verified:
- WorldBossWeapon.cs:21 enum ATTACK_TYPE { ATTACK_ON_EVENT=0, ATTACK_ON_OPEN=1, ATTACK_ON_CHARGE=2, ATTACK_ON_ROTATE=3, ATTACK_ON_SEQUENCE=4 }
- DirectionShotE.cs:82 IE_SpawnBullet, :142 `SpawnBullet(FireDirection)`; DestinationShotE.cs:84 `SpawnBullet(Vector3 _targetPos, FireDirection _fireDir)`
- Assets/Project/Scenes/MakeAll.unity tồn tại
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md, library mục 15.3–15.4
files: MakeAll.unity (không save), prefab đạn mới trong Assets/Project/Bullets/Prefabs/Enemies/ (Bullet_WB6_P1_G1, _G3)
done_when: Play qua WB-Shooter từng súng + cả phase, Console 0 error, hành vi khớp brief; ghi bảng số TEMP

## T4 — Class Phase 2: PylonLinkShot, RotatingBurstShot + BulletStopShakeBurst, RandomExclusiveSequenceShot
owner: muse
depends: -
context:
- `PylonLinkShot : BaseShotE` (`BaseShot/`), 2 cột C1/C2 × n cọc, spawn cọc 1 lần giữ suốt phase, không bị phá huỷ.
  - mode `PairUnique` (P2·S6, 3 cọc/cột, KHÔNG cooldown, loop mãi): mỗi O = 3 L; mỗi L random cột bắt đầu (độc lập L trước), chọn 1 D mỗi cột chưa dùng trong O, tia từ điểm cột bắt đầu → cột kia; nối `linkDuration` → chờ `delayBetweenLinks` → L kế; hết O reset.
  - mode `OneToMany` (P3·S9, 4 cọc/cột): đầu O chọn 4 điểm đầu, mỗi cột ≥1, thứ tự random; mỗi L: điểm đầu nối CÙNG LÚC tới N điểm random cột đối diện (`1<=N<=4` input, được trùng/đã nối), chung `linkDuration` → `delayBetweenLinks`; hết 4 L → `cooldown` → O mới.
  - Dây: `LinkLaser2D_CL`; cọc tham khảo `BulletDestinationWB5`.`LinkPoint`.
- `RotatingBurstShot` + `BulletStopShakeBurst` (P2·S5): 5 viên hình sao → chậm → dừng → rung 0.5s → toả nhanh; lượt sau lệch +30°, bắn khi lượt trước dừng; 4 lượt; 2 khẩu cùng lúc. Nền: UbhBulletStopAndFall.cs (delayStop, stopTime, decelBeforeStop, spin), BulletStopAndFall.cs:47 OnMove.
- `RandomExclusiveSequenceShot` (P2·S4): 5 khẩu random lần lượt, không 2 khẩu cùng lúc, không lặp tới khi hết lượt. Thử mở rộng `RandomShotSelectE` trước (RandomShotSelectE.cs:48 `IE_FireRandomShot`); chỉ tạo class mới nếu không đủ.
verified:
- LinkLaserShotE.cs:66 OnLaser, :89 LinkEnd, :108 OffLaser; LaserShotE.cs:83 OnStartWarning
- DestinationLinkShotWB5.cs (SpecialCase) — mẫu dùng LinkLaser + BulletDestinationWB5
- Prefab Assets/Project/GamePlay/Weapons/Prefabs/LinkLaser2D_CL.prefab; Bullet_Electric_MB43.prefab (Project/Bullets/Prefabs/Enemies)
- RandomShotSelectE.cs:17 Init, :31 IE_SpawnBullet
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md (spec S9)
files: BaseShot/PylonLinkShot.cs, BaseShot/RotatingBurstShot.cs, Bullets/Scripts/BulletStopShakeBurst.cs, BaseShot/RandomExclusiveSequenceShot.cs (nếu cần)
done_when: compile 0 error; PylonLinkShot có gizmo vẽ cọc + log L/O khi bật debug

## T5 — Dựng + test Phase 2 (S4, S5, S6)
owner: claude
depends: T1, T2, T4, T9
context:
- HẠN CHẾ MCP: MenuItem `Tools/WB6/Build P2` do Muse viết ở task T9 (Editor script, dựng root + súng + gán field + prefab đạn, ghi tóm tắt ra file) và log debug runtime ghi `~/.claude/logs/flow-capture.log`. Claude chỉ: chạy 1 lệnh menu → Play 1 lần → đọc file log. Compile check bằng grep Editor.log.
- BƯỚC 0 — REVIEW CODE trước khi test (bắt buộc): đọc code Muse giao ở T2, T4 (chỉ file trong `files:`), đối chiếu brief/spec + base class signature; refresh_unity + read_console (compile 0 error). Code sai spec/thiết kế kém → `muse-ask --bug` kèm điểm cần sửa cụ thể (file:line + cách đúng), Muse sửa lại; lặp tới khi ổn (tối đa 3 vòng, quá thì báo Linh). CHỈ khi code ổn mới vào Play/test.
- Root `EnemyWB6_P2_A`; `G4-GrowOrbDrop` (con G4-1..5, hàng ngang khi boss xoay) đạn rơi thẳng + `BulletGrowOverTime` (max = màn/5, TEMP); `G5-StarBurst-L/R`; `G6-PylonLink` mode PairUnique, spawn đầu Phase 2, chạy SONG SONG ngoài chain S4/S5.
- Boss xoay ngang trước S4, giữ ngang hết phase.
verified:
- (dùng kết quả T3 cho cấu trúc WB-Shooter)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md, library mục 12 (bẫy chain)
files: MakeAll.unity (không save), prefab đạn Bullet_WB6_P2_G4/_G5, cọc Pylon_WB6
done_when: Play từng súng + cả phase; S6 log đúng luật R1–R6 qua ≥3 O; Console 0 error

## T6 — Class Phase 3: PolygonLaserShot (P3·S8)
owner: muse
depends: -
context:
- `PolygonLaserShot : LaserShotE` hoặc BaseShotE (`BaseShot/`): n cạnh laser khép kín (n=3), charge `chargeTime` 3s có warning, bắn `duration` (TEMP), chỉ các góc an toàn; mỗi lần bắn xoay `rotateStep` (30° CW). Kích thước = field TEMP (chốt khi có anim).
- S8 tự loop riêng, KHÔNG chain với S9.
verified:
- LaserShotE.cs:31 Init, :50 IE_SpawnBullet, :77 SetLaserShotAction, :83 OnStartWarning, :89 OnStartAttacking
- LinkLaserShotE.cs (mẫu nối 2 điểm)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PolygonLaserShot.cs
done_when: compile 0 error; gizmo vẽ đa giác + góc an toàn

## T7 — Dựng + test Phase 3 (S7, S8, S9) + báo cáo + library
owner: claude
depends: T2, T4, T6, T10
context:
- HẠN CHẾ MCP: MenuItem `Tools/WB6/Build P3` do Muse viết ở task T10 (Editor script, dựng root + súng + gán field + prefab đạn, ghi tóm tắt ra file) và log debug runtime ghi `~/.claude/logs/flow-capture.log`. Claude chỉ: chạy 1 lệnh menu → Play 1 lần → đọc file log. Compile check bằng grep Editor.log.
- BƯỚC 0 — REVIEW CODE trước khi test (bắt buộc): đọc code Muse giao ở T2, T4, T6 (chỉ file trong `files:`), đối chiếu brief/spec + base class signature; refresh_unity + read_console (compile 0 error). Code sai spec/thiết kế kém → `muse-ask --bug` kèm điểm cần sửa cụ thể (file:line + cách đúng), Muse sửa lại; lặp tới khi ổn (tối đa 3 vòng, quá thì báo Linh). CHỈ khi code ổn mới vào Play/test.
- Root `EnemyWB6_P3_A`; `G7-MissileSplit-L/R`: `LauncherShotE` → `HomingMissileSplitWB5` (3 quả, delay 1.5s, splitDelay 1, tách 4 quả nón, sau 0.5s tăng tốc, maxTrackAngle 20); `G8-TriangleLaser`: PolygonLaserShot; `G9-PylonLink`: mode OneToMany, N input, loop riêng có CD.
- Kết thúc: báo cáo bảng (AGENT.md mục 5); cập nhật library mục 11, 17, 18.6, 19 + đồng bộ project doc `claude/worldboss-boss-library.md`; ghi bảng số TEMP vào briefs.
verified:
- HomingMissileSplitWB5.cs:29 `Init(BulletHomingMissileData)`, :96 Split; dùng ở WB5 P2 G4 (library dòng G4-Rocket-L/R)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: MakeAll.unity (không save), BR_WorldBoss/library/worldboss-boss-library.md, briefs WB6_*.md
- CHỈ SỐ (tiết kiệm token): KHÔNG tự dựng bảng chỉ số. Chỉ khi Play hoặc tự viết code thì gắn `&& muse-ask --log "[play] …"` / `"[self-code] …"` vào CÙNG lệnh Bash đang chạy (không thêm lượt riêng); không ghi log `[mcp]` (Musude đếm từ transcript). Cuối mission báo Linh 1 dòng: xem chỉ số trong Musude / ~/AGENTS/Reports/WB/metrics.md.
done_when: Play từng súng + cả phase, Console 0 error; báo cáo gửi Linh; library + project doc cập nhật

## T8 — Editor script dựng Phase 1 (MenuItem Tools/WB6/Build P1)
owner: muse
depends: T1, T2
context:
- Dựng theo task T3: root EnemyWB6_P1_A; G1-CurveToPlayer-L/R, G2-RingPulseSpiral-L/R, G3-GrowOrb-L/R với đúng thông số trong brief WB6_P1.md (mục Chốt + S1 chốt sau video). Đạn: Bullet_WB6_P1_G1 (BulletDelayedCurveToTarget), Bullet_WB6_P1_G3 (BulletGrowOverTime).
- Muse KHÔNG thấy Editor: viết Editor script dựa trên code đã có, không đoán tên field — đọc class liên quan + `WorldBossWeapon.cs` + library mục 15.3 (quy ước dựng dưới `WB-Shooter` trong Assets/Project/Scenes/MakeAll.unity) + 1 mẫu đã dựng (WB5 P2: `G4-Rocket-L/R`, `G6-ElectricLink` trong library mục 10).
- Được phép TẠO prefab đạn MỚI (task này cho phép rõ) qua PrefabUtility vào Assets/Project/Bullets/Prefabs/Enemies/; KHÔNG sửa prefab/scene có sẵn, KHÔNG lưu scene (không SaveScene), không đụng WorldBossSO/Addressables/prefab boss.
- Menu idempotent: chạy lại không nhân đôi object (xoá/tạo lại root `EnemyWB6_P{n}_A` của mình). Placeholder = SpriteRenderer mượn; súng đặt TEMP theo sketch.
- Cuối menu ghi tóm tắt (đường dẫn hierarchy từng súng, thông số TEMP đã gán, cảnh báo) ra `~/.claude/logs/wb6-build-P{n}.log`; runtime log debug của từng súng ghi `~/.claude/logs/flow-capture.log`.
verified:
- Quy ước dựng + mẫu: library worldboss-boss-library.md mục 15.3, 10, 12; WorldBossWeapon.cs:21 enum ATTACK_TYPE
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md
files: Assets/Editor/WB6/Build_WB6_P1.cs
done_when: compile 0 error (Claude đọc review code); chạy menu tạo đúng hierarchy theo brief (Claude kiểm bằng 1 lần chạy + đọc log)

## T9 — Editor script dựng Phase 2 (MenuItem Tools/WB6/Build P2)
owner: muse
depends: T1, T2, T4
context:
- Dựng theo task T5: root EnemyWB6_P2_A; G4-GrowOrbDrop (con G4-1..5), G5-StarBurst-L/R, G6-PylonLink (mode PairUnique, spawn đầu phase, song song). Boss xoay ngang tween giữ ngang hết phase. Đạn: Bullet_WB6_P2_G4, _G5, cọc Pylon_WB6.
- Muse KHÔNG thấy Editor: viết Editor script dựa trên code đã có, không đoán tên field — đọc class liên quan + `WorldBossWeapon.cs` + library mục 15.3 (quy ước dựng dưới `WB-Shooter` trong Assets/Project/Scenes/MakeAll.unity) + 1 mẫu đã dựng (WB5 P2: `G4-Rocket-L/R`, `G6-ElectricLink` trong library mục 10).
- Được phép TẠO prefab đạn MỚI (task này cho phép rõ) qua PrefabUtility vào Assets/Project/Bullets/Prefabs/Enemies/; KHÔNG sửa prefab/scene có sẵn, KHÔNG lưu scene (không SaveScene), không đụng WorldBossSO/Addressables/prefab boss.
- Menu idempotent: chạy lại không nhân đôi object (xoá/tạo lại root `EnemyWB6_P{n}_A` của mình). Placeholder = SpriteRenderer mượn; súng đặt TEMP theo sketch.
- Cuối menu ghi tóm tắt (đường dẫn hierarchy từng súng, thông số TEMP đã gán, cảnh báo) ra `~/.claude/logs/wb6-build-P{n}.log`; runtime log debug của từng súng ghi `~/.claude/logs/flow-capture.log`.
verified:
- Quy ước dựng + mẫu: library worldboss-boss-library.md mục 15.3, 10, 12; WorldBossWeapon.cs:21 enum ATTACK_TYPE
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md
files: Assets/Editor/WB6/Build_WB6_P2.cs
done_when: compile 0 error (Claude đọc review code); chạy menu tạo đúng hierarchy theo brief (Claude kiểm bằng 1 lần chạy + đọc log)

## T10 — Editor script dựng Phase 3 (MenuItem Tools/WB6/Build P3)
owner: muse
depends: T2, T4, T6
context:
- Dựng theo task T7: root EnemyWB6_P3_A; G7-MissileSplit-L/R (LauncherShotE → HomingMissileSplitWB5), G8-TriangleLaser (PolygonLaserShot), G9-PylonLink (OneToMany, loop riêng có CD). S8/S9 chạy riêng, không chain.
- Muse KHÔNG thấy Editor: viết Editor script dựa trên code đã có, không đoán tên field — đọc class liên quan + `WorldBossWeapon.cs` + library mục 15.3 (quy ước dựng dưới `WB-Shooter` trong Assets/Project/Scenes/MakeAll.unity) + 1 mẫu đã dựng (WB5 P2: `G4-Rocket-L/R`, `G6-ElectricLink` trong library mục 10).
- Được phép TẠO prefab đạn MỚI (task này cho phép rõ) qua PrefabUtility vào Assets/Project/Bullets/Prefabs/Enemies/; KHÔNG sửa prefab/scene có sẵn, KHÔNG lưu scene (không SaveScene), không đụng WorldBossSO/Addressables/prefab boss.
- Menu idempotent: chạy lại không nhân đôi object (xoá/tạo lại root `EnemyWB6_P{n}_A` của mình). Placeholder = SpriteRenderer mượn; súng đặt TEMP theo sketch.
- Cuối menu ghi tóm tắt (đường dẫn hierarchy từng súng, thông số TEMP đã gán, cảnh báo) ra `~/.claude/logs/wb6-build-P{n}.log`; runtime log debug của từng súng ghi `~/.claude/logs/flow-capture.log`.
verified:
- Quy ước dựng + mẫu: library worldboss-boss-library.md mục 15.3, 10, 12; WorldBossWeapon.cs:21 enum ATTACK_TYPE
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md
files: Assets/Editor/WB6/Build_WB6_P3.cs
done_when: compile 0 error (Claude đọc review code); chạy menu tạo đúng hierarchy theo brief (Claude kiểm bằng 1 lần chạy + đọc log)

## T11 — Dọn script dựng tạm (T8–T10)
owner: claude
depends: T3, T5, T7
context:
- `Assets/Editor/WB6/Build_WB6_P1/P2/P3.cs` chỉ là công cụ dựng placeholder, không phải code sản phẩm. Dọn khi xong để không để rác trong project.
- ĐIỂM DỪNG xoá file: không tự xoá. Đánh task này `--status blocked "Xoá Assets/Editor/WB6/ (3 script dựng tạm + .meta) và ~/.claude/logs/wb6-build-P*.log? Chỉ OK sau khi Linh đã lưu MakeAll hoặc xác nhận không cần dựng lại"` rồi hỏi Linh gộp ở cuối. MakeAll không được tự lưu nên xoá sớm = mất khả năng dựng lại.
- Linh đồng ý → xoá đúng: Assets/Editor/WB6/ (thư mục + `Assets/Editor/WB6.meta` + file .cs/.meta bên trong), ~/.claude/logs/wb6-build-P*.log. TUYỆT ĐỐI không xoá class pattern (BaseShot/, Bullets/Scripts/), prefab đạn, EnemyWB6_P*.cs, hay gì ngoài danh sách này. Rồi grep Editor.log kiểm không có `error CS` mới.
verified:
- Đường dẫn do T8–T10 tạo: Assets/Editor/WB6/Build_WB6_P{1,2,3}.cs
refs: -
files: Assets/Editor/WB6/ (xoá)
done_when: Linh xác nhận → thư mục Assets/Editor/WB6 không còn, compile 0 error; hoặc task ở trạng thái blocked kèm câu hỏi xoá nếu Linh chưa trả lời

## Câu hỏi mở
- Số TEMP chưa có: tốc độ đạn S1/S2/S3/S5; CD S4, S5, S7, S8, S9; delay giữa khẩu S4; linkDuration/delayBetweenLinks S6/S9 — đặt TEMP, Linh chỉnh sau.
- Kích thước/hình dạng/vị trí súng (thân Sketchfab vs concept S1–S9, size tam giác S8, max scale cầu) — chốt khi có anim.
- Chưa tải lại Google Sheet sau 2026-10-19 (bước 0 AGENT.md) — design WB6 local không đổi so với bản 10-19.
- Prefab boss `_A`, Node, Addressables, WorldBossSO — ngoài quyền agent (dev).
