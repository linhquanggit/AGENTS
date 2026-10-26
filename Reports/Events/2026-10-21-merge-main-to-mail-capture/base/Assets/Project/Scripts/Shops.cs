﻿using DP.Tools;
using DP.Utilities;
using Games;
using I2.Loc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Shops", menuName = "Data/Shops")]
public class Shops : ScriptableObject
{
    private const string ITEM_RESOURCE_FOLDER = "Data/Shops";

    public static string /*KEY_OFFER_DAY_OF_YEAR = "1945.Shops.Offer.DayOfYear",*/
        KEY_OFFER_SHOW_INGAME_LEVEL10 = "1945.Shops.Offer.StarterPack.ShowLevel10",
        KEY_UPGRADE_OFFER_COUNT_ON_DAY = "1941.Shops.Offer.Upgrade.CountOnDay",
        FORMAT_KEY_GEM_PACK_FIRST_SALE = "1941.Shops.Gem[{0}].FirstSale";

    //
    
    public static int UpgradeOfferCountOnDay
    {
        get
        {
            var _rs = DPPlayerPrefs.GetInt(KEY_UPGRADE_OFFER_COUNT_ON_DAY, 0);
            return _rs;
        }
        set
        {
            if (value < 0) value = 0;
            DPPlayerPrefs.SetInt(KEY_UPGRADE_OFFER_COUNT_ON_DAY, value);
        }
    }

    public static bool IsShowOfferInGameLevel10
    {
        get
        {
            var _rs = DPPlayerPrefs.GetBool(KEY_OFFER_SHOW_INGAME_LEVEL10, false);
            return _rs;
        }
        set { DPPlayerPrefs.SetBool(KEY_OFFER_SHOW_INGAME_LEVEL10, value); }
    }

    //public static int OfferDayOfYear
    //{
    //    get
    //    {
    //        var _rs = PlayerPrefs.GetInt(KEY_OFFER_DAY_OF_YEAR, -1);
    //        return _rs;
    //    }
    //    set { PlayerPrefs.SetInt(KEY_OFFER_DAY_OF_YEAR, value); }
    //}
    static string OfferLastOpen
    {
        get
        {
            var _str = PlayerPrefs.GetString("1941.Shop.Offer.LastOpen");
            if (!DPUtils.ParseTicks(_str, out var _))
            {
                PlayerPrefs.SetString("1941.Shop.Offer.LastOpen", DPUtils.GetUtcNow.Ticks.ToString());
            }
            return PlayerPrefs.GetString("1941.Shop.Offer.LastOpen");
        }
        set { PlayerPrefs.SetString("1941.Shop.Offer.LastOpen", value); }
    }

    public static bool IsOfferResetNewDay
    {
        get // check new day 
        {
            //var _curDayOfYear = DPUtils.GetNow.DayOfYear;
            //DPDebug.Log("Shop Offer DayOfYear[" + OfferDayOfYear + "] - DayOfYear[" + _curDayOfYear + "]");
            //var _value = _curDayOfYear - OfferDayOfYear;
            //if (_value <= 1) return false; // not reset new day
            if (!DPUtils.IsNewDay(OfferLastOpen)) return false;
            // set new day of year
            //OfferDayOfYear = _curDayOfYear;
            OfferLastOpen = DPUtils.GetUtcNow.Ticks.ToString();
            return true;
        }
    }

    public static bool GetGemPackFirstSale(PACK_TYPE _type)
    {
        var _strKey = string.Empty;
        switch (_type)
        {
            case PACK_TYPE.GEM_2:
            case PACK_TYPE.GEM_3:
            case PACK_TYPE.GEM_6:
            case PACK_TYPE.GEM_9:
                _strKey = string.Format(FORMAT_KEY_GEM_PACK_FIRST_SALE, _type.ToString());
                break;
            default: _strKey = string.Empty; break;
        }

        if (string.IsNullOrEmpty(_strKey)) return false;
        //
        return DPPlayerPrefs.GetBool(_strKey, true);
    }

    public static void SetGemPackFirstOffSale(PACK_TYPE _type)
    {
        switch (_type)
        {
            case PACK_TYPE.GEM_2:
            case PACK_TYPE.GEM_3:
            case PACK_TYPE.GEM_6:
            case PACK_TYPE.GEM_9:
                var _strKey = string.Format(FORMAT_KEY_GEM_PACK_FIRST_SALE, _type.ToString());
                DPPlayerPrefs.SetBool(_strKey, false);
                break;
        }
    }

    //
    private static ResourceAsset<Shops> asset = new ResourceAsset<Shops>(ITEM_RESOURCE_FOLDER);

    //
    [SerializeField] private List<ConvertPackData> converts = new List<ConvertPackData>();
    [SerializeField] private List<GemPackData> gemPacks = new List<GemPackData>();
    [SerializeField] private List<GoldPackData> goldPacks = new List<GoldPackData>();

    [SerializeField]
    private List<OfferPackData> offerPacks = new List<OfferPackData>();

    [SerializeField]
    private List<OfferUpgradeData> offerUpgrade = new List<OfferUpgradeData>();
    //================================================================================================================

    #region [CONVERT]

    public static List<ConvertPackData> Converts => asset.Value.converts;
    public static ConvertPackData GetConvertPackData(CONVERT_PACKAGE _id)
    {
        if (Converts == null || Converts.Count == 0) return null;
        return Converts.FirstOrDefault(x => x.ID == _id);
    }

    #endregion

    #region [UPGRADE-OFFER]

    public static void OnClearAllUpgradeOffer()
    {
        UpgradeOfferCountOnDay = 0;
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return;
        //
        foreach (var _offer in _offers)
        {
            _offer.OnClear();
        }
    }

    // Reset all offer on new day
    public static void OnResetAllUpgradeOffer()
    {
        UpgradeOfferCountOnDay = 0;
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return;
        //
        foreach (var _offer in _offers)
        {
            if (_offer.IsBought) continue;
            _offer.OnReset();
        }
    }

    // Check show offer upgrade on day
    public static bool CheckShowUpgradeOfferOnDay(OFFER_UPGRADE_TYPE _offerID)
    {
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return false;
        //
        //DPDebug.Log("Check show upgrade offer[" + _offerID.ToString() + "]--1");
        if (UpgradeOfferCountOnDay >= 2) return false; // moi 1 ngay chi show cung luc nhieu nhat 2 goi
        //
        //DPDebug.Log("Check show upgrade offer[" + _offerID.ToString() + "]--2");
        var _offer = _offers.Find(x => x.OfferID == _offerID);
        if (_offer == null) return false;
        //
        DPDebug.Log($"Check show upgrade offer[{_offerID.ToString()}]---[{_offer.IsShow}]");
        return _offer.IsShow;
    }

    public static OfferUpgradeData GetUpgradeOfferDataOnDay()
    {
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return null;
        //
        List<OfferUpgradeData> _rs = new List<OfferUpgradeData>();
        foreach (var _offer in _offers)
        {
            if (!_offer.IsShowPopupOnHome) continue; // popup chua show --> return true
            _rs.Add(_offer);
        }

        if (_rs.Count == 0) return null;
        var _data = _rs[_rs.Count - 1];
        return _data;
    }

    // Get offers upgrade dataTutorial on day
    public static List<OfferUpgradeData> GetAllUpgradeOfferDataOnDay()
    {
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return null;
        //
        List<OfferUpgradeData> _rs = new List<OfferUpgradeData>();
        foreach (var _offer in _offers)
        {
            if (_offer.IsBought) continue;
            if (_offer.IsShowed) continue;
            if (!_offer.IsShow) continue;
            if (_offer.IsShowing)
                _rs.Add(_offer);
        }

        return _rs;
    }

    // Get offer upgrade dataTutorial with id
    public static OfferUpgradeData GetUpgradeOfferData(OFFER_UPGRADE_TYPE _offerID)
    {
        var _offers = asset.Value.offerUpgrade;
        if (_offers.Count == 0) return null;
        //
        var _offer = _offers.Find(x => x.OfferID == _offerID);
        return _offer;
    }

    #endregion

    #region [GOLD & GEM]

    // Get gem packs dataTutorial with Type
    public static GemPackData GetGemPackData(PACK_TYPE _type)
    {
        var _pack = asset.Value.gemPacks.Find(x => x.Type.Equals(_type));
        return _pack;
    }

    // Get ticket packs dataTutorial with Type
    public static GoldPackData GetGoldPackData(PACK_TYPE _type)
    {
        var _pack = asset.Value.goldPacks.Find(x => x.Type.Equals(_type));
        return _pack;
    }

    #endregion

    #region [PACKS]

    public static List<OfferPackData> OfferPacks => asset.Value.offerPacks;

    // Get offer packs dataTutorial with Type
    public static OfferPackData GetOfferPackData(PACK_TYPE _type)
    {
        var _pack = asset.Value.offerPacks.Find(x => x.Type.Equals(_type));
        return _pack;
    }

    // Get pack show in home
    public static OfferPackData GetPackOnHomeData()
    {
        var _packs = asset.Value.offerPacks;
        if (_packs == null || _packs.Count == 0) return null;
        //
        foreach (var _pack in _packs)
        {
            if (!_pack.IsUnlocked) continue;
            if (_pack.IsBought) continue;
            if (!_pack.UsePowerOffer) continue; // khong phai goi power thi bo qua
            if (_pack.IsShowed) continue;
            if (_pack.IsFighterOverRequier) continue; // fighter da 10 sao thi khong show
            if (!_pack.IsShowHome) continue; // k show on home
            //
            return _pack;
        }

        return null;
    }

    // Get all packs short on home
    public static List<OfferPackData> GetAllOfferPackOnHome()
    {
        var _packs = asset.Value.offerPacks;
        List<OfferPackData> _data = new List<OfferPackData>();
        List<OfferPackData> _packsBoughted = new List<OfferPackData>();
        List<OfferPackData> _powerOffers = new List<OfferPackData>();
        if (_packs.Count > 0)
        {
            foreach (var _pack in _packs)
            {
                if (!_pack.IsUnlocked) continue;
                if (_pack.UsePowerOffer)
                {
                    // power boost offer
                    if (!_pack.IsBought)
                    {
                        _powerOffers.Add(_pack);
                    }

                    continue;
                }

                if (_pack.IsBought)
                {
                    _packsBoughted.Add(_pack);
                    continue;
                }

                _data.Add(_pack);
            }
        }

        if (_powerOffers.Count > 0)
        {
            OfferPackData _pack = _powerOffers[0];
            //DPDebug.Log("Get pack power boost offer in home: " + _pack.Type.ToString());
            _data.Insert(0, _pack); // add vao vi tri dau tien
        }

        if (_packsBoughted.Count > 0) // add pack da mua
        {
            foreach (var _pack in _packsBoughted)
            {
                _data.Add(_pack);
            }
        }

        return _data;
    }

    // Get offer packs dataTutorial with Type
    public static OfferPackData RandomPackShowInGame()
    {
        var _packs = asset.Value.offerPacks;
        if (_packs == null || _packs.Count <= 0) return null;
        //
        List<OfferPackData> _packList = new List<OfferPackData>();
        foreach (var _pack in _packs)
        {
            if (_pack.UseShowShortcutInGame)
            {
                // check bought
                DPDebug.Log("pack[" + _pack.Type.ToString() + "]-IsBought[" + _pack.IsBought + "]");
                if (!_pack.IsBought)
                    _packList.Add(_pack);
            }
        }

        //
        DPDebug.Log("Pack show ingame count = " + _packList.Count);
        if (_packList.Count <= 0) return null;
        var _data = _packList.RandomElement();
        return _data;
    }

    #endregion
}

[System.Serializable]
public class ConvertPackData
{
    public CONVERT_PACKAGE ID;
    //
     public IAP_Product IDProduct;
     public IAP_Product IDProductOrigin;
     public FIGHTER_TYPE FighterID;
     public STAR_RANK StarRank;
     public int Star;
     public int StarLevel;
     public ItemConvertShort ItemPrefab;
     public double ExpireTime;// số giây hết hạn gói này
}
[System.Serializable]
public class GemPackData
{
    public PACK_TYPE Type;
    public IAP_Product IDProduct;
     public string Name;
     public int Value;
     public bool IsSale;

    
    public int ValueSale;
}

[System.Serializable]
public class GoldPackData
{
    public PACK_TYPE Type;
     public string Name;
     public PRICE_TYPE PriceType;

     public int Price;

    //
    GOLD_BY_CHAPTER_ID GoldByChapterID
    {
        get
        {
            switch (Type)
            {
                case PACK_TYPE.GOLD_1: return GOLD_BY_CHAPTER_ID.GOLD_SHOP_1;
                case PACK_TYPE.GOLD_2: return GOLD_BY_CHAPTER_ID.GOLD_SHOP_2;
                case PACK_TYPE.GOLD_3: return GOLD_BY_CHAPTER_ID.GOLD_SHOP_3;
                default: return GOLD_BY_CHAPTER_ID.NONE;
            }
        }
    }

    public int Value
    {
        get
        {
            if (GoldByChapterID == GOLD_BY_CHAPTER_ID.NONE)
            {
                DPDebug.LogWarning($"Get gold pack[{Type.ToString()}]--Gold by chapter ID is NONE--return 0");
                return 0;
            }

            var _gold = GoldByChapter.GetGoldByChapter(GoldByChapterID);
            DPDebug.Log($"Get gold pack[{Type.ToString()}]--gold[{_gold}]");
            return _gold;
        }
    }
}

[System.Serializable]
public class OfferPackData
{
    const string KEY_BOUGHT = "1945.Shop.Offer[{0}].Bought",
        KEY_SHOWED = "1945.Shop.Offer[{0}].IsShowed",
        FORMAT_KEY_TIME_START = "1941.Shop.Offer[{0}].TimeStart",
        FORMAT_KEY_FAKE_USER_BOUGHT = "1941.Shop.Offer[{0}].Fake.UserBoughted",
        FORMAT_KEY_FAKE_PACK_EXIST = "1941.Shop.Offer[{0}].Fake.PacksExist";

    //
    public PACK_TYPE Type;
    public IAP_Product IDProduct;
     public string Name;
     public LocalizedString LocalizedName;
     public bool IsUnlockedDefault = true;
     public GameObject PrefabShortcut;
     public bool OneTimePurchase = true;
     public bool UseSale = false;

    
    public IAP_Product IDProductOriginIos;

    
    public IAP_Product IDProductOriginAndroid;

    
    public int SaleValue = 0;

     public bool UseCountDown = false;

    
    public COUNT_DOWN_KEY CountDownKey = COUNT_DOWN_KEY.NONE;

    
    public int CountDownDuration = 0;

    
    public TimeType CountDownTimeType = TimeType.Minutes;

     public bool UseShowShortcutInGame = false;

    [SerializeField]
    GameObject prefabShortcutInGame;

    //
    
    public bool IsFighter = false;

    
    public FIGHTER_TYPE FighterID;

    
    public bool IsWingman = false;

    
    public WINGMAN_TYPE WingmanID;

    
    bool IsCharacter
    {
        get
        {
            if (IsFighter || IsWingman) return true;
            return false;
        }
    }

    
    public STAR_RANK StarRank = STAR_RANK.NORMAL;

    
    public int Star = 10;

    
    public int StarLevel = 10;

     public List<OfferRewardData> OfferRewards = new List<OfferRewardData>();
     public bool UsePowerOffer = false;

    [SerializeField]
    Vector2 fakeUserBoughted;

    [SerializeField]
    Vector2 fakePackExist;

    [SerializeField]
    int levelRequest;

     public bool IsShowHome = true;

    //
    
    public IAP_Product IDProductOrigin
    {
        get
        {
#if UNITY_IOS
            return IDProductOriginIos;
#else
            return IDProductOriginAndroid;
#endif
        }
    }

    public GameObject PrefabShortcutInGame
    {
        get
        {
            if (UseShowShortcutInGame)
                return prefabShortcutInGame;
            return null;
        }
    }

    
    public bool IsBought
    {
        get
        {
            if (!OneTimePurchase) return false;
            //
            if (Type == PACK_TYPE.REMOVE_ADS)
            {
                if (!SystemVariables.IsShowInterAds)
                    return true;
            }

            var _key = string.Format(KEY_BOUGHT, Type.ToString());
            var _isBought = DPPlayerPrefs.GetBool(_key, false);
            return _isBought;
        }
        private set
        {
            var _key = string.Format(KEY_BOUGHT, Type.ToString());
            DPPlayerPrefs.SetBool(_key, value);
        }
    }

    
    public bool IsUnlocked
    {
        get
        {
            if (UsePowerOffer) // offer power
            {
                var _data = FighterDataCache.Get(FighterID, AIRPORT_CONTEXT.NORMAL, InitMode.Standard);
                if (_data == null) return false;
                DPDebug.Log(
                    $"Check star unlock pack[{Type.ToString()}/{StarRank}/{Star}]--Process[{_data.StarRankCurrent}/{_data.StarCurrent}]");
                if ((int)_data.StarRankCurrent > (int)StarRank) return false; // star rank lon hon rank ban --> not show
                if ((int)_data.StarRankCurrent == (int)StarRank)
                    if (_data.StarCurrent >= Star)
                        return false; // max star --> not show
                //
                var _lvlMax = Level.LevelMax;
                if (_lvlMax >= levelRequest) return true;
                return false;
            }

            return IsUnlockedDefault;
        }
    }

    
    public bool IsShowed
    {
        get
        {
            var _key = string.Format(KEY_SHOWED, Type.ToString());
            var _rs = DPPlayerPrefs.GetBool(_key, false);
            return _rs;
        }
        private set
        {
            var _key = string.Format(KEY_SHOWED, Type.ToString());
            DPPlayerPrefs.SetBool(_key, value);
        }
    }

    public bool IsFighterOverRequier // khi fighter da duoc nang cap hon goi pack thi k show
    {
        get
        {
            var _data = FighterDataCache.Get(FighterID, AIRPORT_CONTEXT.NORMAL, InitMode.Standard);
            if (_data == null) return true;
            DPDebug.Log(
                $"Check star show popup pack[{Type.ToString()}]--star[{_data.StarRankCurrent}/{_data.StarCurrent}]");
            if (_data.StarRankCurrent > StarRank) return true; // star rank lon hon rank ban --> not show
            if (_data.StarRankCurrent == StarRank)
            {
                if (_data.StarCurrent >= Star) return true; // max star --> not show
            }

            return false;
        }
    }

    public virtual System.DateTime TimeStart
    {
        get
        {
            var _key = string.Format(FORMAT_KEY_TIME_START, Type.ToString());
            var _time = DPPlayerPrefs.GetDateTime(_key, DPUtils.GetNow);
            return _time;
        }
        set
        {
            var _key = string.Format(FORMAT_KEY_TIME_START, Type.ToString());
            if (DPPlayerPrefs.HasKey(_key))
                return;
            DPPlayerPrefs.SetDateTime(_key, value);
        }
    }

    public virtual int FakeUserBoughtCount
    {
        get
        {
            var _default = Random.Range((int)fakeUserBoughted.x, (int)fakeUserBoughted.y);
            var _key = string.Format(FORMAT_KEY_FAKE_USER_BOUGHT, Type.ToString());
            var _rs = DPPlayerPrefs.GetInt(_key, _default);
            return _rs;
        }
        set
        {
            var _key = string.Format(FORMAT_KEY_FAKE_USER_BOUGHT, Type.ToString());
            DPPlayerPrefs.SetInt(_key, value);
        }
    }

    public virtual int FakePackExistCount
    {
        get
        {
            var _default = Random.Range((int)fakePackExist.x, (int)fakePackExist.y);
            var _key = string.Format(FORMAT_KEY_FAKE_PACK_EXIST, Type.ToString());
            var _rs = DPPlayerPrefs.GetInt(_key, _default);
            return _rs;
        }
        set
        {
            var _key = string.Format(FORMAT_KEY_FAKE_PACK_EXIST, Type.ToString());
            DPPlayerPrefs.SetInt(_key, value);
        }
    }

    //
    public void OnShow()
    {
        if (!IsUnlocked) return;
        if (IsShowed) return;
        //
        IsShowed = true;
        TimeStart = DPUtils.GetNow;
        FakeUserBoughtCount = Random.Range((int)fakeUserBoughted.x, (int)fakeUserBoughted.y);
        FakePackExistCount = Random.Range((int)fakePackExist.x, (int)fakePackExist.y);
    }

    public void RestorePurchase()
    {
        DPDebug.Log($"Restor offer pack--[{Type.ToString()}]--1");
        if (IsBought)
            return;
        DPDebug.Log($"Restor offer pack--[{Type.ToString()}]--2");
        //
        if (OneTimePurchase)
        {
            IsBought = true;
        }

        if (IsFighter)
        {
            Fighter.Process _process = Fighter.GetProcess(FighterID);
            Fighter.Unlock(_process);
        }

        if (IsWingman)
        {
            Wingman.Process _process = Wingman.GetProcess(WingmanID);
            Wingman.Unlock(_process);
        }

        // add power boost
        AddPowerBoostOfferReward();
    }

    public void OnBought()
    {
#if UNITY_IOS
        if (!RemoteConfigManager.Config.data.IOS_InReview)
            if (!IsUnlocked)
                return;
#else
        if (!IsUnlocked)
            return;
#endif
        if (IsBought)
            return;
        //
        if (OneTimePurchase)
        {
            IsBought = true;
        }

        if (IsFighter)
        {
            Fighter.Process _process = Fighter.GetProcess(FighterID);
            Fighter.Unlock(_process);
        }

        if (IsWingman)
        {
            Wingman.Process _process = Wingman.GetProcess(WingmanID);
            Wingman.Unlock(_process);
        }

        // add reward
        AddRewardNormal();
        // add offer reward
        AddPowerBoostOfferReward();
    }

    public List<RewardCount> OpenContainer()
    {
        List<RewardCount> _rewards = new List<RewardCount>();
        if (OfferRewards.Count > 0)
        {
            foreach (var _reward in OfferRewards)
            {
                if (_reward.IsContainer)
                {
                    for (int i = 0; i < _reward.Count; i++)
                    {
                        // open all container
                        List<RewardCount> _containerRewards = _reward.OpenContainer();
                        if (_containerRewards != null && _containerRewards.Count > 0)
                        {
                            foreach (var _item in _containerRewards)
                            {
                                var _itemOld = _rewards.Find(x => x.Type == _item.Type);
                                if (_itemOld != null)
                                    _itemOld.Count += _item.Count;
                                else
                                    _rewards.Add(_item);
                            }
                        }
                    }
                }
            }
        }

        if (_rewards.Count <= 0) return null;
        // add container lstReward
        string _whereLogReport = "SHOP_OPEN_CONTAINER";
        Rewards.AddReward(_rewards, _whereLogReport: _whereLogReport);
        return _rewards;
    }

    void AddRewardNormal()
    {
#if UNITY_IOS
        if (RemoteConfigManager.Config.data.IOS_InReview) return;
#endif
        List<RewardCount> _rewards = new List<RewardCount>();
        if (OfferRewards.Count > 0)
        {
            foreach (var _reward in OfferRewards)
            {
                if (!_reward.IsContainer)
                {
                    switch (_reward.RewardType)
                    {
                        case REWARD_TYPE.NONE:
                        case REWARD_TYPE.REMOVE_ADS:
                            break;
                        default:
                            RewardCount _newReward = new RewardCount();
                            _newReward.Type = _reward.RewardType;
                            _newReward.Count = _reward.Count;
                            _rewards.Add(_newReward);
                            break;
                    }
                }
            }
        }

        if (_rewards.Count <= 0) return;
        //
        string _whereLogReport = "SHOP_OFFER_PACK";
        Rewards.AddReward(_rewards, _whereLogReport: _whereLogReport);
    }

    void AddPowerBoostOfferReward()
    {
        bool _isAdd = false;
        switch (Type)
        {
            case PACK_TYPE.POWER_BOOST_OFFER_1:
            case PACK_TYPE.POWER_BOOST_OFFER_2:
            case PACK_TYPE.POWER_BOOST_OFFER_3:
            case PACK_TYPE.POWER_BOOST_OFFER_4:
            case PACK_TYPE.POWER_BOOST_OFFER_5:
            case PACK_TYPE.POWER_BOOST_OFFER_6:
            case PACK_TYPE.POWER_BOOST_OFFER_7:
            case PACK_TYPE.POWER_BOOST_OFFER_8:
            case PACK_TYPE.POWER_BOOST_OFFER_9:
                _isAdd = true; break;
            default: _isAdd = false; break;
        }

        if (!_isAdd) return;
        // set fighter to 10 star
        Fighter.Process _process = Fighter.GetProcess(FighterID);
        if (!_process.IsUnlocked)
            _process.IsUnlocked = true;
        _process.StarRank = StarRank;
        _process.Star = Star;
        _process.StarLevel = StarLevel;
        Fighter.Update(_process);
        DPDebug.Log(
            $"Add power boost offer--fighter[{FighterID.ToString()}]--[{_process.StarRank}/{_process.Star}/{_process.StarLevel}]");
    }
}

[System.Serializable]
public class OfferRewardData
{
    public REWARD_TYPE RewardType = REWARD_TYPE.NONE;
     public int Count;

    
    public List<ContainerRewardData> ContainerRewards = new List<ContainerRewardData>();

    //
    public bool IsContainer
    {
        get
        {
            switch (RewardType)
            {
                case REWARD_TYPE.CONTAINER_ALL:
                case REWARD_TYPE.CONTAINER_WINGMAN:
                case REWARD_TYPE.CONTAINER_FIGHTER:
                    return true;
                default:
                    return false;
            }
        }
    }

    public List<RewardCount> OpenContainer()
    {
        if (!IsContainer) return null;
        if (ContainerRewards.Count <= 0) return null;
        //
        List<RewardCount> _rewards = new List<RewardCount>();
        for (int i = 0; i < ContainerRewards.Count; i++)
        {
            var _reward = ContainerRewards[i];
            List<RewardCount> _rewardsList = GetContainerReward(_reward);
            if (_rewardsList == null || _rewardsList.Count == 0) continue;
            //
            foreach (var _item in _rewardsList)
                _rewards.Add(_item);
        }

        return _rewards;
    }

    List<RewardCount> GetContainerReward(ContainerRewardData _containerReward)
    {
        List<RewardCount> _rewards = new List<RewardCount>();
        switch (_containerReward.Type)
        {
            case REWARD_TYPE.NONE:
                _rewards = null;
                break;
            case REWARD_TYPE.GOLD:
            case REWARD_TYPE.GEM:
            case REWARD_TYPE.ENERGY:
                RewardCount _reward = new RewardCount();
                _reward.Type = _containerReward.Type;
                _reward.Count = _containerReward.Value;
                _rewards.Add(_reward);
                break;
            // fighter rank
            case REWARD_TYPE.FIGHTER_RANK_N:
            case REWARD_TYPE.FIGHTER_RANK_R:
            case REWARD_TYPE.FIGHTER_RANK_E:
            case REWARD_TYPE.FIGHTER_RANK_S:
            // wingman rank
            case REWARD_TYPE.WINGMAN_RANK_N:
            case REWARD_TYPE.WINGMAN_RANK_R:
            case REWARD_TYPE.WINGMAN_RANK_E:
            case REWARD_TYPE.WINGMAN_RANK_S:
                List<Pieces.PieceCount> _pieces = _containerReward.GetPieces();
                if (_pieces.Count > 0)
                {
                    foreach (var _item in _pieces)
                    {
                        RewardCount _newReward = new RewardCount();
                        _newReward.Type = Rewards.Convert(_item.Type);
                        _newReward.Count = _item.Count;
                        _rewards.Add(_newReward);
                    }
                }

                break;
        }

        return _rewards;
    }
}

public enum PACK_TYPE
{
    GEM_1,
    GEM_2,
    GEM_3,
    GEM_4,
    GEM_5,
    GEM_6,
    GOLD_1,
    GOLD_2,
    GOLD_3,
    REMOVE_ADS,
    LIMITED_BUNDLE,
    STARTER_PACK,
    SUPPLY_PACK,
    BEGINNER_PACK,
    GEM_7,
    GEM_8,
    GEM_9,
    POWER_BOOST_OFFER_1,
    POWER_BOOST_OFFER_2,
    POWER_BOOST_OFFER_3,
    POWER_BOOST_OFFER_4,
    POWER_BOOST_OFFER_5,
    POWER_BOOST_OFFER_6,
    POWER_BOOST_OFFER_7,

    //Only for review IOS
    GEM_10,
    GEM_11,
    GEM_12,
    GEM_13,
    GEM_14,
    GEM_15,
    //End

    POWER_BOOST_OFFER_8,
    POWER_BOOST_OFFER_9,

    GEM_16,

    //
    NONE = 99,
}

public enum SHOP_TYPE
{
    NONE,
    MONEY,
    OFFER,
    CONTAINER,
    MODULE,
    EXCHANGE,
    CHIP
}