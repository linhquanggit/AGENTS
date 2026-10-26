# WB6 Phase 1 — gắn art Spine (S1, S2, S3), làm từng súng một (chạy lại, Muse đã có quy tắc thường trực)
goal: Gắn flow anim Spine cho thân boss + 6 súng Phase 1 trên khung Linh đã dựng trong MakeAll, rồi test từng súng (chỉ súng đang làm được active, các súng khác tắt). Phase 2, 3 làm sau khi Linh dựng khung.
project: ~/wkspaces/WB
source: Project 1941 — BR_WorldBoss/briefs/WB6_P1.md, WB6_PLAN.md, library mục 12/15/20; khung Linh dựng trong Assets/Project/Scenes/MakeAll.unity (đọc 2026-10-26)

## Luật điều phối (áp dụng cho cả plan, ưu tiên hơn skill nếu khác)
- Claude = PM/Leader, làm ÍT NHẤT có thể, chịu trách nhiệm chất lượng. Claude chỉ làm: chạy menu Editor, Play, review, nghiệm thu, báo Linh. MỌI việc viết/sửa code giao Muse; Claude tự viết code chỉ khi Muse BLOCKED/TIMEOUT hoặc >3 vòng review cùng lỗi (ghi `--log "[self-code] lý do"` gắn vào cùng lệnh Bash).
- Cổng review code trước mọi Play: đọc đúng file Muse liệt kê, đối chiếu `verified:`; lỗi → `muse-ask --bug` chỉ rõ file:line + cách đúng. Chỉ khi PASS mới chạy menu/Play.
- Hạn chế Unity MCP (rất tốn token): ưu tiên Read/Grep/Bash; lỗi compile → grep ~/Library/Logs/Unity/Editor.log (`error CS`); kết quả test → đọc ~/.claude/logs/flow-capture.log; thao tác scene bằng 1 lệnh menu do Muse viết; không dùng MCP để review/xem thử; gom thao tác.
- ACTIVE TỪNG SÚNG (Linh chốt): khi làm/test súng nào thì chỉ súng đó (và thân boss) active; mọi súng/phase khác TẮT (EnemyWB6_P2_A, EnemyWB6_P3_A tắt trong lúc làm). Menu TestP1/RestoreAndSave ở T2 lo việc này. Cuối mission trả trạng thái active về như lúc bắt đầu.
- KHÔNG LÀM RÁC project: chỉ tạo/sửa đúng file trong `files:`. Không tạo thư mục ScreenShot/Capture/Temp, không file thử, không backup trong project; ảnh/log tạm ở ~/.claude/logs hoặc scratch. Review so danh sách file Muse báo với `files:`, thừa → bắt dọn. Mọi thứ tạm có chỗ dọn (T7). Không tạo tính năng/công cụ ngoài plan.
- Lưu scene: Linh đã OK (2026-10-26) — chỉ lưu MakeAll MỘT LẦN ở T7 trong menu RestoreAndSave sau khi test PASS; các task khác KHÔNG lưu. Không sửa prefab/scene có sẵn ngoài các object của EnemyWB6_P1_A, không đụng WorldBossSO/Addressables/prefab boss `_A`/Node.
- CHIA VIỆC SONG SONG (tự động): T1 và T2 độc lập (`files:` khác nhau) → giao cùng lúc, mỗi task 1 lệnh `muse-ask --plan <id> --task Tn "..."` chạy nền, mỗi task tự có chat riêng; không tự tạo được chat riêng thì chạy tuần tự. Các task test (T4–T6) dùng chung 1 Unity Editor nên luôn tuần tự.
- Ngân sách MCP: tối đa ~5 lần (AttachArt, TestP1 ×3, RestoreAndSave); vượt thì ghi lý do.
- Chỉ số: Play/tự-viết-code ghi log gắn vào cùng lệnh Bash (`[play]`, `[self-code]`); không ghi `[mcp]`, không tự dựng bảng chỉ số.

## T1 — Editor script gắn flow anim Phase 1 (MenuItem Tools/WB6/AttachArt P1)
owner: muse
depends: -
context:
- Khung đã dựng sẵn (KHÔNG tạo lại object, KHÔNG đổi skeleton/bone Linh đã gán): root `EnemyWB6_P1_A` → child `Animation` (SkeletonAnimation, World_Boss_6_SkeletonData) + 6 súng, mỗi súng có child `Animation` (SkeletonAnimation) và `WorldBossWeapon` + `BoneFollower`:
  • G1-CurveToPlayer-L: Gun1_SkeletonData, bone Gun1_2 · G1-CurveToPlayer-R: Gun1_SkeletonData, bone Gun1_1 (shot con `CurveShot` DirectionShotE, 11 FireDirection)
  • G2-RingPulseSpiral-L: Gun2_SkeletonData, bone Gun2_Mix_Gun5 · G2-RingPulseSpiral-R: Gun2_SkeletonData, bone Gun2_Mix_Gun6 (shot con RingPulseSpiralShot)
  • G3-GrowOrb-L: Gun3_SkeletonData, bone Gun3_Mix_Gun9.1 · G3-GrowOrb-R: Gun3_SkeletonData, bone Gun3_Mix_Gun9.2 (shot con `GrowOrbShot` DirectionShotE, FireDirection_L/_R)
- Linh đã sửa xương thân (World_Boss_6.json 07:33, 172 bone; Gun3_1/Gun3_2 → Gun3_Mix_Gun9.1/.2, Gun2_1 → Gun2_Mix_Gun5; thân vẫn chỉ có In/Idle1/Idle1_to_2/Idle2/Idle2_to_3/Idle3/Dead) và scene (07:36). BoneFollower trong scene đã khớp tên bone mới; Muse KHÔNG đổi bone, chỉ kiểm tên bone tồn tại trong JSON thân và ghi cảnh báo nếu lệch.
- Tình trạng hiện tại: cả 6 `WorldBossWeapon` có attackType=1 (ATTACK_ON_OPEN), `anim` CHƯA gán, không useChain; chưa có SpineAnimFlow nào trên thân hay súng.
- Anim có trong Spine (độ dài s): thân World_Boss_6: In 4.0, Idle1 1.67, Idle1_to_2 2.37, Idle2 1.67, Idle2_to_3 2.37, Idle3 4.9, Dead 2.67 (không có event). Gun1: Open 2.73, Attack 0.53, Idle 1.0, Close. Gun2: Open 2.27, Attack 0.87, Idle 4.8, Close. Gun3: In 2.83, Out 2.0, Attack 0.4, Idle 0.13, anim "1" 1.17, "null/In_Gun2" 1.8. Linh chốt: `In` CHÍNH LÀ Open của Gun3 → flowOpen = In.
- Việc: (1) thân `Animation`: thêm SpineAnimFlow với flow `In` (anim In, 1 lần) → `Idle1` (loop); gán `EnemyWorldBoss.animationFlow` + `flowAppear` = `In` (xem EnemyWorldBoss.cs:25–27, 63). (2) mỗi súng: thêm SpineAnimFlow trên child `Animation` với các flow Open/Idle/Attack/Close theo anim có của skeleton đó; gán `WorldBossWeapon.anim` + flowIdle/flowOpen/flowAttack/flowCloseInit (WorldBossWeapon.cs:38–41); cặp bắn CÙNG LÚC (G2-L/R, G3-L/R) dùng SpineAnimSync cho khẩu thứ hai (SpineAnimSync.cs:12 source); G1-L/R thay phiên → KHÔNG sync. (3) bắn khi flowAttack bắt đầu (không có event): căn delay theo độ dài flow, số tạm đánh dấu `// TEMP`; thay `chargeDelay 0.6` TEMP của S1 bằng delay lấy từ độ dài flow.
- Mẫu cần theo: library mục 15.3, 20, và súng WB5 `G4-Rocket-L/R`, `G6-ElectricLink` (EnemyWB5_P2_A.prefab trong Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/). Đọc đúng chỗ cần, không quét cả project.
- Menu idempotent (chạy lại không nhân đôi component/flow). KHÔNG lưu scene trong menu này. Cuối menu ghi tóm tắt (flow đã tạo cho từng súng, delay đã đặt, số TEMP, cảnh báo) ra ~/.claude/logs/wb6-art-p1.log.
verified:
- Spine: Assets/Project/WorldBoss/Addressable/Enemies/Spine/World_Boss_6/{World_Boss_6,Gun1..Gun6}.json (Spine 4.1.24)
- WorldBossWeapon.cs:19 class WorldBossWeapon; :38 `[SerializeField] protected SpineAnimFlow anim`; :39 `flowIdle, flowOpen, flowCloseInit, flowAttack, flowStop`; :41 `flowCharge`; :44 openOnce
- Spine/SpineTools/SpineAnimFlow.cs:32 class SpineAnimFlow, :35 `List<AnimFlow> Flows`, :74 GetFlowDuration, :100 PlayFlow; AnimFlow{Name, Actions}, AnimAction{Loop, Delay, TrackIndex, NewDuration}
- Spine/SpineTools/SpineAnimSync.cs:10 class SpineAnimSync, :12 `SkeletonAnimation source`
- EnemyWorldBoss.cs:25 `SpineAnimFlow animationFlow`, :27 `flowAppear`, :63 GetComponentInChildren<SpineAnimFlow>
- MakeAll.unity: EnemyWB6_P1_A đang OFF, EnemyWB6_P2_A OFF, EnemyWB6_P3_A OFF (đọc lại 2026-10-26 07:36; P2 đã có G5 dùng chung bone Gun2_Mix_Gun5/Gun5 — không đụng, chỉ P1); EnemyWB6_P1.cs rỗng (Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-6/)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md
files: Assets/Editor/WB6/AttachArt_WB6_P1.cs
done_when: compile 0 error (Claude đọc review code); file Editor script chỉ có đúng file trên trong Assets/Editor/WB6/

## T2 — Editor script test từng súng 1 lệnh + Restore&Save (MenuItem Tools/WB6/TestP1 G1|G2|G3 và Tools/WB6/RestoreAndSave)
owner: muse
depends: -
context:
- Mục tiêu giảm MCP (Linh chốt 2026-10-26): mỗi súng chỉ 1 lệnh MCP. `Tools/WB6/TestP1 G1` làm TRỌN: Focus (bên dưới) → vào Play → tự chạy ~2 chu kỳ bắn (thời gian cấu hình, mặc định tính theo CD+flow, có `// TEMP`) → tự thoát Play (dùng EditorApplication.update + SessionState/EditorPrefs vì domain reload; nếu project tắt domain reload thì dùng biến static) → ghi kết quả vào ~/.claude/logs/flow-capture.log (kèm dòng `RESULT TestP1 G1: shots=N, errors=M, exceptions=...`, đếm lỗi/NullReference bắt qua Application.logMessageReceived) → GIỮ nguyên trạng thái Focus sau khi thoát Play (không Restore). Tương tự TestP1 G2, TestP1 G3.
- `Tools/WB6/RestoreAndSave`: trả active mọi object về trạng thái gốc rồi LƯU scene MakeAll (EditorSceneManager.MarkSceneDirty + SaveOpenScenes; Linh OK lưu 1 lần ở cuối), ghi kết quả vào ~/.claude/logs/wb6-focus-state.json/flow-capture.log. Chỉ chạy khi 3 test PASS.
- Logic Focus (dùng nội bộ, không cần menu riêng): bật EnemyWB6_P1_A + thân `Animation` + chỉ cặp G1-CurveToPlayer-L/R; TẮT G2-*, G3-*; TẮT EnemyWB6_P2_A và EnemyWB6_P3_A. Tương tự Focus P1 G2, Focus P1 G3. Logic Restore: trả active mọi object (P1_A, 6 súng, P2_A, P3_A) về trạng thái ghi lúc Focus đầu tiên.
- Lần Focus đầu tiên ghi trạng thái gốc ra ~/.claude/logs/wb6-focus-state.json (không ghi trong project). Idempotent; KHÔNG lưu scene; không đổi gì khác ngoài activeSelf.
- Trạng thái gốc dự kiến (để kiểm Restore): EnemyWB6_P1_A OFF, 6 súng ON, EnemyWB6_P2_A OFF, EnemyWB6_P3_A OFF.
- Thêm log debug runtime cho test nhanh: mỗi lần súng bắn ghi 1 dòng vào ~/.claude/logs/flow-capture.log (giây từ lúc vào Play, tên súng, khẩu L/R, tên flow đang chạy), chỉ khi bật cờ debug, mặc định tắt trong code sản phẩm; nếu cần thêm hook vào các shot WB6 thì chỉ sửa file trong `files:`.
verified:
- Tên object chính xác: EnemyWB6_P1_A; G1-CurveToPlayer-L/R; G2-RingPulseSpiral-L/R; G3-GrowOrb-L/R; EnemyWB6_P2_A; EnemyWB6_P3_A (MakeAll.unity)
- Shot WB6 hiện có: Enemies/Scripts/BaseEnemy/BaseShot/RingPulseSpiralShot.cs; DirectionShotE.cs:82 IE_SpawnBullet; Bullets/Scripts/BulletDelayedCurveToTarget.cs, BulletGrowOverTime.cs
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: Assets/Editor/WB6/TestGun_WB6_P1.cs
done_when: compile 0 error (Claude đọc review code); chỉ đúng file trên được tạo/sửa (trừ file shot WB6 nếu thêm hook log, phải báo rõ)

## T3 — Chạy AttachArt P1 và kiểm tra cấu hình
owner: claude
depends: T1, T2
context:
- Review code T1, T2 qua Cổng review (đọc code, grep Editor.log, KHÔNG MCP). PASS → chạy 1 lệnh `Tools/WB6/AttachArt P1` (1 lần MCP `execute_menu_item`), rồi đọc ~/.claude/logs/wb6-art-p1.log để kiểm cấu hình từng súng. Không Play ở task này.
- Ghi trạng thái active gốc (Focus lần đầu tự ghi vào wb6-focus-state.json).
verified:
- (kết quả T1, T2)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: -
done_when: Log liệt kê đủ 6 súng + thân có flow/anim gán; không NullReference

## T4 — Test súng 1 (G1-CurveToPlayer-L/R, chỉ G1 active)
owner: claude
depends: T3
context:
- Chạy 1 lệnh MCP `Tools/WB6/TestP1 G1` (Focus + Play + tự thoát) → đọc flow-capture.log bằng file, không MCP. Kiểm: thân chạy `In` rồi `Idle1`; G1 mở (Open) rồi bắn đúng thay phiên L/R, delay 2s (design), đạn 11 hướng × 4 viên nối đuôi ~0.1s, sau ~1s cong dần về player; không còn súng khác bắn.
- Lỗi/lệch → `muse-ask --bug` kèm log; sửa xong test lại (tối đa 3 vòng).
verified:
- Design S1: brief WB6_P1.md mục "S1 — chốt sau video"
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md
files: -
done_when: log khớp brief; Console không NullReference/error (đọc qua file log, không MCP read_console)

## T5 — Test súng 2 (G2-RingPulseSpiral-L/R, chỉ G2 active)
owner: claude
depends: T4
context:
- 1 lệnh MCP `Tools/WB6/TestP1 G2` → đọc flow-capture.log. Kiểm: 2 khẩu bắn CÙNG LÚC (SpineAnimSync khớp), CD 2.5s, vòng 8 viên nở/co theo nhịp rồi toả xoắn ốc.
verified:
- Design S2: brief WB6_P1.md; RingPulseSpiralShot.cs (TEMP: pulseStepDuration 0.35, spiralDuration 2, spiralAngularSpeedStart/End 360/90)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md
files: -
done_when: log khớp brief; bảng số TEMP ghi cho Linh chỉnh

## T6 — Test súng 3 (G3-GrowOrb-L/R, chỉ G3 active)
owner: claude
depends: T5
context:
- 1 lệnh MCP `Tools/WB6/TestP1 G3` → đọc flow-capture.log. Kiểm: 2 khẩu cùng lúc, mỗi khẩu 2 viên chữ V ngược 15–20° bay tới player, to dần (BulletGrowOverTime), CD 3s; flowOpen = `In` (Linh chốt In là Open của Gun3) chạy đúng.
verified:
- Design S3: brief WB6_P1.md; BulletGrowOverTime.cs (TEMP maxScale 3, growDuration 2)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P1.md
files: -
done_when: log khớp brief

## T7 — Nghiệm thu Phase 1, báo cáo, dọn
owner: claude
depends: T6
context:
- Nếu 3 test PASS: 1 lệnh MCP `Tools/WB6/RestoreAndSave` (trả active như gốc + lưu MakeAll). Không Play cả phase (tiết kiệm MCP; chỉ Play thêm nếu Linh yêu cầu). Báo cáo bảng ngắn theo AGENT.md mục 5 (đã dựng, thông số so với design, test, số TEMP, việc dev/artist làm tiếp: prefab boss `_A`, Node, Addressables, SO). Cập nhật library mục 11 + 19 và briefs/WB6_P1.md (`Trạng thái`), đồng bộ project doc `claude/worldboss-boss-library.md`.
- LƯU SCENE: nằm trong RestoreAndSave (Linh OK); báo rõ đã lưu.
- DỌN (ĐIỂM DỪNG xoá file): không tự xoá. Đánh task này `--status blocked "Xoá Assets/Editor/WB6/ (AttachArt_WB6_P1.cs, TestGun_WB6_P1.cs + .meta) và ~/.claude/logs/wb6-*.log/json? Chỉ xoá sau khi Claude đã lưu MakeAll và Linh OK"` rồi hỏi Linh gộp ở cuối; Linh OK → xoá đúng danh sách đó (không đụng class pattern, prefab đạn, EnemyWB6_P*.cs).
- Phase 2, 3: chờ Linh dựng khung rồi tạo plan riêng (wb6-art-p2, wb6-art-p3).
verified:
- Mẫu báo cáo: BR_WorldBoss/AGENT.md mục 5
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md
files: BR_WorldBoss/library/worldboss-boss-library.md, BR_WorldBoss/briefs/WB6_P1.md
done_when: MakeAll đã lưu 1 lần sau Restore (trạng thái active như gốc); báo cáo gửi Linh; library + project doc cập nhật; thư mục Assets/Editor/WB6 đã xoá sau khi Linh OK (hoặc task blocked kèm câu hỏi xoá)

## Câu hỏi mở
- Gun1/2/3 có anim Close/Out khi sang Phase 2 không dùng ở Phase 1 — xử lý khi làm Phase 2 (P1→P2: Idle1_to_2 của thân).
- Chưa tải lại Google Sheet sau 2026-10-19 (bước 0 AGENT.md); design WB6 local không đổi.
- Muse không thấy Unity Editor nên viết Editor script dựa trên code/JSON; nếu tên flow/field khác mẫu WB5 thì báo BLOCKED kèm câu hỏi.
