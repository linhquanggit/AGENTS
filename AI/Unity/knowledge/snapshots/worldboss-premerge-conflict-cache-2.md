# Snapshot: World Boss — cache code trong 7 file conflict trước merge (lần 2)

**Mục đích:** Plastic SCM báo 7 file dưới đây có "changes in both source and destination" (conflict cần merge tay) khi pull/merge `main`. Đây KHÔNG phải file enum (đã có cache riêng ở `worldboss-premerge-enum-cache.md` — đừng đụng lại file đó). 7 file này là các file gameplay/UI **dùng chung toàn project**, trong đó có xen code WorldBoss (gọi method, if/switch theo mode, wiring UI, tutorial trigger...). Mục đích cache: sau khi user resolve conflict xong, so lại với baseline trong file này để xác nhận không mất code WorldBoss nào; nếu mất thì dán lại nguyên văn (đây không phải enum tuần tự nên không cần renumber/tịnh tiến gì cả — cứ dán lại y nguyên).

**Ngày cache:** 2026-09-14, trước khi merge (chưa merge).

**Cập nhật 2026-09-14 (sau khi user báo merge xong) — kết quả đối chiếu + restore:**
- File 1 (CC_Interface.cs), 3 (PopupNoelGamePlay.cs — vẫn không có gì, đúng dự đoán), 4 (GamePlayController.cs), 5 (UIHome.cs), 7 (UIGamePlay.cs) — **nguyên vẹn 100%**, merge không đụng vào, không cần restore.
- File 2 (Firebase_Analytics.cs) — **`isWorldBossUnlock` mất sạch cả 4 vị trí** (field/default/fetch/debug-log) — đã restore lại đúng nguyên văn baseline (đặt cạnh `isTournamentUnlock`). Đây là compile-blocker vì `UISelectMode.cs:45,271` gọi field này.
- File 6 (UIGamePlay.prefab) — **XÁC NHẬN MẤT THẬT**: field `popupWB` trên component `UIGamePlay` biến mất, nested prefab instance `Popup-WB-GamePlay` cũng biến mất hoàn toàn khỏi hierarchy (các popup lân cận Noel/AirDefense/BossFight/Endless vẫn còn nguyên, chỉ riêng WB bị rớt). **CHƯA restore được** — cần thao tác tay trong Unity Editor (kéo lại `Assets/Project/WorldBoss/Use/UI/Prefabs/Popup-WB-GamePlay.prefab` vào đúng vị trí cha, gán lại field `popupWB`), không sửa an toàn qua YAML text vì rủi ro sai fileID/guid. `UIGamePlay.cs` (file 7) vẫn nguyên vẹn về code nhưng sẽ NPE khi gọi các method forward tới `popupWB` cho tới khi fix xong file 6.

---

## 1. Assets/CC_Manager/CC_Interface.cs
**Tìm thấy 1 block WorldBoss** (nằm trong switch `case IAP_Product...` xử lý sau khi mua IAP thành công), có 1 dòng phụ (669-670) là AirDefense-case nhưng lại gọi vào UIWorldBoss nên cache luôn cho đủ ngữ cảnh:

```csharp
// dòng 661-686 (case IAP_Product.aa_airdefense* ... case IAP_Product.aa_worldboss1..5)
                _onCompleted = delegate ()
                {
                    SystemVariables.IsRemoveAds = true;
                    if (UIManager.Instance.IsHasPopup(PopupId.AirDefenseShop, out UIAirDefenseShop _uiAirDefenseShop))
                        _uiAirDefenseShop.OnPurchaseSuccess();
                    else if (UIManager.Instance.IsHasPopup(PopupId.AirDefense, out UIAirDefense _uiAirDefense))
                        _uiAirDefense.OnPurchaseSuccess();
                    //
                    if (UIManager.Instance.IsHasPopup(PopupId.UIWorldBoss, out UIWorldBoss _uiWorldBoss))
                        _uiWorldBoss.OnPurchaseSuccess();
                };
                break;
            case IAP_Product.aa_worldboss1:
            case IAP_Product.aa_worldboss2:
            case IAP_Product.aa_worldboss3:
            case IAP_Product.aa_worldboss4:
            case IAP_Product.aa_worldboss5:
                SystemVariables.IsRemoveAds = true;
                if (UIManager.Instance.IsHasPopup(PopupId.UIWorldBossShop, out UIWorldBossShop _uiWorldBossShop))
                    _uiWorldBossShop.OnPurchaseSuccess();
                if (UIManager.Instance.IsHasPopup(PopupId.UIWorldBoss, out UIWorldBoss _uiWorldBoss))
                    _uiWorldBoss.OnPurchaseSuccess();
                _onCompleted = delegate ()
                    {
                    };
                break;
```
**Mô tả:** dòng 669-670 — trong case AirDefense IAP, có bắn thêm callback `OnPurchaseSuccess()` cho popup `UIWorldBoss` (nếu đang mở) — có thể là code dùng chung/nhầm lẫn giữa 2 event, giữ nguyên khi merge. Dòng 673-685 — case riêng cho 5 gói IAP `aa_worldboss1..5`: set `IsRemoveAds`, báo `OnPurchaseSuccess()` cho cả `UIWorldBossShop` và `UIWorldBoss` nếu đang mở.

---

## 2. Assets/CC_Manager/Firebase_Analytics.cs
**Tìm thấy 4 dòng WorldBoss** (1 field + 3 chỗ dùng field đó — remote-config flag bật/tắt unlock WorldBoss):

```csharp
// dòng 79 (khai báo field, cạnh các flag unlock chế độ chơi khác)
    public bool isWorldBossUnlock;
```
```csharp
// dòng 195 (default value khi remote config chưa fetch được)
        _defaults.Add(nameof(isWorldBossUnlock), true);
```
```csharp
// dòng 334 (đọc giá trị thật từ Firebase Remote Config sau khi fetch xong)
        isWorldBossUnlock = (bool)FirebaseRemoteConfig.DefaultInstance.GetValue(nameof(isWorldBossUnlock)).BooleanValue;
```
```csharp
// dòng 391 (debug string log toàn bộ config hiện tại)
      $"--world boss unlock[{isWorldBossUnlock}]\n" +
```
**Mô tả:** `isWorldBossUnlock` là remote-config flag (Firebase Remote Config) dùng để bật/tắt tính năng WorldBoss từ xa, cùng pattern với `isEndlessUnlock`/`isTournamentUnlock`. Field này rất có thể được đọc ở nơi khác (vd. `WorldBoss.cs`/`WorldBossManager`) để quyết định `WorldBoss.IsUnlock` — không nằm trong 7 file này nên không cache thêm, chỉ cần đảm bảo 4 dòng trên còn nguyên sau merge.

---

## 3. Assets/Project/Events/Holiday/Noel/Scripts/UI/PopupNoelGamePlay.cs
**Không tìm thấy code liên quan WorldBoss trong file này.** Đã đọc toàn bộ file (146 dòng) và grep tất cả biến thể ("WorldBoss", "WB_", "world_boss", "aa_worldboss", v.v.) — không có kết quả nào. File này thuần về UI gameplay của sự kiện Noel (đếm giờ, thanh điểm, reward text, `ITEM_TYPE.HOLIDAY_NOEL_POINT`), không có any if/switch/method nào gọi vào class/enum liên quan WorldBoss. Có thể file này nằm trong danh sách conflict vì lý do khác hoàn toàn (không phải WB) — ví dụ cả 2 nhánh (source/destination) cùng sửa logic Noel song song, hoặc file bị đụng do refactor chung không liên quan WB. **Không cần restore gì cho file này ở mục WorldBoss** — nếu sau merge thấy nghi ngờ, thử tìm thêm theo tên class/method WorldBoss hay gọi tới UI gameplay khác (không tìm thấy trong lần rà soát này).

---

## 4. Assets/Project/GamePlay/Scripts/GamePlayController.cs
**Tìm thấy nhiều block WorldBoss**, rải khắp file (đây là file có nhiều WB code nhất trong 7 file):

### 4a. Gọi WorldBossShowIntro khi bắt đầu trận (dòng ~600-614)
```csharp
        System.Action _action = null;
        _action = OnStartGame(_action);
        _action = PVPShowIntro(_action);
        _action = BossFightShowIntro(_action);
        _action = NoelShowIntro(_action);
        _action = AirDefenseShowIntro(_action);
        _action = WorldBossShowIntro(_action);
        if (_action != null)
            _action.Invoke();
```

### 4b. Switch khi thắng trận (dòng ~660-673)
```csharp
        {
            case GAMEPLAY_MODE.BOSS_FIGHT: OnBossFightWin(); break;
            case GAMEPLAY_MODE.CAMPAIGN:
                if (IsAbyssCampaign) OnAbyssWin();
                else OnCampaignWin();
                break;
            case GAMEPLAY_MODE.ENDLESS: OnEndlessWin(); break;
            case GAMEPLAY_MODE.LEVEL_BONUS: OnLevelBonusWin(); break;
            case GAMEPLAY_MODE.PVP: OnWinPVP(); break;
            case GAMEPLAY_MODE.NOEL: OnWinHolidayNoel(); break;
            case GAMEPLAY_MODE.AIR_DEFENSE: OnAirDefenseCompleted(); break;
            case GAMEPLAY_MODE.WORLD_BOSS: OnCompleteWorldBoss(); break;
            default: break;
        }
        PiggyBank.OnCalculateMatch();
```

### 4c. Switch khi thua trận (dòng ~1108-1123)
```csharp
                else OnCampaignLose();
                break;
            case GAMEPLAY_MODE.BOSS_FIGHT: OnBossFightLose(); break;
            case GAMEPLAY_MODE.ENDLESS: OnEndlessLose(); break;
            case GAMEPLAY_MODE.LEVEL_BONUS: OnLevelBonusLose(); break;
            case GAMEPLAY_MODE.TOURNAMENT: OnTournamentLose(); break;
            case GAMEPLAY_MODE.PVP: OnLosePVP(); break;
            case GAMEPLAY_MODE.NOEL: OnLoseHolidayNoel(); break;
            case GAMEPLAY_MODE.WORLD_BOSS: OnCompleteWorldBoss(); break;
        }
        PiggyBank.OnCalculateMatch();
```
(chú ý: thắng và thua đều gọi chung `OnCompleteWorldBoss()` — WorldBoss coi thắng/thua như nhau, luôn chạy hết animation kết thúc trận.)

### 4d. Toàn bộ region `[WORLD BOSS]` (dòng 1464-1507)
```csharp
    #region [WORLD BOSS]
    System.Action WorldBossShowIntro(System.Action _nextAction)
    {
        return delegate
        {
            if (LevelManagers.Instance is not LevelWorldBossManager)
            {
                if (_nextAction != null)
                    _nextAction.Invoke();
                return;
            }
            var _item = Instantiate(WorldBossSO.IntroPrefab, Vector3.zero, Quaternion.identity);
            _item.Show(() =>
            {
                uiGamePlay.ShowWorldBossBonusTime();
                if (_nextAction != null) _nextAction.Invoke();
            });
        };
    }
    private void OnCompleteWorldBoss()
    {
        StartCoroutine(IE_CompleteWorldBoss());
    }
    IEnumerator IE_CompleteWorldBoss()
    {
        var _delay = LevelManagers.Instance != null ? LevelManagers.Instance.DelayWin : 0f;
        yield return new WaitForSeconds(_delay);
        uiGamePlay.SetMatchCompletedWorldBoss(true);
        yield return new WaitForSeconds(2f);
        SoundManager.Instance.MusicPause();
        uiGamePlay.PlaySoundMatchComplete();
        LevelManagers.Instance.ChangeMapSpeed(20f);
        yield return new WaitForSeconds(2f);
        uiGamePlay.SetMatchCompletedWorldBoss(false);
        objFly?.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        EventDispatcher.Instance.Dispatch(EventName.OnLevelClear, null);
        objFly?.SetActive(false);
        MyPoolManager.ReleaseAll();
        AddressableManager.instance.ReleaseAllHandle();
        yield return new WaitForSeconds(0.2f);
        ((LevelWorldBossManager)LevelManagers.Instance).OnMatchCompleted();
    }
    #endregion
```
**Mô tả:** `WorldBossShowIntro` — chạy màn hình intro riêng cho WorldBoss khi `LevelManagers.Instance` là `LevelWorldBossManager` (spawn `WorldBossSO.IntroPrefab`, gọi `uiGamePlay.ShowWorldBossBonusTime()`). `IE_CompleteWorldBoss` — coroutine kết thúc trận WorldBoss riêng (khác coroutine kết thúc trận thường), gọi `((LevelWorldBossManager)LevelManagers.Instance).OnMatchCompleted()` ở cuối.

### 4e. `IsOneAirportOnly` — cho hồi sinh 1 lần + chỉ 1 sân bay (dòng ~1855-1930)
```csharp
        // TH chỉ có 1 sân bay duy nhất
        if (IsOneAirportOnly)
        {
            // chế độ tournament / world boss được hồi sinh 1 lần
            if (Mode == GAMEPLAY_MODE.TOURNAMENT || Mode == GAMEPLAY_MODE.WORLD_BOSS)
            {
```
```csharp
    bool IsOneAirportOnly
    {
        get
        {
            switch (Mode)
            {
                case GAMEPLAY_MODE.PVP:
                case GAMEPLAY_MODE.TOURNAMENT:
                case GAMEPLAY_MODE.WORLD_BOSS:
                    return true;
                default:
                    return false;
```
**Mô tả:** WorldBoss dùng chung logic "chỉ 1 sân bay + được revive 1 lần" với Tournament.

### 4f. Chọn Fighter/Wingman theo mode (dòng ~2080-2136)
```csharp
        var _idFighter = airportCurrent.FighterID;
        if (mode == GAMEPLAY_MODE.TOURNAMENT)
        { _idFighter = Tournament.FighterType; }
        else if (mode == GAMEPLAY_MODE.WORLD_BOSS)
        { _idFighter = WorldBoss.FighterType; }

        //DPDebug.Log("Create fighter selected: " + _idFighter.ToString());

        var _bypassFighterCache = mode == GAMEPLAY_MODE.TOURNAMENT || mode == GAMEPLAY_MODE.WORLD_BOSS;
```
```csharp
    void CreateWingman()
    {
        if (airportCurrent == null || airportCurrent.WingmanID == WINGMAN_TYPE.NONE
                                   || (mode == GAMEPLAY_MODE.TOURNAMENT
                                       && (Tournament.TournamentMode == TOURNAMENT_MODE.WITHOUT_WINGMAN
                                           || Tournament.WINGMAN_TYPE == WINGMAN_TYPE.NONE))
                                   || (mode == GAMEPLAY_MODE.WORLD_BOSS && WorldBoss.WingmanType == WINGMAN_TYPE.NONE))

        {
            ...
        }
        ...
        var _idWingman = airportCurrent.WingmanID;
        if (mode == GAMEPLAY_MODE.TOURNAMENT)
            _idWingman = Tournament.WINGMAN_TYPE;
        else if (mode == GAMEPLAY_MODE.WORLD_BOSS)
            _idWingman = WorldBoss.WingmanType;
```
**Mô tả:** khi mode là `WORLD_BOSS`, lấy `_idFighter`/`_idWingman` từ static class `WorldBoss` (`WorldBoss.FighterType`, `WorldBoss.WingmanType`) thay vì từ `airportCurrent`, và bypass fighter cache — cùng pattern với Tournament.

---

## 5. Assets/Project/Home/Scripts/UIHome.cs
**Tìm thấy 5 block WorldBoss** (tutorial flow ở màn Home):

### 5a. Đăng ký vào chuỗi action hiển thị tutorial tuần tự (dòng ~588)
```csharp
        _action = ShowBossFightTutorial(_action);
        _action = ShowEndlessTutorial(_action);
        _action = ShowWorldBossTutorial(_action);
        _action = ShowTutorialMilitaryRank(_action);
```

### 5b. 3 chỗ check "đang show tut WorldBoss thì return" (dòng 793, 1287, 1350 — cùng 1 pattern lặp lại trong 3 hàm khác nhau)
```csharp
        if (!_isTutorial)
            _isTutorial =
                UIManager.Instance.IsHasPopup(PopupId.TutorialWorldBoss, out _); // dang show tut world boss -> return
```

### 5c. Toàn bộ hàm `ShowWorldBossTutorial` (dòng 1389-1438)
```csharp
    System.Action ShowWorldBossTutorial(System.Action _nextAction)
    {
        return delegate
        {
            var _isTutorial =
                UIManager.Instance.IsHasPopup(PopupId.TutorialUpgrade, out _); // dang show tut upgrade -> return
            if (!_isTutorial)
                _isTutorial =
                    UIManager.Instance.IsHasPopup(PopupId.TutorialUseWingman, out _); // dang show tut wingman -> return
            if (!_isTutorial)
                _isTutorial =
                    UIManager.Instance.IsHasPopup(PopupId.TutorialBossFight, out _); // dang show tut boss fight -> return
            if (!_isTutorial)
                _isTutorial =
                    UIManager.Instance.IsHasPopup(PopupId.TutorialEndless, out _); // dang show tut endless -> return
            if (_isTutorial)
            {
                if (_nextAction != null)
                    _nextAction.Invoke();
                return;
            }

            DPDebug.Log("Check show world boss tut--1");
            if (WorldBoss.IsUnlock)
            {
                DPDebug.Log("Check show world boss tut--2");
                var _isTutWorldBossPassed = Tutorials.GetTutorialPassed(TUTORIAL.UNLOCK_WORLD_BOSS);
                if (!_isTutWorldBossPassed)
                {
                    DPDebug.Log("Check show world boss tut--3");
                    UIManager.Instance.GetPopup(PopupId.TutorialWorldBoss).Show(_actionOnClose: () =>
                    {
                        if (_nextAction != null)
                            _nextAction.Invoke();
                    });
                }
                else
                {
                    if (_nextAction != null)
                        _nextAction.Invoke();
                    return;
                }
            }
            else
            {
                DPDebug.Log("Check show world boss tut--4");
                if (_nextAction != null)
                    _nextAction.Invoke();
            }
        };
    }
```
**Mô tả:** hàm này nằm trong chuỗi hiển thị tutorial tuần tự ở Home — nếu tutorial khác (upgrade/wingman/boss-fight/endless) đang mở thì bỏ qua; nếu không, check `WorldBoss.IsUnlock` (đọc lại flag remote-config `isWorldBossUnlock` ở mục 2 gián tiếp qua class `WorldBoss`), nếu unlock và chưa pass tutorial `TUTORIAL.UNLOCK_WORLD_BOSS` thì show popup `PopupId.TutorialWorldBoss`.

---

## 6. Assets/Project/UI/Resources/UI/UIGamePlay.prefab
File YAML rất lớn (51936 dòng). Grep các biến thể WorldBoss chỉ ra **1 field tham chiếu + 1 nested-prefab-instance liên quan**:

### 6a. Field `popupWB` trên component `UIGamePlay` (MonoBehaviour, dòng ~17080-17129, field tại dòng 17122)
```yaml
--- (block MonoBehaviour UIGamePlay, m_GameObject: {fileID: 1805667563012273363})
  popupNoel: {fileID: 7531423928523327295}
  popupAD: {fileID: 5910786561422769712}
  sprIcon: {fileID: 8319192598253283180}
  txtEventPoint: {fileID: 4860132745909662733}
  objEventPoint: {fileID: 1742105266299880468}
  popupWB: {fileID: 6713009366563851936}
  objCampaign: {fileID: 1322634081763550560}
  popupBossFight: {fileID: 2617845286738666416}
  popupEndless: {fileID: 5361195391151591207}
```
**Mô tả:** field `popupWB` (kiểu `PopupWorldBossGamePlay`, tương ứng `[FoldoutGroup("WORLD-BOSS"), SerializeField] PopupWorldBossGamePlay popupWB;` trong `UIGamePlay.cs` — xem mục 7) trỏ tới fileID `6713009366563851936`.

### 6b. Nested PrefabInstance "Popup-WB-GamePlay" (dòng 47532-47602, stripped ref dòng 47687-47694)
```yaml
--- !u!1001 &1001807636499014767
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    m_TransformParent: {fileID: 1805667561371093090}
    m_Modifications:
    - target: {fileID: 3859168801920011581, guid: 2f4437b3246cc4db4b87c99d06a91858,
        type: 3}
      propertyPath: m_Name
      value: Popup-WB-GamePlay
      objectReference: {fileID: 0}
    - target: {fileID: 6636232381172421406, guid: 2f4437b3246cc4db4b87c99d06a91858,
        type: 3}
      propertyPath: m_RootOrder
      value: 17
      objectReference: {fileID: 0}
    ... (các dòng modification khác: pivot/anchor/sizeDelta/localPosition)
  m_SourcePrefab: {fileID: 100100000, guid: 2f4437b3246cc4db4b87c99d06a91858, type: 3}
--- !u!114 &6713009366563851936 stripped
MonoBehaviour:
  m_CorrespondingSourceObject: {fileID: 5822667026117995215, guid: 2f4437b3246cc4db4b87c99d06a91858,
    type: 3}
  m_PrefabInstance: {fileID: 1001807636499014767}
  m_PrefabAsset: {fileID: 0}
```
**Mô tả:** GameObject `Popup-WB-GamePlay` là 1 nested prefab instance của `Assets/Project/WorldBoss/Use/UI/Prefabs/Popup-WB-GamePlay.prefab` (guid `2f4437b3246cc4db4b87c99d06a91858`), parent dưới `m_TransformParent: {fileID: 1805667561371093090}` (transform cha chung, cùng cha với các popup sự kiện khác như Noel/AirDefense/Campaign/BossFight/Endless), `m_RootOrder: 17`. FileID stripped `6713009366563851936` chính là field `popupWB` ở mục 6a trỏ tới.

**⚠️ Rủi ro khi merge prefab YAML:** đây là file YAML rất lớn (~52k dòng), Plastic có thể merge lệch guid/fileID hoặc xóa nhầm block PrefabInstance. Sau merge, PHẢI mở prefab này trong Unity Editor, xác nhận GameObject `Popup-WB-GamePlay` vẫn còn trong hierarchy (dưới cùng cha với Noel/AirDefense/Campaign/BossFight/Endless), và field `popupWB` trên component `UIGamePlay` vẫn được gán (không bị None).

---

## 7. Assets/Project/UI/Scripts/GamePlay/UIGamePlay.cs
**Tìm thấy 3 block WorldBoss:**

### 7a. Field khai báo (dòng 49)
```csharp
    [FoldoutGroup("WORLD-BOSS"), SerializeField] PopupWorldBossGamePlay popupWB;
```

### 7b. Bật/tắt popup theo mode lúc Init (dòng 178-181)
```csharp
        //worldBoss
        var _isWB = GamePlayController.Mode == GAMEPLAY_MODE.WORLD_BOSS;
        popupWB.SetActive(_isWB);
        InitPowerButton();
```

### 7c. Toàn bộ region `[WORLD-BOSS]` — 5 method public forward xuống `popupWB` (dòng 613-635)
```csharp
    #region [WORLD-BOSS]
    public void UpdateWorldBossPoint(BigDouble _damage, int _point)
    {
        popupWB.UpdatePointAndScore(_damage, _point);
    }
    public void ShowWorldBossPopUp(double _playTime, double _bonusTime = 0, int _phase = 1, WORLD_BOSS_ID _currentBossID = WORLD_BOSS_ID.WORLD_BOSS_1)
    {
        popupWB.Show(_playTime, _bonusTime, _phase, _currentBossID);
    }
    public void ShowWorldBossBonusTime()
    {
        popupWB.ShowBonusTime();
    }
    public void UpdateWorldBossTime(double _totalSeconds)
    {
        popupWB.OnUpdateTime(_totalSeconds);
    }
    public void SetMatchCompletedWorldBoss(bool _isActive)
    {
        popupWB.SetMatchCompleted(_isActive);
    }

    #endregion
```
**Mô tả:** đây là lớp forwarding API mà `GamePlayController.cs` (mục 4d) gọi vào (`uiGamePlay.ShowWorldBossBonusTime()`, `uiGamePlay.SetMatchCompletedWorldBoss(...)`) — 5 method này đều chỉ gọi xuống `popupWB` (component trên GameObject `Popup-WB-GamePlay`, mục 6b).

---

## Checklist restore (làm SAU KHI user báo đã merge xong)
1. Mở lại từng file trong 7 file trên, so với baseline mục 1-7, xác nhận block nào bị mất do conflict resolution.
2. Nếu mất, dán lại nguyên văn từ baseline (không đổi vì đây không phải enum, không cần renumber).
3. Note riêng cho từng file nếu có rủi ro đặc biệt:
   - **File 3 (`PopupNoelGamePlay.cs`)**: không có gì để restore (không tìm thấy code WB) — nếu sau merge vẫn thấy file này báo conflict WB thì có nghĩa lần rà soát này bỏ sót, cần grep lại kỹ hơn (thử thêm các tên method/class WorldBoss khác ngoài danh sách đã dùng).
   - **File 6 (`UIGamePlay.prefab`)**: rủi ro cao nhất vì là YAML lớn — sau merge phải mở trong Unity Editor kiểm tra GameObject `Popup-WB-GamePlay` (nested prefab instance của `Assets/Project/WorldBoss/Use/UI/Prefabs/Popup-WB-GamePlay.prefab`) còn tồn tại trong hierarchy, và field `popupWB` trên component `UIGamePlay` (dòng ~17122 baseline) không bị None. Nếu bị mất, kéo lại prefab `Popup-WB-GamePlay.prefab` vào đúng vị trí cha (cùng cha với Noel/AirDefense/Campaign/BossFight/Endless, `m_RootOrder: 17` ở baseline) rồi gán lại field.
   - **File 4 (`GamePlayController.cs`)**: nhiều block rải rác (7 vị trí khác nhau) — dễ sót nhất là các dòng `switch`/`if` chỉ thêm 1 `case GAMEPLAY_MODE.WORLD_BOSS:` xen giữa các case khác (mục 4b, 4c, 4e) — nếu main thêm case mới ở gần đó, cẩn thận merge tay tránh xóa nhầm case WORLD_BOSS.
   - **File 5 (`UIHome.cs`)**: 3 chỗ lặp lại pattern check `PopupId.TutorialWorldBoss` (mục 5b) nằm trong 3 hàm riêng biệt (không phải cùng 1 chỗ) — phải kiểm tra đủ cả 3, không chỉ 1.
   - **File 1, 2, 7**: mỗi file chỉ có 1 block liền mạch, dễ verify — so trực tiếp với baseline.
