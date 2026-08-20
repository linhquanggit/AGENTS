# Snapshot: Kiến trúc EnhancedScroller trong WorldBoss — recipe cho scroller mới

**Mục đích:** gom lại cách EnhancedScroller hoạt động + các case đặc trưng đã có trong code, để khi user đưa request 1 scroller mới, code + hướng dẫn step-by-step ngay mà không phải grep lại từ đầu.
**Snapshot ngày:** 2026-07-31. Nguồn: `Assets/EnhancedScroller v2/Plugins/EnhancedScroller.cs`, `Assets/Project/Scripts/Leaderboard*Base.cs`, và các case đại diện liệt kê dưới.
**Không phải inventory đầy đủ** — 32 controller / 34 cellview dùng EnhancedScroller trong project (survey 2026-07-31), file này chỉ chọn case đặc trưng cho từng nhóm nhu cầu.

---

## 0. Cơ chế lõi (bắt buộc biết trước khi build)

- `EnhancedScroller : MonoBehaviour` (namespace `EnhancedUI.EnhancedScroller`), `[RequireComponent(typeof(ScrollRect))]` — [EnhancedScroller.cs:70-71](Assets/EnhancedScroller%20v2/Plugins/EnhancedScroller.cs#L70-L71).
- **Component stack trên root GameObject** (mirror từ prefab thật `Assets/Project/UI/Scripts/ViewAllPrize/Scroller.prefab:53-161`): `Image` (background, có thể alpha thấp) → `Mask` (`showMaskGraphic`) → `ScrollRect` (`horizontal`/`vertical`, `movementType=Clamped`, `elasticity=0.1`, `inertia=1`, `decelerationRate=0.135`) → `EnhancedScroller` (`scrollDirection`, `spacing`, `padding`, `loop`, `loopWhileDragging`).
- ⚠️ **`EnhancedScroller.Awake()` chỉ tự dựng `Container` (RectTransform + Layout Group + First/Last Padder) lúc RUNTIME** — [EnhancedScroller.cs:1871-1952](Assets/EnhancedScroller%20v2/Plugins/EnhancedScroller.cs#L1871-L1952). Không có `[ExecuteAlways]` → KHÔNG chạy ở Edit Mode. Nó còn `DestroyImmediate` bất kỳ `ScrollRect.content` nào đã gán sẵn (dòng 1881-1884).
  → Vì vậy prefab thật luôn có sẵn 1 child RectTransform (tên `placeholder`) wire vào `ScrollRect.m_Content` — thuần để Editor không lỗi content-null lúc design-time; nó sẽ bị Awake() destroy và thay bằng `Container` thật khi Play.
- Data-binding entrypoint cho content thật: `scroller.GetCellView(prefab)` (dequeue/instantiate theo `cellIdentifier` của prefab) rồi cast về đúng type.

## 1. Contract controller (`IEnhancedScrollerDelegate`)

Bất kể tự viết tay hay dùng base, mọi controller đều cần 3 method + 1 hàm nạp data:
- `GetNumberOfCells(EnhancedScroller) → int` — số lượng item.
- `GetCellViewSize(EnhancedScroller, dataIndex) → float` — kích thước theo trục scroll.
- `GetCellView(EnhancedScroller, dataIndex, cellIndex) → EnhancedScrollerCellView` — `scroller.GetCellView(prefab) as TCell; cellView.SetData(...); return cellView;`.
- `LoadData(...)`: set `scroller.Delegate = this;` rồi `scroller.ReloadData();`.

## 2. Base class generic đã có sẵn — CHỈ DÙNG KHI SCROLLER LÀ LEADERBOARD THẬT

⚠️ **Đã sửa sau feedback trực tiếp (2026-07-31): mặc dù implementation của `LeaderboardScrollerBase<T,TCell>`/`LeaderboardItemBase<T>` generic (không leaderboard-specific), KHÔNG dùng làm base cho scroller thường (reward summary, shop, mission, banner...) — tên "Leaderboard" gây nhầm ngữ nghĩa. Base này CHỈ dùng khi scroller đúng là bảng xếp hạng. Scroller thường → viết plain `MonoBehaviour, IEnhancedScrollerDelegate` (xem case "Reward-Scroller" mục 3).**

- **[`LeaderboardScrollerBase<T, TCell>`](Assets/Project/Scripts/LeaderboardScrollerBase.cs:6)** — controller base cho leaderboard, `where TCell : EnhancedScrollerCellView`. Đã implement sẵn `LoadData`, `GetNumberOfCells`, `GetCellViewSize` (`virtual`, override được). Chỉ `GetCellView` là `abstract`.
- **[`LeaderboardItemBase<T>`](Assets/Project/Scripts/LeaderboardItemBase.cs:7)** — cellview base cho leaderboard, `SetData(T) → UpdateUI()` (abstract), có sẵn `txtRank/txtName/txtScore` + `SetActive(bool)`.
- **[`LeaderboardPopupBase<TEntry>`](Assets/Project/Scripts/LeaderboardPopupBase.cs:10)** — cao hơn 1 tầng: `PopupBase` + fetch server + top-3 + my-rank + hook `SetNormalRank()` abstract (chỗ cắm scroller). Chỉ dùng khi scroller thật sự là 1 bảng xếp hạng nằm trong popup.
- **Ví dụ dùng đúng, tối giản:** [`LeaderBoardWorldBoss.cs`](Assets/Project/WorldBoss/Use/Scripts/UI/LeaderBoardWorldBoss.cs:5) — kế thừa `LeaderboardScrollerBase<Entry, ItemNormalRankWorldBoss>`, code còn lại **chỉ 5 dòng** (`GetCellView`).
- Nhiều controller (`LeaderboardPVP`, `LeaderboardAirDefense`, `LeaderboardBT`) vẫn viết tay lại boilerplate thay vì kế thừa base leaderboard — không sao, đó vẫn là leaderboard nên việc thiếu đồng bộ chỉ là nợ kỹ thuật, không phải sai phạm vi dùng.

## 3. Case đặc trưng theo độ phức tạp

| Case | File | Đặc điểm |
|---|---|---|
| Viết tay tối thiểu (không base) | [`LeaderboardPVP.cs:9`](Assets/Project/PVP/Scripts/LeaderboardPVP.cs#L9) | Bản sao gần như y hệt demo gốc — dùng để hiểu boilerplate trần trụi, KHÔNG dùng làm mẫu cho code mới. |
| Dùng base đúng cách | [`LeaderBoardWorldBoss.cs`](Assets/Project/WorldBoss/Use/Scripts/UI/LeaderBoardWorldBoss.cs) | Mẫu chuẩn nên copy khi tạo controller mới. |
| Nhiều loại cell + gesture + jump | [`ScrollerWorldBossPass.cs`](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs) | 2 prefab (`cellViewPrefab`/`lastCellViewPrefab`), mỗi loại set `.cellIdentifier` riêng ([:102-105](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs#L102-L105)); tap-vs-drag qua `IPointerDown/Up/Drag` đo `totalScrollAmount` ([:178-200](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs#L178-L200)); `myScroller.JumpToDataIndex(index, tweenType, tweenTime, loopJumpDirection)` ([:40-43](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs#L40-L43)); reload giữ vị trí scroll qua `ReloadData(startNormalized)` ([:174](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs#L174)); lặp `myScroller.ActiveCellViews` để bơm hiệu ứng vào cell đang hiển thị ([:107-116](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPass.cs#L107-L116)). |
| Scroller nằm trong Popup | [`LeaderboardPopupBase<TEntry>.Show()`](Assets/Project/Scripts/LeaderboardPopupBase.cs:27) | `LoadData()`/scroller-reload gọi từ `Show()` (override `PopupBase.Show`), KHÔNG phải `Start()/OnEnable()` — khớp convention popup đã snapshot ở [worldboss-ui-premerge.md](worldboss-ui-premerge.md). |
| Scroller thường, KHÔNG phải leaderboard (aggregate-by-type) | [`ScrollerWorldBossPassReward.cs`](Assets/Project/WorldBoss/Use/Scripts/UI/ScrollerWorldBossPassReward.cs) | Plain `MonoBehaviour, IEnhancedScrollerDelegate` (không kế thừa base mục 2 dù data shape tương tự) — gom `WorldBossSO.BattlePassMilestones` theo `REWARD_TYPE` vào `Dictionary` rồi build `List<RewardCount>`; mỗi phần tử → 1 cell `ItemRewardCount`. Mẫu chuẩn cho "scroller đơn giản không phải leaderboard". |

## 4. Idiom binding data vào cellview

- **Chủ đạo:** `SetData(T)` trực tiếp (xem `LeaderboardItemBase<T>` mục 2) — controller tự giữ `List<T>`, cellview không tự lấy data.
- **Khác biệt hiếm gặp:** cellview index-only + static event (`UIDailyMission`/`ItemMission` — cellview tự tra data qua static lookup, expose static event `OnClaim`/`OnGoMission` cho controller subscribe ở `OnEnable/OnDisable`). Chỉ nên biết để nhận diện, KHÔNG phải mẫu mặc định cho scroller mới.
- **Case "icon [+ count]" reward** (đang được hợp nhất): xem [`ItemRewardCount.cs`](Assets/Project/UI/Scripts/ItemRewardCount.cs) — nếu case mới thuộc dạng show reward icon/count thì dùng lại class này (field `showCount` + overload `Init(REWARD_TYPE, count)`) thay vì tạo cellview mới.

## 5. Recipe khi có request scroller mới

1. Xác định loại: **đúng là bảng xếp hạng** (rank/score, thường trong popup) → base ở mục 2 (`LeaderboardScrollerBase<T,TCell>` + `LeaderboardItemBase<T>`, hoặc `LeaderboardPopupBase<TEntry>` nếu cần top-3/my-rank). **Scroller thường** (shop/mission/banner/reward-summary/list bất kỳ) → viết plain `MonoBehaviour, IEnhancedScrollerDelegate`, KHÔNG kế thừa base "Leaderboard*" dù implementation generic — xem case `ScrollerWorldBossPassReward.cs` (mục 3).
2. Dựng GameObject structure theo mục 0 (Image→Mask→ScrollRect→EnhancedScroller + child `placeholder` wire vào `ScrollRect.content`).
3. Cellview: nếu là leaderboard → kế thừa `LeaderboardItemBase<T>`; nếu không → `EnhancedScrollerCellView` thẳng, expose field UI qua `[SerializeField]`, tự viết `SetData`/`Init`.
4. Controller: nếu là leaderboard → kế thừa `LeaderboardScrollerBase<T,TCell>`, chỉ cần viết `GetCellView`. Nếu không → viết đủ 3 method `IEnhancedScrollerDelegate` + hàm `LoadData` (vẫn ngắn, ~20-30 dòng, xem `ScrollerWorldBossPassReward.cs`).
5. Nếu sống trong Popup: hook `LoadData`/`ReloadData()` vào `InitData()`/`Show()`/`OnShowCompleted()` của `PopupBase`, không phải `Start()/OnEnable()` đơn thuần.
6. Nếu cần nhiều loại cell / tap-detect / jump-to-index / hiệu ứng theo cell đang hiện → tham chiếu `ScrollerWorldBossPass.cs` (mục 3).
7. Nếu là reward icon/count đơn giản → tái dùng `ItemRewardCount` (mục 4), đừng tạo cellview mới.
