# WB2 P2+P3 — súng (OnlyAnim) chạy Out → Close cùng lúc súng swap Open
goal: Đổi `OnlyAnimGunWB2` thành script chung `WorldBossOnlyAnimGun` (giữ GUID) và sửa logic: bỏ tự chạy anim lúc OnEnable. Súng (OnlyAnim) đứng pose mở của phase trước (CloseInit), rồi phát flow `Out_* → Close` CÙNG FRAME với lúc súng swap được chỉ định bắt đầu `flowOpen`. Áp cho P2 (2× G1, Out_gun1) và P3 (4× G2, Out_Gun2).
project: ~/wkspaces/WB
source: Project 1941 — worldboss-boss-library.md mục 20 (quy ước anim WB2); plan wb2-close; Linh chốt 2026-10-16; dò project 2026-10-16

## Luật (Linh chốt 2026-10-16)
- Súng (OnlyAnim) = vị trí súng phase cũ bị bỏ. Không có `WorldBossWeapon`, không nằm trong chain.
- Flow `CloseInit` = Idle của súng phase trước, loop (P2 G1: `Idle_Gun1`; P3 G2: `Idle_Gun2`).
- Flow `Open` = `Out_*` (loop=false) → `Close` (loop=true).
- Phát `Open` cùng frame với `flowOpen` của súng swap được gán (`syncOpenWith`); súng swap có `openOnce: 1` nên chỉ một lần mỗi fight. Fight lại thì `Init()` trả về `CloseInit`.
- Độ dài Out_* và anim Mix không cần khớp, mỗi bên chạy độc lập.
- Không đổi serialize field có sẵn. Ngoại lệ Linh yêu cầu 2026-10-16: đổi tên script OnlyAnimGunWB2 → WorldBossOnlyAnimGun (script chung), giữ GUID. Không lưu MakeAll.unity khi chưa hỏi Linh.

## T1 — WorldBossWeapon: thêm event OnOpenStart + OnInitDone (chỉ thêm, không đổi logic)
owner: muse
depends: -
context:
Thêm `public event Action OnOpenStart;` gọi trong `Play()` NGAY TRƯỚC `anim.PlayFlow(flowOpen, Attack)` (chỉ khi thật sự phát Open; không gọi ở nhánh openOnce&&opened hoặc nhánh không có flowOpen).
Thêm `public event Action OnInitDone;` gọi cuối `Init()` sau `anim?.PlayFlow(flowCloseInit)`.
Không đổi thứ tự/hành vi hiện có. Class này dùng chung mọi WB, chỉ được thêm. Không kèm comment.
verified:
- WorldBossWeapon.cs:71 `public event Action OnCycleDone;` (mẫu khai báo event, `using System` đã có).
- WorldBossWeapon.cs:83-93 `public virtual void Init(BigDouble damage = default, float cooldown = 0)` → reset `opened=false` (:85), cuối hàm `anim?.PlayFlow(flowCloseInit)` (:93).
- WorldBossWeapon.cs:113-128 `Play()`: :115-120 openOnce&&opened → Attack(); :121-125 không dùng Open → Attack(); :126 `opened = true;` :127 `anim.PlayFlow(flowOpen, Attack);`.
- 3 class con kế thừa WorldBossWeapon (không override Play vì Play không virtual).
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBossWeapon.cs
done_when: compile 0 error; 2 event mới được invoke đúng vị trí; diff chỉ có thêm dòng, không có dòng comment.

## T2 — Đổi OnlyAnimGunWB2 → script chung WorldBossOnlyAnimGun (giữ GUID) + refactor CloseInit/Open, đồng bộ theo syncOpenWith
owner: muse
depends: T1
context:
ĐỔI TÊN để dùng chung cho mọi boss: `OnlyAnimGunWB2` → `WorldBossOnlyAnimGun`, chuyển file ra cạnh WorldBossWeapon.cs:
- `mv WorldBoss-2/OnlyAnimGunWB2.cs ../WorldBossOnlyAnimGun.cs` VÀ `mv WorldBoss-2/OnlyAnimGunWB2.cs.meta ../WorldBossOnlyAnimGun.cs.meta` (đi cùng cặp, KHÔNG tạo .meta mới) → giữ guid 172305a964b854dcd80ce5a299c9a524 nên prefab P2 không mất tham chiếu. Đổi tên class trong file cho khớp tên file.
- Bỏ `/// <summary>` cũ và mọi comment trong file; không thêm comment/Tooltip mới.
Fields mới: `[SerializeField] SpineAnimFlow anim;` `[SerializeField, FlowName(nameof(anim))] string flowCloseInit = "CloseInit", flowOpen = "Open";` `[SerializeField] WorldBossWeapon syncOpenWith;`
`OnEnable`: subscribe `syncOpenWith.OnOpenStart += PlayOpen; syncOpenWith.OnInitDone += ResetPose;` rồi gọi `ResetPose()`. `OnDisable`: unsubscribe.
`ResetPose()`: `opened=false; anim?.PlayFlow(flowCloseInit)`.
`PlayOpen()`: nếu `opened` return; `opened=true`; có `anim` → `anim.PlayFlow(flowOpen)`; không có `anim` → fallback field cũ `SetAnimation(0,outAnim,false)` + `AddAnimation(0,closeAnim,true,0)`.
GIỮ nguyên 2 field `outAnim`, `closeAnim` (đang serialize trong prefab P2).
Không cần sửa EnemyWB2_P2 / EnemyWB2_P3.
verified:
- OnlyAnimGunWB2.cs:14-20 OnEnable SetAnimation(0,outAnim,false)+AddAnimation(0,closeAnim,true,0f) → chạy ngay khi boss bật, đây là lỗi.
- OnlyAnimGunWB2.cs guid 172305a964b854dcd80ce5a299c9a524 hiện chỉ có trong EnemyWB2_P2_A.prefab (dòng 1073, 1844).
- SpineAnimFlow.cs:100 `public void PlayFlow(string _flowName, System.Action _onCompleted = null)`; GO inactive → gọi callback rồi return (:102-106).
- FlowNameAttribute: Assets/Spine/SpineTools/FlowNameAttribute.cs; dùng như WorldBossWeapon.cs:39.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-2/OnlyAnimGunWB2.cs(+.meta) → Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBossOnlyAnimGun.cs(+.meta)
done_when: compile 0 error; file mới không có comment/summary/Tooltip; file cũ không còn, WorldBossOnlyAnimGun.cs.meta giữ guid 172305a9…; prefab P2 mở không có Missing Script; OnEnable không còn play Out; PlayOpen chạy đúng 1 lần cho tới lần ResetPose kế tiếp.

## T3 — Prefab P2: 2× G1-(OnlyAnim) → SpineAnimFlow + sync với G2
owner: claude
depends: T2
context:
Trên `EnemyWB2_P2_A/G1-L-(OnlyAnim)/Animation` và `G1-R-(OnlyAnim)/Animation`: thêm `SpineAnimFlow` (skeleton Gun1_Swap2):
- `CloseInit`: `Idle_Gun1` Loop=true.
- `Open`: `Out_gun1` Loop=false → `Close` Loop=true (Delay 0, Track 0).
WorldBossOnlyAnimGun: `anim` = SpineAnimFlow cùng GO; `syncOpenWith` = WorldBossWeapon của `G2-L` (cho G1-L) / `G2-R` (cho G1-R).
verified:
- OnlyAnim P2 dùng Gun1_Swap2_SkeletonData (guid e2836ee987b1647dfa04a68fc222fe3e, prefab dòng 1028/1799); SkeletonAnimation `_animationName` rỗng.
- Anim có trong Gun1_Swap2.skel.bytes: Out_gun1, Out_Gun2, Close, Idle_Gun1, Idle_Gun2, Gun1_Mix_Gun2, Attack_Gun1/2, In_Gun1.
- G2-L/G2-R: WorldBossWeapon, flowCloseInit `CloseInit`=Idle_Gun1, flowOpen `Open`=Gun1_Mix_Gun2→Idle_Gun2, `openOnce: 1` (prefab :2327-2336, :3377-3386); EnemyWB2_P2.weaponS2L/S2R :4367-4368.
- EnemyWB2_P2.cs:57-61, :74-75 → G2-L/R `Play()` cùng frame ở lượt S2 đầu.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P2_A.prefab
done_when: 2 OnlyAnim có SpineAnimFlow đúng; anim/syncOpenWith không null; Console sạch.

## T4 — Prefab P3: 4× G2-(OnlyAnim) → WorldBossOnlyAnimGun + SpineAnimFlow + sync với G5
owner: claude
depends: T2
context:
4 GO `EnemyWB2_P3_A/G2-(OnlyAnim)/Animation` (cùng tên, hiện chỉ có SkeletonAnimation start `Out_Gun2`):
- Thêm `SpineAnimFlow`: `CloseInit` = `Idle_Gun2` Loop=true; `Open` = `Out_Gun2` Loop=false → `Close` Loop=true.
- Thêm `WorldBossOnlyAnimGun`: anim = SpineAnimFlow cùng GO, outAnim `Out_Gun2`, closeAnim `Close`, `syncOpenWith` = WorldBossWeapon `G5` (súng swap mở đầu tiên ở P3: S5 trong IE_MS1).
- Đổi SkeletonAnimation `_animationName` từ `Out_Gun2` → `Idle_Gun2` loop (tránh nháy Out 1 frame trước khi ResetPose chạy).
verified:
- P3 OnlyAnim: 4× `G2-(OnlyAnim)/Animation`, skeleton e2836ee9 (Gun1_Swap2), start `Out_Gun2`, KHÔNG có WorldBossOnlyAnimGun / SpineAnimFlow.
- Swap guns P3: G5 (Gun3_Swap5, CloseInit Idle_Gun3, Open Gun3_mix_Gun5→Idle_Gun5, openOnce 1), G7 (Gun4_Swap7, CloseInit Idle_Gun4, Open Gun4_Mix_Gun7→Idle_Gun7, openOnce 1); G6 súng mới (CloseInit Close, Open In→Idle).
- EnemyWB2_P3_A.prefab :11178-11181 weapons = G5, G6, G7, MS1-Opening; :11189 weaponS5 = G5 (fileID 4492187944407180330).
- EnemyWB2_P3.cs:148-158 IE_MS1: weaponMS1 → teleport → `IE_PlayWeaponOnce(weaponS5)` (G5 mở lần đầu ở đây).
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB2_P3_A.prefab
done_when: 4 OnlyAnim có WorldBossOnlyAnimGun + SpineAnimFlow đúng, syncOpenWith = G5; Console sạch.

## T5 — Test Play P2 + P3
owner: claude
depends: T1, T3, T4
context:
P2: lúc Appear (Idle1_to_2) OnlyAnim đứng `Idle_Gun1`; khi G2-L/R bắt đầu `Gun1_Mix_Gun2` thì 2 OnlyAnim bắt đầu `Out_gun1` cùng frame → `Close` loop; lượt S2 thứ 2–3 không phát lại.
P3: 4 OnlyAnim đứng `Idle_Gun2` qua MS1-Opening + teleport; khi G5 bắt đầu `Gun3_mix_Gun5` thì `Out_Gun2` cùng frame → `Close`; vòng MS1 sau không phát lại.
Fight lại (Init) → về CloseInit rồi lặp đúng.
verified:
- Node test: [GD-Khoa]/TestEnemyNode/WB/WB2_P2.prefab (tham chiếu EnemyWB2_P2_A); MakeAll.unity cũng có instance.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/learnings/worldboss-wb2-onlyanim-gun.md
files: - (chỉ test)
done_when: track0 OnlyAnim = Out_* bắt đầu cùng frame (±1) với track0 súng sync = anim Mix; sau đó Close; không phát lại; Console 0 error.

## T6 — Cập nhật library mục 20
owner: claude
depends: T5
context:
Thay dòng `(OnlyAnim)` ở mục 20 bằng luật mới (xem đầu plan), ghi ngày 2026-10-16. Ghi rõ: P2 G1 sync G2-L/R; P3 G2 sync G5.
verified:
- Library mục 20 hiện ghi "SkeletonAnimation start anim Out_* loop=false → giữ frame cuối. P2: 2× Out_gun1; P3: 4× Out_Gun2".
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
files: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
done_when: mục 20 phản ánh luật mới.

## Câu hỏi mở
- P3: plan cho 4 OnlyAnim G2 sync với **G5** (súng swap đầu tiên mở ở P3). Muốn sync với súng khác (vd G7) thì chỉ cần đổi field `syncOpenWith` ở T4.
