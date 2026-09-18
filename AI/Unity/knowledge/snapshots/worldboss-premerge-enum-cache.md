# Snapshot: World Boss — cache 4 enum/config file + vị trí dùng, trước khi pull main

**Mục đích:** cache lại các entry do WorldBoss thêm vào 4 file enum/config **dùng chung toàn project**, để mỗi lần pull/merge `main` xong thì restore lại — **KHÔNG dùng lại index cũ**, mà tịnh tiến tiếp theo index mới sau merge (trừ `REWARD_TYPE` dùng range riêng, xem mục 4).
**Lịch sử:**
- 2026-09-10 (lần 1): cache trước merge — PopupId 203-210, IAP_Product 155-159.
- 2026-09-10 (đã merge + restore lần 1): main giữ `AbyssTimeout=203` / `aa_bosstimeabyss=156` làm entry cuối → WB renumber thành PopupId **204-211**, IAP_Product **157-161**. Đã sửa toàn bộ vị trí serialize liên quan (prefab popupId, PopupsLazy.asset, WorldBossSO.asset, LocalizePrice) + fix luôn 1 bug có sẵn (prefab popupId lệch -2 từ trước, không liên quan merge).
- 2026-09-10 (bổ sung): phát hiện thiếu 1 vị trí nữa — bảng giá `idProductIAP` (CC_IAP) trong scene `Menu.unity` không có 5 dòng `aa_worldboss1..5` → game hiển thị "mất hết gói WorldBoss cũ" (không phải do renumber, mà do bảng giá theo TÊN chưa từng được merge/wire lại). User đã tự thêm lại thủ công trong Editor; đã cache nội dung mục 2 bên dưới để lần merge sau không bị thiếu nữa.
- 2026-09-14 (merge lần 2 + restore lần 2): main lần này thêm `MidAutumnEvent=204`/`MidAutumnIntro=205` (chiếm lại đúng 2 số PopupId cũ của WB) → PopupId renumber tiếp thành **206-213** (max mới của enum là 205). IAP_Product **KHÔNG đổi** (157-161 sống sót qua merge, main không đụng). REWARD_TYPE 60000-60006 bị mất sạch khỏi `Rewards.cs` (đã restore lại đúng số cũ vì range vẫn còn trống) — đây là compile-blocker (WorldBoss.cs gọi thẳng các tên này). Đã sửa: `Popups.cs`, 8 prefab `popupId`, `PopupsLazy.asset` (8 entry), `Rewards.cs`. **CHƯA sửa `Rewards.asset`** (7 dòng metadata icon/tên cho WB_TICKET/WB_CURRENCY_1-6) — xem mục 4 ghi chú riêng, đây là gap có từ TRƯỚC merge lần 2 (phát hiện 2026-09-14 lúc check định kỳ), không tự dựng lại được vì thiếu icon/prefab guid thật, đang chờ user quyết định.

**File này LÀ baseline hiện tại (post-restore lần 2) — dùng làm cache cho LẦN PULL MAIN TIẾP THEO.** Khi pull xong lần sau: đọc lại 4 file + đúng các vị trí serialize liệt kê dưới đây, so sánh với baseline này, thêm lại phần nào bị conflict/mất, tịnh tiến index mới (không paste số cũ).

---

## 1. PopupId — `Assets/DP/Scripts/UI/Popups.cs`
Enum tuần tự, giá trị tường minh. **Baseline hiện tại** (đã restore lần 2, 2026-09-14):
```csharp
    SevenDaysMission = 200,
    UIPVPSeasonBP = 201,
    UIBPBuyExp = 202,

    // campaign extra offers (revive / abyss +30s)
    AbyssTimeout = 203,

    MidAutumnEvent = 204,
    MidAutumnIntro = 205,

    UIWorldBossInformation = 206,
    UIWorldBossRewardInformation = 207,
    UIWorldBossLeaderBoard = 208,
    UIWorldBossShop = 209,
    UIWorldBoss = 210,
    TutorialWorldBoss = 211,
    UIWorldBossFinished = 212,
    UIWorldBossLeaderBoardReward = 213,
}
```
(`MidAutumnEvent`/`MidAutumnIntro` là entry của main, không phải WB — giữ nguyên, chỉ để biết vì sao WB không còn ở 204-205 nữa.)
**Khi pull main lần sau:** nếu 8 dòng `UIWorldBoss*`/`TutorialWorldBoss` biến mất do conflict, thêm lại đúng 8 tên này (giữ thứ tự), renumber liên tục từ giá trị max mới + 1 của enum sau merge (KHÔNG dùng lại 206-213).

### Vị trí đang dùng (đã đồng bộ đúng ở baseline hiện tại — verify lại sau lần merge kế tiếp)
**Code call site (symbolic, tự đúng sau renumber, không cần sửa):** `UIWorldBoss.cs` (136,536,542,548,554,565), `PopupWorldBossHack.cs:104,106`, `ItemWorldBossBooster.cs:32,37`, `TabTestWorldBoss.cs:406,407`, `CC_Interface.cs:669,679,681`, `UISelectMode.cs:305`, `PopupSelectAirport.cs:169`, `UIWorldBossFinished.cs:94`, `ScrollerWorldBossPass.cs:110`, `ItemWorldBossPurchase.cs:176`, `PopupWorldBossPurchaseBP.cs:74`, `UIHome.cs` (793,1287,1350,1419 — `TutorialWorldBoss`), `UIManager.cs:39` (`TutorialPopups` HashSet), `LevelWorldBossManager.cs:129`, `UIWorldBossLeaderBoard.cs:120`.

**⚠️ Vị trí serialize (raw int trong YAML — RỦI RO khi renumber, phải tay sửa lại mỗi lần):**
Cơ chế: `PopupBase.cs:11` — `[SerializeField] protected PopupId popupId;` set thủ công trên từng prefab trong Editor. Field đọc lại lúc `OnHideCompleted()` → `UIManager.RemovePopup(popupId)` — sai giá trị làm remove nhầm popup.

8 prefab tại `Assets/Project/WorldBoss/Use/Resources/*.prefab`, field `popupId:` — **giá trị đúng hiện tại (baseline):**

| PopupId | Giá trị hiện tại | Prefab | Dòng |
|---|---|---|---|
| UIWorldBossInformation | 206 | UIWorldBossInformation.prefab | 55 |
| UIWorldBossRewardInformation | 207 | UIWorldBossRewardInfor.prefab | 221 |
| UIWorldBossLeaderBoard | 208 | UIWorldBossLeaderBoard.prefab | 26926 |
| UIWorldBossShop | 209 | UIWorldBossShop.prefab | 6608 |
| UIWorldBoss | 210 | UIWorldBoss.prefab | 18419 |
| TutorialWorldBoss | 211 | UITutorialWorldBoss.prefab | 799 |
| UIWorldBossFinished | 212 | UIWorldBossFinished.prefab | 1327 |
| UIWorldBossLeaderBoardReward | 213 | UIWorldBossLeaderBoardReward.prefab | 824 |

(Lịch sử: trước 2026-09-10 các dòng này từng lệch -2 so với enum — bug có sẵn không do merge gây ra — đã fix trong đợt restore lần 1. 2026-09-14: renumber lần 2 từ 204-211 → 206-213 do main chiếm 204/205 cho MidAutumnEvent/Intro.)

- **`Assets/Project/Resources/Data/PopupsLazy.asset`** (`List<PopupLazyDataInit>{ PopupId Id; ResourcePath }`, map PopupId → Resources path lazy-load) — **baseline hiện tại đã có đủ 8 entry** (thêm vào cuối list, sau 2 entry MidAutumn của main):
```yaml
  - Id: 206
    ResourcePath: UIWorldBossInformation
  - Id: 207
    ResourcePath: UIWorldBossRewardInfor
  - Id: 208
    ResourcePath: UIWorldBossLeaderBoard
  - Id: 209
    ResourcePath: UIWorldBossShop
  - Id: 210
    ResourcePath: UIWorldBoss
  - Id: 211
    ResourcePath: UITutorialWorldBoss
  - Id: 212
    ResourcePath: UIWorldBossFinished
  - Id: 213
    ResourcePath: UIWorldBossLeaderBoardReward
```
Có nút Editor `[Button]` trong `PopupsLazy.cs` tự quét Resources để add Id/ResourcePath (đọc lại field `popupId` trên prefab) — chỉ chạy SAU KHI prefab đã có `popupId` đúng.

**Việc cần làm ở lần pull main TIẾP THEO:**
1. Renumber 8 entry enum theo max mới + 1.
2. Set lại `popupId` trên 8 prefab theo số mới.
3. Cập nhật/chạy lại nút Editor `PopupsLazy.cs` để 8 entry trong `PopupsLazy.asset` khớp số mới.

## 2. IAP_Product — `Assets/CC_Manager/IAP_Product.cs`
`enum IAP_Product : byte`, giá trị tường minh. **Baseline hiện tại** (đã restore, đúng):
```csharp
    // campaign extra offers (revive / abyss +30s)
    aa_1upcampaign = 155,
    aa_bosstimeabyss = 156,

    //
    aa_worldboss1 = 157,
    aa_worldboss2 = 158,
    aa_worldboss3 = 159,
    aa_worldboss4 = 160,
    aa_worldboss5 = 161,

    //
    None = 255,
}
```
**Khi pull main lần sau:** nếu 5 dòng `aa_worldboss1..5` mất do conflict, thêm lại đúng 5 tên này, renumber liên tục từ max mới + 1 (KHÔNG dùng lại 157-161). ⚠️ `byte` — max 254, kiểm tra không tràn.

### Vị trí đang dùng (đã đồng bộ đúng ở baseline hiện tại — verify lại sau lần merge kế tiếp)
**Code call site (symbolic, an toàn):** `CC_Interface.cs:673-677` — `case IAP_Product.aa_worldboss1..5:`.

**⚠️ Vị trí serialize theo INT (RỦI RO khi renumber):** WorldBoss không dùng hệ Shop chung — bảng riêng trong `Assets/Project/Resources/Data/WorldBossSO.asset` (field `idProduct` kiểu `IAP_Product`, class `PurchaseShopData`). **Giá trị đúng hiện tại (baseline):**

| Product | Int hiện tại | Vị trí | Context |
|---|---|---|---|
| aa_worldboss1 | 157 | `WorldBossSO.asset:7294` | `ShopData.Purchases[ID=1].idProduct` |
| aa_worldboss2 | 158 | `WorldBossSO.asset:7314` | `ShopData.Purchases[ID=2].idProduct` |
| aa_worldboss3 | 159 | `WorldBossSO.asset:7334` | `ShopData.Purchases[ID=3].idProduct` |
| aa_worldboss4 | 160 | `WorldBossSO.asset:46` | `ticketPurchases[ID=1].idProduct` |
| aa_worldboss5 | 161 | `WorldBossSO.asset:66` | `ticketPurchases[ID=2].idProduct` |

2 vị trí hiển thị giá (`LocalizePrice.idProduct`, chỉ localize text — không phải nguồn mua hàng), baseline hiện tại:
- `Assets/Project/WorldBoss/Use/Resources/UIWorldBoss.prefab:20484` — `idProduct: 157` (aa_worldboss1)
- `Assets/Project/WorldBoss/Use/UI/Prefabs/Item-WB-Purchase.prefab:1138` — `idProduct: 157` (aa_worldboss1)
- (lân cận, KHÔNG thuộc range WB, không đổi: `UIWorldBoss.prefab:12413` = 153, `UIWorldBossShop.prefab:1107,3807` = 153/154.)

Không có CSV import nào tái tạo các số này (`CsvToWorldBoss.cs` không đụng `ShopData`/`idProduct`) → `WorldBossSO.asset` là nguồn duy nhất, sửa tay 100%.

**⚠️ Vị trí serialize theo TÊN (an toàn với renumber, nhưng RỦI RO khi Menu.unity bị conflict/merge đè mất nguyên block) — MỚI PHÁT HIỆN 2026-09-10:**
`Assets/Project/Scenes/Menu.unity` — component **CC_IAP** (GameObject trong scene Menu), field `idProductIAP: List<...>` (mỗi entry: `idProd` (tên enum, string) + `idAndroid` + `idIOS` + `type` + `price` + `realPrice`) — đây là bảng giá/mapping store-id dùng khi hiển thị & xử lý mua hàng. Đây LÀ nguyên nhân gây ra hiện tượng "mất hết gói WorldBoss cũ" khi 5 dòng dưới đây bị thiếu (không liên quan gì đến việc renumber enum — chỉ đơn giản là 5 entry này không tồn tại/bị mất trong Menu.unity). **Baseline hiện tại (đã user thêm lại, khớp ~dòng 1900-1928):**
```yaml
  - idProd: aa_worldboss1
    idAndroid: aa_worldboss1
    idIOS: aa_gempack3
    type: 0
    price: 4.99
    realPrice: 5
  - idProd: aa_worldboss2
    idAndroid: aa_worldboss2
    idIOS: aa_gempack3
    type: 0
    price: 4.99
    realPrice: 5
  - idProd: aa_worldboss3
    idAndroid: aa_worldboss3
    idIOS: aa_gempack7
    type: 0
    price: 9.99
    realPrice: 10
  - idProd: aa_worldboss4
    idAndroid: aa_worldboss4
    idIOS: aa_gempack1
    type: 0
    price: 0.99
    realPrice: 1
  - idProd: aa_worldboss5
    idAndroid: aa_worldboss5
    idIOS: aa_gempack7
    type: 0
    price: 9.99
    realPrice: 10
```
Vì key theo TÊN (`idProd`, string) nên **không bị ảnh hưởng bởi việc renumber enum** ở lần merge sau — chỉ cần lo nếu Plastic SCM merge Menu.unity (file scene rất lớn) làm rớt mất nguyên khối 5 entry này (giống việc đã xảy ra lần này, dù không rõ do merge hay do chưa từng được thêm). **Sau lần pull main tiếp theo, luôn grep lại `Assets/Project/Scenes/Menu.unity` cho `aa_worldboss` để xác nhận đủ 5 entry — nếu thiếu, dán lại y nguyên khối trên** (không cần đổi gì vì key theo tên).

**Việc cần làm ở lần pull main TIẾP THEO:**
1. Renumber 5 entry enum theo max mới + 1.
2. Sửa lại 5 dòng `idProduct:` trong `WorldBossSO.asset` + 2 dòng `LocalizePrice.idProduct` theo số mới.
3. Grep `Menu.unity` cho `aa_worldboss` — nếu thiếu block `idProductIAP` ở trên, dán lại nguyên văn (không đổi, vì key theo tên).

## 3. EnumVariables — `Assets/Project/Scripts/EnumVariables.cs`
2 chỗ WB thêm, đều là **entry cuối cùng, không ghi số tường minh** (implicit = index kế trước +1). **Baseline hiện tại (không đổi qua lần merge vừa rồi — file này không hề bị conflict):**
- `enum ITEM_TYPE` — dòng cuối trước `}`: `WB_TICKET` (giá trị hiện tại = 125).
- `enum GAMEPLAY_MODE` — dòng cuối trước `}`: `WORLD_BOSS` (giá trị hiện tại = 9).

**Khi pull main lần sau:** nếu 2 dòng này mất do conflict, append lại vào cuối enum tương ứng (implicit value, không set số tường minh).

### Vị trí đang dùng
**Code call site (symbolic, an toàn):**
- `WB_TICKET`: `PopupWorldBossBuyTicket.cs:265` (duy nhất).
- `WORLD_BOSS`: `UIGamePlay.cs:179`, `PopupSelectAirport.cs:30,157`, `UIWorldBoss.cs:529`, `Fighters.cs:688`, `GamePlayController.cs` (669,1119,1855,1921,2079,2084,2111,2129), `CharacterStats.cs:18`, `Manager.cs:1027`.

**Vị trí serialize:** đã rà toàn bộ field kiểu `ITEM_TYPE`/`GAMEPLAY_MODE` trong project — **KHÔNG có field nào serialize WB_TICKET/WORLD_BOSS ra YAML** (không `[SerializeField]`, hoặc dùng cho hệ thống khác). Rewards của WorldBoss dùng `REWARD_TYPE` (mục 4), không dùng `ITEM_TYPE`. → **2 entry này an toàn 100%**, chỉ cần compile lại là xong, không cần sửa asset/prefab/scene nào.

## 4. Rewards (REWARD_TYPE) — `Assets/Project/Rewards/Scripts/Rewards.cs`
`enum REWARD_TYPE`, dùng convention numeric range riêng theo module. **Baseline hiện tại (không đổi qua lần merge vừa rồi — range vẫn còn trống, giữ nguyên số):**
```csharp
    WB_TICKET = 60000,
    WB_CURRENCY_1 = 60001,
    WB_CURRENCY_2 = 60002,
    WB_CURRENCY_3 = 60003,
    WB_CURRENCY_4 = 60004,
    WB_CURRENCY_5 = 60005,
    WB_CURRENCY_6 = 60006,
}
```
**Khi pull main lần sau:** nếu 7 dòng này mất do conflict, kiểm tra range `60000-60006` còn trống không (grep `= 6000` trong enum) — nếu trống, thêm lại y nguyên số cũ; nếu main đã chiếm range này cho module khác, đổi sang range trống mới (đây là numeric-range convention, không phải index tuần tự nên KHÔNG tịnh tiến nối đuôi).

### Vị trí đang dùng (ĐÂY LÀ ENUM RỦI RO NHẤT nếu range phải đổi)
**Code call site (symbolic, an toàn):** `WorldBoss.cs` — `GetCurrencyRewardType` (765-770) + `TryGetCurrencyBoss` (779,782,785,788,791,794); `PopupWorldBossResultReward.cs:89-90` (nút debug `#if UNITY_EDITOR`).

**⚠️ Vị trí serialize (raw int trong YAML — chỉ RỦI RO nếu phải đổi range, hiện tại 60000-60006 vẫn đúng ở phía enum):**
1. `Assets/Project/Resources/Data/Rewards.asset` (bảng metadata tên/icon/prefab, lookup `Rewards.GetData(type)`) — **⚠️ CẬP NHẬT 2026-09-14: 7 dòng `Type: 60000-60006` này KHÔNG tồn tại trong file thực tế** (đã verify trực tiếp, file dừng ở `Type: 84`, không có bất kỳ entry 6000x nào). Bảng dưới đây là baseline ĐÃ GHI TRƯỚC ĐÓ nhưng có thể chưa từng chính xác hoặc đã mất trước cả merge lần 2 — KHÔNG dùng số dòng này để verify nữa, chỉ giữ lại làm tham khảo lịch sử:

| Member | Dòng (lịch sử, KHÔNG còn đúng) | YAML |
|---|---|---|
| WB_TICKET | ~3736 | `- Type: 60000` |
| WB_CURRENCY_1 | ~3774 | `- Type: 60001` |
| WB_CURRENCY_2 | ~3812 | `- Type: 60002` |
| WB_CURRENCY_3 | ~3850 | `- Type: 60003` |
| WB_CURRENCY_4 | ~3888 | `- Type: 60004` |
| WB_CURRENCY_5 | ~3926 | `- Type: 60005` |
| WB_CURRENCY_6 | ~3964 | `- Type: 60006` |

Mỗi entry (nếu tồn tại) kèm `SprRewardShow` (guid icon riêng) — **KHÔNG tự dựng lại được** vì không có icon/prefab guid thật để điền — cần user tự thêm 7 entry này trong Unity Editor (Inspector list), hoặc cung cấp guid icon đúng để mình điền tay. Cho tới khi thêm: `ItemWorldBossPackage.cs:83` (`Rewards.GetData(...).SprRewardShow` không null-check) sẽ NullReferenceException khi hiển thị package chứa reward WB_CURRENCY/WB_TICKET.

2. `Assets/Project/Resources/Data/WorldBossSO.asset` (`rewards.RewardsByLevel[].Rewards[]`) — **CHỈ `WB_CURRENCY_1 (60001)` xuất hiện, lặp ~100 lần** (mỗi level 1-100, bắt đầu dòng 105, lặp mỗi ~42 dòng tới ~4263). `WB_TICKET`/`WB_CURRENCY_2..6` không xuất hiện ở đây (boss 2-6 chưa có data reward theo level).
3. Đã grep toàn bộ `Assets/**/*.asset` cho `Type: 60000`..`Type: 60006` — không file nào khác chứa.

**Việc cần làm ở lần pull main TIẾP THEO:** CHỈ cần sửa nếu range 60000-60006 bị main chiếm — khi đó update lại 7 dòng trong `Rewards.asset` + toàn bộ ~100 dòng `Type: 60001` trong `WorldBossSO.asset`. Nếu range vẫn trống thì không cần đụng gì ở mục này.

---

## Checklist restore (làm SAU KHI user báo đã pull/merge main xong)
1. Mở lại `Popups.cs`, `IAP_Product.cs`, `EnumVariables.cs`, `Rewards.cs` — xác nhận phần nào trong baseline mục 1-4 đã bị mất do conflict.
2. PopupId + IAP_Product: append lại đúng tên, renumber liên tục từ max mới + 1 (không dùng lại số baseline hiện tại — 204-211 / 157-161 — số này sẽ tiếp tục đổi ở lần sau).
3. EnumVariables: append lại `WB_TICKET`/`WORLD_BOSS` vào cuối nếu mất (implicit, không set số) — an toàn, không có serialize phụ thuộc.
4. REWARD_TYPE: giữ nguyên 60000-60006 nếu range còn trống, đổi range mới nếu bị chiếm.
5. Code call site: tự đúng sau compile, không cần sửa tay (danh sách đầy đủ ở mục 1-4).
6. Sửa tay các vị trí serialize theo INT (bắt buộc nếu PopupId/IAP_Product đổi số):
   - 8 prefab `Assets/Project/WorldBoss/Use/Resources/*.prefab` — field `popupId`.
   - `Assets/Project/Resources/Data/PopupsLazy.asset` — 8 entry `Id`/`ResourcePath`.
   - `Assets/Project/Resources/Data/WorldBossSO.asset` — 5 dòng `idProduct:` (+ ~100 dòng `Type: 60001` chỉ nếu REWARD_TYPE range đổi).
   - `Assets/Project/WorldBoss/Use/Resources/UIWorldBoss.prefab` + `Item-WB-Purchase.prefab` — field `LocalizePrice.idProduct`.
   - `Assets/Project/Resources/Data/Rewards.asset` — 7 dòng `Type:` (chỉ nếu REWARD_TYPE range đổi).
7. Kiểm tra vị trí serialize theo TÊN (không phụ thuộc renumber nhưng có thể bị merge làm rớt nguyên khối): grep `Assets/Project/Scenes/Menu.unity` cho `aa_worldboss` — phải đủ 5 entry trong `idProductIAP` (component CC_IAP), nếu thiếu dán lại nguyên văn ở mục 2.
8. Mở lại 8 prefab WorldBoss trong Editor 1 lần để Unity re-serialize sạch, rồi test: mở từng popup, thử mua từng gói `aa_worldboss1..5` trong Shop (xác nhận hiện giá đúng, không bị "mất gói"), và nhận thử reward `WB_CURRENCY_1` ở vài level.
