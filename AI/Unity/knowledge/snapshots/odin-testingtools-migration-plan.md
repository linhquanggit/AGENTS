# Snapshot: TestingTools — loại bỏ Odin, thay bằng Unity thuần (CHỜ THỰC THI)

**Trạng thái:** plan đã chốt nội dung, **chưa code dòng nào**. User tự làm / nhờ Claude làm theo từng bước, chờ user duyệt từng bước.
**Ngày chụp:** 2026-09-15.
**Lý do:** Odin sẽ bị gỡ hoàn toàn khỏi project trong tương lai (toàn bộ, không chỉ module này). Module `TestingTools` được xử lý trước, độc lập với phần còn lại của project — Odin package vẫn giữ nguyên cho các module khác cho tới khi project gỡ hẳn.
**Phạm vi:** `Assets/QL_Tools/TestingTools/` — 15 file `.cs` (1 root + 14 Tabs), **toàn bộ 15 file đều dùng Odin**.
**Đã xác minh không có call site bên ngoài module** (chỉ có comment nhắc "TabTest" trong `Assets/Project/Events/WorldCup/Scripts/WorldCup.cs`, không phải code phụ thuộc) → an toàn để refactor cô lập.

> Lưu ngoài git repo Events (project dùng Plastic SCM) ⇒ không đụng git/Plastic, không cần hỏi add/private.

---

## 0. Đánh giá bản chất công việc

Đây **không phải** đổi namespace/attribute đơn giản. Odin trong module này là engine vẽ GUI tự động (reflection trên attribute). Unity thuần **không có** control dựng sẵn tương đương cho: Button, BoxGroup/HorizontalGroup/VerticalGroup/FoldoutGroup, TabGroup, ValueDropdown, OnValueChanged, EnableIf/HideIf, GUIColor, TableList, MultiLineProperty, DrawWithUnity.

⇒ Loại Odin = viết lại toàn bộ tầng vẽ GUI bằng IMGUI thủ công cho cả 15 file (~2000 dòng). **Logic nghiệp vụ giữ nguyên 100%** (mọi lệnh gọi `Chip.AddChip`, `MilestoneProgression.HackAddPoint`, `WorldCup.OnPublishResult`...) — chỉ thay tầng khai báo attribute bằng code vẽ tay.

## 1. Migration Map (Odin → Unity thuần)

| Odin | Thay bằng |
|---|---|
| `OdinEditorWindow` | `UnityEditor.EditorWindow` + `OnGUI()` tự viết |
| `[TabGroup]` (12 tab hiện có) | **Sidebar dọc trái** (kiểu Project Settings) — đã chốt với user, không dùng Toolbar ngang hay Dropdown |
| `[BoxGroup("X")]` | `EditorGUILayout.BeginVertical(EditorStyles.helpBox)` + `LabelField("X", EditorStyles.boldLabel)` tiêu đề + `EndVertical()` |
| `[HorizontalGroup]` / `[VerticalGroup]` | `EditorGUILayout.BeginHorizontal()/BeginVertical()` ... `EndHorizontal()/EndVertical()` |
| `[FoldoutGroup("X")]` | 1 `bool` field lưu trạng thái mở/đóng + `EditorGUILayout.Foldout(bool, "X", true)`, bọc nội dung trong `if` |
| `[Button("Label")]` (110 chỗ) | `if (GUILayout.Button("Label")) MethodBody();` |
| `[Button]` không label | `GUILayout.Button("TênMethod")` — dùng thẳng tên method làm text |
| `[Button("X", ButtonSizes.Medium)]` | bỏ `ButtonSizes` — không có tương đương, không cần cho tool nội bộ |
| `[EnableIf(nameof(Cond))]` | `using (new EditorGUI.DisabledScope(!Cond)) { ... }` |
| `[GUIColor(r,g,b)]` (màu cố định) | lưu `var prev = GUI.color; GUI.color = new Color(r,g,b);` vẽ; `GUI.color = prev;` |
| `[GUIColor(nameof(ColorProperty))]` (màu động theo state) | đọc property màu **ngay trước khi vẽ** mỗi lần `OnGUI` chạy (property đã tự tính theo state hiện tại), áp như trên |
| `[ShowInInspector, ReadOnly]` (computed property) | gọi thẳng `EditorGUILayout.LabelField("Label", value)` — không cần property drawer |
| `[LabelText("...")]` | truyền label trực tiếp vào lệnh `EditorGUILayout.*` |
| `[OnValueChanged(nameof(Handler))]` | `EditorGUI.BeginChangeCheck(); newValue = Field(...); if (EditorGUI.EndChangeCheck()) { field = newValue; Handler(); }` |
| `[ValueDropdown]` trên field kiểu `enum` | `(T)EditorGUILayout.EnumPopup(label, value)` — không cần cache list nữa |
| `[ValueDropdown]` custom trả `int` theo id (chip/award/synergy...) | dùng cache có sẵn (`_chipNames/_chipIds`, `_awardNames/_awardIds`...) build `string[]` hiển thị, `EditorGUILayout.Popup(currentIndex, displayNames)`, map ngược qua mảng id |
| `[InfoBox("...", InfoMessageType.Info)]` | `EditorGUILayout.HelpBox("...", MessageType.Info)` |
| `[MultiLineProperty(n)]` (readonly text nhiều dòng) | `EditorGUILayout.TextArea(value, GUILayout.Height(n * 14f))` hoặc `LabelField` với style `wordWrap = true` |
| `[TableList(IsReadOnly = true)]` (bảng `Milestones`) | tự vẽ bảng: 1 hàng header `BeginHorizontal` + `GUILayout.Label` cột cố định độ rộng, loop từng `MilestoneRow` vẽ 1 hàng tương tự |
| `[TableColumnWidth(50)]` | `GUILayout.Width(50)` trên `GUILayout.Label` cột đó |
| `[DrawWithUnity]` trên field `BigDouble` | `EditorGUILayout.DoubleField` bind qua `.ToDouble()` (get) và implicit `double→BigDouble` (set) — `BigDouble` có `implicit operator BigDouble(double)`, an toàn với giá trị nhỏ dùng trong tool này |
| `[PropertyOrder(n)]`, `[HideReferenceObjectPicker]` | bỏ hẳn — thứ tự = thứ tự code viết tay |
| `[HideIf(nameof(Cond))]` | `if (!Cond) { ... }` |
| `GeneralDrawerConfig` (Sirenix, tắt animation trong `OnEnable/OnDestroy` của root window) | xoá hẳn — không còn animation Odin để tắt |

## 2. Quyết định đã chốt với user

- **Điều hướng tab:** Sidebar dọc bên trái (kiểu Unity Project Settings) — không dùng Toolbar ngang hay Dropdown.
- **Kiến trúc tối thiểu bắt buộc phải thêm:** `interface ITestTab { string TabName { get; } void Draw(); }` — mỗi Tab class implement, vì không còn Odin tự dò `[TabGroup]` bằng reflection.

## 3. Kế hoạch tăng dần (increment, không big-bang)

### Bước 0 — Dựng khung `TestingTools.cs` (compatibility shim)
- Đổi kế thừa `OdinEditorWindow` → `EditorWindow`.
- Thêm `interface ITestTab { string TabName { get; } void Draw(); }`.
- Vẽ sidebar trái (`GUILayout.BeginVertical(GUILayout.Width(140))` + nút chọn tab kiểu `EditorStyles.miniButton`/`GUILayout.Toggle`) + panel phải bọc `EditorGUILayout.BeginScrollView`.
- Xoá đoạn `GeneralDrawerConfig` trong `OnEnable`/`OnDestroy`.
- Thay `[ShowInInspector, HideIf(nameof(IsRuntime))] runtimeWarning` + `[InfoBox]` bằng 1 dòng `if (!IsRuntime) EditorGUILayout.HelpBox("The game is not running or Player data is not loaded. Most operations are disabled.", MessageType.Info);` vẽ đầu panel nội dung.
- **Shim tạm thời**: Tab nào *chưa* migrate ở các bước dưới, `Draw()` của nó gọi `Sirenix.OdinInspector.Editor.PropertyTree.Create(this).Draw(false)` — cả cửa sổ chạy được ngay từ đầu, mỗi tab migrate độc lập, không phá tab khác.
- Giữ nguyên toàn bộ static helper (`RefreshChipCache`, `MaxAllChips`, `ResetAbyssFeatureUnlockForTesting`...), `Update()`, `EditorApplication.playModeStateChanged`.

### Bước 1 — Nhóm lá, rủi ro thấp nhất (convert trước để định hình pattern)
1. **`TabTestTournament.cs`** — 2 `HorizontalGroup` ("Points"/"Rank"/"Division"), 3 `Button`+`EnableIf`.
2. **`TabTestRewards.cs`** — `ValueDropdown` trên enum `REWARD_TYPE` → `EnumPopup`; 4 `Button` (2 trần, 2 trong `BoxGroup("Quick Rewards")`).
3. **`TabTestAbyss.cs`** — `BoxGroup("ABYSS")` bọc ngoài + nhiều `HorizontalGroup` con (`L`, `Unlock`, `OP`, `Ticket`, `TicketAdd`); 3 property `ShowInInspector` 2 chiều nối thẳng `LevelAbyss.*` (giữ nguyên get/set); `GUIColor` đỏ cho nút Clear; 2 property readonly (`ReviveTicketCount`, `ExtraTimeTicketCount`).
4. **`TabTestLevels.cs`** — 3 `ShowInInspector,ReadOnly` string (computed) → `LabelField` thẳng; `HorizontalGroup` ("Level Unlock", "Boss Fight"); `BoxGroup("Quick Access")` 3 nút.
5. **`TabTestStats.cs`** — `ValueDropdown` enum `AIRPORT_TYPE` → `EnumPopup`; `OnValueChanged` → `BeginChangeCheck/EndChangeCheck`; `MultiLineProperty(10)` → `TextArea`; 1 `Button` không nằm trong group nào.

### Bước 2 — Nhóm trung bình
6. **`TabTestGeneral.cs`** — 4 field `bool` với `OnValueChanged`+`LabelText` (top-level, không group); `BoxGroup("Game Modifiers")` chứa `damageFactor`/`hpFactor`/`countryFake`+nút Apply (`HorizontalGroup` con "Country") và `onlineRewardIndex`+nút Unlock (`HorizontalGroup` con "Online Reward").
7. **`TabTestGamePlay.cs`** — `BoxGroup("Time Scale")`+`HorizontalGroup("Controls")` (2 nút +/- , 1 readonly `CurrentTimeScale`, 1 nút Reset); `BoxGroup("Scene Management")`+`HorizontalGroup("Abyss")`; **3 nút dùng `GUIColor(nameof(Property))` màu động** (`FighterAtkColor`/`WingmanAtkColor`/`AutoRepairColor`) — xử lý theo mục Migration Map; 2 nút trần cuối file (`DES-F`, `RESET MIGRATE`); có `Update()` public được gọi từ root window — **giữ nguyên chữ ký, không đổi**.
8. **`TabTestAwards.cs`** — `BoxGroup("Award Inventory")`+`HorizontalGroup("Op")`; `BoxGroup("Award Selection Quick Set")` (ValueDropdown custom `awardID`, readonly `AwardInfo`, `HorizontalGroup` "Level"/"Count", nút Equip); `BoxGroup("Synergy Activator")` (2 ValueDropdown custom lồng nhau — `synergyGroupID` rồi `synergyTargetLevel` phụ thuộc group đã chọn, `MultiLineProperty(6)` cho `SynergyTargetInfo`, 2 nút).
9. **`TabTestEnemies.cs`** *(hiện đang bị comment-out trong `TestingTools.cs`, không có UI nào gọi tới — rủi ro = 0)* — vẫn phải bỏ Odin để đúng nghĩa "hoàn toàn": `[DrawWithUnity] BigDouble` ×3 (theo Migration Map), `ValueDropdown` custom `ENEMY_TYPE` (list động theo enemy đang active), `ShowInInspector,ReadOnly` trên `IEnumerable<Enemy>` (khó hiển thị đẹp bằng native — đơn giản hoá thành đếm số lượng + liệt kê tên bằng `TextArea`), `BoxGroup`+`HorizontalGroup` nhiều lớp.

### Bước 3 — Nhóm phức tạp
10. **`TabTestAirDefense.cs`** — nhiều `BoxGroup` độc lập (EXP, ENERGY, CURRENCY, STATS, MISSION, EXP-LEVEL, SKINS-IAP, SHOW, CHEAT) mỗi cái có `HorizontalGroup` con riêng; `ValueDropdown` enum `AirDefense.STATS` + `OnValueChanged`; nhiều `ShowInInspector,ReadOnly`; 1 `[Button]` không label (`GetExp`); 2 nút trần không group (`Level Up`, `Reset`).
11. **`TabTestFighterStar.cs`** — cấu trúc lặp lại 2 lần y hệt (FIGHTER rồi WINGMAN): `BoxGroup` chứa 2 `ValueDropdown` enum (FighterID/StarRank), `HorizontalGroup("Stats")` 2 int, `HorizontalGroup("1")` 3 nút, `HorizontalGroup("2")` 4 nút — copy pattern y hệt cho phần Wingman.
12. **`TabTestMilestone.cs`** — 4 `ShowInInspector,ReadOnly` computed string đầu file; nhiều `BoxGroup` (Total Point, Point Additional, Multiplier, Milestones) với `HorizontalGroup` con + `GUIColor` đỏ cho nút Reset Progress; **`[TableList(IsReadOnly = true)]` trên `List<MilestoneRow>` — phần khó nhất của bước này**, tự vẽ bảng 4 cột (Id có `TableColumnWidth(50)`, PointRequire, Stats, Reward) theo Migration Map.
13. **`TabTestWorldCup.cs`** — 8 `ShowInInspector,ReadOnly` computed string đầu file; nhiều `BoxGroup` (Setup, Prediction, Reward & Offer) với `HorizontalGroup` con + 2 `GUIColor` (đỏ Reset, xanh nhạt Open Popup); **3 `FoldoutGroup` riêng biệt** ("Advanced Time" — 5 field + 3 nút, "Connect Response" — 1 readonly + 2 field + 2 nút, "Server Data (last connect)" — 8 field readonly) — nhiều Foldout nhất trong module, cần 3 biến `bool` trạng thái riêng.

### Bước 4 — Khó nhất, để cuối
14. **`TabTestChips.cs`** (267 dòng, phức tạp nhất module) — `BoxGroup("Chip Inventory")`+`HorizontalGroup("Op")`; `BoxGroup("Chip Selection Quick Set")` (ValueDropdown custom `chipID`, 2 readonly computed, `HorizontalGroup` "Level"/"Count"); `BoxGroup("Equipped Chips Visual")` — **lồng 3 lớp**: `HorizontalGroup("Main")` chia 2 `VerticalGroup` (Col1/Col2), mỗi cột 3 `BoxGroup("Slot N")` chứa `HorizontalGroup("H")` gồm 1 `ValueDropdown` custom theo slot (6 dropdown khác nhau, mỗi cái filter theo `SlotAvaliable`) + 1 nút "X" với `GUIColor` đỏ và `Width = 20`. Đây là phần cần cẩn thận nhất khi map layout lồng nhau sang `Begin/End Horizontal/Vertical` thủ công — sai thứ tự Begin/End sẽ vỡ layout hoặc throw `GUILayout` mismatch exception ở Editor.

### Bước 5 — Dọn dẹp cuối cùng (chỉ làm khi TẤT CẢ 14 tab + root đã chuyển xong và verify)
- Xoá shim `PropertyTree` trong `TestingTools.cs`.
- Xoá `using Sirenix.OdinInspector;` / `using Sirenix.OdinInspector.Editor;` khỏi cả 15 file.
- Chạy `grep -rn "Sirenix\|Odin" Assets/QL_Tools/TestingTools/` — phải trả về **rỗng**.
- Xác nhận project build/compile sạch (Editor không còn lỗi CS đỏ).
- Gói Odin **không bị đụng tới** ở phần còn lại của project.

## 4. Xác minh mỗi bước

Sau mỗi tab: mở `Tools/Testing Tool _/`, vào Play Mode, kiểm tra tab đó — layout giống cấu trúc cũ (box/nhóm ngang đúng chỗ), nút bấm gọi đúng hàm (so log/kết quả với bản Odin cũ), dropdown chọn đúng giá trị/id, `EnableIf` disable đúng lúc chưa chạy game, `GUIColor` đổi màu đúng điều kiện.

## 5. Việc CHƯA quyết định / cần hỏi lại khi thực thi

- Cách trình bày bảng `Milestones` (`TabTestMilestone.cs`) tự vẽ — có cần sort/scroll riêng không, hay bảng ngắn nên không cần.
- `TabTestEnemies.cs` đang disconnect khỏi UI — hỏi lại user lúc tới Bước 2.9: có muốn gắn lại vào sidebar luôn hay giữ nguyên trạng thái ẩn (chỉ đổi code, không thêm entry `ITestTab` vào danh sách hiển thị)?
