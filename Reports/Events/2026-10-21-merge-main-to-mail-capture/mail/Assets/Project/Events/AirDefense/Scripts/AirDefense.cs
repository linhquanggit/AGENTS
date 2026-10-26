using DP.Utilities;
using Games;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TweenPlayables;
using UnityEngine;
using static AirDefenses;
using static DailyMission;
using static OnlineRewards;

public static class AirDefense
{
    static AirDefense() => EnsureProcess();
    static Process process;
    static AirDefenses.InputData data;
    public static void Refresh()
    {
        EnsureProcess();
    }
    static void EnsureProcess()
    {
        data = AirDefenses.Data;
        process = Player.AirDefenseEventProcess;
        if (process == null || process.IsOutdatedVersion(currentVersion))
        {
            ResetProcess();
        }
        OnNewDay();
    }
    static void SaveData()
    {
        Player.AirDefenseEventProcess = process;
    }
    public static Process GetProcess() { return process; }
    static string currentVersion = "0.2";
    static void ResetProcess()
    {
        process = new Process()
        {
            Currency = 0,
            Energy = 0,
            Exp = 0,
            Level = 0,
            LevelRewardClaimed = 0,
            IsClaimedFighterPiece = false,
            IsClaimedWingmanPiece = false,
            CurrencyEarnedToday = 0,
            Stats = new List<StatsProcess>()
            {
                new StatsProcess{Type=STATS.ATK},
                new StatsProcess{Type=STATS.FR},
                new StatsProcess{Type=STATS.PROJECTILE},
                new StatsProcess{Type=STATS.DIRECTION},
                new StatsProcess{Type=STATS.EXP},
                new StatsProcess{Type=STATS.CURRENCY},
                new StatsProcess{Type=STATS.DURATION},
                new StatsProcess{Type=STATS.SPAWN_RATE},
            },
            SkinsUnlocked = new List<int>() { 0 },// mặc định skin 0 luôn có
            SkinCurrent = 0,
            PointUpgraded = 0,
            lstMissionProcess = new(),
            Shop = null,
            LastOpen = string.Empty,
            IsFirstOpenUpgrade = true,
            IsChooseDontShowIntro = false,
            IsPlayX50 = false,
            Poker = null,
            Version = currentVersion
        };
        SaveData();
        //
        OnNewDay();
    }
    //-----POKER
    public static PokerProcess GetPoker()
    {
        if (process.Poker == null)
        {
            var _pokerProcess = new PokerProcess()
            {
                Level = 1,
                Exp = 0,
            };
            process.Poker = _pokerProcess;
            SaveData();
        }
        return process.Poker;
    }
    public static void PokerSave(PokerProcess _newProcess)
    {
        process.Poker = _newProcess;
        SaveData();
    }
    //
    public static readonly int LevelUnlock = 30;
    public static bool IsUnlocked
    {
        get
        {
            if (Level.LevelMax >= LevelUnlock) return true;
            return false;
        }
    }
    // tổng số lượng quà theo cấp độ
    public static AirDefenses.RewardLevelData[] GetAllRewards() { return data.RewardLevels; }
    public static int GetRewardIndex(int _level)
    {
        var _levelIndex = data.RewardLevels
            .Where(x => x.Level > _level)
            .OrderBy(x => x.Level)
            .FirstOrDefault().Level;
        //
        var _value = data.RewardLevels.FirstOrDefault(x => x.Level == _levelIndex);
        return System.Array.IndexOf(data.RewardLevels, _value);
    }
    // hàm lấy trạng thái phần thưởng theo cấp độ
    public static REWARD_STATS GetRewardStats(int _level)
    {
        if (_level <= process.LevelRewardClaimed)
        {
            return REWARD_STATS.CLAIMED;
        }
        else
        {
            if (_level <= process.Level)
            {
                return REWARD_STATS.CLAIM;
            }
            else
            {
                return REWARD_STATS.LOCK;
            }
        }
    }
    public static void CheckAndDownLoadAsset()
    {
        if (Level.LevelLimited < 25) return;
        //
        AddressableManager.instance.CheckAndDownload("AirDefense");
    }
    public static int EnergyPlayBase => data.EnergyPlay;
    public static int SkinCurrent => process.SkinCurrent;
    public static void SetGunSkin(int _idSkin)
    {
        process.SkinCurrent = _idSkin;
        SaveData();
    }
    public static void UnlockSkin(int _idSkin)
    {
        if (process.SkinsUnlocked.Contains(_idSkin)) return;// nếu đã có skin này rồi thì return
        //
        DPDebug.Log($"Air Defense---Mở khóa Gun skin[{_idSkin}]");
        process.SkinsUnlocked.Add(_idSkin);
        process.SkinCurrent = _idSkin;
        SaveData();
    }
    // hàm kiểm tra xem đã sở hữu skin súng theo id
    public static bool CheckSkinOwned(int _idSkin)
    {
        if (process.SkinsUnlocked == null || process.SkinsUnlocked.Count <= 0) return false;
        var _isOwned = process.SkinsUnlocked.Contains(_idSkin);
        return _isOwned;
    }
    public static int LevelCurrent => process.Level;
    // hàm lấy lượng Exp cần để lên cấp
    public static double GetExpUpLevel(int _level)
    {
        if (data.ExpUpgrades == null || data.ExpUpgrades.Length == 0)
        {
            DPDebug.LogError("ExpUpgrades array is null or empty in AirDefenses.InputData. Cannot calculate experience.");
            return 0;
        }

        if (_level >= 1 && _level <= data.ExpUpgrades.Length)
        {
            var _exp = data.ExpUpgrades[_level - 1].Exp;
            DPDebug.Log($"Lấy lượng kinh nghiệm của level[{_level}] từ ExpUpgrades---Exp[{_exp}]");
            return _exp;
        }
        else
        {
            float _baseExp = data.ExpUpgrades[data.ExpUpgrades.Length - 1].Exp;
            int _baseLevel = data.ExpUpgrades.Length;
            double _exp = (double)(_baseExp * Mathf.Pow(1f + data.BonusExp / 100f, _level - _baseLevel));
            DPDebug.Log($"Lấy lượng kinh nghiệm của level[{_level}] từ BonusExp---Exp[{_exp}]");
            return _exp;
        }
    }
    public static void UpLevel()
    {
        var _expUpLevel = GetExpUpLevel(process.Level + 1);
        if (process.Exp < _expUpLevel)
        {
            DPDebug.Log($"Không đủ kinh nghiệm lên cấp: Exp[{process.Exp}]--Exp Up[{_expUpLevel}]");
            return;
        }
        // đủ kinh nghiệm để lên cấp
        while (process.Exp >= _expUpLevel)
        {
            process.Exp -= _expUpLevel;// trừ đi lượng kinh nghiệm cần để lên cấp
            process.Level += 1;
            DPDebug.Log($"Nâng lên level[{process.Level}] thành công!");
            SaveData();
            // lấy lại lượng exp cần để lên cấp tiếp theo
            _expUpLevel = GetExpUpLevel(process.Level + 1);
        }
    }
    // ----- EVENT TYPE SERVER: 2 chế độ của cùng 1 event (bộ đàm / poker) -----
    static Events.EVENT_TYPE eventTypeActive = Events.EVENT_TYPE.AIRDEFENSE_EVENT_POKER;
    /// <summary>Loại event AirDefense đang active trên server, set khi sync all event. Dùng cho các call DHS.</summary>
    public static Events.EVENT_TYPE EventTypeActive { get => eventTypeActive; set => eventTypeActive = value; }
    /// <summary>Check 1 event type server có phải AirDefense không (cả 2 chế độ).</summary>
    public static bool IsAirDefenseEventType(Events.EVENT_TYPE _type)
    {
        return _type == Events.EVENT_TYPE.AIRDEFENSE_EVENT || _type == Events.EVENT_TYPE.AIRDEFENSE_EVENT_POKER;
    }
    // Version của event
    public static AirDefenses.EVENT_VERSION Version => data.Version;
    public static bool IsShopAvailable => Version == AirDefenses.EVENT_VERSION.WALKIE_TALKIE;// v1 mới có shop ngày/tuần
    public static bool IsPokerAvailable => Version == AirDefenses.EVENT_VERSION.POKER;// v2 mới có poker
    // Currency
    public static int CurrencyCurrent => process.Currency;
    // (v2) lượng currency nhận mỗi ván: chịu ảnh hưởng của nâng cấp stat CURRENCY, skin 1, x2 chủ nhật và PlayX (x1/5/10/50 như v1)
    public static int CurrencyEarnPerMatch
    {
        get
        {
            var _base = data.CurrencyPerMatch;
            var _rs = _base + _base * (GetStatsValue(STATS.CURRENCY) / 100f);// nâng cấp stat CURRENCY: cộng %
            if (CheckSkinOwned(1)) _rs *= 2;// sở hữu skin 1
            if (IsX2Currency) _rs *= 2;// x2 chủ nhật (ngày DayX2Currency)
            _rs *= PlayX;// nhân theo chế độ chơi x1/5/10/50 (giống v1)
            return Mathf.RoundToInt(_rs);
        }
    }
    // (v2) giới hạn currency nhận mỗi ngày: chịu ảnh hưởng của nâng cấp stat CURRENCY, skin 1 và x2 chủ nhật (KHÔNG nhân PlayX)
    public static int CurrencyLimitDaily
    {
        get
        {
            var _base = data.CurrencyLimitDaily;
            // nâng cấp stat CURRENCY: cộng % vào limit
            var _limit = _base + _base * (GetStatsValue(STATS.CURRENCY) / 100f);
            if (IsX2Currency) _limit *= 2;// x2 chủ nhật
            if (CheckSkinOwned(1)) _limit *= 2;// sở hữu skin 1
            return Mathf.RoundToInt(_limit);
        }
    }
    public static int CurrencyEarnedToday => process.CurrencyEarnedToday;
    // (v2) lượng currency còn có thể nhận trong ngày
    public static int CurrencyRemainToday => Mathf.Max(0, CurrencyLimitDaily - process.CurrencyEarnedToday);
    public static void OnAddCurrency(int _amount)
    {
        process.Currency += _amount;
        DPDebug.Log($"Thêm Currency cho Air Defense Event---Currency nhận được[{_amount}]---Currency hiện tại[{process.Currency}]");
        SaveData();
        EventDispatcher.Instance.Dispatch(EventName.OnChangeAirDefenseCurrency, process.Currency);
    }
    public static void OnSubtractCurrency(int _amount)
    {
        process.Currency -= _amount;
        if (process.Currency < 0) process.Currency = 0;
        DPDebug.Log($"Trừ Currency cho Air Defense Event---Currency trừ đi[{_amount}]---Currency hiện tại[{process.Currency}]");
        SaveData();
        EventDispatcher.Instance.Dispatch(EventName.OnChangeAirDefenseCurrency, process.Currency);
    }
    // Energy
    public static int EnergyCurrent => process.Energy;
    public static void OnAddEnergy(int _amount)
    {
        process.Energy += _amount;
        DPDebug.Log($"Thêm Energy cho Air Defense Event---Energy nhận được[{_amount}]---Energy hiện tại[{process.Energy}]");
        SaveData();
        EventDispatcher.Instance.Dispatch(EventName.OnChangeAirDefenseEnergy, process.Energy);
    }
    public static void OnSubtractEnergy(int _amount)
    {
        process.Energy -= _amount;
        if (process.Energy < 0) process.Energy = 0;
        DPDebug.Log($"Trừ Energy cho Air Defense Event---Energy trừ đi[{_amount}]---Energy hiện tại[{process.Energy}]");
        SaveData();
        EventDispatcher.Instance.Dispatch(EventName.OnChangeAirDefenseEnergy, process.Energy);
    }
    // hàm thêm exp
    public static void OnAddExp(double _exp)
    {
        process.Exp += _exp;
        DPDebug.Log($"Thêm Exp cho Air Defense Event---Exp nhận được[{_exp}]---Exp hiện tại[{process.Exp}]");
        SaveData();
        // kiểm tra có thể lên cấp không
        UpLevel();
    }
    // hàm khi nhận phần thưởng theo cấp độ
    public static void OnClaimReward(int _level)
    {
        if (process.LevelRewardClaimed >= _level) return;
        //
        process.LevelRewardClaimed = _level;
        DPDebug.Log($"Air Defense---Đã nhận phần thưởng cấp độ[{_level}] thành công!");
        SaveData();
    }
    // trạng thái nhận mảnh Fighter/Wingman của event
    public static bool IsClaimedFighterPiece => process.IsClaimedFighterPiece;
    public static bool IsClaimedWingmanPiece => process.IsClaimedWingmanPiece;
    public static void OnClaimFighterPiece()
    {
        if (process.IsClaimedFighterPiece) return;
        //
        process.IsClaimedFighterPiece = true;
        DPDebug.Log($"Air Defense---Đã nhận mảnh Fighter thành công!");
        SaveData();
    }
    public static void OnClaimWingmanPiece()
    {
        if (process.IsClaimedWingmanPiece) return;
        //
        process.IsClaimedWingmanPiece = true;
        DPDebug.Log($"Air Defense---Đã nhận mảnh Wingman thành công!");
        SaveData();
    }
    static string LastOpen
    {
        get
        {
            if (!DPUtils.ParseTicks(process.LastOpen, out var _))
            {
                process.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
                SaveData();
            }
            return process.LastOpen;
        }
    }
    public static void OnNewDay()
    {
        if (process == null) return;
        //if (process.DayOfYear == DPUtils.GetUtcNow.DayOfYear) return;
        if (!DPUtils.IsNewDay(LastOpen)) return;
        // sang ngày mới
        process.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
        process.CurrencyEarnedToday = 0;// (v2) reset limit currency nhận trong ngày
        // reset số lượt mua gói hàng ngày
        var _dailyList = new List<PackageProcess>();
        for (int i = 0; i < data.ShopData.DailyShops.Length; i++)
        {
            var _item = data.ShopData.DailyShops[i];
            _dailyList.Add(new PackageProcess()
            {
                ID = _item.ID,
                Amount = _item.Limit,
            });
        }
        process.Shop.Daily = new List<PackageProcess>(_dailyList);
        ResetDailyMissions();
        SaveData();
    }
    /// <summary>
    /// Resets all AirDefense missions for a new day.
    /// </summary>
    public static void ResetDailyMissions()
    {
        var _process = Player.AirDefenseEventProcess;
        if (_process == null) return;

        if (_process.lstMissionProcess == null)
        {
            InitializeMissions(_process, true);
        }
        else
        {
            for (int i = 0; i < _process.lstMissionProcess.Count; i++)
            {
                var _missionProcess = _process.lstMissionProcess[i];
                _missionProcess.ProcessValue = 0;
                _missionProcess.IsClaimed = false;
            }
        }
        process = _process;
        SaveData();
    }
    public static bool IsFirstOpenUpgrade
    {
        get { return process.IsFirstOpenUpgrade; }
        set { process.IsFirstOpenUpgrade = value; }
    }
    public static void OnBoughtX50Success()
    {
        process.IsPlayX50 = true;
        SaveData();
    }
    // X2 vào thứ 7 và chủ nhật
    static bool isX2Exp;
    public static bool IsX2Exp
    {
        get { return isX2Exp; }
        set { isX2Exp = value; }
    }
    static bool isX2Currency;
    public static bool IsX2Currency
    {
        get { return isX2Currency; }
        set { isX2Currency = value; }
    }
    public static void SetChooseDontShowIntro(bool _dontShow)
    {
        process.IsChooseDontShowIntro = _dontShow;
        SaveData();
    }
    public static bool IsShowIntro
    {
        get
        {
            if (IsEndEvent) return false;
            if (process.IsChooseDontShowIntro) return false;
            return IsShowIntroOnSession;
        }
    }
    // biến show intro mỗi lần mở game
    static bool isShowIntroOnSession = true;
    public static bool IsShowIntroOnSession { private get => isShowIntroOnSession; set => isShowIntroOnSession = value; }
    // ngày x2
    static System.DayOfWeek dayX2Exp = System.DayOfWeek.Saturday;// thứ 7 x2 Exp
    public static System.DayOfWeek DayX2Exp
    {
        get { return dayX2Exp; }
        set { dayX2Exp = value; }
    }
    static System.DayOfWeek dayX2Currency = System.DayOfWeek.Sunday;// chủ nhật x2 currency
    public static System.DayOfWeek DayX2Currency
    {
        get { return dayX2Currency; }
        set { dayX2Currency = value; }
    }
    // end event
    static bool isEndEvent = false;
    public static bool IsEndEvent
    {
        get
        {
            DPDebug.Log($"Air Defense[{EventTypeActive}]---Check IsEndEvent: {isEndEvent}");
            return isEndEvent;
        }
        set { isEndEvent = value; }
    }

    #region [SHOP]

    // hàm set lại số lượng giới hạn mới cho gói theo ID
    public static void SetPurchaseProcess(int _ID, int _newAmount)
    {
        var _item = process.Shop.Purchase.FirstOrDefault(x => x.ID == _ID);
        if (_item != null)
        {
            _item.Amount = _newAmount;
            SaveData();
        }
    }
    // hàm lấy toàn bộ cái gói trong purchase
    public static List<PackageProcess> GetPurchaseProcess()
    {
        if (process.Shop == null || process.Shop.Purchase == null || process.Shop.Purchase.Count <= 0)
        {
            // khởi tạo nếu chưa có
            var _list = new List<PackageProcess>();
            for (int i = 0; i < data.ShopData.Purchases.Length; i++)
            {
                var _item = data.ShopData.Purchases[i];
                _list.Add(new PackageProcess()
                {
                    ID = _item.ID,
                    Amount = _item.IsLimit ? _item.Limit : -1,// khởi tạo bằng số lượng cao nhất, hoặc -1 nếu không có giới hạn
                });
            }
            //
            if (process.Shop == null)
            {
                process.Shop = new ShopProcess()
                {
                    Purchase = new List<PackageProcess>(_list)
                };
            }
            else
                process.Shop.Purchase = new List<PackageProcess>(_list);
            SaveData();
        }
        return process.Shop.Purchase;
    }
    // hàm lấy gói purchase dựa theo ID
    public static PackageProcess GetPurchaseProcess(int _ID)
    {
        if (process.Shop == null || process.Shop.Purchase.Count <= 0) return null;
        //
        var _rs = process.Shop.Purchase.FirstOrDefault(x => x.ID == _ID);
        return _rs;
    }
    // hàm lấy toàn bộ data các gói bán energy
    public static AirDefenses.PurchaseShopData[] GetPurchaseData()
    {
        if (data.ShopData.Purchases == null) return new AirDefenses.PurchaseShopData[0];
        return data.ShopData.Purchases;
    }
    // hàm lấy purchase data theo ID
    public static AirDefenses.PurchaseShopData GetPurchaseData(int _ID)
    {
        var _rs = data.ShopData.Purchases.FirstOrDefault(x => x.ID == _ID);
        return _rs;
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------
    // [TURRET LEVEL] các gói IAP bán thẳng level của turret, mua không giới hạn số lần
    // biến remote firebase bật/tắt shop bán level turret
    public static bool IsShowTurretLevelShop
    {
        get
        {
            if (Firebase_Analytics.current == null) return true;// chưa init firebase --> mặc định bật
            return Firebase_Analytics.current.isAirDefenseShowTurretLevelShop;
        }
    }
    // hàm lấy toàn bộ data các gói bán level
    public static AirDefenses.TurretLevelShopData[] GetTurretLevelData()
    {
        if (data.ShopData.TurretLevels == null) return new AirDefenses.TurretLevelShopData[0];
        return data.ShopData.TurretLevels;
    }
    // hàm lấy data gói bán level theo ID
    public static AirDefenses.TurretLevelShopData GetTurretLevelData(int _ID)
    {
        if (data.ShopData.TurretLevels == null) return default;
        var _rs = data.ShopData.TurretLevels.FirstOrDefault(x => x.ID == _ID);
        return _rs;
    }
    // hàm cộng thẳng level cho turret (mua gói IAP). Mỗi level tặng 1 point nâng cấp
    // theo công thức freePoint = Level - PointUpgraded nên không cần cộng point riêng
    public static void OnAddLevel(int _amount)
    {
        if (_amount <= 0) return;
        //
        process.Level += _amount;
        // reset exp dư về 0: tránh exp thừa tự dồn lên level tiếp theo, thanh exp bắt đầu lại từ đầu ở level mới
        process.Exp = 0;
        DPDebug.Log($"Cộng thẳng Level cho Air Defense Event---Level cộng thêm[{_amount}]---Level hiện tại[{process.Level}]---reset Exp về 0");
        SaveData();
        EventDispatcher.Instance.Dispatch(EventName.OnChangeAirDefenseLevel, process.Level);
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------
    public static ShopProcess GetShopProcess() { return process.Shop; }
    // hàm tìm shop nào còn lượt mua
    public static bool GetShopActive(int _idShop)
    {
        if (_idShop == 0 || _idShop == 1)
        {
            var _process = GetShopProcesses(_idShop);
            foreach (var _item in _process)
            {
                if (_item.Amount <= 0) continue;
                return true;// còn lượt
            }
            return false;// hết lượt mua
        }
        // shop purchase luôn còn
        return true;
    }
    // hàm lấy các gói trong shop
    public static List<PackageProcess> GetShopProcesses(int _idShop)
    {
        if (process.Shop == null)
        {
            // khởi tạo nếu chưa có
            var _listDaily = new List<PackageProcess>();
            for (int i = 0; i < data.ShopData.DailyShops.Length; i++)
            {
                var _item = data.ShopData.DailyShops[i];
                _listDaily.Add(new PackageProcess()
                {
                    ID = _item.ID,
                    Amount = _item.Limit,// khởi tạo bằng số lượng cao nhất
                });
            }
            var _listLimit = new List<PackageProcess>();
            for (int i = 0; i < data.ShopData.LimitShops.Length; i++)
            {
                var _item = data.ShopData.LimitShops[i];
                _listDaily.Add(new PackageProcess()
                {
                    ID = _item.ID,
                    Amount = _item.Limit,// khởi tạo bằng số lượng cao nhất
                });
            }
            var _listPurchase = new List<PackageProcess>();
            for (int i = 0; i < data.ShopData.Purchases.Length; i++)
            {
                var _item = data.ShopData.Purchases[i];
                _listPurchase.Add(new PackageProcess()
                {
                    ID = _item.ID,
                    Amount = _item.IsLimit ? _item.Limit : -1,// khởi tạo bằng số lượng cao nhất, hoặc -1 nếu không có giới hạn
                });
            }
            //
            process.Shop = new ShopProcess()
            {
                Daily = new List<PackageProcess>(_listDaily),
                Limit = new List<PackageProcess>(_listLimit),
                Purchase = new List<PackageProcess>(_listPurchase)
            };
            SaveData();
        }
        //
        var _data = data.ShopData.DailyShops;
        var _process = process.Shop.Daily;
        if (_idShop == 1)
        {
            _data = data.ShopData.LimitShops;
            _process = process.Shop.Limit;
        }
        if (_process == null || _process.Count <= 0)
        {
            // khởi tạo nếu chưa có
            var _list = new List<PackageProcess>();
            for (int i = 0; i < _data.Length; i++)
            {
                var _item = _data[i];
                _list.Add(new PackageProcess()
                {
                    ID = _item.ID,
                    Amount = _item.Limit,// khởi tạo bằng số lượng cao nhất
                });
            }
            //
            switch (_idShop)
            {
                case 0: process.Shop.Daily = new List<PackageProcess>(_list); break;
                case 1: process.Shop.Limit = new List<PackageProcess>(_list); break;
            }
            SaveData();
        }
        //
        switch (_idShop)
        {
            case 0: return process.Shop.Daily;
            case 1: return process.Shop.Limit;
            default: return null;
        }
    }
    // hàm lấy gói theo ID
    public static PackageProcess GetShopProcesses(int _idShop, int _ID)
    {
        var _shop = GetShopProcesses(_idShop);
        var _rs = _shop.FirstOrDefault(x => x.ID == _ID);
        return _rs;
    }
    // hàm cập nhật số lượt đã mua gói theo ID
    public static void SetShopProcess(int _idShop, int _ID, int _newAmount)
    {
        var _shopProcess = GetShopProcesses(_idShop, _ID);
        if (_shopProcess == null) return;
        _shopProcess.Amount = _newAmount;
        DPDebug.Log($"Shop[{_idShop}]--Cập nhật gói ID[{_ID}]---Số lượt còn lại[{_shopProcess.Amount}]");
        SaveData();
    }
    // hàm lấy data gói theo ID
    public static AirDefenses.PackageShopData GetShopData(int _idShop, int _ID)
    {
        var _data = data.ShopData.DailyShops.FirstOrDefault(x => x.ID == _ID);
        if (_idShop == 1) _data = data.ShopData.LimitShops.FirstOrDefault(x => x.ID == _ID);
        return _data;
    }
    // hàm lấy tất cả data gói
    public static AirDefenses.PackageShopData[] GetShopData(int _idShop)
    {
        switch (_idShop)
        {
            case 0: return data.ShopData.DailyShops;
            case 1: return data.ShopData.LimitShops;
            default: return null;
        }
    }

    #endregion

    #region [STATS]

    // hàm cập nhật số điểm đã sử dụng để nâng cấp
    public static void UpdatePointUpgraded(int _pointUsed)
    {
        process.PointUpgraded += _pointUsed;
        SaveData();
    }
    public static AirDefenses.StatsData GetStatsData(STATS _type)
    {
        var _data = data.Stats.FirstOrDefault(x => x.Type == _type);
        return _data;
    }
    public static StatsProcess GetStatsProcess(STATS _type)
    {
        var _stats = process.Stats.FirstOrDefault(x => x.Type == _type);
        return _stats;
    }
    // nâng cấp chỉ số
    public static void UpgradeStats(STATS _type, int _levelTarget)
    {
        var _process = process.Stats.FirstOrDefault(s => s.Type == _type);
        if (_process == null) return;
        //
        var _data = GetStatsData(_type);
        if (!_data.IsLimited)
        {
            // TH không giới hạn nâng cấp
            _process.Level = _levelTarget;
            DPDebug.Log($"Nâng cấp Stats[{_type}]---lên level[{_process.Level}]");
            SaveData();
        }
        else
        {
            // TH có giới hạn nâng cấp
            if (_process.Level >= _data.LevelLimit) return;
            _process.Level = _levelTarget;
            DPDebug.Log($"Nâng cấp Stats[{_type}]---lên level[{_process.Level}]");
            SaveData();
        }
    }
    // hàm lấy bonus của chỉ số
    public static string GetStatsBonus(STATS _type)
    {
        var _data = data.Stats.FirstOrDefault(x => x.Type == _type);
        string _strBonus = string.Empty;
        switch (_type)
        {
            case STATS.ATK:// chỉ ATK và DURATION thì sẽ show chỉ số của level tiếp theo của nó
            case STATS.DURATION:
                var _bonus = _data.Base * (_data.Bonus / 100f);
                _strBonus = $"+{_bonus}";
                break;
            case STATS.PROJECTILE:
            case STATS.DIRECTION:
                _strBonus = $"+{_data.Bonus}";
                break;
            default:
                _strBonus = $"+{_data.Bonus}%";
                break;
        }
        DPDebug.Log($"Lấy thông số bonus-[{_type}]---> result-[{_strBonus}]");
        return _strBonus;
    }
    // hàm lấy chỉ số nâng cấp của stats theo level
    static int levelStats = 0;
    public static float GetStatsValue(STATS _type, int _level = -1)
    {
        // nếu k điền level thì sẽ lấy level hiện tại của stats đó
        levelStats = _level == -1 ? GetStatsProcess(_type).Level : _level;
        switch (_type)
        {
            case STATS.ATK: return GetATKValue(levelStats);
            case STATS.FR: return GetFRValue(levelStats);
            case STATS.PROJECTILE: return GetProjectileValue(levelStats);
            case STATS.DIRECTION: return GetDirectionValue(levelStats);
            case STATS.EXP: return GetExpValue(levelStats);
            case STATS.CURRENCY: return GetCurrencyValue(levelStats);
            case STATS.DURATION: return GetDurationValue(levelStats);
            case STATS.SPAWN_RATE: return GetSpawnRateValue(levelStats);
            //
            default: return -1;
        }
    }
    // hàm lấy thông số ATK
    static AirDefenses.StatsData dataATK;
    static float GetATKValue(int _level)
    {
        dataATK = data.Stats.FirstOrDefault(x => x.Type == STATS.ATK);
        if (_level == 0) return dataATK.Base;
        // rs = base + (base*(level*bonus%));
        var _rs = dataATK.Base + (dataATK.Base * (_level * (dataATK.Bonus / 100f)));
        DPDebug.Log($"Lấy thông số ATK-[{_level}]---> result-[{_rs}]");
        return _rs;
    }
    // hàm lấy thông số fire rate
    static AirDefenses.StatsData dataFR;
    static float GetFRValue(int _level)
    {
        dataFR = data.Stats.FirstOrDefault(x => x.Type == STATS.FR);
        if (_level == 0) return dataFR.Base;
        // rs = base-(base(level*bonus%))
        var _rs = dataFR.Base - (dataFR.Base * (_level * (dataFR.Bonus / 100f)));
        DPDebug.Log($"Lấy thông số FR-[{_level}]---> result-[{_rs}]");
        return _rs;
    }
    // hàm lấy thông số số lượng viên đạn 
    static AirDefenses.StatsData dataProj;
    static int GetProjectileValue(int _level)
    {
        dataProj = data.Stats.FirstOrDefault(x => x.Type == STATS.PROJECTILE);
        if (_level == 0) return 0;
        // rs = base*level
        var _rs = Mathf.RoundToInt(dataProj.Base * _level);
        DPDebug.Log($"Lấy thông số PROJECTILE-[{_level}]---> result-[{_rs}]");
        return _rs;
    }
    // hàm lấy thông số số lượng tia đạn 
    static AirDefenses.StatsData dataDir;
    static int GetDirectionValue(int _level)
    {
        dataDir = data.Stats.FirstOrDefault(x => x.Type == STATS.DIRECTION);
        if (_level == 0) return 0;
        // rs = base*level
        var _rs = Mathf.RoundToInt(dataDir.Base * _level);
        DPDebug.Log($"Lấy thông số DIRECTION-[{_level}]---> result-[{_rs}]");
        return _rs;
    }
    // hàm lấy thông số kinh nghiệm
    static AirDefenses.StatsData dataExp;
    static float GetExpValue(int _level)
    {
        dataExp = data.Stats.FirstOrDefault(x => x.Type == STATS.EXP);
        //if (_level == 0) return dataExp.Base;
        //
        var _rs = _level * dataExp.Bonus;// kết quả là %
        DPDebug.Log($"Lấy thông số EXP-[{_level}]---> result-[{_rs}%]");
        return _rs;
    }
    // hàm lấy thông số currency
    static AirDefenses.StatsData dataCurrency;
    static float GetCurrencyValue(int _level)
    {
        dataCurrency = data.Stats.FirstOrDefault(y => y.Type == STATS.CURRENCY);
        //if (_level == 0) return (int)dataCurrency.Base;
        //
        var _rs = _level * dataCurrency.Bonus;// kết quả là %
        DPDebug.Log($"Lấy thông số CURRENCY-[{_level}]---> result-[{_rs}%]");
        return _rs;
    }
    // hàm lấy thông số thời gian ván đấu
    static AirDefenses.StatsData dataDuration;
    static float GetDurationValue(int _level)
    {
        dataDuration = data.Stats.FirstOrDefault(z => z.Type == STATS.DURATION);
        if (_level == 0) return dataDuration.Base;
        // rs = base + (base*(bonus%*level))
        var _rs = dataDuration.Base + (dataDuration.Base * (_level * (dataDuration.Bonus / 100f)));
        DPDebug.Log($"Lấy thông số DURATION-[{_level}]---> result-[{_rs}]");
        return _rs;
    }
    // hàm lấy thông số thời gian sinh quái
    static AirDefenses.StatsData dataSR;
    static float GetSpawnRateValue(int _level)
    {
        dataSR = data.Stats.FirstOrDefault(x => x.Type == STATS.SPAWN_RATE);
        if (_level == 0) return dataSR.Base;
        // rs = base - (base*(bonus%*level))
        var _rs = dataSR.Base - (dataSR.Base * (_level * (dataSR.Bonus / 100f)));
        DPDebug.Log($"Lấy thông số SPAWN RATE-[{_level}]---> result-[{_rs}]");
        return _rs;
    }

    #endregion

    #region  [MISSION]

    public static void InitializeMissions(Process _process, bool _forceInit = false)
    {
        if (_process == null) return;

        if (_process.lstMissionProcess == null || _process.lstMissionProcess.Count == 0 || _forceInit)
        {
            DPDebug.Log("Mission] Initializing mission list...");
            _process.lstMissionProcess = new List<MissionProcess>();

            // Lấy data mission từ ScriptableObject
            var missionData = AirDefenses.Missions;
            if (missionData != null && missionData.Length > 0)
            {
                foreach (var missionConfig in missionData)
                {
                    var newMission = new MissionProcess
                    {
                        IDMission = missionConfig.ID,
                        ProcessValue = 0,
                        IsClaimed = false
                    };

                    _process.lstMissionProcess.Add(newMission);
                    DPDebug.Log($"[Mission] Added mission ID {missionConfig.ID}");
                }
            }
            if (_forceInit)
            {
                process = _process;
                SaveData();
            }
        }
        DPDebug.Log($"[Mission] Initialized {_process.lstMissionProcess.Count} missions");
    }

    /// <summary>
    /// Lấy thông tin mission process theo ID
    /// </summary>
    public static MissionProcess GetMissionProcess(int missionID)
    {

        var _process = Player.AirDefenseEventProcess;
        if (_process == null) return null;

        if (_process.lstMissionProcess == null || _process.lstMissionProcess.Count == 0)
            InitializeMissions(_process, true);

        return _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionID);
    }

    /// <summary>
    /// Cập nhật tiến độ mission
    /// </summary>
    public static void UpdateMissionProgress(TYPE_MISSION missionType, int incrementValue = 1)
    {
        var _process = Player.AirDefenseEventProcess;
        if (_process == null) return;


        // Tìm tất cả mission có type tương ứng
        var missionData = AirDefenses.Missions;
        if (missionData == null) return;

        foreach (var missionConfig in missionData)
        {
            if (missionConfig.mission.Type != missionType) continue;

            var missionProcess = _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionConfig.ID);
            if (missionProcess == null) continue;

            // Nếu đã completed hoặc claimed thì bỏ qua
            if (missionProcess.IsClaimed) continue;

            // Cập nhật progress
            missionProcess.ProcessValue += incrementValue;

            // Kiểm tra hoàn thành
            if (missionProcess.ProcessValue >= missionConfig.mission.Value)
            {
                missionProcess.ProcessValue = missionConfig.mission.Value;
                DPDebug.Log($"[Mission] ✅ Mission {missionConfig.ID} completed!");
            }
        }
        process = _process;
        SaveData();
    }

    public static bool ClaimMissionReward(int missionID, out RewardCount reward)
    {
        reward = null;
        var _process = process;
        if (_process == null) return false;

        // Tìm mission process
        var missionProcess = _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionID);
        if (missionProcess == null)
        {
            DPDebug.Log($"[Mission] Mission ID {missionID} not found!");
            return false;
        }

        var _missionData = AirDefenses.GetMission(missionID);
        if (_missionData == null || _missionData.reward == null)
        {
            DPDebug.LogError($"[Mission] Mission {missionID} has no reward config!");
            return false;
        }

        // Kiểm tra đã completed chưa
        if (missionProcess.ProcessValue < _missionData.mission.Value)
        {
            DPDebug.LogWarning($"[Mission] Mission {missionID} not completed yet!");
            return false;
        }

        // Kiểm tra đã claimed chưa
        if (missionProcess.IsClaimed)
        {
            DPDebug.LogWarning($"[Mission] Mission {missionID} already claimed!");
            return false;
        }
        reward = new RewardCount
        {
            Type = _missionData.reward.Type,
            Count = _missionData.reward.Count
        };
        // Đánh dấu đã claim
        missionProcess.IsClaimed = true;
        OnAddEnergy(reward.Count);
        process = _process;
        SaveData();

        DPDebug.Log($"[Mission] ✅ Claimed mission {missionID} - Reward: {reward.Type} x{reward.Count}");
        return true;
    }

    /// <summary>
    /// Kiểm tra có mission nào có thể claim không
    /// </summary>
    public static bool HasClaimableMissions()
    {
        var _process = Player.AirDefenseEventProcess;
        if (_process == null) return false;
        for (int i = 0; i < _process.lstMissionProcess.Count; i++)
        {
            var missionProcess = _process.lstMissionProcess[i];
            var _missionData = AirDefenses.GetMission(missionProcess.IDMission);
            if (_missionData == null) continue;

            if (missionProcess.ProcessValue >= _missionData.mission.Value && !missionProcess.IsClaimed)
                return true;
        }
        return false;
    }


    #endregion

    #region [HACK]

    public static void HackSetLevel(int _level)
    {
        process.Level = _level;
        SaveData();
    }
    public static void HackResetAll()
    {
        process.PointUpgraded = 0;
        for (int i = 0; i < process.Stats.Count; i++)
            process.Stats[i].Level = 0;
        process.Level = 0;
        process.Currency = 0;
        process.Energy = 0;
        process.Exp = 0;
        SaveData();
    }
    public static void HackSimulateNewDay()
    {
        if (process == null) return;
        DPDebug.Log($"[HACK] Simulating new day. Old DayOfYear: {process.LastOpen}");
        process.LastOpen = DPUtils.GetUtcNow.AddDays(-1).Ticks.ToString(); // Set to an arbitrary different value to force OnNewDay to trigger
        SaveData();
        OnNewDay();
        DPDebug.Log($"[HACK] New day simulated. New DayOfYear: {process.LastOpen}");
    }
    public static double HackGetExpNeededToLevelUp(int _targetLevel)
    {
        if (_targetLevel <= 0)
        {
            DPDebug.LogWarning($"Target level ({_targetLevel}) must be greater than 0.");
            return 0;
        }

        double _totalExpNeeded = 0;
        for (int _level = 1; _level <= _targetLevel; _level++)
        {
            _totalExpNeeded += GetExpUpLevel(_level);
        }
        DPDebug.Log($"Total EXP needed to reach level {_targetLevel} from level 0: {_totalExpNeeded}");
        return _totalExpNeeded;
    }

    #endregion

    #region [INGAME]

    public static int PlayX// chọn x1, x5, x10, x50 lượt để chơi nhanh
    {
        get
        {
            switch (IDPlayX)
            {
                case 1: return 1;
                case 5: return 5;
                case 10: return 10;
                case 50: return 50;
                default: return 1;
            }
        }
    }
    public static int IDPlayX = 1;// id của chế độ x1,5,10,50
    public static int ExpEarnedInMatch
    {
        get { return expEarnedInMatch; }
    }
    static int expEarnedInMatch = 0;
    // hàm thêm exp trong ván chơi
    public static void AddExpInMatch(int _exp)
    {
        if (isMatchCompleted) return;
        expEarnedInMatch += _exp;
    }
    public static int CurrencyEarnedInMatch
    {
        get { return currencyEarnedInMatch; }

    }
    static int currencyEarnedInMatch = 0;
    // hàm thêm currency trong ván chơi
    public static void AddCurrencyInMatch(int _currency)
    {
        if (isMatchCompleted) return;
        currencyEarnedInMatch += _currency;
    }
    // hàm khởi tạo ván chơi
    public static void MatchInit()
    {
        isMatchCompleted = false;
        expEarnedInMatch = 0;
        currencyEarnedInMatch = 0;
        isCurrencyCapMaxed = false;
    }
    // biến kiểm tra ván chơi đã hoàn thành hay chưa
    static bool isMatchCompleted = false;
    // hàm hoàn thành ván chơi
    public static void OnMatchCompleted()
    {
        isMatchCompleted = true;
        //thêm exp và currency nhận được trong ván chơi
        if (expEarnedInMatch > 0) OnAddExp(expEarnedInMatch);
        switch (Version)
        {
            case EVENT_VERSION.WALKIE_TALKIE:
                // v1: currency tích từ giết quái trong ván
                if (currencyEarnedInMatch > 0) OnAddCurrency(currencyEarnedInMatch);
                break;
            case EVENT_VERSION.POKER:
                // v2: nhận currency cuối ván (đã tính stat CURRENCY + skin 1 + x2 CN + PlayX như v1).
                // Hiển thị luôn là số chuẩn, chỉ lượng cộng thật vào process mới bị clamp theo limit còn lại trong ngày
                var _rewardFull = CurrencyEarnPerMatch;
                var _added = Mathf.Min(_rewardFull, CurrencyRemainToday);
                isCurrencyCapMaxed = _added <= 0;// đạt limit ngày, không nhận được nữa
                currencyEarnedInMatch = _rewardFull;// số hiển thị trên popup complete
                DPDebug.Log($"Air Defense---Currency cuối ván: Reward[{_rewardFull}]--Còn lại trong ngày[{CurrencyRemainToday}]--Cộng thật[{_added}]");
                if (_added > 0)
                {
                    process.CurrencyEarnedToday += _added;
                    OnAddCurrency(_added);// đã SaveData bên trong
                }
                break;
        }
    }
    // (v2) ván này có bị chặn nhận currency do đạt limit ngày không
    static bool isCurrencyCapMaxed = false;
    public static bool IsCurrencyCapMaxedInMatch => isCurrencyCapMaxed;
    // hàm tính toán exp khi tiêu diệt quái
    static float factor;
    static int expAdd;
    public static int GetExpOnEnemyKilled(int _base, float _timeToLive)
    {
        factor = 1f;
        foreach (var _item in data.Exps)
        {
            if (_timeToLive <= _item.Time)
            {
                factor = _item.Factor;
                break;
            }
        }
        //
        expAdd = Mathf.RoundToInt(_base * factor);
        DPDebug.Log($"Exp khi tiêu diệt quái---Base[{_base}]--TimeToLive[{_timeToLive}]--Factor[{factor}]--ExpAdd[{expAdd}]");
        return expAdd;
    }
    static float healthFactor = 1;
    //hàm lấy factor health khi spawn Enemy theo thời gian chơi của game 
    public static float GetHealthFactorOnEnemyInit(float _timePlay)
    {
        healthFactor = 1f;
        foreach (var _item in data.EnemyConfig.EnemyHealth)
        {
            if (_timePlay <= _item.TimePlay)
            {
                healthFactor = _item.Factor;
                break;
            }
        }
        return healthFactor;
    }

    #endregion

    //===================================================================================================================================================
    [System.Serializable]
    public class Process : EventProcessBase
    {
        public int Level;// cấp độ hiện tại
        public int LevelRewardClaimed;// cấp độ phần thưởng đã nhận
        public int CurrencyEarnedToday;// (v2) lượng currency đã nhận trong ngày, reset OnNewDay
        public bool IsClaimedFighterPiece;// đã nhận mảnh Fighter chưa
        public bool IsClaimedWingmanPiece;// đã nhận mảnh Wingman chưa
        // tất cả những thông số nâng cấp
        public List<StatsProcess> Stats = new List<StatsProcess>()
        {
            new StatsProcess{Type=STATS.ATK},
            new StatsProcess{Type=STATS.FR},
            new StatsProcess{Type=STATS.PROJECTILE},
            new StatsProcess{Type=STATS.DIRECTION},
            new StatsProcess{Type=STATS.EXP},
            new StatsProcess{Type=STATS.CURRENCY},
            new StatsProcess{Type=STATS.DURATION},
            new StatsProcess{Type=STATS.SPAWN_RATE},
        };
        //
        public double Exp;// kinh nghiệm của cấp độ hiện tại
        public int SkinCurrent;// skin hiện tại
        public List<int> SkinsUnlocked = new List<int>();// danh sách skin đã mở khóa
        public int Energy;
        public int Currency;
        public int PointUpgraded;// số điểm nâng cấp đã sử dụng. mỗi 1 level sẽ được tặng 1 point upgraded
        public List<MissionProcess> lstMissionProcess = new();
        public ShopProcess Shop;
        public string LastOpen;
        public bool IsFirstOpenUpgrade = true;// lần đầu mở popup upgrade
        public bool IsPlayX50 = false;// biến mở chế độ chơi x50 lần
        public bool IsChooseDontShowIntro = false; // Người chơi chọn không hiển thị intro nữa
        public PokerProcess Poker;
    }
    public enum STATS
    {
        NONE = 0,
        ATK = 1,
        FR = 2,// tốc độ bắn
        PROJECTILE = 3,// số lượng đạn 1 lần bắn ra
        DIRECTION = 4,// số tia đạn
        EXP = 5,// kinh nghiệm
        CURRENCY = 6,
        DURATION = 7,// thời gian 1 trận đấu
        SPAWN_RATE = 8,// tốc độ sinh quái
    }
    [System.Serializable]
    public class StatsProcess
    {
        public STATS Type;
        public int Level;
    }
    [System.Serializable]
    public class ShopProcess
    {
        public List<PackageProcess> Daily = new();
        public List<PackageProcess> Limit = new();
        public List<PackageProcess> Purchase = new();
    }
    [System.Serializable]
    public class PackageProcess
    {
        public int ID;// ID của gói
        public int Amount;// số lượng còn lại có thể mua của gói này
    }
}
