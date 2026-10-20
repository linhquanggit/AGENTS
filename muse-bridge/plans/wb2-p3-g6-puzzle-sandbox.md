# WB2 P3 Súng 6 — Puzzle sandbox (MakeAll) + FX Boss2.prefab
goal: Dựng một sandbox độc lập trong MakeAll để Linh chơi thử logic puzzle mới của S6 (chuỗi random-walk N bước, đi đúng thứ tự, sai là kill ngay), với player thay bằng một GameObject proxy điều khiển tay. FX dùng luôn Boss2.prefab (mask hình quạt). CHƯA đụng EnemyWB2_P3 / PuzzleZoneWB2 / KillZoneLaserWB2 / G6. Linh test xong mới apply sang boss.
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library (mục 7, 15), brief WB2_P3.md, chat chốt logic 2026-10-19

## T1 — Core logic puzzle (plain C#, dùng chung sandbox + boss sau này)
owner: muse
depends: -
context:
LOGIC ĐÃ CHỐT (Linh, 2026-10-19):
- N ô quạt đều quanh TÂM (tham số center truyền vào). Sandbox: tâm màn hình. Boss sau này: vị trí Súng 6 (không phải tâm boss). Ô 0 bắt đầu từ +X, đánh số ngược kim đồng hồ, góc ô = 360/N. N = 6 (lượt 1), 8 (lượt 2+).
- Chuỗi dài đúng N bước. Bước 1 random 0..N-1. Bước sau random (i-1) hoặc (i+1) mod N. Mỗi ô xuất hiện tối đa 2 lần trong chuỗi (nếu một hướng đã full thì bắt buộc hướng kia; nếu cả 2 full thì bỏ ràng buộc max-2 cho bước đó, KHÔNG được kẹt). Hai bước liền nhau không bao giờ trùng ô.
- Pha input: con trỏ step = 0. Player vào ô seq[step] → step++ (ô đó báo "đúng" để FX bật XANH giữ nguyên). Ô lặp (2→3→2) phải rời ô rồi vào lại mới tính (vì seq[k] != seq[k-1] nên chỉ cần so ô hiện tại == seq[step] khi ô đổi).
- SAI khi: (a) step >= 1 và player vào ô KHÔNG phải seq[step-1] (ô đang đứng hợp lệ) và KHÔNG phải seq[step]; (b) hết touchTime. Trước khi chạm seq[0] thì đi đâu cũng được.
- safeRadius (TEMP 0 = tắt): player cách tâm < safeRadius thì không xét ô (giữ ô trước đó).
- Kết quả fail: wrongZone (ô vừa vào sai, -1 nếu timeout) + danh sách ô đỏ = các ô seq[step..] còn lại (distinct) + wrongZone. Kill NGAY ở lỗi đầu tiên.
- Ô xanh đã sáng mà chuỗi còn cần quay lại nhưng player không quay lại → nằm trong seq[step..] → vẫn chuyển ĐỎ.
Thiết kế: class thuần `SectorPuzzleWB2` (không MonoBehaviour), API gợi ý:
  `static int GetSector(Vector2 center, Vector2 pos, int n)` · `static float ZoneStartAngle(int zone,int n)`
  `static List<int> BuildSequence(int n, int length, System.Random rng)` (rng inject để test)
  `void Begin(List<int> seq, int n)` · `StepResult Feed(int sector)` → enum {None, Correct, Wrong, Completed} · `int Step` · `List<int> GetRedZones(int wrongZone)`.
verified:
PuzzleZoneWB2.cs (SpecialCase) đang có GetSector/GetZoneCenterAngle cùng quy ước góc (Atan2, 0..360, Floor(angle/(360/n))) — giữ y hệt quy ước để apply sau không lệch.
Folder WorldBoss-2 hiện chỉ có EnemyWB2_Drone/P1/P2/P3.cs.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md (chỉ tham khảo quy ước WB2)
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/Puzzle/SectorPuzzleWB2.cs (mới)
done_when: compile 0 error. Có thêm 1 EditMode test (hoặc menu Editor log) chạy BuildSequence 1000 lần N=6 và N=8: độ dài = N, mọi bước kề nhau ±1 mod N, không bước nào trùng bước trước, mỗi ô ≤ 2 lần (trừ trường hợp fallback, phải log nếu xảy ra). Feed() đúng các case: đi đúng hết → Completed; vào ô sai sau bước 1 → Wrong; lặp ô 2→3→2.

## T2 — SectorMaskFxWB2: mask hình quạt + điều khiển FX Boss2
owner: muse
depends: T1
context:
- FX = 1 vùng toàn cảnh, KHÔNG chia góc. Code tạo N SpriteMask hình quạt (Sprite.Create từ texture trắng nhỏ + Sprite.OverrideGeometry polygon quạt: tâm + cung chia ~ mỗi 5°, bán kính `radius` TEMP 20). Bật mask ô i → chỉ phần FX trong quạt i hiện.
- XANH: mask ô đã đi đúng (giữ) + mask ô đang nháy. ĐỎ: mask các ô đỏ (nhiều ô cùng lúc). Vì XANH và ĐỎ có thể hiện cùng lúc ở ô khác nhau → 2 bộ mask tách biệt bằng sorting range (bộ G dùng range bao Warning_G, bộ R bao Warning_R; isCustomRangeActive = true) hoặc dùng 2 sorting layer/order khác nhau. Không để mask ảnh hưởng sprite khác trên màn.
- Smoke: nhân bản child `Smoke` thành N đường, mỗi đường xoay quanh Z theo biên ô (k*360/N). Smoke gốc có rotation X -90 nên tia chạy dọc +Y local cha → cần bù góc (đường biên ở góc k*360/N tính từ +X ⇒ rotZ = k*360/N - 90). Kiểm bằng mắt.
- API: `Build(int n)`, `FlashBlue(int zone)` / `SetBlueHeld(IEnumerable<int>)`, `ShowRed(IEnumerable<int>)`, `HideAll()`, `ShowSmoke(bool)`. Mọi thông số [SerializeField] chỉnh trên Inspector.
- Tự set ParticleSystemRenderer.maskInteraction = VisibleInsideMask cho toàn bộ renderer dưới Warning_G / Warning_R lúc Awake (để prefab không phụ thuộc chỉnh tay), nhưng T3 vẫn chỉnh sẵn trên prefab.
verified:
Boss2.prefab (Assets/Effect_1945/Prefab/effect_WorldBoss/Boss2.prefab, guid 97e9b0e7599e70a408f5126790f420b2, 5.4MB) cây: Boss2 → Warning_R (PS loop 0.5s, order -5; con Wave, Flash) · Smoke (PS rot X -90, ~37 con "trang"/"den" order 1–2, con "point" ở y 8.22) · Rada (PS loop) · Warning_G (PS loop, order -5; con Wave, Flash). Không có MonoBehaviour nào. Toàn bộ m_MaskInteraction: 0.
Chưa có SpriteMask nào trong Assets/Project (grep "SpriteMask" rỗng).
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/Puzzle/SectorMaskFxWB2.cs (mới)
done_when: compile 0 error; trong Editor gọi Build(6) + ShowRed({1,3}) + FlashBlue(0) thấy đúng 3 quạt, smoke 6 đường đúng biên.

## T3 — Gắn SectorMaskFxWB2 vào Boss2.prefab
owner: claude
depends: T2
context:
Linh yêu cầu làm luôn trên Boss2.prefab để test xong chỉ cần apply thẳng cho boss. Không đổi tên/xoá child có sẵn. Chỉ: add component SectorMaskFxWB2 ở root Boss2, gán ref Warning_G / Warning_R / Smoke / Rada, set Mask Interaction = Visible Inside Mask cho mọi ParticleSystemRenderer dưới Warning_G và Warning_R. Smoke, Rada giữ None.
verified:
Boss2.prefab cấu trúc như T2. Root Boss2 chỉ có Transform.
files: Assets/Effect_1945/Prefab/effect_WorldBoss/Boss2.prefab (sửa)
done_when: prefab lưu, mở lại không Missing Script; instance trong scene: tắt hết mask thì Warning_G/R không hiện.

## T4 — Sandbox controller + player proxy
owner: muse
depends: T1, T2
context:
- `PuzzleSandboxWB2` (MonoBehaviour) chạy vòng lặp: Build(N) → smoke → nháy chuỗi (mỗi bước flashTime, nghỉ flashGap) → delayBeforeInput → pha input (Feed sector của proxy mỗi frame, Correct → giữ xanh) → Completed: log "PASS", hoặc Wrong/timeout: ShowRed + chờ redWarnTime (mặc định 0) → laser (LineRenderer tạm, từ tâm Gun6Pivot tới proxy, laserDuration) → proxy nháy đỏ + log "KILL" → nghỉ restartDelay → lượt sau (lượt 1 N=6, lượt 2+ N=8, phím N đổi cưỡng bức 6/8, phím R chơi lại từ đầu, phím Space bỏ qua chờ).
- Không dùng GamePlayController / FighterCurrent. Mục tiêu = `PuzzleProxyPlayerWB2`: sprite tròn, kéo bằng chuột (giữ chuột trái) + WASD/arrow, tốc độ moveSpeed, clamp trong vùng camera.
- Thông số [SerializeField] TEMP: flashTime 0.35, flashGap 0.1, delayBeforeInput 0.5, touchTimePerStep 1.0 (touchTime = N × giá trị này), redWarnTime 0, laserDuration 0.6, restartDelay 1.5, safeRadius 0.
- HUD OnGUI góc trên trái: lượt, N, chuỗi (vd "2 → 3 → 2 → 1 → 0 → 5"), step hiện tại, thời gian còn lại, kết quả lượt trước, phím tắt. Có toggle `showSequenceDebug` để ẩn chuỗi khi chơi thật.
- Gizmo: vẽ N tia biên + số ô.
verified:
GamePlayController.cs:219 `public FighterBase FighterCurrent` — sandbox KHÔNG được phụ thuộc cái này.
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/Puzzle/PuzzleSandboxWB2.cs, PuzzleProxyPlayerWB2.cs (mới)
done_when: compile 0 error.

## T5 — Dựng sandbox trong MakeAll + test Play
owner: claude
depends: T3, T4
context:
- Linh (2026-10-19): trong MakeAll TẮT mọi root object không liên quan tới sandbox (SetActive false, kể cả WB-Shooter). Ghi danh sách root đã tắt vào log + vào component sandbox (field `disabledRoots`, kèm nút context menu "Restore disabled roots") để bật lại được. Không xoá object nào.
- TÂM VÙNG = TÂM MÀN HÌNH: `Gun6Pivot` đặt tại tâm camera (x, y của Main Camera, z = 0). Không đặt ở mũi boss trong sandbox. Khi apply cho boss mới đổi tâm sang transform G6.
- Root mới `WB2-PuzzleSandbox`. Con: `Gun6Pivot`, instance `Boss2` dưới Gun6Pivot (pos 0), `PlayerProxy` (sprite tròn tạm, ở (0,-3)), LineRenderer laser tạm. Sprite proxy mượn tạm → ghi rõ trong báo cáo.
- LUẬT: không save MakeAll khi Linh chưa đồng ý (scene dùng chung) — dựng xong hỏi Linh có save không (nếu save thì các root vẫn ở trạng thái tắt, báo rõ cho Linh).
verified:
Assets/Project/Scenes/MakeAll.unity có root `WB-Shooter` (dòng 102742). Scene file sửa lần cuối 2026-10-16.
files: Assets/Project/Scenes/MakeAll.unity (chưa save)
done_when: Play: chỉ còn sandbox hiện trên Game view; tâm quạt trùng tâm màn hình; đủ 3 case chạy đúng và log rõ — (1) đi đúng hết N=6 → PASS, lượt 2 N=8; (2) vào ô sai sau bước 1 → đỏ ngay + laser + KILL; (3) đứng im tới hết giờ → đỏ các ô còn lại + KILL. Console 0 error. Có screenshot Game view lúc nháy xanh và lúc đỏ.

## T6 — Báo cáo + ghi knowledge
owner: claude
depends: T5
context:
Báo cáo bảng ngắn cho Linh (đã dựng / code mới / thông số TEMP / test 3 case / việc còn lại khi apply cho boss: PuzzleZoneWB2 dùng SectorPuzzleWB2 + SectorMaskFxWB2, KillZoneLaserWB2 aim player ONE_SHOT, EnemyWB2_P3.IE_MS2 nối lại, tâm = transform G6).
files: ~/wkspaces/Brain/BR_WorldBoss/briefs/WB2_P3.md (thêm mục "S6 puzzle v2 — sandbox"), library mục 7 (1 dòng trạng thái)
done_when: Linh đã đọc báo cáo.

## Câu hỏi mở
- Warning_G / Warning_R có phủ đủ toàn màn khi đặt ở tâm màn không? Nếu không, T5 scale tạm instance Boss2 và ghi lại để artist chỉnh.
- safeRadius để 0 (tắt) — Linh chơi thử rồi quyết có bật không.
- Laser kill trong sandbox là LineRenderer tạm; khi apply cho boss dùng LaserShotE con của G6 như hiện tại.
