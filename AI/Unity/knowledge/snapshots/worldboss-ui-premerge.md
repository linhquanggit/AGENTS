# Snapshot: World Boss UI — pre-merge (PopupBase-derived, load/show/hide)

**Mục đích:** chụp lại cách các UI World Boss **kế thừa `PopupBase`** *load / show / hide* TRƯỚC khi merge branch `main` (refactor UI, đặc biệt Popups). Sau merge dùng file này để convert sang cách work Popup mới mà không mất reference Editor.
**Snapshot ngày:** 2026-07-24. Nguồn: `Assets/Project/WorldBoss/Scripts/UI/` + `Assets/DP/Scripts/UI/PopupBase.cs`.
**Scope người dùng chốt:** chỉ focus UI kế thừa `PopupBase`; độ sâu = cách load/show/hide.

> Lưu ngoài git repo WorldBoss ⇒ merge trong repo không đụng tới. Trong repo có sẵn `Project/WorldBoss/WORLDBOSS_OVERVIEW.md` (sẽ bị refactor đè — không tin cậy sau merge).

---

## 0. Contract PopupBase hiện tại (anchor để convert)
`Assets/DP/Scripts/UI/PopupBase.cs` — `PopupBase : MonoBehaviour`.

- **Đăng ký / lấy popup:** `UIManager.Instance.GetPopup<T>(PopupId.X)` (hoặc non-generic `GetPopup(PopupId.X)`). Mỗi popup gắn `PopupId` qua `SetId(PopupId)` (field `[ReadOnly] popupId`).
- **SHOW:** `Show(object _data=null, float _delay=0, Action _actionOnStart=null, Action _actionOnClose=null, bool _isSetLastSibling=true)`
  Trình tự cố định trong base:
  1. `transform.SetAsLastSibling()` (nếu `_isSetLastSibling`)
  2. `this.data = _data`
  3. `InitData()` → `gameObject.SetActive(true)` → `Init()` → `OnAddListener()`
  4. animation: `MenuAnim.Changing_Show(_delay, onComplete: OnShowCompleted)`; nếu không có `MenuAnimControl` → gọi thẳng `OnShowCompleted()` (invoke `actionOnStart`).
- **Điểm override để load:** `InitData()` (đọc `data`), `Init()` (bắt đầu tải/hiển thị), `OnAddListener()/OnRemoveListener()`, `OnShowCompleted()`.
- **HIDE:** `Hide()` → `MenuAnim.Changing_Hide` → `OnHideCompleted()`:
  - `OnRemoveListener()`, invoke `actionOnClose`,
  - nếu `isDestroyAfterHide` (mặc định **true**) → `UIManager.RemovePopup(popupId)` + `Destroy(gameObject)`; ngược lại chỉ `SetActive(false)`.
  - ⇒ **Popup mặc định bị Destroy khi đóng** → lần mở sau `GetPopup` tạo instance mới. Đây là điểm dễ khác biệt nhất khi convert sang hệ mới (nếu hệ mới cache/pool popup).
- **Đóng:** `_OnCloseClick()` (virtual, wired UnityEvent) → `SoundUI.OnButtonClick()` + `Hide()`.
- `IsShow`: theo `MenuAnim.IsShow` nếu có, else `gameObject.activeSelf`.
- Cờ: `useIgnoreTimeScale`, `isDestroyAfterHide` (đều `[SerializeField]`).

**Khi convert:** giữ nguyên (a) mapping `PopupId → prefab`, (b) chữ ký `Show(object _data)` + kiểu cast `data` của từng popup (bảng dưới), (c) mọi handler `_`-prefix (wired trong prefab/scene — đổi tên/xóa = mất ref Editor thầm lặng), (d) hành vi destroy-after-hide vs giữ sống.

---

## 1. Bảy popup kế thừa PopupBase — bảng load/show/hide

| Popup | PopupId | data cast trong Show | Load (Init/InitData) | Đóng / Hide | Điều hướng ra popup khác |
|---|---|---|---|---|---|
| **UIWorldBoss** (hub) | `UIWorldBoss=204` | `ConnectResponse` (`InitData`) | server chain async | `_OnCloseClick`→`SelectMode` | Information, RewardInformation, LeaderBoard, Shop |
| **UIWorldBossShop** | `UIWorldBossShop=203` | `WORLD_BOSS_PAGE` | `SelectPage` + toggles | base (destroy) | — (pages con là MonoBehaviour) |
| **UIWorldBossLeaderBoard** | `UIWorldBossLeaderBoard=202` | `WorldBossSO.WorldBossPhaseProgressData`→`PhaseData` | `ConnectToServer`→`OnSuccess`→`DeserializeJSON` | base | LeaderBoardReward (`_OnBoxRewardClick`) |
| **UIWorldBossLeaderBoardReward** | `UIWorldBossLeaderBoardReward=207` | `WorldBossPhaseProgressData`→`PhaseData` | `InitData`+`Init` dựng danh sách rank reward | base | — |
| **UIWorldBossInformation** | `UIWorldBossInformation=200` | — | **STUB rỗng** (chỉ base) | base | — |
| **UIWorldBossRewardInformation** | `UIWorldBossRewardInformation=201` | — | `Show()` (che chữ ký base, không param) → `base.Show()` + `scrollView.LoadData()` | base | — |
| **UIWorldBossFinished** | `UIWorldBossFinished=206` | `ResultData`→`resultData` | hiển thị damage/currency/phase | `_OnCloseClick`→load scene `HomeNew` → mở lại `UIWorldBoss` | UIWorldBoss (non-generic) |

`TutorialWorldBoss=205` cũng có trong enum PopupId (dùng cho `UITutorialWorldBoss : UITutorial`, không phải PopupBase).

---

## 2. Chi tiết từng popup

### 2.1 UIWorldBoss (`: PopupBase`) — hub chính
**Mở bởi (điểm vào):**
- `Project/UI/Scripts/SelectMode/UISelectMode.cs:266` → `GetPopup(PopupId.UIWorldBoss).Show()`
- `Project/UI/Scripts/SelectMode/UISelectMode.cs:303` → `GetPopup(PopupId.UIWorldBoss).Show(_data: _response)` (`_response` = `ConnectResponse`)
- `UIWorldBossFinished._OnCloseClick` (mở lại sau trận)
- `ScrollerWorldBossPass.OnClickClaim` → `GetPopup<UIWorldBoss>(PopupId.UIWorldBoss)?.RefreshBoosters()`

**Load flow:**
- `InitData()`: nếu `data is ConnectResponse` → lưu `response`, set `currentTime/startTime/endTime`, sync `WorldBoss.SetEventStartTime/SetServerTimeSync/SetCurrentBoss`, `ApplyServerProgress`.
- `Init()`: nếu debug time → `ShowUI()`; else `LoadFromServer()`.
- **Chuỗi server async:** `LoadFromServer` → `UIManager.GetPopup(PopupId.LoadingIAP).Show()` → `DHS.GetAllEvent` → `OnGetAllEventCallback` (tìm season ENDED chưa claim → hiện child `popupPreviousWorldBossResult`, gọi `DHS.ClaimEvent`) → `ConnectNewSeason` → `DHS.ConnectEvent` → `OnConnectNewSeasonCallback` → `UpdateRuntimeData()` + `ShowUI()`.
- `ShowUI()` = ShowCurrentReward + ShowProgressBar + ShowBossLevel + ShowProgressRewards + ShowPlay + ShowPhaseCurrent + ShowTicket + ShowBooster.

**Hide/đóng:** `_OnCloseClick()` override → `base._OnCloseClick()` (Hide, destroy) rồi `GetPopup(PopupId.SelectMode).Show()`.

**Điều hướng ra (đều `IsShow`-guard + `SoundUI.OnButtonClick`):**
- `_OnMainInfoClick` → `GetPopup<UIWorldBossInformation>(PopupId.UIWorldBossInformation).Show()`
- `_OnRewardInfoClick` → `GetPopup<UIWorldBossRewardInformation>(PopupId.UIWorldBossRewardInformation).Show()`
- `_OnLeaderBoardClick` → `GetPopup<UIWorldBossLeaderBoard>(PopupId.UIWorldBossLeaderBoard).Show(_data: PhaseData)`
- `_OnShopClick` → `GetPopup<UIWorldBossShop>(PopupId.UIWorldBossShop).Show()`
- `_OnBattlePassClick` → `...Show(UIWorldBossShop.WORLD_BOSS_PAGE.BATTLE_PASS)`
- `_OnPlayClick` → nếu không đủ vé mở child `popupBuyTicket.Show(...)`; else `Manager.Instance.LoadWorldBoss("WorldBoss", …)` rồi `this.Hide()`.
- `_OnBuyTicketClick` → child `popupBuyTicket.Show(...)`.

**Child MonoBehaviour (KHÔNG qua UIManager, wired serialize trực tiếp):** `popupBuyTicket : PopupWorldBossBuyTicket`, `popupPreviousWorldBossResult : PopupPreviousWorldBossResult`.
**Handlers `_` cần giữ:** `_OnCloseClick, _OnPlayClick, _OnMainInfoClick, _OnRewardInfoClick, _OnLeaderBoardClick, _OnShopClick, _OnBattlePassClick, _OnBuyTicketClick` (+ `OnPurchaseSuccess`).

### 2.2 UIWorldBossShop (`: PopupBase`)
- **Nested enum** `WORLD_BOSS_PAGE { EXCHANGE=0, PURCHASE=1, BATTLE_PASS=2 }`.
- `Show(object _data)`: nếu `_data is WORLD_BOSS_PAGE` → chọn trang đó; `OnAddListener/OnRemoveListener` override.
- Load: `[Button] Load()` gom `pages`/`toggles`; `SelectPage(WORLD_BOSS_PAGE)`, `SelectPurchaseFromBattlePass()`, `OnPurchaseSuccess()`.
- **Trang con** = `List<PopupWorldBossPage> pages` (MonoBehaviour, `Show(bool)/OnRefresh()`), điều khiển bằng `SetActive`, KHÔNG qua UIManager/PopupId.
- Đóng: `_OnCloseClick` override.
- Mở bởi: `UIWorldBoss._OnShopClick`/`_OnBattlePassClick`, `PopupWorldBossPageBattlePass`.

### 2.3 UIWorldBossLeaderBoard (`: LeaderboardPopupBase<LeaderBoardWorldBoss.LeaderboardEntryWorldBoss>` → PopupBase)
- `Show(object _data)`: `_data as WorldBossSO.WorldBossPhaseProgressData` → `PhaseData`.
- Load: override `ConnectToServer()` → `OnSuccess(string)` → `DeserializeJSON(string)` (parse qua nested `[Serializable] LeaderboardView { List<...> leaderboard }`), rồi `SetTopRank()/SetNormalRank()`.
- `_OnBoxRewardClick` → `GetPopup<UIWorldBossLeaderBoardReward>(PopupId.UIWorldBossLeaderBoardReward).Show(_data: PhaseData)`.
- ⚠️ Liên quan learnings: `leaderboard-popup-score-sort-trap`, `jsonutility-parse-view-pattern`, `worldboss-damage-two-vars`.

### 2.4 UIWorldBossLeaderBoardReward (`: PopupBase`)
- `Show(object _data)`: `_data as WorldBossPhaseProgressData` → `PhaseData`; override `InitData()`+`Init()` dựng `itemsTop[]` + spawn `itemPrefab (ItemWorldBossRankingReward)` từ `dataShow`.
- Mở bởi: 2.3.

### 2.5 UIWorldBossInformation (`: PopupBase`)
- **Body rỗng** — chỉ dùng `base.Show()` mở prefab tĩnh. Mở bởi `UIWorldBoss._OnMainInfoClick`.

### 2.6 UIWorldBossRewardInformation (`: PopupBase`)
- `public void Show()` — **che chữ ký base** (không param): gọi `base.Show()` rồi `scrollView.LoadData()` (`scrollView : WorldBossRewardInfoScrollView`).
- Mở bởi `UIWorldBoss._OnRewardInfoClick`. ⚠️ Vì che chữ ký, gọi qua `GetPopup<UIWorldBossRewardInformation>(...).Show()` (không truyền data).

### 2.7 UIWorldBossFinished (`: PopupBase`)
- Nested `[Serializable] ResultData { BigDouble TotalDamage; int Currency; WORLD_BOSS_ID WorldBossID; int Phase; }`.
- `Show(object _data)`: `_data as ResultData` → `resultData` → set `txtDmg/txtCurrency/imgCurrencyBossCurrent/textPhaseCurrent/imgPhaseCurrent`.
- Mở bởi: `Project/WorldBoss/Scripts/LevelWorldBossManager.cs:92` `OnMatchCompleted` → `GetPopup<UIWorldBossFinished>(PopupId.UIWorldBossFinished).Show(_data: _result)`.
- `_OnCloseClick` override → load scene `HomeNew` → `GetPopup(PopupId.UIWorldBoss).Show()` (non-generic).

---

## 3. Companion KHÔNG kế thừa PopupBase (ngoài scope focus — chỉ để migration đừng nhầm)
Các lớp này dùng lifecycle riêng (`gameObject.SetActive` / chữ ký `Show(...)` typed), **không có PopupId, không `object _data`** — convert theo cách khác popup:
- `PopupWorldBossGamePlay : MonoBehaviour` — HUD gameplay, `Show(double _playTime, ...)`, `SetActive/Hide`.
- `PopupWorldBossBuyTicket : MonoBehaviour` — child của UIWorldBoss, `Show(Action _onBuySuccess, int, int)`, tự `SetActive(false)` khi đóng; handler add/remove listener trong `OnEnable/OnDisable`.
- `PopupPreviousWorldBossResult : MonoBehaviour` — child của UIWorldBoss, `Show(string,DateTime,DateTime,int)`.
- `PopupWorldBossPage : MonoBehaviour` + subclass (`PopupWorldBossPageBattlePass/Exchange/Purchase`, `PopupWorldBossShopExchange`) — trang con trong Shop, `Init/Show(bool)/OnRefresh`.
- Cell views (`EnhancedScrollerCellView`), scrollers, item views — xem inventory đầy đủ ở phần 4.

## 4. Inventory serialized fields (wiring cần bảo toàn)
> Ghi đầy đủ để khi convert giữ đúng field wiring. Trích từ 3 lượt quét ngày 2026-07-24. (Danh sách item/cell views & pages con — giữ ở đây làm tham chiếu, không phải PopupBase.)

- **UIWorldBoss**: scroll(WorldBossScrollViewReward), barScore(BarScore), objFxScoreUp, textPlayFee, textTicket, textCurrentRewardLevel, imgCurrentRewardIcon, itemProgressRewards[], textBossLevel(LocalizationParamsManager), textPhaseCurrent, imgIconPhaseCurrent, imgBannerBossCurrent, popupBuyTicket, popupPreviousWorldBossResult, objLockPlay/objLockReward/objLockBanner, btnPlay, boosters[].
- **UIWorldBossShop**: pages(List<PopupWorldBossPage>), toggles(ToggleWorldBoss[]).
- **UIWorldBossLeaderBoard**: leaderBoard(LeaderBoardWorldBoss) (+ inherited itemInternet/itemsTop/allEntryList).
- **UIWorldBossLeaderBoardReward**: itemsTop(ItemWorldBossRankingReward[]), rtfItem, itemPrefab(ItemWorldBossRankingReward).
- **UIWorldBossRewardInformation**: scrollView(WorldBossRewardInfoScrollView).
- **UIWorldBossFinished**: txtDmg, txtCurrency, imgCurrencyBossCurrent, textPhaseCurrent, imgPhaseCurrent.

Chi tiết field của pages con + item/cell views (17 file) đã quét đầy đủ — nếu cần khi convert, quét lại `Scripts/UI/Item*.cs`, `Popup*Page*.cs`, `*Scroll*.cs`, `ToggleWorldBoss.cs` theo cùng schema (class/base, SerializeField, Show/Init, handler `_`).

## 5. Checklist convert sau merge
1. So sánh `PopupBase` mới vs cũ (mục 0): chữ ký `Show`, thứ tự `InitData→Init→OnAddListener`, cơ chế Hide/destroy (`isDestroyAfterHide`), cách gắn `PopupId`.
2. Map lại 7 popup: giữ `PopupId` (200–207), giữ kiểu cast `data`, chuyển override `Init/InitData/OnAddListener/OnShowCompleted` sang hook tương ứng của hệ mới.
3. Giữ nguyên mọi handler `_`-prefix (wired trong prefab) — không rename/xóa (xem Rules: UnityEvent handlers).
4. Kiểm điểm vào ngoài WB: `UISelectMode.cs:266/303`, `LevelWorldBossManager.cs:92`, `CC_Interface.cs:618/628/630` (`IsHasPopup`), `TabTestWorldBoss.cs:261` (`DebugRefreshRuntimeData`).
5. Companion MonoBehaviour (mục 3) convert riêng nếu hệ mới muốn gom vào Popup.