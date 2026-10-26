
using System;
using System.Collections.Generic;
using System.Linq;
using BreakInfinity;
using DP.Utilities;
using I2.Loc;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
[CreateAssetMenu(fileName = "HolidayNoels", menuName = "Data/HolidayNoels")]
#endif
public class HolidayNoels : ScriptableObject
{
    const string NOEL_RESOURCE_FOLDER = "Data/HolidayNoels";

    public HolidayType currentHolidayType;
    public int currentHolidayId;
    public List<HolidayInfo> holidayInfos;
    
    [SerializeField] public int capRewardInsideBossFight;
    [SerializeField] public int capRewardOutsideBossFight;
    [SerializeField] public List<BossNoelRewardCap> rewardCaps;
    [SerializeField] public List<BossNoelDataInit> bossWaveData;
    [SerializeField] public int playTime;
    [SerializeField] public int enemiesPerLevel;
    [SerializeField] public ItemBossFightEffect itemIntroPrefab;

    
    public List<HolidayNoels.HolidayNoelMilestoneReward> milestoneRewards;

    
    public LuckyWheelData.LuckyWheelConfig[] config;
    
    
    public LuckyWheelData.LuckyWheelConfig[] configCycle2;

    
    public int MilePrizeUltimate, MilePrizeUltimateCycle2;

    
    public int MilePrizeLimitReward;

     public Sprite[] arrTierFrames;
     public List<TypeBoxLuckyWheel> lstSortItemsShow;
     public MissionHolidayNoel[] missionsHolidayNoel;

     public GiftPackNoel[] giftPackNoels;

    private static HolidayNoels CurrentHolidayInfo => asset.Value;//.holidayInfos[asset.Value.currentHolidayId];

    // private void OnValidate()
    // {
    //     var _currentHoliday = holidayInfos.FirstOrDefault(x => x.HolidayType == currentHolidayType);
    //     currentHolidayId = holidayInfos.IndexOf(_currentHoliday);
    // }

    public static PopupId CurrentPopupId => PopupId.MidAutumnEvent;//CurrentHolidayInfo.PopupId;
    public static PopupId CurrentIntroId => PopupId.MidAutumnIntro;//CurrentHolidayInfo.IntroId;

    public int GetTierInternal(TypeBoxLuckyWheel type)
    {
        // Nếu cache chưa được khởi tạo thì build cache
        if (_tierCache == null)
        {
            _tierCache = new Dictionary<TypeBoxLuckyWheel, int>();

            if (CurrentHolidayInfo.config != null)
            {
                // Vòng lặp 1: Duyệt qua các Tier (LuckyWheelConfig)
                foreach (var tierCfg in CurrentHolidayInfo.config)
                {
                    // Lấy giá trị Tier (biến Level trong class của bạn)
                    int currentTier = tierCfg.Level;

                    // Kiểm tra mảng con LevelConfig có tồn tại không
                    if (tierCfg.LevelConfig != null)
                    {
                        // Vòng lặp 2: Duyệt qua các Item trong Tier đó
                        foreach (var itemCfg in tierCfg.LevelConfig)
                        {
                            // Map: Key = Type -> Value = Level (Tier)
                            // Dùng ContainsKey để tránh lỗi nếu 1 item lỡ tay config ở 2 nơi (lấy cái đầu tiên tìm thấy)
                            if (!_tierCache.ContainsKey(itemCfg.Type))
                            {
                                _tierCache.Add(itemCfg.Type, currentTier);
                            }
                        }
                    }
                }
            }
        }

        // Tìm trong cache trả về kết quả
        if (_tierCache.TryGetValue(type, out int tier))
        {
            return tier;
        }

        // Trả về 0 hoặc -1 nếu không tìm thấy config cho item này
        return 0;
    }
    private static readonly ResourceAsset<HolidayNoels> asset = new ResourceAsset<HolidayNoels>(NOEL_RESOURCE_FOLDER);
    public static List<BossNoelDataInit> BossWaveData => CurrentHolidayInfo.bossWaveData;
    public static int CapRewardInsideBossFight => CurrentHolidayInfo.capRewardInsideBossFight;

    public static int CapRewardOutsideBossFight
    {
        get
        {
            if(Firebase_Analytics.current == null) return CurrentHolidayInfo.capRewardOutsideBossFight;
            return Firebase_Analytics.current.dailyCapHolidayLantern;
        }
    }
    public static int PlayTime => CurrentHolidayInfo.playTime;
    public static int EnemiesPerLevel => CurrentHolidayInfo.enemiesPerLevel;
    public static ItemBossFightEffect IntroPrefab => CurrentHolidayInfo.itemIntroPrefab;

    public static List<HolidayNoels.HolidayNoelMilestoneReward> MilestoneRewards => CurrentHolidayInfo.milestoneRewards;

    public static LuckyWheelData.LuckyWheelConfig[] Config(bool _isCycle2)
    {
        return _isCycle2 ? CurrentHolidayInfo.configCycle2 : CurrentHolidayInfo.config;
    }

    public static List<BossNoelRewardCap> RewardCaps => CurrentHolidayInfo.rewardCaps;

    public static List<TypeBoxLuckyWheel> GetLstSort()
    {
        return CurrentHolidayInfo.lstSortItemsShow;
    }

    public static int MilePrizeUltimateValue(bool _isCycle2) //=> CurrentHolidayInfo.MilePrizeUltimate;
    {
        return _isCycle2 ? asset.Value.MilePrizeUltimateCycle2 : asset.Value.MilePrizeUltimate;
    }

    public static int MilePrizeLimitRewardValue => CurrentHolidayInfo.MilePrizeLimitReward;

    public static MissionHolidayNoel[] MissionsHolidayNoel => CurrentHolidayInfo.missionsHolidayNoel;

    public static GiftPackNoel[] GiftPackNoels => CurrentHolidayInfo.giftPackNoels;

    public static HolidayNoels.GiftPackNoel GetGiftPackNoel(int _id)
    {
        var _giftPack = CurrentHolidayInfo.giftPackNoels.FirstOrDefault(x => x.ID == _id);
        return _giftPack;
    }

    public static HolidayNoels.GiftPackNoel GetGiftPackNoelByProduct(IAP_Product _product)
    {
        return CurrentHolidayInfo.giftPackNoels.FirstOrDefault(x => x.IAP_Product == _product);
    }

    public static HolidayNoels.MissionHolidayNoel GetMission(int _id)
    {
        var _mission = CurrentHolidayInfo.missionsHolidayNoel.FirstOrDefault(x => x.ID == _id);
        return _mission;
    }

    public static Sprite GetTierFrame(TypeBoxLuckyWheel _type, REWARD_TYPE _rewardType = REWARD_TYPE.NONE)
    {
        int tier = HolidayNoels.GetTier(_type);
        DPDebug.LogWarning($"GetTierFrame - Type: {_type}, Tier: {tier}, RewardType: {_rewardType}");

        var arr = CurrentHolidayInfo.arrTierFrames;
        if (arr == null || arr.Length == 0)
            return null;

        if (_type == TypeBoxLuckyWheel.ItemLuckyWheel10)

            return arr[1];
        else if (_type == TypeBoxLuckyWheel.ItemLuckyWheel13 || _type == TypeBoxLuckyWheel.ItemLuckyWheel14)
            return arr[2];
        return arr[0];
    }

    public static int GetTier(TypeBoxLuckyWheel type) => asset.Value.GetTierInternal(type);

    public static int GetIndexInSort(TypeBoxLuckyWheel _type)
    {
        var lstSort = CurrentHolidayInfo.lstSortItemsShow;
        if (lstSort == null || lstSort.Count == 0)
            return -1;
        return lstSort.IndexOf(_type);
    }
    // public static LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig GetLevelConfig(TypeBoxLuckyWheel _type)
    // {
    //     var config = CurrentHolidayInfo.config;
    //     if (config == null || config.Length == 0)
    //         return null;
    //
    //     foreach (var tierCfg in config)
    //     {
    //         if (tierCfg.LevelConfig != null)
    //         {
    //             foreach (var itemCfg in tierCfg.LevelConfig)
    //             {
    //                 if (itemCfg.Type == _type)
    //                     return itemCfg;
    //             }
    //         }
    //     }
    //     return null;
    //
    // }
    public static bool IsOutOfMaxLevel(int _level)
    {
        var _lastLevel = BossWaveData.Last();
        if (_lastLevel == null) return false;
        return _lastLevel.Level < _level;
    }
    public static bool IsLimitBoxType(TypeBoxLuckyWheel type)
    {
        // Hoặc check trong tier 3
        return type == TypeBoxLuckyWheel.ItemLuckyWheel11 ||
                type == TypeBoxLuckyWheel.ItemLuckyWheel12 ||
                type == TypeBoxLuckyWheel.ItemLuckyWheel13 ||
               type == TypeBoxLuckyWheel.ItemLuckyWheel14;
    }

    public static void SetConfigs(LuckyWheelData.LuckyWheelConfig[] _configs, bool _isCycle2)
    {
        if(_isCycle2) asset.Value.configCycle2 = _configs;
        else asset.Value.config = _configs;
    }
    
    public static void SetMilestones(List<HolidayNoelMilestoneReward> _milestones)
    {
        asset.Value.milestoneRewards = _milestones;
    }

    private Dictionary<TypeBoxLuckyWheel, int> _tierCache;

    public void RandomizeSortList()
    {
        List<TypeBoxLuckyWheel> targetList = CurrentHolidayInfo.lstSortItemsShow;
        if (targetList == null) targetList = new List<TypeBoxLuckyWheel>();
        targetList.Clear();
        //
        // 2. Tạo danh sách các item cần random (loại trừ 13, 14 và NONE)
        List<TypeBoxLuckyWheel> pool = new List<TypeBoxLuckyWheel>();
        foreach (TypeBoxLuckyWheel type in Enum.GetValues(typeof(TypeBoxLuckyWheel)))
        {
            if (type != TypeBoxLuckyWheel.NONE &&
                type != TypeBoxLuckyWheel.ItemLuckyWheel13 &&
                type != TypeBoxLuckyWheel.ItemLuckyWheel14)
            {
                pool.Add(type);
            }
        }
        // 3. Trộn ngẫu nhiên danh sách pool (Fisher-Yates shuffle)
        for (int i = 0; i < pool.Count; i++)
        {
            TypeBoxLuckyWheel temp = pool[i];
            int randomIndex = UnityEngine.Random.Range(i, pool.Count);
            pool[i] = pool[randomIndex];
            pool[randomIndex] = temp;
        }
        // 4. Chọn 12 item đầu tiên từ danh sách đã trộn và thêm vào targetList
        if (pool.Count < 12)
        {
            Debug.LogError("Không đủ item trong Enum để tạo danh sách (cần ít nhất 12 item ngẫu nhiên).");
            return;
        }
        for (int i = 0; i < 6; i++)
        {
            targetList.Add(pool[i]);
        }
        targetList.Add(TypeBoxLuckyWheel.ItemLuckyWheel13);
        for (int i = 6; i < 12; i++)
        {
            targetList.Add(pool[i]);
        }
        targetList.Add(TypeBoxLuckyWheel.ItemLuckyWheel14);
    }

    public void FakeData()
    {
        bossWaveData = new List<BossNoelDataInit>();
        for (int i = 0; i < 10; i++)
        {
            BossNoelDataInit _data = new BossNoelDataInit();
            _data.Level = i + 1;
            _data.HP = 100000;
            _data.DMG = 100;
            _data.BodyDMG = -1;
            _data.CurrencyCount = i * 5 + 5;
            bossWaveData.Add(_data);
        }
    }

    private void DeletePlayerData()
    {
        Player.HolidayNoelProcess = null;
        SceneManager.LoadScene(0);
    }
    
    [System.Serializable]
    public class BossNoelRewardCap
    {
        public int Level;
        public double MaxTotalReward;
    }

    [System.Serializable]
    public class BossNoelDataInit
    {
        public int Level;
        public BigDouble HP;
        public BigDouble DMG;
        public BigDouble BodyDMG;
        public double CurrencyCount;
    }

    [System.Serializable]
    public class HolidayNoelMilestoneReward
    {
        public int Mile;
        public REWARD_TYPE RewardType;
        public int Count;
    }

    [Serializable]
    public class MissionHolidayNoel
    {
        public int ID;
        public Mission mission;
        public LocalizedString description;

        public RewardCount reward;
    }

    [Serializable]
    public class GiftPackNoel
    {
        public int ID;
        public int Value;

        public bool IsLimitedPack;

        public IAP_Product IAP_Product;
    }
    
    [Serializable]
    public class HolidayInfo
    {
        public HolidayType HolidayType;
         public PopupId PopupId;
         public PopupId IntroId;
         public Sprite SprIcon;
        [SerializeField] public int capOverallRewardLevel;
        [SerializeField] public List<BossNoelRewardCap> rewardCaps;
        [SerializeField] public List<BossNoelDataInit> bossWaveData;
        [SerializeField] public int playTime;
        [SerializeField] public int enemiesPerLevel;
        [SerializeField] public ItemBossFightEffect itemIntroPrefab;

        
        public List<HolidayNoels.HolidayNoelMilestoneReward> milestoneRewards;

        
        public LuckyWheelData.LuckyWheelConfig[] config;

        
        public int MilePrizeUltimate;

        
        public int MilePrizeLimitReward;

         public Sprite[] arrTierFrames;
         public List<TypeBoxLuckyWheel> lstSortItemsShow;
         public MissionHolidayNoel[] missionsHolidayNoel;

         public GiftPackNoel[] giftPackNoels;
    }
}

public enum HolidayType
{
    NONE = 0,
    NOEL = 1,
    RAMADAN = 2,
    MIDAUTUMN = 3,
}
