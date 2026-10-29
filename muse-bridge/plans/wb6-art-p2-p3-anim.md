# WB6 — ghép nốt anim Phase 2 + Phase 3 và kiểm tra anim cả 3 phase
goal: Ghép nốt flow anim Spine cho phần còn thiếu của P2 (thân, G4, G5) và P3 (thân, G7, G8), sửa mount lỗi của P1 G3, rồi kiểm tra anim cả 3 phase (P1/P2/P3) chạy ổn. G6 (P2) và G9 (P3) đã ghép xong trong 2 plan trước — chỉ kiểm lại.
project: ~/wkspaces/WB
source: Project 1941 — BR_WorldBoss/library/worldboss-boss-library.md (mục 15.3, 20, 18.6, quy ước anim ~:983–991), briefs/WB6_P2.md, WB6_P3.md; kết quả dò live MakeAll 2026-10-28; status plan wb6-p2-gun6-split, wb6-p3-gun9-split (đều done)

## Luật điều phối (áp dụng cho cả plan, ưu tiên hơn skill nếu khác)
- Claude = PM/Leader, làm ÍT NHẤT có thể: chạy menu Editor, Play, review, nghiệm thu, báo Linh. Việc viết/sửa code giao Muse; Claude tự viết code chỉ khi Muse BLOCKED/TIMEOUT hoặc >3 vòng review cùng lỗi (ghi `--log "[self-code] lý do"`).
- Cổng review code trước mọi Play: đọc đúng file Muse liệt kê, đối chiếu `verified:`; lỗi → `muse-ask --bug` chỉ rõ file:line. Lỗi compile → grep ~/Library/Logs/Unity/Editor.log (`error CS`); kết quả test → đọc file log, không dùng MCP read_console. Ngân sách MCP: ~6 lần (AttachArt, AnimCheck ×3, thêm tối đa 2 chạy lại).
- KHÔNG làm rác project: chỉ tạo/sửa đúng file trong `files:`; ảnh/log tạm ở ~/.claude/logs hoặc scratch (không Temp/ScreenShot trong project). Mọi thứ tạm có chỗ dọn ở T6.
- KHÔNG lưu scene trong plan này (Linh chưa OK; scene MakeAll đang dirty từ 2 plan trước). Active từng phase: chỉ phase đang check ON, phase khác OFF; cuối mission trả active như lúc bắt đầu (ghi trạng thái gốc trước khi đổi).
- T1, T2, T3 độc lập (`files:` khác nhau) → giao song song; các task test dùng chung 1 Unity Editor nên tuần tự.

## T1 — Muse: thêm hook chọn khẩu cho RandomExclusiveSequenceShot + component anim 5 khẩu G4
owner: muse
depends: -
context:
- G4 (P2 S4) có 5 Spine Gun4 (G4-1..G4-5) bắn lần lượt random không trùng. Cần anim: cả 5 khẩu `Open` (In→Idle) khi weapon mở, khẩu nào được chọn bắn thì phát `Attack` đúng khẩu đó, hết phase `Stop` (Out) cả 5.
- Làm theo mẫu `PylonLinkGunAnim` (nối UnityEvent → SpineAnimFlow từng gun): thêm vào `RandomExclusiveSequenceShot` một `UnityEvent<int>` (vd `ueShotPicked`, tham số = chỉ số khẩu trong `shotPool`) bắn ngay lúc rút khẩu; thêm component chung `RandomShotGunAnim` (đặt tên chung, không hậu tố boss) với `SpineAnimFlow[] guns` (thứ tự = `shotPool`), hàm `OnPicked(int)`/`OnOpen()`/`OnStop()`; tên flow là field string (mặc định Open/Attack/Stop). Field/event mới trống → hành vi shot y như cũ.
- Không đổi tên field/enum có sẵn. Số tạm ghi `// TEMP` + Tooltip.
verified:
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/RandomExclusiveSequenceShot.cs: `class RandomExclusiveSequenceShot : RandomShotSelectE` :22; `IE_FireRandomShot` :62; rút khẩu `_index = deck[deckPos++]` :68; log :76.
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PylonLinkGunAnim.cs (mẫu: gunC1/gunC2 SpineAnimFlow, flowOpen "Open", flowAttack "Attack", flowStop "Stop"; OnLaunch/OnLink/OnEnd).
- BaseShotE.cs:48–52 có sẵn UnityEvent ueStartShot/ueOnShot/ueAnimPerWave/ueShotFinish/ueEndShot.
- Live P2: `G4-GrowOrbDrop` (WorldBossWeapon, không BoneFollower) có 5 Spine `Gun4_SkeletonData` (G4-1..G4-4 + 1 khẩu tên con `FireDirection_Down`), WorldBossWeapon.anim = null, flow* rỗng.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 17: class pattern chung)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/RandomExclusiveSequenceShot.cs, Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/RandomShotGunAnim.cs
done_when: compile 0 error; event trống thì log/hành vi G4 y như cũ; có thể nối event trong Inspector.

## T2 — Muse: Editor script ghép anim P2/P3 (MenuItem Tools/WB6/AttachArt P2P3)
owner: muse
depends: -
context:
- Tình trạng live (Unity MCP 2026-10-28): P1 đã có flow (thân In>Idle1; G1/G2/G3: Open/Idle/Attack/Close). P2 và P3: thân `EnemyWorldBoss.animationFlow = null`, `flowAppear = ''`; P2 G4, G5-L/R, P3 G7-L/R, G8: `WorldBossWeapon.anim = null`, mọi flow* rỗng. P2 G6 (Gun6_1/_2) và P3 G9 (Gun9_1/_2) đã có SpineAnimFlow Close/Open(In>Idle L)/Attack(Attack>Idle L)/Stop(Out) + `PylonLinkGunAnim` — KHÔNG đụng.
- Việc (menu idempotent, KHÔNG lưu scene, KHÔNG đổi skeleton/bone/vị trí/shot):
  1. Thân P2: SpineAnimFlow flow `Appear` = `Idle1_to_2`(2.4s) → `Idle2` (loop); gán `animationFlow` (+ `flowAppear = Appear`, `delayActionAppear` theo `GetFlowDuration`). Thân P3: `Appear` = `Idle2_to_3`(2.4s) → `Idle3` (loop). Theo quy ước library (thân P2/P3: flow `Appear` = Idle1_to_2/Idle2_to_3). P1 đã đúng, không đụng.
  2. P2 G5-L/R (Gun5: Attack 1.0, Idle 0.4, In 3.8, Out 1.8, Close): mỗi weapon 1 SpineAnimFlow trên child `Animation`: `Close`[Close(L)], `Open`[In>Idle(L)], `Idle`[Idle(L)], `Attack`[Attack>Idle(L)], `Stop`[Out]; gán `WorldBossWeapon.anim` + flowCloseInit=Close, flowOpen=Open, flowIdle=Idle, flowAttack=Attack, flowStop=Stop. 2 khẩu bắn cùng lúc (delayStart 0).
  3. P3 G7-L/R (Gun7: Attack 0.2, Idle 0.1, In 1.4, Out 1.3, Close) và G8 (Gun8: Attack 1.0, Idle 6.7, In 3.7, Out 2.5, Close): cùng khuôn flow như G5. G8 là súng tự loop charge→bắn→CD: `openOnce` = true (mở 1 lần rồi giữ), `Attack` phát mỗi lần laser bắn, `flowCharge` để trống (TEMP — Linh chốt sau nếu muốn In làm charge).
  4. P2 G4: gán SpineAnimFlow cho từng khẩu (cùng khuôn flow), thêm `RandomShotGunAnim` trên `RandomExclusiveSequenceShot` (T1) nối: weapon mở → OnOpen, event chọn khẩu → OnPicked(index), weapon stop → OnStop; `WorldBossWeapon.anim` trỏ Spine khẩu đầu để mở/close chung nếu cần (theo mẫu P2 G6).
  5. P1 G3-L/R: sửa `BoneFollower.boneName` từ `Gun3_Mix_Gun9.1` / `Gun3_Mix_Gun9.2` thành `Gun3_Mix_Gun9_1` / `Gun3_Mix_Gun9_2` (skeleton thân hiện tên có dấu gạch dưới). Chỉ sửa tên bone, không đụng gì khác của P1.
- Cuối menu ghi tóm tắt (flow đã tạo từng súng, số TEMP, cảnh báo) ra ~/.claude/logs/wb6-art-p23.log. Không có flow nào được trỏ tới tên anim không tồn tại trong skeleton tương ứng.
verified:
- Gun anim (đọc từ SkeletonData assets, World_Boss_6/): Gun3 Attack 0.4/Close/Idle 0.1/In 2.8/Out 2.0; Gun4 Attack 0.3/Close/Idle 0.7/In 2.0/Out 1.3; Gun5 Attack 1.0/Close/Idle 0.4/In 3.8/Out 1.8; Gun6 Attack 4.0/Close/Idle 6.7/In 3.7/Out 2.6; Gun7 Attack 0.2/Close/Idle 0.1/In 1.4/Out 1.3; Gun8 Attack 1.0/Close/Idle 6.7/In 3.7/Out 2.5; Gun9 Attack 4.0/Close/Idle 0.1/In 3.2/Out 1.5. Thân: Dead 2.7, Idle1 1.7, Idle1_to_2 2.4, Idle2 1.7, Idle2_to_3 2.4, Idle3 4.9, In 4.0.
- Bone thân hiện có (World_Boss_6_SkeletonData): Gun1_1/_2, Gun2_1/_2, Gun3_Mix_Gun9_1/_2, Gun4_1..Gun4_5, Gun5_1/_2, Gun6_1/_2, Gun7_1/_2, Gun8 — KHÔNG còn `Gun3_Mix_Gun9.1/.2`.
- Live: P2 G5-L bone Gun5_2, G5-R bone Gun5_1; P3 G7-L Gun7_1, G7-R Gun7_2, G8 Gun8 (đều tồn tại). P1 G3-L bone `Gun3_Mix_Gun9.1` MISSING, G3-R `Gun3_Mix_Gun9.2` MISSING (dò live 2026-10-28).
- WorldBossWeapon.cs: `SpineAnimFlow anim` :38, `flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop` :39, `flowCharge` :41, `openOnce` :44, `ueAttack/ueStop` :51, `OnOpenStart` :72.
- EnemyWorldBoss.cs: `flowAppear` :27 (+ `AutoWireFlowAppear` ưu tiên flow tên `Appear`, :39–:45), `PlayFlow(flowAppear)` :183.
- SpineAnimFlow: Assets/Spine/SpineTools/SpineAnimFlow.cs :32 (Flows :35, GetFlowDuration :74, PlayFlow :100). Mẫu flow đã có: P2 `G6-PylonLink/Gun6_1/Animation`, P3 `G9-PylonLink/Gun9_1/Animation` (Close/Open/Attack/Stop).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 15.3, 20, ~:983–991 quy ước anim thân P2/P3), /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P2.md, WB6_P3.md
files: Assets/Editor/WB6/AttachArt_WB6_P23.cs
done_when: compile 0 error; menu idempotent (chạy 2 lần không nhân đôi component/flow); không flow nào trỏ anim không có; chỉ đúng file trên được tạo trong Assets/Editor/WB6/.

## T3 — Muse: Editor script kiểm tra anim (MenuItem Tools/WB6/AnimCheck P1|P2|P3)
owner: muse
depends: -
context:
- Mục tiêu giảm MCP: 1 lệnh/phase làm TRỌN: (a) kiểm tĩnh không Play; (b) Focus phase (chỉ phase đó + thân ON, 2 phase còn lại OFF; lần Focus đầu ghi trạng thái gốc ra ~/.claude/logs/wb6-focus-state.json) → vào Play → tự chạy ~12s (đủ appear + ≥ 2 chu kỳ bắn, thời gian cấu hình `// TEMP`) → ghi log → tự thoát Play (dùng EditorApplication.update + SessionState/EditorPrefs nếu domain reload). Idempotent, KHÔNG lưu scene, không đổi gì ngoài activeSelf. Thêm `Tools/WB6/AnimCheck Restore`: trả active về trạng thái gốc.
- Kiểm tĩnh (ghi vào log): mỗi súng/thân của phase: bone trong BoneFollower có trong SkeletonData thân (OK/MISSING); mỗi flow trong SpineAnimFlow trỏ anim có/không có trong skeleton của Spine đó; `WorldBossWeapon.anim`/flow* đã gán hay rỗng; thân có `animationFlow` + `flowAppear`.
- Kiểm động (Play): đăng ký `AnimationState.Start` của từng SkeletonAnimation (thân + mỗi gun) từ Editor script (không sửa code sản phẩm), ghi vào ~/.claude/logs/wb6-animcheck-P{n}.log mỗi dòng: giây từ lúc Play, tên object, anim bắt đầu, loop. Cuối log in tóm tắt PASS/WARN theo luật: (1) thân chạy appear flow rồi Idle loop (P1: In→Idle1; P2: Idle1_to_2→Idle2; P3: Idle2_to_3→Idle3); (2) mỗi gun có In→Idle trước lần Attack đầu; (3) mỗi lần bắn thấy Attack→Idle trên đúng gun; (4) không gun nào kẹt ở `Close` khi đang bắn; (5) không anim lạ/NULL; (6) khi hết phase/Stop thấy Out (nếu flowStop gán).
- Mặc định tắt mọi log debug khác trong code sản phẩm; không thêm file ngoài `files:`.
verified:
- Tên object: EnemyWB6_P1_A / P2_A / P3_A trong MakeAll; weapon + Spine con `Animation`; P2/P3 gun6/9 là node `Gun6_1/_2`, `Gun9_1/_2` có BoneFollower + `Animation` + SpineAnimFlow.
- P2 `Rigidbody2D` đã đặt FreezeRotation (scene, chưa lưu); P3 cũng vậy; nếu P3 active cùng lúc thì collider P3 đẩy xoay P2 → mỗi lần chỉ bật 1 phase.
- EnemyWorldBoss.cs:183 PlayFlow(flowAppear); BoneFollower.boneName/SkeletonRenderer; SkeletonDataAsset.GetSkeletonData(true).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 15.4: test bằng WB-Shooter)
files: Assets/Editor/WB6/AnimCheck_WB6.cs
done_when: compile 0 error; chỉ đúng file trên được tạo; log có phần kiểm tĩnh + dòng PASS/WARN.

## T4 — Claude: review code T1–T3, chạy AttachArt P2P3
owner: claude
depends: T1, T2, T3
context:
- Cổng review (đọc code, grep Editor.log, KHÔNG MCP). PASS → 1 lệnh `Tools/WB6/AttachArt P2P3` (MCP `execute_menu_item`) rồi đọc ~/.claude/logs/wb6-art-p23.log. Không Play ở task này.
verified:
- (kết quả T1–T3)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: -
done_when: log liệt kê đủ: thân P2/P3, G4, G5-L/R, G7-L/R, G8 đã gán flow; P1 G3 bone sửa; không NullReference; Editor compile 0 error.

## T5 — Claude: kiểm tra anim P1, P2, P3 (tuần tự)
owner: claude
depends: T4
context:
- Chạy lần lượt `Tools/WB6/AnimCheck P1`, `P2`, `P3` (mỗi phase 1 lệnh MCP), đọc `~/.claude/logs/wb6-animcheck-P{n}.log` bằng file. Phán "anim ổn" khi: kiểm tĩnh sạch (không bone MISSING, không flow trỏ anim thiếu), thân đúng appear→Idle, mọi gun có In→Idle trước Attack đầu, Attack→Idle đúng gun mỗi lần bắn, không gun kẹt Close khi bắn, Out khi Stop; P2 G6 và P3 G9 giữ nguyên hành vi (In 0–3.7s/3.2s rồi cọc bay, Attack theo gun cột bắt đầu).
- Lỗi/lệch → `muse-ask --bug` kèm dòng log (tối đa 3 vòng/lỗi), sửa xong chạy lại phase đó. Ảnh chụp nếu cần để ở scratch, KHÔNG trong project.
verified:
- Kỳ vọng theo T3 (luật 1–6); số TEMP: Attack 0.2s (Gun7) / 1.0 (Gun5, Gun8) / 4.0 (Gun6, Gun9) dài hơn nhịp L ở G6/G9 — đã chấp nhận ở 2 plan trước.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: -
done_when: 3 log AnimCheck đều PASS (hoặc WARN đã giải thích/Linh duyệt); Console không NullReference/error từ code WB6 (đọc qua log).

## T6 — Claude: báo cáo, cập nhật library, trả active, dọn
owner: claude
depends: T5
context:
- `Tools/WB6/AnimCheck Restore` (trả active như gốc). Báo cáo bảng ngắn theo AGENT.md mục 5: từng phase — thân, từng gun, flow, kết quả AnimCheck; số TEMP (Gun8 charge để trống; Attack dài hơn nhịp); việc dev/artist (prefab boss `_A`, Node, Addressables, SO). Cập nhật library mục 18.6 + mục 20 (quy ước anim WB6), brief WB6_P2/P3; đồng bộ project doc `claude/worldboss-boss-library.md` nếu có công cụ.
- KHÔNG lưu scene. Báo rõ: MakeAll đang dirty (gồm thay đổi từ 2 plan trước + plan này), chờ Linh OK lưu.
- DỌN (tự làm sau khi nghiệm thu PASS, không hỏi Linh): xoá Assets/Editor/WB6/AttachArt_WB6_P23.cs, Assets/Editor/WB6/AnimCheck_WB6.cs (+ `.meta`); nếu Assets/Editor/WB6/ rỗng thì xoá cả thư mục + `.meta`; xoá ~/.claude/logs/wb6-art-p23.log, wb6-animcheck-P*.log, wb6-focus-state.json; xoá ảnh tạm trong scratch/Temp. GIỮ: RandomShotGunAnim.cs, sửa trong RandomExclusiveSequenceShot.cs, PylonLinkGunAnim.cs, PylonLinkShot.cs, EnemyWB6_P*.cs. Xin quyền xoá (device_request_delete_permission) đúng 1 lần nếu bị chặn.
verified:
- Mẫu báo cáo: BR_WorldBoss/AGENT.md mục 5; luật dọn: AGENTS.md "Dọn rác".
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: BR_WorldBoss/library/worldboss-boss-library.md, BR_WorldBoss/briefs/WB6_P2.md, BR_WorldBoss/briefs/WB6_P3.md
done_when: báo cáo gửi Linh có dòng `Đã dọn: …`; library + brief cập nhật; active như lúc bắt đầu; không còn file tạm trong project/logs/shots.

## Câu hỏi mở
- Thân P2/P3 dùng flow Appear = Idle1_to_2 / Idle2_to_3 (theo quy ước library). Thân P2 giờ đi vào từ trái trong ~3.5s (Idle1_to_2 dài 2.4s) — Linh muốn dùng anim `In` (4.0s) cho P2 thay vì Idle1_to_2 không?
- Súng phase trước không còn ở phase sau (P1 Gun1/2/3 khi sang P2, P2 → P3): có cần súng (OnlyAnim) `Out` đồng bộ khi súng mới mở theo luật Linh chốt 2026-10-16 không? Hiện tách bone riêng nên plan này KHÔNG làm; nói Linh nếu cần cặp swap cụ thể.
- G8 (tam giác điện, charge 3s): Gun8 `In` dài 3.7s — Linh muốn gắn In làm charge (flowCharge) thay vì chỉ mở 1 lần?
- Multi-burst (G5 4 lượt, G7 3 tên lửa): Attack hiện phát 1 lần/lượt bắn của weapon; muốn Attack theo từng đợt (ueAnimPerWave) thì báo lại.
- Scene MakeAll đang dirty (chưa lưu từ plan P2/P3 trước): Linh OK lưu khi nào?
