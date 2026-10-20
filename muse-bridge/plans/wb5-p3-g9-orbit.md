# WB5 P3 Súng 9 — sửa Kiểu 1 và Kiểu 2 theo design mới
goal: Sửa ElectricOrbitShotWB5 để Kiểu 1 phóng theo cooldown đổi nửa màn, quay quanh tâm random rồi rơi; Kiểu 2 chạy vòng liên tục trong vành khuyên, chỉ huỷ khi chạm player rồi bắn lượt mới ngay (bỏ nổ ×2 MBC).
project: /Users/mobione/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 P3, 18.5, ghi chú súng 9) + Sheet design cột H dòng R523/R528 (cập nhật 2026-10-12)

## T1 — Kiểu 1: phóng theo cooldown, quay quanh tâm random rồi rơi
owner: muse
depends: -
context:
- Kiểu 1 (ô tím, G9-Orbit-S1): mỗi lượt phóng 1 cặp thanh điện, bay 1s tới nửa màn; lượt sau đổi sang nửa màn còn lại (trái/phải xen kẽ).
- Sau 1s nối laser giữa 2 thanh. Random chọn 1 thanh làm tâm (mỗi lượt random lại).
- Thanh tâm bay thẳng xuống. Thanh còn lại quay quanh tâm theo chiều kim đồng hồ, cùng rơi xuống.
- Phóng theo cooldown, không chờ cặp trước biến mất. Cả cặp ra khỏi màn thì huỷ. Thanh chạm player gây damage và mất, dây tắt.
- Thông số tạm (chưa có trong design, ghi là tạm): khoảng cách 2 thanh 110px = bán kính quay, rơi 60px/s, quay 120°/s, cooldown chưa chốt.
verified:
- ElectricOrbitShotWB5.cs:14 `enum MODE { PERSISTENT_ORBIT = 0, WEAVE_ONCE = 1 }`
- ElectricOrbitShotWB5.cs:42 `[SerializeField] protected MODE mode = MODE.PERSISTENT_ORBIT;`
- ElectricOrbitShotWB5.cs:110 `public override void Init(BreakInfinity.BigDouble damage = default, float cooldown = 0)`
- ElectricOrbitShotWB5.cs:124 `public override void StopAttack()`
- ElectricOrbitShotWB5.cs:161 `protected void OrbitPositions(Pair _p, float _orbitTime, int _sign, out Vector3 _a, out Vector3 _b)` (logic cũ: quay tại chỗ, cần thay bằng quay quanh tâm + rơi)
- ElectricOrbitShotWB5.cs:204 `protected void LaunchPair(Pair _p)`
- ElectricOrbitShotWB5.cs:228 `protected void DespawnPair(Pair _p)`
- ElectricOrbitShotWB5.cs:269 `protected override IEnumerator IE_SpawnBullet()`; nhánh PERSISTENT_ORBIT tại dòng 275
- Prefab dùng script: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P3_A.prefab (guid 6a99b29932fdb4535ac91a062be79d42)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10 P3, dòng ghi chú súng 9); /Users/mobione/wkspaces/Brain/BR_WorldBoss/design/ (CSV R523)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/ElectricOrbitShotWB5.cs
done_when: Compile 0 error; trong Play WB-Shooter Kiểu 1 đổi nửa màn mỗi lượt, tâm random, thanh còn lại quay đúng chiều và không dính vào tâm; cặp ra khỏi màn thì huỷ.

## T2 — Kiểu 2: vòng liên tục trong vành khuyên, huỷ khi chạm player rồi bắn lượt mới ngay
owner: muse
depends: -
context:
- Kiểu 2 (ô vàng, G9-Orbit-S2): 1 cặp, 2 thanh di chuyển lần lượt theo đường trong vành khuyên gần nửa dưới màn, chạy liên tục (loop), không tự huỷ, không nổ.
- Bỏ hoàn toàn logic "đi hết 1 vòng thì tự huỷ và nổ ×2 MBC". Không dùng blastPrefab cho kiểu này.
- Chỉ huỷ khi thanh chạm player (gây damage, biến mất, không có hiệu ứng nổ). Lượt mới bắn NGAY sau khi huỷ, không theo cooldown.
- Đổi chiều quay theo từng lượt.
- Quy ước di chuyển (đã chốt với Linh): 2 vòng tròn A1 (vòng ngoài, điểm P1..PN) và A2 (vòng trong, điểm Q1..QN), chia đều góc, N là input. Ban đầu A1 ở P1, A2 ở Q1. Step k: vòng đang di chuyển tịnh tiến tới điểm chỉ định, vòng kia đứng yên. Thứ tự: A1→Q2, A2→P2, A1→P3, A2→Q3, ... (luân phiên, mỗi vòng có chỉ số tăng dần, tịnh tiến không nhảy).
- Vị trí tương đối: đường đi trên Figma Style 2 (Brain/BR_WorldBoss/figma/tiles/WB5_part05.png) là vành khuyên, không phải hình sao đều 7 đỉnh.
verified:
- ElectricOrbitShotWB5.cs:59 `[SerializeField] protected Bullet blastPrefab;`
- ElectricOrbitShotWB5.cs:259 `protected void Blast(Vector3 _pos)` (bỏ gọi khỏi kiểu 2)
- ElectricOrbitShotWB5.cs:300 nhánh `// WEAVE_ONCE` trong IE_SpawnBullet (đang có logic hết vòng thì Blast + cd 2.5)
- Prefab đạn: Assets/Project/WorldBoss/Use/Enemies/Prefabs/Bullets/BulletWB5_P3_G9_Blast (không dùng sau khi sửa)
refs: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md (mục 10 P3, mục 18.5 dòng S9)
files: Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase/ElectricOrbitShotWB5.cs
done_when: Compile 0 error; trong Play kiểu 2 chạy loop liên tục không nổ; chạm player thì cặp huỷ và lượt mới bắn ngay; không còn tham chiếu Blast trong nhánh WEAVE_ONCE.

## T3 — Prefab và cấu hình súng 9 trong EnemyWB5_P3_A
owner: claude
depends: T1, T2
context:
- Giữ nguyên cấu trúc hierarchy. Chỉ đổi thông số shot: N điểm mỗi vòng, bán kính vòng, bỏ blastPrefab ở G9-Orbit-S2.
- Không tạo prefab boss hay node mới. Không sửa WorldBossSO hay Addressables.
- Thông số không có trong design: tự đặt và ghi là tạm.
verified:
- EnemyWB5_P3_A.prefab tham chiếu script ElectricOrbitShotWB5 qua guid 6a99b29932fdb4535ac91a062be79d42
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB5_P3_A.prefab
done_when: Prefab load không lỗi, shot G9-Orbit-S1 và G9-Orbit-S2 có đủ field mới, không còn ref blastPrefab ở S2.

## T4 — Test trong Play (WB-Shooter) và đọc Console
owner: claude
depends: T1, T2, T3
context:
- Vào Play ở scene WB-Shooter. Không lưu MakeAll.unity nếu Linh chưa đồng ý (scene dùng chung).
- Sau khi test, đọc Console, không được còn lỗi. Lỗi NullReference ở GamePlayController.ScaleFactorWithScreen đã biết: sửa bằng DefaultExecutionOrder ở GamePlayControllerTestStub.cs (chỉ Editor).
verified:
- Assets/Project/QL_Tools/TestingTools/GamePlayControllerTestStub.cs (Editor only)
files: MakeAll.unity (chỉ đọc, không lưu nếu chưa có đồng ý)
done_when: Console 0 error trong lúc test; báo kết quả đo cooldown kiểu 1, vòng quay kiểu 2 và va chạm player.

## T5 — Cập nhật library sau khi test
owner: any
depends: T4
context:
- Mục 10 P3 và mục 18.5 dòng S9: bỏ ghi chú "chưa làm trong code" khi đã sửa xong.
- Mục 18.5 dòng S9 đổi mô tả kiểu 2 (bỏ "hết 1 vòng rồi nổ ×2 MBC"), ghi lại thông số tạm mới.
- Bảng mục 15.7 dòng `ElectricOrbitShotWB5` cập nhật mô tả hai mode.
files: /Users/mobione/wkspaces/Brain/BR_WorldBoss/library/worldboss-boss-library.md
done_when: Library không còn ghi "chưa làm trong code" cho súng 9; project doc claude/worldboss-boss-library.md đồng bộ lại.

## Câu hỏi mở
- Cooldown kiểu 1 (chưa có số trong design).
- Ngưỡng va chạm kiểu 2: chỉ chạm player hay cả chạm mép màn.
- Vùng gần nửa dưới màn cho vành khuyên kiểu 2 (tâm và bán kính).
- Số điểm N mỗi vòng và bán kính P/Q: anh cho input.
- Lượt đầu kiểu 2 có đợi 5s sau kiểu 1 (design R527) không.
