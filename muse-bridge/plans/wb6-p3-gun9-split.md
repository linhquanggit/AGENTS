# WB6 P3 — 2 gun 9 (Gun3_Mix_Gun9_1, Gun3_Mix_Gun9_2): gun bắn ra 8 cọc, anim theo nhịp, logic nối giữ nguyên
goal: P3 S9 có 2 gun. Gắn Spine Gun9 thứ 2 vào bone Gun3_Mix_Gun9_2 (gun 1 đã ở Gun3_Mix_Gun9_1). Mỗi gun "bắn ra" cột 4 cọc của mình (spawn tại bệ gun, bay tới anchor D rồi đứng yên) và phát anim theo nhịp (In → Idle → Attack mỗi lượt nối → Out). Luật OneToMany của PylonLinkShot (4 L/O, N=1..4, CD sau O) KHÔNG đổi. Làm y hệt P2 S6 (plan wb6-p2-gun6-split, đã gồm P2 vào ngang).
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (18.6 WB6), BR_WorldBoss/briefs/WB6_P3.md (mục "Chốt Linh 2026-10-23" S9). Linh chốt 2026-10-28: "P3 G9 cũng sẽ có 2 súng" (áp dụng cùng cách làm với P2 G6: gun bắn ra cọc, anim theo nhịp).

## T1 — Muse: bảo đảm PylonLinkShot hỗ trợ launch origin + hook lượt nối cho cả OneToMany
owner: muse
depends: -
context:
- Plan `wb6-p2-gun6-split` T1 (đã done) thêm vào PylonLinkShot: `column1LaunchFrom`/`column2LaunchFrom`, `launchSpeed`, đợi 6 cọc `IsArrived` rồi mới vào O đầu, và event `OnLinkFired(bool startIsC1)` ở `FireLink` (PairUnique). Task này KIỂM xem plan đó đã xong chưa: đã có field thì chỉ bổ sung phần còn thiếu cho OneToMany; chưa có thì làm đủ theo mô tả ở plan đó (không làm trùng, không đổi tên field).
- Với OneToMany: `OnLinkFired` bắn ĐÚNG 1 LẦN mỗi L (không phải mỗi đường), tham số = cột của điểm đầu (`_head.isC1`); "đợi đủ 8 cọc tới nơi" trước O đầu; mỗi O sau (sau CD) không bay lại, cọc vẫn đứng yên.
- Field mới để trống = chạy y như cũ. Số tạm ghi `// TEMP` + Tooltip.
verified:
- Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PylonLinkShot.cs: `SpawnPylon` :161, `PinPylons` :184, `FireLink` :262, `IE_OneToManyCycle` (≈ :344–:366: lặp `_heads`, `FireLink(k, …)` mỗi đích), `BuildHeads` :367. Mode `OneToMany` dùng `OnShotFinish`/`OnEndShot` + cooldown BaseShotE.
- BulletDestinationWB5.cs:14 `IsArrived`.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 16–17)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/PylonLinkShot.cs
done_when: compile 0 error; PylonLinkShot (cả PairUnique và OneToMany) có launch origin + OnLinkFired; để trống thì log O/L y như cũ ở cả P2 S6 và P3 S9.

## T2 — Claude: thêm Spine Gun9 thứ 2 vào bone Gun3_Mix_Gun9_2, chia cột cọc
owner: claude
depends: -
context:
- Gun 1 đã đúng: `G9-PylonLink` có `BoneFollower.boneName = "Gun3_Mix_Gun9_1"` (bone tồn tại). Giữ nguyên, chỉ thêm gun thứ 2.
- Mỗi gun: 1 node có BoneFollower riêng + 1 Spine Gun9. Gun9_1 (trái) → C1 (D1–D4), Gun9_2 (phải) → C2 (D5–D8).
- Hiện cả 2 cột nằm quanh Gun9_1 (C1 x=-2.63, C2 x=-0.13, y 1.19/1.79/2.39/2.99) — TEMP. Đổi C2 sang quanh Gun9_2, đối xứng với C1: D5–D8 x ≈ +2.55 (Gun9_2.x 1.30 + 1.25), giữ nguyên y các hàng. Ghi rõ trong báo cáo đây là TEMP để Linh chỉnh.
- Anim: không dùng `SpineAnimSync` cho Attack (2 gun phát Attack độc lập); In/Idle/Out phát cả 2 gun cùng lúc.
- Không SaveScene khi Linh chưa đồng ý (MakeAll dùng chung). Object tạm tắt phải bật lại.
verified:
- Body skeleton `World_Boss_6_SkeletonData` có bone `Gun3_Mix_Gun9_1` (world −1.38, 2.09) và `Gun3_Mix_Gun9_2` (1.30, 2.08) (Unity MCP live 2026-10-28; root EnemyWB6_P3_A (0,3,0) scale 1, anim body Idle3). Bone dùng chung tên với P1 G3 ("Mix").
- Live: `WB-Shooter/EnemyWB6_P3_A/G9-PylonLink` (WorldBossWeapon + BoneFollower `Gun3_Mix_Gun9_1`) → con `Animation` (Spine `Gun9_SkeletonData`, AnimationName rỗng) + con `PylonLinkShot` (D1–D8, mode OneToMany, linkDuration 0.5, delayBetweenLinks 0.5, targetsPerLink 3, headsPerCycle 4, useSelfCD, cooldown 2, pylonPrefab `Bullet_WB6_P3_G9_Pylon`). WorldBossWeapon.anim = null, flow* rỗng.
- `column1Points` = D1–D4, `column2Points` = D5–D8 (đúng 4+4).
- Spine `Gun9_SkeletonData`: anim `Attack`(4.0s) `Close` `Idle`(0.1s) `In`(3.2s) `Out`(1.5s).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10, 18.6)
files: Assets/Project/Scenes/MakeAll.unity (trong Editor, KHÔNG lưu) — WB-Shooter/EnemyWB6_P3_A/G9-PylonLink
done_when: có 2 node gun (BoneFollower `Gun3_Mix_Gun9_1`, `Gun3_Mix_Gun9_2`), mỗi node 1 Spine Gun9; D1–D4 là con node gun 1, D5–D8 là con node gun 2; C2 đã dời quanh gun 2 (TEMP); `column1Points`/`column2Points` vẫn 4+4; Console 0 error.

## T3 — Claude: nối launch origin + flow anim Gun9
owner: claude
depends: T1, T2
context:
- Gán `column1LaunchFrom`/`column2LaunchFrom` = node gun 1 / gun 2; `launchSpeed` TEMP.
- Flow anim (giống P2 S6): `In` khi mở/phóng cọc → `Idle` → `Attack` ở gun CỘT CỦA ĐIỂM ĐẦU mỗi L (qua `OnLinkFired`), về Idle → `Out` khi hết phase/StopAttack. Cần thêm SpineAnimFlow cho Spine gun theo mẫu WB5 G6; WorldBossWeapon.anim/flow* gán theo đó. Độ dài anim không cần khớp 0.5s của L.
- Lưu ý: S9 là OneToMany có CD sau mỗi O (2s TEMP): cọc đứng yên suốt, gun chỉ chạy Attack lúc có L; giữa các O ở Idle.
- Xác nhận bằng mắt khi Play (không đoán): thời điểm cọc ra khỏi bệ so với `In` (3.2s); nếu lệch chỉnh `launchSpeed`/delay và ghi vào báo cáo.
verified:
- Field/hook do T1 tạo (đọc lại PylonLinkShot.cs sau T1 để dùng đúng tên).
- WorldBossWeapon.cs: flowIdle/flowOpen/flowCloseInit/flowAttack/flowStop :39; PlayFlow :95–:131; StopAttack có flowStop :255–:265.
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10, 20: quy ước anim)
files: MakeAll.unity (trong Editor, KHÔNG lưu) — G9-PylonLink và 2 node gun
done_when: Play P3: 2 gun chạy In → 8 cọc bay ra từ 2 bệ tới anchor → Idle; mỗi L gun của điểm đầu phát Attack; hết phase phát Out; Console 0 error.

## T4 — Claude: test Play 8 cọc + luật OneToMany
owner: claude
depends: T3
context:
- Luật giữ (brief WB6_P3 "Chốt 2026-10-23"): 1 O = 4 L; đầu O chọn 4 điểm đầu, mỗi cột ≥ 1, thứ tự random; mỗi L điểm đầu nối cùng lúc tới N (1..4) điểm random cột đối diện, không trùng đích trong 1 L, giữa các L được trùng; chung linkDuration 0.5s → delayBetweenLinks 0.5s; hết 4 L → CD (2s TEMP) → O mới.
- `PylonLinkShot.debugLog` đang true → log O/L vào `~/.claude/logs/flow-capture.log` (truncate trước khi chạy). Test tuần tự (dùng chung 1 Editor). Kiểm thêm P2 S6 (cùng PylonLinkShot) vẫn y như cũ nếu chạy được.
verified:
- PylonLinkShot.cs log OneToMany `O{n} L{k}: D? -> {…}` (≈ :357).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 15.4)
files: ~/.claude/logs/flow-capture.log (chỉ đọc)
done_when: ≥ 3 O: mỗi O đúng 4 L, mỗi cột có ≥ 1 điểm đầu, không cặp cùng cột, N đúng, có CD giữa các O; link chỉ bắt đầu sau khi 8 cọc tới nơi; cọc đứng yên sau đó; P2 S6 không đổi; Console 0 error.

## T5 — Claude: báo cáo, cập nhật library, dọn
owner: claude
depends: T4
context:
- AGENT.md mục 5: báo cáo dạng bảng (Đã dựng / Code-prefab mới / Thông số / Test / Dev-artist làm tiếp). Cập nhật library mục 18.6, mục 17 (PylonLinkShot: launch origin + OnLinkFired cho OneToMany), dòng WB6 P3 G9; đồng bộ lên project doc `claude/worldboss-boss-library.md`; brief WB6_P3 thêm mục "Chốt Linh 2026-10-28".
- Task DỌN: plan không tạo Editor script/MenuItem/ảnh. Xoá `~/.claude/logs/wb6-p3-gun9-split-*` nếu có; trả `debugLog` về giá trị gốc nếu đã đổi. GIỮ: PylonLinkShot.cs đã sửa, scene (chưa lưu, chờ Linh).
verified:
- Plan không tạo file tạm trong project (chỉ sửa PylonLinkShot.cs + thao tác scene qua MCP).
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/AGENT.md (mục 5)
files: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md, /Users/mobione/wkspaces/Brain/BR_WorldBoss/briefs/WB6_P3.md
done_when: library + brief cập nhật, đã đồng bộ project doc, báo cáo có dòng `Đã dọn: …`, không còn file tạm trong project/logs/shots.

## Câu hỏi mở
- Giả định: P3 S9 làm giống P2 S6 (gun bắn ra cọc + anim theo nhịp). Linh chưa nói rõ cho S9 — nếu không muốn phóng cọc từ bệ, báo lại để bỏ T1/T3.
- Vị trí 8 cọc: C2 dời quanh gun 2 (x ≈ +2.55, TEMP đối xứng C1). Linh muốn chỉnh khác thì nói.
- Nên chạy plan này SAU `wb6-p2-gun6-split` (T1 của plan P2 và T1 của plan này cùng sửa PylonLinkShot.cs — không chạy song song).
