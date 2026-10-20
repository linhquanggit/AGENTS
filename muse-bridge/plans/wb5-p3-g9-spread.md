# WB5 P3 G9 — giãn cọc lúc bay vào (giống P2) + delay từng nhịp sao
goal: Giữ nguyên quay (S1) và đan sao (S2); chỉ đổi đoạn bay vào: 2 cọc giãn dần tới width chỉnh được rồi mới nối dây. S2 thêm delay giữa mỗi nhịp di chuyển, chỉnh riêng cho từng cọc.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 10 P3 Súng 9); plan liên quan wb5-p2-g6-link-straight

## T1 — Đoạn bay vào: giãn khoảng cách thay cho lerp thẳng
owner: muse
depends: -
context:
Đã chốt: KHÔNG đổi OrbitPositions / FallOrbitPositions / WeavePoint (phần quay + sao giữ nguyên).
Hiện: mỗi cọc lerp thẳng (SmoothStep) từ súng tới vị trí đầu quỹ đạo a0/b0; dây bắt đầu nối sớm ở linkDelay 0.1s (giữa đường bay).
Mới (giống P2): tách chuyển động bay vào thành 2 phần — tâm cặp mid = Lerp(mid(fromA,fromB) → mid(a0,b0)); vector khoảng cách gap = Lerp(fromB−fromA → b0−a0) theo cùng k. A = mid − gap/2, B = mid + gap/2 → 2 cọc giãn dần trong approachTime, tới đúng a0/b0 nên nối liền mạch sang quỹ đạo. Dây bật khi giãn xong (t ≥ approachTime) — thêm [SerializeField] bool linkAfterSpread = true (false thì giữ linkDelay cũ).
Width: KHÔNG thêm field linkWidth — đã có sẵn tham số quyết định khoảng cách, chỉnh ở prefab (T3):
- S1 (PERSISTENT_ORBIT): khoảng cách 2 cọc = bán kính quay orbitRadiusPx (FallOrbitPositions).
- S2 (WEAVE_ONCE): khoảng cách theo kích thước sao = pair.radius (innerRatio giữ nguyên → hình sao giữ tỉ lệ, mọi khoảng cách ×cùng hệ số).
Giãn trong T1 tự đi tới a0/b0 theo các tham số này.
verified:
ElectricOrbitShotWB5.cs (Assets/Project/Enemies/Scripts/BaseEnemy/BaseShot/SpecialCase): :56 approachTime, :57 linkDelay 0.1, :59 linkGrowTime 0.6, :64 orbitRadiusPx 110
:410 TickPair: _p.t < approachTime → _a = Lerp(fromA, a0, SmoothStep) / _b tương tự (dòng 417–422); :430 BeginLinkGrow khi t ≥ linkDelay
:275 OrbitPositions; :339 FallOrbitPositions dùng _r = PxToWorld(orbitRadiusPx), cọc tâm lệch ±r/2 so với GetCenter
:371-372 LaunchPair gán fromA/fromB = launchA/launchB.Position
EnemyWB5_P3_A.prefab (Assets/Project/WorldBoss/Addressable/Enemies/Prefabs): G9-Orbit-S1 mode 0 (dòng ~15523) approachTime 1, linkDelay 0.1, 2 pair; G9-Orbit-S2 mode 1 (dòng ~17096) approachTime 0.5, 1 pair (centerOffset 0.25,-2.2; radius 1.2), revolutionTime 2, weavePoints 6, innerRatio 0.45
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: ElectricOrbitShotWB5.cs
done_when: compile 0 error; Play S1 + S2: log khoảng cách 2 cọc mỗi 0.1s tăng dần trong approachTime, không giật khi chuyển sang quay/sao; dây bật lúc giãn xong; Console 0 error

## T2 — S2: delay sau mỗi nhịp di chuyển, riêng từng cọc
owner: muse
depends: T1
context:
WEAVE_ONCE: 2 cọc đi luân phiên (step chẵn A đi, step lẻ B đi), mỗi step dài stepTime = revolutionTime / weavePoints. Thêm nghỉ sau mỗi step: cọc A đi xong → nghỉ stepDelayA → B đi → nghỉ stepDelayB → ... Mặc định 0.5s mỗi bên, chỉnh riêng để lệch nhịp nếu muốn. Đã chốt: "mỗi bên" = cọc A và cọc B của Orbit S2. Đặt 2 field trong class Pair (public float stepDelayA = 0.5f, stepDelayB = 0.5f) để mỗi cặp/bên có giá trị riêng. Đổi cách tính _done/_tau trong OrbitPositions nhánh WEAVE: dùng chu kỳ 2 step = 2*stepTime + stepDelayA + stepDelayB, trong phần nghỉ thì _tau = 1 (đứng yên ở điểm đích). Dây vẫn nối trong lúc nghỉ. S1 không đổi.
verified:
ElectricOrbitShotWB5.cs:286-307 nhánh WEAVE: _stepTime = revolutionTime/_n; _done = Floor(t/_stepTime); _tau = SmoothStep(frac); _done%2==0 → A lerp, ngược lại B lerp
ElectricOrbitShotWB5.cs:19-46 class Pair (Serializable): launchA/B, laser, centerOffset, radius, clockwise, angularSpeed, safeRatio, startAngle
files: ElectricOrbitShotWB5.cs
done_when: compile 0 error; Play S2: log thời điểm mỗi cọc bắt đầu/kết thúc step — giữa 2 step có nghỉ đúng stepDelayA/B; đặt A=0.5, B=1.0 thấy lệch nhịp; hình sao vẫn đúng các điểm cũ; Console 0 error

## T3 — Đặt thông số trong prefab
owner: claude
depends: T1, T2
context:
Đã chốt (Linh): khoảng cách 2 cọc ×1.5 cho cả S1 và S2.
- G9-Orbit-S1: orbitRadiusPx 110 → 165; linkAfterSpread = true.
- G9-Orbit-S2: pair.radius 1.2 → 1.8 (innerRatio 0.45 giữ nguyên → khoảng cách lúc vào sao 0.66 → 0.99, hình sao giữ tỉ lệ); linkAfterSpread = true; stepDelayA = stepDelayB = 0.5.
Lưu ý S2 tâm centerOffset (0.25, -2.2): sao to hơn → kiểm không tràn mép màn (x ±1.8 quanh tâm ở ¼ màn); nếu tràn báo Linh, không tự dời tâm.
verified:
EnemyWB5_P3_A.prefab chứa 2 ElectricOrbitShotWB5 (GO G9-Orbit-S1 dòng 15718, G9-Orbit-S2 dòng 14952)
files: EnemyWB5_P3_A.prefab
done_when: prefab lưu giá trị; log khoảng cách 2 cọc lúc vào quỹ đạo = 1.5× trước khi sửa (S1 ≈ PxToWorld(165), S2 ≈ 0.99); Play thấy cọc giãn rồi mới nối dây, S2 dừng 0.5s mỗi nhịp; Console 0 error

## Câu hỏi mở
- (không còn)
