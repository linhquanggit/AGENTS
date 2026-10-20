# WB5 P2/P3 — đạn link ra xa mới kill, cầu điện tụ to, anim G9/G8
goal: P2_A: 2 thanh link bay ra ngoài màn 1 khoảng mới kill (không kill ngay ở rìa, không kill thanh còn lại theo). P3_A: cầu điện (cọc) G9 tụ to dần ở súng rồi mới bay (cả S1+S2); sửa anim súng G9 (mở + bắn); G8 mở 1 lần rồi giữ nguyên.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md; dò trực tiếp WB (plan liên quan wb5-p3-g9-spread)

## T1 — P2 link: thanh ra khỏi màn 1 khoảng mới kill, cả 2 thanh
owner: muse
depends: -
context:
Hiện: thanh chạm rìa (CameraBouns + offsetBouns 0.1) → Bullet.OnUpdateDestroy kill ngay; shot thấy GetAliveBullets() < 2 → tắt laser + DespawnVolley() kill luôn thanh kia.
Mới (đã chốt killOutDistance = 2f world): thêm [SerializeField] float killOutDistance = 2f (world) trong DestinationLinkShotWB5. Khi spawn volley, set offset hủy của thanh = killOutDistance (thêm setter public SetOffsetBounds(Vector2) vào Bullet/BulletDestinationWB5, reset về giá trị prefab khi DestroyDefault/OnDisable để không ảnh hưởng pool khác). Laser giữ tới khi CẢ 2 thanh đã ra ngoài màn ≥ killOutDistance (hoặc 1 thanh bị hủy) — mỗi thanh tự hủy theo bounds của nó, không DespawnVolley kill thanh còn trong màn; DespawnVolley chỉ dọn thanh còn sót khi StopAttack.
verified:
DestinationLinkShotWB5.cs (Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase):29 linkUntilOutScreen; :246-249 while (GetAliveBullets().Count >= 2) yield; :260 DespawnVolley(); :276-281 DespawnVolley gọi _b.DestroyDefault() cho mọi thanh active
Bullet.cs (Assets/Project/Bullets/Scripts):13 isDestroyOutCamera; :14 offsetBouns = (0.1,0.1); :43-48 Bouns = CameraBouns + offsetBouns (cache bouns); :223-238 OnUpdateDestroy kill khi |x|>Bouns.x hoặc |y|>Bouns.y
BulletDestinationWB5.cs :34 stopAtTarget=false (bay thẳng); :49 override DestroyDefault
Prefab EnemyWB5_P2_A dùng DestinationLinkShotWB5 + LinkLaser_DPS + ColliderFollowLineRender
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: DestinationLinkShotWB5.cs, Bullet.cs (setter offset, chú ý cache `bouns`), BulletDestinationWB5.cs
done_when: compile 0 error; Play P2_A: log vị trí lúc hủy mỗi thanh — cả 2 thanh hủy ở ngoài rìa ≈ killOutDistance, không thanh nào bị hủy khi còn trong màn; laser nối tới lúc thanh cuối ra; Console 0 error

## T2 — P3 G9: thanh điện tụ to dần ở súng rồi mới bay (S1 + S2)
owner: muse
depends: -
context:
Đã chốt: "cầu điện" = chính thanh/cọc điện barPrefab (BulletDestinationWB5) trong docs (P3 Súng 9: "2 thanh/bên nối nhau"), không phải FX riêng.
Thêm [SerializeField] float chargeTime = 0.5f và Vector2 chargeScaleFrom = Vector2.zero trong ElectricOrbitShotWB5. LaunchPair: spawn barA/barB tại fromA/fromB, scale từ chargeScaleFrom → scale prefab trong chargeTime (giữ đứng tại súng, dây chưa nối), xong mới bắt đầu approach (t tính từ sau charge). Áp dụng cho cả PERSISTENT_ORBIT (S1) và WEAVE_ONCE (S2). Không đổi OrbitPositions/FallOrbitPositions/WeavePoint, không đổi logic giãn của plan wb5-p3-g9-spread. Nhớ reset scale khi DespawnPair (pool).
verified:
ElectricOrbitShotWB5.cs:55 barPrefab (BulletDestinationWB5); :60 approachTime; :375-383 SpawnBar (pool, InitTarget, Init speed 0.01, ttl 999); :386-395 LaunchPair gán t=0, fromA/fromB = launchA/B.Position, barA = SpawnBar(fromA); :431-440 TickPair: _p.t += _dt; if (_p.t < approachTime) SmoothStep lerp
Bullet.cs:139 public virtual void InitScale(Vector2)
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: ElectricOrbitShotWB5.cs
done_when: compile 0 error; Play S1 + S2: log scale cọc mỗi 0.1s tăng 0→1 trong chargeTime khi đứng tại súng, sau đó mới bay; pool lần sau scale đúng; Console 0 error

## T3 — P3 G9: anim súng mở + bắn
owner: muse
depends: T2
context:
Đã tự check prefab: wiring ĐÚNG — không sửa prefab. Mỗi G9-Orbit-S1/S2 có Gun9-L/Animation (SkeletonAnimation + SpineAnimFlow, flows Idle/CloseInit/Open/Attack) và Gun9-R/Animation (SpineAnimSync source = Gun9-L cùng weapon). Skeleton Gun9.skel.bytes có đủ anim Open/Attack/Idle. WorldBossWeapon G9: anim → Gun9-L/Animation, flowOpen Open, flowAttack Attack, attackType 1 (ATTACK_ON_OPEN), useSelfAnimAttack 1.
Nguyên nhân: anim bắn chỉ phát qua actStartShot (BaseShotE.OnStartShot) = 1 lần mỗi lượt OnAttack, nhưng ElectricOrbitShotWB5 PERSISTENT chạy vòng while(true) trong IE_SpawnBullet và phóng cặp mới mỗi launchCooldown mà không gọi lại → không có anim Attack mỗi lần phóng; S2 tương tự mỗi lượt weave.
Sửa: trong ElectricOrbitShotWB5.LaunchPair (lúc bắt đầu charge của T2) gọi actStartShot?.Invoke() (field protected ở BaseShotE:62) để súng phát flow Attack mỗi lần phóng cặp. Thêm Debug.Log tạm "[G9Anim] <weapon> PlayFlow <flow>" ở WorldBossWeapon._AnimOpen/_AnimShot/Play để T5 xác nhận Open có chạy; nếu Open không chạy khi test, ghi lại log gửi Claude (không đoán).
verified:
BaseShotE.cs:62 protected System.Action actStartShot; :155-158 OnStartShot invoke actStartShot; :208-214 OnAttack → OnStartShot + StartCoroutine(IE_SpawnBullet)
ElectricOrbitShotWB5.cs:503-540 IE_SpawnBullet PERSISTENT while(true) mỗi launchCooldown → DespawnPair + LaunchPair; :386 LaunchPair
WorldBossWeapon.cs:108-116 Play → PlayFlow(flowOpen, Attack); :124-131 ATTACK_ON_OPEN SetShotAction(_AnimShot) + AutoAttack
EnemyWB5_P3_A.prefab: G9-Orbit-S1 anim 6905402582426459021, S2 anim 6905402582495643768 (Gun9-L/Animation mỗi weapon); SpineAnimSync dòng 16144 (S2 R→L), 16908 (S1 R→L); prefab S1 launchCooldown 2 (dòng 15564), S2 delayStart 5 (dòng 17076)
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: ElectricOrbitShotWB5.cs, WorldBossWeapon.cs (log tạm)
done_when: compile 0 error; Play P3_A: log [G9Anim] thấy Open 1 lần lúc vào rồi Attack mỗi lần phóng cặp (S1 mỗi 2s, S2 mỗi lượt); Gun9-R anim khớp Gun9-L; Console 0 error

## T4 — P3 G8: mở 1 lần rồi giữ nguyên
owner: muse
depends: T3
context:
Hiện: chain gọi weapon.Play() mỗi lần kích hoạt → PlayFlow(flowOpen) lại; Init gọi PlayFlow(flowCloseInit). flowStop rỗng nên Stop không đóng, nhưng Open vẫn phát lại → trông đóng/mở. Thêm vào WorldBossWeapon [SerializeField] bool openOnce = false: lần đầu Play() mở bình thường, set cờ opened; các lần sau bỏ qua flowOpen, gọi Attack() trực tiếp (vẫn idle/attack như cũ); reset cờ ở Init/khi chết. Mặc định false để không ảnh hưởng boss khác. Bật openOnce=1 cho G8-Portal trong EnemyWB5_P3_A.prefab.
verified:
WorldBossWeapon.cs:88 Init → anim?.PlayFlow(flowCloseInit); :108-116 Play(): UseOpenFlow() → anim.PlayFlow(flowOpen, Attack); :237-249 Stop()
EnemyWB5_P3_A.prefab G8-Portal WorldBossWeapon (~dòng 18018): anim → Animation (parent G8-Portal), flowOpen Open, flowStop rỗng, attackType 1
PortalShotWB5.cs không gọi anim súng (chỉ Charge_Portal FX)
files: WorldBossWeapon.cs, EnemyWB5_P3_A.prefab (G8-Portal openOnce=1)
done_when: compile 0 error; Play P3_A qua ≥3 lần kích hoạt G8: Open chỉ phát 1 lần, các lần sau súng giữ trạng thái mở và vẫn bắn portal; boss khác không đổi; Console 0 error

## T5 — Test tổng P2_A + P3_A
owner: claude
depends: T1, T2, T3, T4
context:
Chạy scene test WB5 các phase 2, 3; kiểm cả 4 yêu cầu, đọc Console.
verified:
Prefabs: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P2_A.prefab, EnemyWB5_P3_A.prefab
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: -
done_when: 4 yêu cầu đạt khi xem Play; Console 0 error/exception

## Câu hỏi mở
- (đã chốt hết: killOutDistance 2; cầu điện = thanh điện barPrefab; prefab G9 wiring đúng)
