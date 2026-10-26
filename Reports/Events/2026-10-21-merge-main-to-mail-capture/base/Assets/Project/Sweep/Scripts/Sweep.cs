using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using DP.Tools;
using DP.Utilities;

public static class Sweep
{
    static string KEY_SWEEP_TIMES_LEFT = "Sweep_Times_Left";
    static string KEY_SWEEP_ENERGY_SPENDS = "Sweep_Energy_Spends";
    static string KEY_SWEEP_TIMES_USED = "Sweep_Times_Used";
    static string KEY_SWEEP_CURRENT_DAY = "Sweep_Current_Day";
    static string KEY_SWEEP_FIRST_TIME = "Sweep_First_Time"; 

    static Process Data;

    static Sweep()
    {
        Data = Player.SweepProcess;
        if (Data == null || Data.EndDate == "")
        {
            // set value default variables
            Data = new Process();
            Data.CurrentSweepSub = SweepType.Free;
            Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
            //
            DPDebug.Log("Khang: Sweep Data == null ");
            SaveData();
        }
    }

    public static void Refresh()
    {
        Data = Player.SweepProcess;
        if (Data == null)
        {
            Data.CurrentSweepSub = SweepType.Free;
            Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
            SaveData();
        }
    }

    public static Process GetProcess()
    {
        Data = Player.SweepProcess;
        if (Data == null || Data.EndDate == "")
        {
            // set value default variables
            Data = new Process();
            Data.CurrentSweepSub = SweepType.Free;
            Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
            //
            DPDebug.Log("Khang:: Sweep Data == null GetProcess");
            SaveData();
        }

        return Data;
    }

    static void SaveData()
    {
        Player.SweepProcess = Data;
    }

    public static readonly int MaxMultiplyValue = 10;
    public static readonly int[] MultiplySteps = { 1, 3, 5, 10 };
    public static readonly int EnergyPlusEachSteps = 2;

    public static DateTime GetEndDate()
    {
        var endDateStr = GetProcess().EndDate;
        if (string.IsNullOrEmpty(endDateStr))
        {
            DPDebug.Log("EndDate is null or empty, returning DateTime.Now");
            return DPUtils.GetUtcNow;
        }

        return DateTime.Parse(endDateStr, null, DateTimeStyles.RoundtripKind);
    }

    public static string SetEndDate(DateTime dateTime)
    {
        DPDebug.Log("Khang: Set End Date: " + dateTime.ToString("o"));
        return dateTime.ToString("o");
    }

    public static int EnergySpends
    {
        get
        {
            var _sweepTimeLeft = PlayerPrefs.GetInt(KEY_SWEEP_ENERGY_SPENDS, 3);
            return _sweepTimeLeft;
        }
        set { PlayerPrefs.SetInt(KEY_SWEEP_ENERGY_SPENDS, value); }
    }
    public static int EnergyTodayUsed
    {
        get
        {
            var _sweepTimeLeft = PlayerPrefs.GetInt(KEY_SWEEP_TIMES_USED, 0);
            return _sweepTimeLeft;
        }
        set { PlayerPrefs.SetInt(KEY_SWEEP_TIMES_USED, value); }
    }

    public static int SweepTimesLeft
    {
        get => Data.SweepTimeLeft;
        set
        {
            Data.SweepTimeLeft = value;
            SaveData();
        }
    }

    public static System.DateTime CurrentDate
    {
        get
        {
            if (string.IsNullOrEmpty(Data.CurrentDate))
            {
                Data.CurrentDate = DPUtils.GetUtcNow.Date.ToString();
                SaveData();
            }
            return DateTimeOffset.Parse(Data.CurrentDate).UtcDateTime;
        }
        set
        {
            Data.CurrentDate = value.ToString();
            SaveData();
        }
        // get
        // {
        //     var _time = DPPlayerPrefs.GetDateTime(KEY_SWEEP_CURRENT_DAY, DPUtils.GetNow);
        //     return _time;
        // }
        // set { DPPlayerPrefs.SetDateTime(KEY_SWEEP_CURRENT_DAY, value); }
    }

    public static bool IsFirstTime
    {
        get => Data.IsOpenFirstTime;
        set
        {
            Data.IsOpenFirstTime = value;
            SaveData();
        }
    }

    public static int GetSweepTimeLeft(SweepType sweepType)
    {
        return sweepType switch
        {
            SweepType.Free => 10,
            SweepType.Basic => 2,
            SweepType.Advance => 3,
            SweepType.Premium => 9999,
            _ => 1
        };
    }

    public static void PurchasedSweepSub(SweepType sweepType)
    {
        if (Data == null)
        {
            // set value default variables
            Data = new Process();
            Data.CurrentSweepSub = SweepType.Free;
            Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
            //
        }

        Data.CurrentSweepSub = sweepType;
        Data.EndDate = SetEndDate(DPUtils.GetUtcNow.AddDays(30));

        SweepTimesLeft = GetSweepTimeLeft(sweepType);
        
        ResetRewardLimit();

        SaveData();
        DPDebug.Log($"Khang: PurchasedSweepSub {Data.CurrentSweepSub} {sweepType} {GetEndDate()}");
    }

    public static void ResetRewardLimit()
    {
        foreach (var _rwLimit in Sweeps.RewardLimits)
        {
            // var _countRemain = Sweep.GetCurrentRwLimit(_rwLimit.Type);
            // var _addCount = _rwLimit.Count - _countRemain;
            SetRewardLimit(_rwLimit.Type, Sweeps.GetRewardLimit(_rwLimit.Type));
        }
    }

    public static void PurchasedSweepSub()
    {
        if (UIManager.Instance.IsHasPopup(PopupId.Sweep, out UISweep _uISweep))
        {
            SweepType sweepType = _uISweep._sweepSubscription.currentSweepType;

            if (Data == null)
            {
                // set value default variables
                Data = new Process();
                Data.CurrentSweepSub = SweepType.Free;
                Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
                //
            }

            Data.CurrentSweepSub = sweepType;
            Data.EndDate = SetEndDate(DPUtils.GetUtcNow.AddDays(30));

            SweepTimesLeft = GetSweepTimeLeft(sweepType);

            SaveData();

            DPDebug.Log($"Khang: PurchasedSweepSub {Data.CurrentSweepSub} {sweepType} {GetEndDate()}");
        }
    }

    public static void HandlePlusEnergySpends()
    {
        EnergyTodayUsed++;

        if (EnergyTodayUsed % EnergyPlusEachSteps == 0)
        {
            EnergySpends++;
            DPDebug.Log("Khang: EnergySpends " + EnergySpends);
        }
    }

    public static void ResetNewDay()
    {
        EnergyTodayUsed = 0;
        EnergySpends = 3;
        SweepTimesLeft = GetSweepTimeLeft(Data.CurrentSweepSub);
        
        ResetRewardLimit();

        var utcNow = DPUtils.GetUtcNow;
        DateTime nextUtcMidnight =
            new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        CurrentDate = nextUtcMidnight;
        SaveData();
        DPDebug.Log($"Khang: reset sweep new day {CurrentDate} {nextUtcMidnight}");
    }

    public static void ResetNewSubs()
    {
        Data = new Process();
        Data.CurrentSweepSub = SweepType.Free;
        Data.EndDate = SetEndDate(DPUtils.GetUtcNow);
        ResetNewDay();
        SaveData();
        DPDebug.Log("Khang: reset sweep new subs ");
    }

    public static void SetRewardLimit(REWARD_TYPE _type, int _count, bool _isAddNotSet = false)
    {
        if (Sweeps.GetRewardLimit(_type) < 0)
        {
            DPDebug.Log("(SetRewardLimit) reward unlimited::" + _type);
            return;
        }

        var _rwLimits = Data.RewardLimits;
        if (_rwLimits == null || _rwLimits.Count <= 0)
            CreateRewardLimit();
        
        foreach (var _rwLimit in _rwLimits)
        {
            if (_rwLimit.Type == _type)
            {
                if ((_rwLimit.Count + _count < 0 && _isAddNotSet) || (_count < 0 && !_isAddNotSet))
                {
                    _count = 0;
                    _isAddNotSet = false;
                }

                _rwLimit.SetCount(_count,  _isAddNotSet);
                SaveData();
                return;
            }
        }

        // TH ko tim thay trong player data -> them moi 
        var _newLimit = new RewardLimit();
        _newLimit.SetType(_type);
        _newLimit.SetCount(_count, _isAddNotSet);
        _rwLimits.Add(_newLimit);
        SaveData();
    }

    public static int GetCurrentRwLimit(REWARD_TYPE _type)
    {
        var _rwLimits = Data.RewardLimits;
        if (_rwLimits == null ||  _rwLimits.Count <= 0)
            CreateRewardLimit();
        foreach (var _rwLimit in _rwLimits)
        {
            if (_rwLimit.Type == _type)
            {
                return _rwLimit.Count;
            }
        }

        DPDebug.Log("(GetCurrentRwLimit) ko tim thay type hoac unlimited::" + _type);
        return 0;
    }

    private static void CreateRewardLimit()
    {
        foreach (var _rwLimit in Sweeps.RewardLimits)
        {
            var _newLimit = new RewardLimit();
            _newLimit.SetType(_rwLimit.Type);
            _newLimit.SetCount(_rwLimit.Count);
            Data.RewardLimits.Add(_newLimit);
        }
        SaveData();
    }

    [System.Serializable]
    public class Process
    {
        public bool IsOpenFirstTime;
        public int SweepTimeLeft = 1;
        public SweepType CurrentSweepSub;
        public string EndDate;
        public string CurrentDate;
        public List<RewardLimit> RewardLimits = new List<RewardLimit>();
    }

    [System.Serializable]
    public class RewardLimit
    {
        [SerializeField] private REWARD_TYPE type;
        [SerializeField] private int count;
        public REWARD_TYPE Type { get => type; private set => type = value; }
        public int Count { get => count; private set => count = value; }

        public void SetCount(int _count, bool _isAddNotSet = false)
        {
            if(_isAddNotSet)
                Count += _count;
            else
                Count = _count;
        }
        
        public void SetType(REWARD_TYPE _type)
        {
            Type = _type;
        }
    }
}