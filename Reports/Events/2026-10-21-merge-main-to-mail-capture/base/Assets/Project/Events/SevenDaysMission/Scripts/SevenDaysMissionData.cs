using System;
using System.Collections;
using System.Collections.Generic;
using Games;
using UnityEngine;

[CreateAssetMenu(menuName = "SevenDaysMissionData", fileName = "SevenDaysMissionData")]
public class SevenDaysMissionData : ScriptableObject
{
    const string FILE_PATH = "Data/SevenDaysMissionData";
    
    private static readonly ResourceAsset<SevenDaysMissionData> asset = new(FILE_PATH);

    [SerializeField] private int levelUnlock;
    public static int LevelUnlock => asset.Value.levelUnlock;

    [SerializeField] private int totalDays;
    public static int TotalDays => asset.Value.totalDays;

    [SerializeField] private List<SevenDaysTaskDict> taskDict;

    [SerializeField] private List<SevenDaysRewardDict> rewardDict;

    public static List<SevenDaysTaskDict> TaskDict => asset.Value.taskDict;
    
    public static List<SevenDaysRewardDict> RewardDict => asset.Value.rewardDict;

    public static SevenDaysRewardDict GetRewardDict(SEVEN_DAYS_MISSION_RW _rewardType)
    {
        foreach (var _rwDict in asset.Value.rewardDict)
        {
            if (_rewardType == _rwDict.Type) return _rwDict;
        }
        return null;
    }

    public static SevenDaysTaskDict GetTaskDict(TYPE_MISSION _taskType)
    {
        foreach (var _taskDict in asset.Value.taskDict)
        {
            if (_taskType == _taskDict.Type) return _taskDict;
        }
        return null;
    }

    [SerializeField] private List<SevenDaysMissionMilestone> milestones;
    public static List<SevenDaysMissionMilestone> Milestones => asset.Value.milestones;

    [SerializeField] private List<SevenDaysMissionDay> days;
    public static List<SevenDaysMissionDay> Days => asset.Value.days;

    public static List<SevenDaysMissionTask> GetTasks(int _dayId)
    {
        if(_dayId < 0 || _dayId >= Days.Count) return null;
        return Days[_dayId].Tasks;
    }

    public static SevenDaysMissionTask GetTask(int _dayId, int _taskId)
    {
        if(_dayId < 0 || _dayId >= Days.Count) return null;
        if(_taskId < 0 || _taskId >= GetTasks(_dayId).Count) return null;
        return GetTasks(_dayId)[_taskId];
    }
    
    public static List<SevenDaysMissionIap> GetIaps(int _dayId)
    {
        if(_dayId < 0 || _dayId >= Days.Count) return null;
        return Days[_dayId].Iaps;
    }

    public static SevenDaysMissionIap GetIap(int _dayId, int _iapId)
    {
        if(_dayId < 0 || _dayId >= Days.Count) return null;
        if(_iapId < 0 || _iapId >= GetIaps(_dayId).Count) return null;
        return GetIaps(_dayId)[_iapId];
    }

    public static SevenDaysMissionIap GetIap(IAP_Product _iapProduct)
    {
        foreach (var _day in Days)
        {
            foreach (var _iap in _day.Iaps)
            {
                if(_iap.IapName == _iapProduct) return _iap;
            }
        }
        return null;
    }

    public static SevenDaysMissionMilestone GetMilestone(int _milestoneId)
    {
        if(_milestoneId < 0 || _milestoneId >= Milestones.Count) return null;
        return Milestones[_milestoneId];
    }
    // public static SevenDaysMissionMilestone GetMilestone(int _id)
    // {
    //     if (_id < 0 || _id >= asset.Value.Milestones.Count) return null;
    //     return asset.Value.Milestones[_id];
    // }
    //
    // public static SevenDaysMissionTask GetTask(int _day)
    // {
    //     if (_day < 0 || _day >= asset.Value.Tasks.Count) return null;
    //     return asset.Value.Tasks[_day];
    // }
}

[Serializable]
public class SevenDaysMissionReward
{
    public SEVEN_DAYS_MISSION_RW Type;
    [SerializeField]
    private int Amount;
    [SerializeField]
    private int GoldMultiplier = 1;
    private bool isGold => Type == SEVEN_DAYS_MISSION_RW.GOLD;
    public int AmountByLv
    {
        get
        {
            if (Type != SEVEN_DAYS_MISSION_RW.GOLD) return Amount;
            return SevenDaysMission.GetGoldByChapter() * GoldMultiplier;
        }
    }
    public Sprite Sprite => SevenDaysMissionData.GetRewardDict(Type).Sprite;
    // public GameObject PrefabAward => SevenDaysMissionData.GetRewardDict(Type).PrefabAward;
}

[Serializable]
public class SevenDaysMissionTask
{
    public TYPE_MISSION Type;
    public int Count;
    public Sprite Sprite => SevenDaysMissionData.GetTaskDict(Type).Sprite;
    public string Description => SevenDaysMissionData.GetTaskDict(Type).Description;
    public List<SevenDaysMissionReward> Rewards;
}

[Serializable]
public class SevenDaysMissionMilestone
{
    public int PointRequire;
    [SerializeField] List<SevenDaysMissionReward> Rewards;
    [SerializeField] List<SevenDaysMissionReward> AlternateRewards;

    public List<SevenDaysMissionReward> GetRewards(bool isAlternate)
    {
        return isAlternate? AlternateRewards : Rewards;
    }
}

[Serializable]
public class SevenDaysMissionIap
{
    public IAP_Product IapName;
    public string PackName;
    public string LogId;
    public string LogCancelId;
    public List<SevenDaysMissionReward> Rewards;
}

[Serializable]
public class SevenDaysMissionDay
{
    [SerializeField] private int day;
    public List<SevenDaysMissionTask> Tasks;
    public List<SevenDaysMissionIap> Iaps;
}

[Serializable]
public class SevenDaysRewardDict
{
    public SEVEN_DAYS_MISSION_RW Type;
    public Sprite Sprite;
    // []
    // public string namePlane;
    //
    // private bool isPlane => Type is SEVEN_DAYS_MISSION_RW.WINGMAN_4 or SEVEN_DAYS_MISSION_RW.FIGHTER_6;
}

[Serializable]
public class SevenDaysTaskDict
{
    public TYPE_MISSION Type;
    public string Description;
    public Sprite Sprite;
}

[Serializable]
public class SevenDaysEvent
{
    public TYPE_7DAYS_EVENT Type;
    public int LevelUnlock;
    public int TotalDays;
    
}

public enum SEVEN_DAYS_MISSION_RW
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    CHIP_TICKET_NORMAL = 3,
    WINGMAN_4 = 4,
    FIGHTER_SPECIAL_PIECES = 5,
    AWARD_EPIC_RANDOM = 6,
    FIGHTER_6 = 7,
    EVENT_POINT = 8,
    FIGHTER_ULTRA_PIECES = 9,
    ENERGY = 10,
    BF_TICKET = 11,
    AWARD_COMMON_RANDOM = 12,
    AWARD_RARE_RANDOM = 13,
    AWARD_LEGEND_RANDOM = 14,
    AWARD_MYTHIC_RANDOM = 15,
    CHIP_TICKET_SPECIAL = 16,
    WINGMAN_SPECIAL_PIECES = 17,
}


