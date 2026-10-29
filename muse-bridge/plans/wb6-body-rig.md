# WB6 — thân 3 phase: spline (Path 1 appear + Path 2 di chuyển) + Rigidbody2D + collider
goal: Cho 3 object thân EnemyWB6_P1_A / P2_A / P3_A trong MakeAll.unity có đủ bộ "thân boss" như EnemyWB5_P2_A: 6 prefab path Path_WB6_P{1,2,3}_{1,2}, 2 splineMove nối vào mySplineMoves, Rigidbody2D, collider trên root, tag Enemies. Verify bằng probe Play (đường bay đúng 1/3 trên màn, boss không trôi, collider trúng).
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 5, 12, 15.3, 15.6, 18.6); briefs WB6_PLAN/WB6_P2/WB6_P3 (BR_WorldBoss); đối chiếu YAML MakeAll.unity + EnemyWB5_P2_A.prefab ngày 2026-10-27

## Luật điều phối (ưu tiên hơn skill nếu khác)
- Claude = PM/Leader, làm ÍT NHẤT: chạy menu Editor, Play, review, nghiệm thu, báo Linh. Viết/sửa code giao Muse; Claude tự viết chỉ khi Muse BLOCKED/TIMEOUT hoặc >3 vòng review cùng lỗi (ghi `--log "[self-code] lý do"`).
- Review code trước mọi Play: đọc đúng file Muse liệt kê, đối chiếu `verified:`. Hạn chế Unity MCP: lỗi compile → grep ~/Library/Logs/Unity/Editor.log (`error CS`); kết quả test → đọc log file; thao tác scene bằng 1 lệnh menu.
- Chỉ tạo/sửa đúng file trong `files:`. Không đụng WorldBossSO / Addressables / prefab boss `_A` / Node / script EnemyWB6_P*.cs. Không file backup/thử trong project.
- Trạng thái active: EnemyWB6_P1_A, P2_A, P3_A hiện đều OFF trong MakeAll → cuối mission trả đúng như vậy (menu test tự bật 1 phase để test rồi tắt lại).
- Lưu MakeAll: chỉ 1 lần ở T5 sau khi test PASS (Linh đã OK kiểu này ở plan wb6-art-p1-2). Các task khác KHÔNG lưu scene.

## T1 — Editor script dựng rig thân (MenuItem Tools/WB6/BuildBodyRig)
owner: muse
depends: -
context:
- Mẫu = EnemyWB5_P2_A (root): layer 6, tag `Enemies`; 1 Rigidbody2D Dynamic (BodyType 0), gravityScale 0, mass 1, drag 0, angularDrag 0.05, constraints 0, interpolate 0, collisionDetection 0; 3 CapsuleCollider2D trên ROOT (2 trigger + 1 không trigger); 2 splineMove.
- splineMove #1 (appear): pathContainer = Path_{WB}_P{p}_1 (prefab asset, không phải instance trong scene), speed 0.68, loopType 0 (none), pathType 1, easeType 3, moveToPath 0, timeValue 1, localType 0.
- splineMove #2 (di chuyển trong phase): pathContainer = Path_{WB}_P{p}_2, speed 0.5, loopType 1 (loop), pathType 1, easeType 4, moveToPath 0, timeValue 1, localType 0.
- Gán `EnemyBoss.mySplineMoves = [splineMove#1, splineMove#2]` (đúng thứ tự: index 0 = Path 1, index 1 = Path 2; code gọi StartPath(1) rồi StartPath(2)). Set bằng SerializedObject.
- Path prefab mới: clone từ `Path_WB5_P1_1.prefab` / `Path_WB5_P1_2.prefab` (cùng thư mục) → `Path_WB6_P1_1/_2`, `Path_WB6_P2_1/_2`, `Path_WB6_P3_1/_2` (AssetDatabase.CopyAsset giữ child waypoint; path 1 = bay vào từ trên màn xuống tới điểm đầu path 2; path 2 = di chuyển quanh 1/3 trên màn, design R180/R240 "xung quanh giữa 1/3 màn hình trên"). Hình dạng TEMP: dùng nguyên hình WB5 P1; Linh chỉnh bằng PathManager sau.
- Điều kiện nối: waypoint cuối Path 1 (thế giới) phải trùng waypoint đầu Path 2 (thế giới) để boss không nhảy khi đổi path — Muse kiểm và đặt `Path_WB6_P{p}_2` root position cho khớp (mẫu WB5: Path_WB5_P2_1 root y=11, waypoint cuối local y=-6.07 → 4.93; Path_WB5_P2_2 root y=4.93).
- Collider (TEMP): đo bounds thân từ child `Animation` (SkeletonAnimation + MeshRenderer; gọi skeletonAnimation.Initialize(true) + Update(0)/LateUpdate trước khi đọc MeshRenderer.bounds, đổi sang local của root). Tạo: 1 CapsuleCollider2D không trigger (direction Vertical, size ≈ 0.55w × 0.8h, tâm = tâm bounds) + 1 CapsuleCollider2D trigger (size ≈ 0.9w × 0.95h). Tên/số TEMP ghi ra log. Collider PHẢI nằm trên root (EnemyWorldBoss lấy `GetComponents<Collider2D>()` của root).
- Menu idempotent: chạy lại không nhân đôi Rigidbody2D/collider/splineMove, không tạo lại prefab path đã có (bỏ qua + ghi log). Không lưu scene. Không bật root (giữ trạng thái active như lúc đọc). Cuối menu ghi tóm tắt (path đã tạo, vị trí nối, collider size, cảnh báo) ra ~/.claude/logs/wb6-bodyrig-build.log.
verified:
- EnemyBoss.cs:17 `[SerializeField] protected splineMove[] mySplineMoves;` · EnemyBoss.cs:93-97 `protected void StartPath(int index)` gọi `mySplineMoves[index - 1].StartMove()` (guard `Length < index`)
- EnemyWorldBoss.cs:20 `class EnemyWorldBoss : EnemyBoss` · :66 `colliders = GetComponents<Collider2D>();` · :165-169 `ChangeToPlayingMode`: SetColliderEnable(true) → PLAYING → OnAttack() → StartPath(2) · :171 `OnEndPath1` → StopPath(1) → IE_ActionAppear
- splineMove.cs (SimpleWaypointSystem/Scripts/Movement/splineMove.cs):103 `enum LoopType { none, loop, ...}`
- EnemyWB5_P2_A.prefab: root layer 6 tag Enemies; Rigidbody2D m_BodyType 0 gravity 0 (dòng ~305-312); 3 CapsuleCollider2D (isTrigger 1, 1, 0) dòng ~328-365; 2 splineMove dòng ~474 (path guid 3f3d6a8c… = Path_WB5_P2_1, speed 0.68, loop 0, ease 3) và ~535 (guid 489a5112… = Path_WB5_P2_2, speed 0.5, loop 1, ease 4); mySplineMoves 2 phần tử dòng 442-444
- Paths: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/ có Path_WB2_P{1-3}_{1,2}, Path_WB5_P{1-3}_{1,2}; CHƯA có Path_WB6_*
- MakeAll.unity: EnemyWB6_P1_A (go 1333278135), P2_A (go 1100855544), P3_A (go 554191095): layer 6, tag Untagged; root chỉ có Transform + script (EnemyWB6_P1 guid 2b57459b…, P2 335dcf7f…, P3 f82f8703…); KHÔNG có Rigidbody2D / Collider2D / splineMove; localPos (0,3,0), scale 1; mỗi root có child `Animation` (SkeletonAnimation guid d247ba06…); parent fileID 1483811199 (WB-Shooter)
- Spine thân: World_Boss_6.json skeleton width 580.71 × height 991.7 (Spine units), anim In/Idle1/…/Dead
- Library mục 15.6: collider KHÔNG trigger trong boss có Rigidbody2D dynamic có thể đẩy boss trôi → phải đo `bossY` khi test (T4)
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md (quy ước thân/súng WB), claude/worldboss-boss-library.md mục 15.3, 15.6 (project 1941)
files: Assets/Editor/WB6/BuildBodyRig_WB6.cs; (menu tạo) Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/Path_WB6_P1_1.prefab, Path_WB6_P1_2.prefab, Path_WB6_P2_1.prefab, Path_WB6_P2_2.prefab, Path_WB6_P3_1.prefab, Path_WB6_P3_2.prefab
done_when: compile 0 error (Claude đọc review code); file Editor script là file duy nhất trong Assets/Editor/WB6/; code idempotent và không lưu scene

## T2 — Editor script test rig 1 lệnh (MenuItem Tools/WB6/TestBodyRig P1|P2|P3)
owner: muse
depends: -
context:
- Mỗi menu: chỉ bật root phase đó (các root/phase khác OFF), focus + vào Play, chạy probe, tự thoát Play, tắt lại root, ghi log ~/.claude/logs/wb6-bodyrig-test-P{n}.log, in dòng cuối `RESULT PASS|FAIL <lý do>`.
- Boss không qua Init/Node trong MakeAll → probe gọi bằng reflection: `StartPath(1)` (protected, EnemyBoss.cs:93), đăng ký `splineMove.movementEndEvent` của mySplineMoves[0] (hoặc poll vị trí) → khi hết Path 1 gọi `StopPath(1)` + `StartPath(2)` (giả lập ChangeToPlayingMode, EnemyWorldBoss.cs:165-169). Không gọi OnAttack (không cần bắn).
- Probe EditorApplication.update / coroutine ghi mỗi 0.5s: world pos boss (x,y), `Rigidbody2D.velocity`, số Collider2D trên root, trạng thái path (1/2). Chạy tới hết Path 1 + ≥ 10s ở Path 2.
- Kiểm (PASS khi đủ): (a) trong Path 1 y giảm đều từ ngoài màn xuống điểm đầu Path 2, không nhảy > 0.5 đơn vị khi đổi path; (b) trong Path 2 boss luôn nằm trong 1/3 trên màn (y ≥ khoảng 1/3 trên, lấy theo camera orthographic size của scene; ghi số đo), x trong màn; (c) Rigidbody2D.velocity ≈ 0 khi spline đang điều khiển và boss không trôi ra ngoài đường path (sai lệch so với waypoint gần nhất < 0.2); (d) `Physics2D.OverlapPoint(tâm thân)` trả ≥ 1 collider thuộc root, tag Enemies, layer 6; collider không trigger đúng 1, trigger đúng 1; (e) 0 lỗi Console trong lúc Play.
- P2 thêm: gọi `OnAttack()` trước (EnemyWB6_P2.cs OnAttack → xoay ngang `sidewaysAngle` -90° / 0.8s, rotateTarget trống = xoay root) rồi kiểm (f) sau khi xoay: `collider.bounds` rộng > cao (thân đã ngang), OverlapPoint tâm vẫn trúng, boss không trôi; ghi `isSideways`.
- Không lưu scene. Không tạo thư mục/ảnh trong project; ảnh (nếu có) chỉ ở ~/AGENTS/muse-bridge/.agent/shots/.
verified:
- ShooterWorldBoss.cs:4 chỉ điều khiển 1 `WorldBossWeapon` (Init/Play dòng 24, 41-51) → KHÔNG chạy path của boss; vì vậy test rig phải tự gọi StartPath bằng reflection
- EnemyBoss.cs:93 StartPath(int) protected · :99 StopPath(int) · :134-137 movementEndEvent += OnEndPath1 (đăng ký ở chỗ đang đọc), OnEndPath1 virtual
- EnemyWorldBoss.cs:171-175 `OnEndPath1` → `StopPath(1); StartCoroutine(nameof(IE_ActionAppear))`
- EnemyWB6_P2.cs: `class EnemyWB6_P2 : EnemyWorldBoss`, field rotateTarget / sidewaysAngle (-90) / rotateDuration (0.8), `OnAttack()` xoay trước rồi base.OnAttack(); `Init` reset góc
- EnemyWB6_P1.cs, EnemyWB6_P3.cs: class rỗng `: EnemyWorldBoss` (không có logic di chuyển — Path 2 do splineMove/Node lo)
- Mẫu probe & luật "vào Play → đọc Console ngay, có lỗi thì dừng Play": library mục 15.6 (Test nhanh, an toàn)
refs: claude/worldboss-boss-library.md mục 15.6 (project 1941)
files: Assets/Editor/WB6/TestBodyRig_WB6.cs
done_when: compile 0 error (Claude đọc review code); file Editor script là file duy nhất thêm vào Assets/Editor/WB6/; menu ghi log đúng đường dẫn và có dòng `RESULT`

## T3 — Chạy BuildBodyRig và kiểm cấu hình
owner: claude
depends: T1, T2
context:
- Chỉ chạy sau khi review code T1 + T2 PASS. Chạy `Tools/WB6/BuildBodyRig` 1 lần (execute_menu_item), rồi chạy lần 2 để chắc idempotent (số component không đổi).
- Đọc ~/.claude/logs/wb6-bodyrig-build.log + liệt kê 6 file Path_WB6_*.prefab + .meta. Không dùng MCP để duyệt hierarchy từng object; ưu tiên Read/Grep log. Nếu cần xác nhận scene thì 1 lệnh đọc (SerializedObject/find) cho cả 3 root.
verified:
- (điền từ T1: sau khi chạy lại mới biết số thật)
refs: -
files: (không sửa code) — đọc ~/.claude/logs/wb6-bodyrig-build.log
done_when: compile 0 error; mỗi root P1/P2/P3 có đúng 1 Rigidbody2D (Dynamic, gravity 0), 2 CapsuleCollider2D (1 trigger + 1 không), 2 splineMove, mySplineMoves = 2 phần tử đúng thứ tự (Path_WB6_P{n}_1, Path_WB6_P{n}_2), tag Enemies layer 6; 6 prefab Path_WB6_* tồn tại; chạy lần 2 không nhân đôi; trạng thái active 3 root vẫn OFF

## T4 — Test rig từng phase (P1 → P2 → P3, tuần tự, chung 1 Editor)
owner: claude
depends: T3
context:
- Chạy lần lượt `Tools/WB6/TestBodyRig P1`, rồi P2, rồi P3 (mỗi lệnh = 1 Play, tự thoát, 1 log). Đọc dòng `RESULT` + log; không xem bằng ảnh.
- FAIL thì xác định nguyên nhân từ log (trôi do collider đẩy? path lệch? Path 1 cuối ≠ Path 2 đầu?) → `muse-ask --bug` nêu file:line + cách đúng, sửa → chạy lại đúng phase đó. Tối đa 3 vòng/lỗi.
- Gợi ý lỗi hay gặp (từ WB5): collider không trigger + Rigidbody2D dynamic đẩy boss trôi xuống (chỉnh: đổi collider đó sang trigger, hoặc Rigidbody2D kinematic); Path đổi hướng/ease làm nhảy khi chuyển Path 1 → Path 2.
verified:
- (điền từ log T2/T3)
refs: claude/worldboss-boss-library.md mục 15.6
files: (không sửa code trừ khi vòng sửa lỗi) — đọc ~/.claude/logs/wb6-bodyrig-test-P1.log, -P2.log, -P3.log
done_when: cả 3 log có `RESULT PASS`; P2 có thêm (f) PASS sau khi xoay ngang; 0 lỗi Console; 3 root trở về OFF

## T5 — Nghiệm thu, lưu scene, dọn, cập nhật library
owner: claude
depends: T4
context:
- Báo Linh bảng số đo (độ cao Path 2 so với 1/3 trên, collider size TEMP, tốc độ spline) + việc cần Linh chỉnh tay (hình path, size collider theo art).
- Lưu MakeAll.unity MỘT LẦN (menu/Editor save) sau khi PASS; kiểm `git status`/diff scene chỉ đổi 3 root EnemyWB6_P{1,2,3}_A.
- Cập nhật library (project 1941, `claude/worldboss-boss-library.md` mục 18.6 hoặc mục 11): thân WB6 đã có spline + Rigidbody2D + collider, danh sách Path_WB6_*, số TEMP. Đọc doc → sửa → `project_write` đúng đường dẫn cũ.
- DỌN (BẮT BUỘC, TỰ XOÁ sau khi PASS, không hỏi lại; nếu hệ thống chặn xoá thì xin `device_request_delete_permission` đúng 1 lần):
  XOÁ: Assets/Editor/WB6/BuildBodyRig_WB6.cs + .meta, Assets/Editor/WB6/TestBodyRig_WB6.cs + .meta, thư mục Assets/Editor/WB6 (+ .meta của thư mục) nếu rỗng; ~/.claude/logs/wb6-bodyrig-*; ~/AGENTS/muse-bridge/.agent/shots/* nếu có ảnh của mission này; mọi file thử/backup Muse lỡ tạo ngoài `files:`.
  GIỮ LẠI: 6 prefab Path_WB6_P{1,2,3}_{1,2}.prefab (+ .meta); component Rigidbody2D/CapsuleCollider2D/splineMove + tag trên 3 root trong MakeAll.unity (đã lưu); mọi script EnemyWB6_*.cs và prefab đạn WB6.
- Báo cáo cuối có dòng `Đã dọn: …`.
verified:
- Assets/Editor/WB6 đã bị xoá ở mission trước (wb6-art-p1-2 T7) → hiện không còn thư mục; mission này tạo lại → phải dọn lại
refs: claude/worldboss-boss-library.md
files: MakeAll.unity (lưu), claude/worldboss-boss-library.md (project 1941), dọn đồ tạm như trên
done_when: scene đã lưu và chỉ đổi 3 root WB6; library đã cập nhật; không còn file tạm trong project (Assets/Editor/WB6 không tồn tại) / ~/.claude/logs/wb6-bodyrig-* / shots

## Câu hỏi mở
- Hình dạng Path 1/Path 2 của WB6: đang dùng tạm hình Path WB5 P1 (vào từ trên, rồi lượn quanh). Linh có muốn hình khác (số 8, oval…) cho từng phase không, hay tự chỉnh bằng PathManager sau?
- Collider: WB5 P2 có 3 capsule (2 trigger + 1 không); plan này dựng 2 (1 + 1) cho gọn. Nếu GD cần 3 vùng nhận hit (vd mũi/thân/đuôi) thì cho biết vị trí, nếu không giữ 2.
- P2 xoay ngang -90° bằng xoay transform root: Rigidbody2D constraints đang 0 (cho phép xoay). Nếu thấy boss giật khi xoay ở T4 thì cần đổi sang xoay `rotateTarget` là child `Animation` (collider đứng yên) — chờ kết quả test.
