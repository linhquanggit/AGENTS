# WB6 P2 — boss vào ngang + giữ ngang cả phase; 2 gun 6 (Gun6_1, Gun6_2) bắn ra 6 cọc; logic nối giữ nguyên
goal: (1) Phase 2 boss xuất hiện (appear) đã nằm ngang, ĐI TỪ TRÁI SANG vào màn, và hiển thị ngang cả phase — bỏ cơ chế "đợi xoay ngang rồi mới bắn". (2) Thêm Spine Gun6 thứ 2 vào bone Gun6_2 (gun 1 Linh đã sửa sang Gun6_1); mỗi gun "bắn ra" cột 3 cọc của mình (spawn tại bệ, bay tới anchor, đứng yên) và phát anim theo nhịp. Luật nối PylonLinkShot PairUnique (3 L/O, R1–R6) KHÔNG đổi.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (18.6 WB6, mục 15), BR_WorldBoss/briefs/WB6_P2.md (mục "Chốt Linh 2026-10-23"). Linh chốt 2026-10-28: "có 2 gun 6, logic bắn ra 6 cọc như cũ"; có gắn anim theo nhịp; cọc spawn tại bệ gun; "phase 2 đi ngang vào và hiển thị ngang con tàu cả phase", "đi ngang là đi từ trái qua, không phải từ trên xuống" (thay cho chốt cũ 2026-10-23 "boss xoay ngang trước S4 bằng tween").

## T1 — Muse: PylonLinkShot hỗ trợ "phóng cọc từ bệ" + hook lượt nối
owner: muse
depends: -
context:
- Giữ nguyên hành vi mặc định (P3 S9 dùng chung PylonLinkShot, không được đổi): thêm field TUỲ CHỌN, để trống = chạy y như cũ.
- Cần: (a) `Transform[] column1LaunchFrom` / `column2LaunchFrom` (hoặc 1 Transform/cột, = bệ gun) + `launchSpeed` (TEMP); cọc spawn tại launch origin rồi bay tới anchor D (dùng `BulletDestinationWB5.InitTarget` + speed thật), KHÔNG pin vào anchor cho tới khi cọc `IsArrived`; (b) `IE_PairUniqueLoop` chỉ bắt đầu O đầu tiên sau khi cả 6 cọc đã tới nơi (nếu có launch origin); (c) event/UnityEvent `OnLinkFired(bool startIsC1)` ngay lúc `FireLink` của mỗi L; (d) `OnLaunchStarted` để phát anim gun.
- Quy ước repo: class pattern chung đặt tên chung, không hậu tố boss; `// TEMP` + Tooltip cho số tạm; không đổi tên field/enum có sẵn. Plan `wb6-p3-gun9-split` T1 sẽ mở rộng hook cho OneToMany — làm API đủ tổng quát (`OnLinkFired` 1 lần mỗi L).
verified:
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PylonLinkShot.cs: `EnsurePylons` :144, `SpawnPylon` :161 (đang `InitTarget(_pos, null)` :173, speed 0.01, TTL 9999), `PinPylons` :184 (ghim mỗi frame trong `Update` :139), `FireLink` :262, `IE_PairUniqueLoop` :318 (FireLink :334), `StopAttack` :106, `DespawnPylons` :199.
- BulletDestinationWB5.cs: `IsArrived` :14 (set :52, reset :33/:57), `LinkPoint` :12.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 16–17: DestinationLinkShotWB5 làm mẫu phóng cọc)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PylonLinkShot.cs
done_when: compile 0 error; launch origin trống → log O/L y như cũ; có launch origin → cọc bay từ origin tới anchor, link chỉ bắt đầu sau khi cả 6 tới nơi, OnLinkFired gọi 1 lần mỗi L.

## T2 — Claude: thêm Spine Gun6 thứ 2 vào bone Gun6_2, chia cột cọc 
owner: claude
depends: -
context:
- Giữ phần Linh đã sửa tay: G6-PylonLink `BoneFollower.boneName = "Gun6_1"`, G5 trỏ `Gun5_1/_2`. Chỉ thêm gun thứ 2.
- Mỗi gun: 1 node có BoneFollower riêng + 1 Spine Gun6. Gun6_2 giữ C1 (D1–D3), Gun6_1 giữ C2 (D4–D6); giữ world position D hiện tại khi re-parent (đã làm xong, trạng thái boss đứng). Việc đặt lại anchor theo trạng thái boss ngang nằm ở T7.
- Anim: không dùng `SpineAnimSync` cho Attack (2 gun phát Attack độc lập); In/Idle/Out phát cả 2 gun cùng lúc.
- Không SaveScene khi Linh chưa đồng ý (MakeAll dùng chung). Object tạm tắt/xoay phải trả lại.
verified:
- Body skeleton `World_Boss_6_SkeletonData` có bone `Gun6_1` (world ≈ 1.04, 4.27) và `Gun6_2` (≈ 1.24, 2.07) khi root EnemyWB6_P2_A (0,3,0) scale 1 rot 0 (Unity MCP live 2026-10-28); anim body P2 = `Idle2`.
- Live: `WB-Shooter/EnemyWB6_P2_A/G6-PylonLink` (WorldBossWeapon + BoneFollower `Gun6_1`) → con `PylonLinkShot` (Column1: D1–D3; Column2: D4–D6; D local (±1, ∓2) quanh anchor group) + con `Animation` (Spine `Gun6_SkeletonData`, anim Idle). WorldBossWeapon.anim = null, flow* rỗng.
- Spine `Gun6_SkeletonData`: `Attack`(4.0s) `Close` `Idle`(6.7s) `In`(3.7s) `Out`(2.6s).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10: G6 WB5 — 2 Spine Gun6 + SpineAnimSync + flow Open/Attack/Close)
files: Assets/Project/Scenes/MakeAll.unity (trong Editor, KHÔNG lưu) — WB-Shooter/EnemyWB6_P2_A/G6-PylonLink
done_when: có 2 node gun (BoneFollower `Gun6_1`, `Gun6_2`), mỗi node 1 Spine Gun6; D4–D6 con node Gun6_1, D1–D3 con node Gun6_2; `column1Points`/`column2Points` vẫn 3+3; Console 0 error.

## T3 — Claude: nối launch origin + flow anim Gun6
owner: claude
depends: T1, T2
context:
- Gán `column1LaunchFrom`/`column2LaunchFrom` = node Gun6_2 / Gun6_1 (bệ gun); `launchSpeed` TEMP.
- Flow anim (WorldBossWeapon có `anim` kiểu SpineAnimFlow + flowOpen/flowAttack/flowIdle/flowStop; thêm SpineAnimFlow cho Spine gun theo mẫu WB5 G6): `In` (khi mở/phóng cọc) → `Idle` (loop) → `Attack` ở gun CỘT BẮT ĐẦU mỗi L (qua `OnLinkFired`) → về Idle → `Out` khi hết phase (StopAttack). Độ dài anim không cần khớp 0.5s của L.
- Xác nhận bằng mắt khi Play: cọc "ra khỏi bệ" so với anim `In`; lệch thì chỉnh `launchSpeed`/delay, ghi báo cáo.
verified:
- Hook/field do T1 tạo (đọc lại PylonLinkShot.cs sau T1).
- WorldBossWeapon.cs: flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop :39; PlayFlow :95–:131; StopAttack có flowStop :255–:265.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10, 20)
files: MakeAll.unity (trong Editor, KHÔNG lưu) — G6-PylonLink và 2 node gun
done_when: Play P2: 2 gun In → 6 cọc bay ra từ 2 bệ tới anchor → Idle; mỗi L gun cột bắt đầu phát Attack; hết phase Out; Console 0 error.

## T4 — Claude: test Play — boss vào ngang cả phase + 6 cọc + luật nối
owner: claude
depends: T3, T6, T7
context:
- Boss: spline appear (`Path_WB6_P2_1` sau T7: từ mép TRÁI ngoài màn → vào điểm đầu Path 2) chạy KHI boss đã ngang; sau đó `Path_WB6_P2_2` (loop, y≈4.63–5.22, x≈±0.97). Boss ngang cao ~4.04 (bounds 8.76×4.04) → phần thân có thể vượt khung; kiểm đủ 9 node súng + 6 cọc luôn trong khung (thân tàu tràn là chấp nhận, đúng chốt Linh).
- Luật nối giữ: 1 O = 3 L; mỗi L nối 1 D của C1 với 1 D của C2; mỗi D tối đa 1 lần/O; mỗi L random cột bắt đầu; linkDuration 0.5s, delayBetweenLinks 0.5s (TEMP); không cooldown, loop tới hết phase.
- `PylonLinkShot.debugLog = true` → `~/.claude/logs/flow-capture.log` (truncate trước khi chạy). Test dùng `WB-Shooter` (library mục 15.4), tuần tự (dùng chung 1 Editor). Kiểm P3 S9 (cùng PylonLinkShot) y như cũ nếu chạy được.
verified:
- PylonLinkShot.cs IE_PairUniqueLoop log `O{n} L{k}: D? -> D? (start C?)` (:333).
- Test cũ 2026-10-27: Rigidbody2D.velocity = 0, lệch path ≤ 0.07, collider P2 ngang vẫn trúng.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 15.4, 18.6)
files: ~/.claude/logs/flow-capture.log (chỉ đọc)
done_when: boss P2 xuất hiện đã ngang (rotation Z ≈ −90° ngay từ frame đầu của appear, không xoay giữa chừng), bay vào từ mép TRÁI sang phải, mọi súng P2 + 6 cọc luôn trong khung camera, giữ ngang tới hết phase, S4/S5/S6 bắn từ lúc vào PLAYING không chờ xoay; ≥ 3 O: mỗi O đúng 3 L, không D lặp trong O, không cặp cùng cột; link chỉ bắt đầu sau khi 6 cọc tới nơi; cọc đứng yên; collider/velocity ổn (lệch path ≤ 0.07); P3 S9 không đổi; Console 0 error.

## T5 — Claude: báo cáo, cập nhật library/brief, dọn
owner: claude
depends: T4
context:
- AGENT.md mục 5: báo cáo dạng bảng (Đã dựng / Code-prefab mới / Thông số / Test / Dev-artist làm tiếp). Cập nhật library mục 18.6 (WB6 P2: vào ngang, 2 gun 6), mục 17 (PylonLinkShot: launch origin + OnLinkFired), dòng WB6 P2 G6; đồng bộ lên project doc `claude/worldboss-boss-library.md`; brief WB6_P2 thêm mục "Chốt Linh 2026-10-28" (vào ngang cả phase; ghi đè mục S4 "xoay ngang trước S4").
- Task DỌN: plan không tạo Editor script/MenuItem/ảnh. Xoá `~/.claude/logs/wb6-p2-gun6-split-*` nếu có; trả `debugLog` về giá trị gốc nếu đã đổi. GIỮ: EnemyWB6_P2.cs và PylonLinkShot.cs đã sửa, scene (chưa lưu, chờ Linh).
verified:
- Plan không tạo file tạm trong project (chỉ sửa 2 file .cs + thao tác scene qua MCP).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md (mục 5)
files: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md
done_when: library + brief cập nhật, đã đồng bộ project doc, báo cáo có dòng `Đã dọn: …`, không còn file tạm trong project/logs/shots.

## T6 — Muse: boss P2 vào ngang ngay từ đầu, bỏ tween xoay
owner: muse
depends: -
context:
- Thay đổi yêu cầu: KHÔNG còn bước "vào PLAYING → xoay ngang → rồi mới OnAttack". Boss mang góc ngang từ lúc Init/appear và giữ tới hết phase; `OnAttack` bắn ngay như base.
- Giữ `sidewaysAngle = -90°` (hướng đã test PASS, G4 đang bù z=270° cho đúng hướng rơi thẳng xuống). Áp góc lúc `Init` (trước khi spline appear chạy) lên `RotateTarget`: `rotation = startRotation * Euler(0,0,sidewaysAngle)`; `startRotation` chụp ở Init đầu tiên (Init trận mới vẫn trả về gốc rồi áp lại, không cộng dồn góc).
- Xoá đoạn tween TEMP: `IE_RotateSidewaysThenAttack`, `rotateRoutine/rotateTween`, `rotateDuration`, `rotateEase`, `StopRotateRoutine`; `isSideways` luôn true sau Init. Giữ `rotateTarget`, `weaponS4`, `sidewaysAngle` (field serialized đang dùng trong scene/prefab — không đổi tên). Giữ OnDie/OnStopAttack không xoay trả lại.
- `splineMove.pathMode` phải là `Ignore` (đã đặt cho P2) để spline appear/phase không xoay đè lên góc ngang.
verified:
- Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-6/EnemyWB6_P2.cs (116 dòng): fields rotateTarget/weaponS4/sidewaysAngle(-90)/rotateDuration(0.8)/rotateEase :18–:30; `Init` :39 (trả startRotation, isSideways=false); `OnAttack` :49 (nếu chưa ngang → coroutine xoay :58); `IE_RotateSidewaysThenAttack` :88–:115; `OnStopAttack`/`OnDie` :62/:68.
- Library 18.6: `splineMove.pathMode` mặc định Full3D làm suy biến collider 2D; P2 đã `Ignore (0)`; collider PolygonCollider2D 2 cái (22 điểm) — bounds khi xoay −90°: 8.76×4.04.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md (mục Chốt S4), /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 18.6 thân boss WB6)
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-6/EnemyWB6_P2.cs
done_when: compile 0 error; Init áp góc ngang ngay, không còn coroutine/tween xoay; OnAttack bắn ngay; không còn tham chiếu `rotateDuration/rotateEase` trong code; field serialized còn lại giữ nguyên tên (prefab/scene không Missing).

## T7 — Claude: path appear từ TRÁI sang PHẢI + tự căn vị trí boss cho thấy đủ súng phase 2
owner: claude
depends: T2, T3, T6
context:
- Linh chốt 2026-10-28: (a) "đi ngang là đi từ trái qua, không phải từ trên xuống"; (b) "tự căn vị trí và chỉ cần nhìn đủ các súng của phase đó, không cần hiển thị toàn bộ con thuyền" → KHÔNG cần thấy cả thân tàu; thân được phép tràn ra ngoài màn. Điều kiện bắt buộc: mọi súng của P2 (G4 × 5 khẩu, G5 × 2, G6 × 2 gun) nằm trong khung nhìn camera, kèm 6 cọc ở anchor của chúng.
- Hướng: boss vào từ MÉP TRÁI, bay sang phải (mũi tàu hướng phải — khớp `sidewaysAngle = −90°`), dừng vào vùng đứng của phase rồi chạy Path 2 (loop) như cũ.
- Cách căn (Claude tự làm, không hỏi): đo live khi root đang xoay −90° — tọa độ world của các node súng P2 (5 con của G4, G5-L/R, 2 node gun G6) và 6 anchor D, đo khung nhìn camera thật (ortho size + aspect → min/max x,y). Chọn offset (x,y) cho trạng thái dừng sao cho TOÀN BỘ các điểm đó nằm trong khung với lề ≥ 0.3 ở MỌI vị trí trong vòng lặp Path 2 (biên độ hiện y≈4.63–5.22, x≈±0.97 — nếu biên độ làm súng văng ra khỏi khung thì thu nhỏ biên độ, ghi báo cáo). Thân tàu ngoài khung thì chấp nhận.
- Sửa waypoint 2 path TEMP do agent tạo: `Path_WB6_P2_1` = điểm đầu NGOÀI mép trái màn (x ≈ −(nửa bề ngang khung + nửa bề rộng boss ngang ≈ 4.4), đo thật) → waypoint cuối KHỚP điểm đầu Path 2 mới (không nhảy); `Path_WB6_P2_2` = dịch tịnh tiến cả hình theo offset đã căn (giữ hình/biên độ trừ khi phải thu nhỏ). Giữ `splineMove` loop none/CatmullRom/`pathMode = Ignore`; speed/ease #1 (0.68/OutSine) TEMP — chỉnh cho thời gian vào ~3–4s, ghi báo cáo.
- Sau khi căn: đặt lại 6 anchor D trong trạng thái boss NGANG (tạm xoay root −90° khi dựng rồi trả rotation gốc — Init của T6 áp lại): Gun6_2 (bên TRÁI khi ngang; ước tính world ≈ (−0.93, 1.76)) giữ C1 = cột dọc 3 cọc phía mép trái màn, Gun6_1 (bên PHẢI; ≈ (1.27, 1.96)) giữ C2 = cột phía mép phải; mục tiêu TEMP ≈ x −4 / +4, cách nhau 1 đơn vị theo y quanh y bệ; đo lại live. Giữ `column1Points`/`column2Points` 3+3, 6 D vẫn con của node gun tương ứng.
- KHÔNG sửa Path_WB6_P1_*/P3_*, không tạo prefab boss/Node, không đụng WorldBossSO/Addressables, không sửa collider/vị trí súng. Linh vẫn chỉnh tay path sau.
verified:
- Library 18.6: 6 prefab path TEMP `Path_WB6_P{1,2,3}_{1,2}` ở Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/; hiện P2: Path 1 root y=11 → y=4.93 khớp đầu Path 2; Path 2 lượn y≈4.63–5.22, x≈±0.97; camera ortho 5 (đỉnh màn y=5); splineMove #1 speed 0.68 OutSine(3), #2 speed 0.5 loop InOutSine(4); cả hai `pathMode = Ignore`.
- Bounds boss khi xoay −90°: 8.76×4.04 (library 18.6). Vị trí súng P2 TEMP: G4 5 khẩu cách 1, G5 L/R, G6 2 bone Gun6_1/Gun6_2 (xem T4) — đo lại live.
- `EnemyBoss.mySplineMoves = [Path 1, Path 2]` (library 18.6) — đọc live để xác nhận tham chiếu không đổi.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 18.6 thân boss WB6)
files: MakeAll.unity (trong Editor, KHÔNG lưu — anchor D6), Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/Path_WB6_P2_1.prefab, Assets/Project/WorldBoss/Use/Enemies/Prefabs/Paths/Path_WB6_P2_2.prefab (+ .meta giữ nguyên)
done_when: Play P2 (WB-Shooter): boss ngang xuất hiện từ ngoài mép TRÁI, bay sang phải, nối mượt vào Path 2 (không nhảy vị trí); ở mọi thời điểm trong loop, 9 node súng + 6 cọc nằm trong khung camera (lề ≥ 0.3); thân tàu tràn ngoài màn là chấp nhận; khi boss ngang C1 ở trái, C2 ở phải, root rotation đã trả về; Console 0 error. Báo cáo ghi offset đã căn + khung đo được.

## Câu hỏi mở
- Đã chốt: boss vào từ TRÁI sang; Claude tự căn vị trí để thấy đủ súng, thân tàu được tràn ngoài màn (T7). Offset/điểm đầu path/thời gian vào là TEMP, Linh chỉnh tay path khi xem.
- Mỗi L chỉ gun cột bắt đầu phát Attack — muốn cả 2 gun cùng phát thì báo lại.
- `launchSpeed`/thời điểm cọc ra khỏi bệ là TEMP, chỉnh khi xem anim `In` thật.
