using System;
using System.Collections;
using System.Collections.Generic;
using DP.Utilities;
using Games;
using UnityEngine;

public static class PiggyBank
{
    private static Process Data;

    private static DateTime EventEndTime;

    public static bool IsCanShowPassMile => !IsShowedPassMile &&
                                            Data.GemCollected >= PiggyBankData.DataPiggyBanks
                                                .PiggyLevels[Data.LevelPiggy].MileStonesGem1;

    public static bool IsCanShowFull => !IsShowedPassFull && Data.GemCollected >= PiggyBankData.DataPiggyBanks
        .PiggyLevels[Data.LevelPiggy].MileStonesGem2;


    public static bool IsShowedNewDay
    {
        get => SystemVariables.PiggyBankShowedNewDay;
        set => SystemVariables.PiggyBankShowedNewDay = value;
    }

    public static bool IsShowedWhenReset
    {
        get => SystemVariables.PiggyBankShowedWhenReset;
        set => SystemVariables.PiggyBankShowedWhenReset = value;
    }

    public static bool IsShowedPassMile
    {
        get => SystemVariables.PiggyBankShowedPassMile(Data.LevelPiggy);
        set => SystemVariables.SetPiggyBankShowedPassMile(Data.LevelPiggy, value);
    }

    public static bool IsShowedPassFull
    {
        get => SystemVariables.PiggyBankShowedFull(Data.LevelPiggy);
        set => SystemVariables.SetPiggyBankShowedFull(Data.LevelPiggy, value);
    }


    public static TimeSpan TimeRemaining => Data.SavedEndTime - DPUtils.GetUtcNow;

    public static int EnemyKilledOnMatch = 0;

    public static int LevelPiggy
    {
        set
        {
            if (!IsMax)
                Data.LevelPiggy = value;
        }
        get => Data.LevelPiggy;
    }

    public static int GemCollected => Data.GemCollected;

    public static bool BoughtAtMax => Data.BoughtAtMax;

    public static bool IsMax => Data.LevelPiggy == PiggyBankData.DataPiggyBanks.PiggyLevels.Count - 1;

    public static bool IsOpened()
    {
        var _data = PiggyBankData.LevelData(Data.LevelPiggy);
        return Data.GemCollected >= _data.MileStonesGem1;
    }

    static PiggyBank()
    {
        Data = Player.PiggyBankProcess;
        if (Data != null && Data.SavedEndTimeTicks != 0)
        {
            EventEndTime = Data.SavedEndTime;
            if (TimeRemaining.TotalSeconds <= 0)
                OnRefresh();
            return;
        }

        OnRefresh();
    }

    public static void Refresh()
    {
        Data = Player.PiggyBankProcess;
        if (Data == null)
            OnRefresh();
    }

    static void OnRefresh()
    {
        var _endTime = DPUtils.GetUtcNow.AddDays(7);

        Data = new Process
        {
            LevelPiggy = 0,
            GemCollected = 0,
            BoughtAtMax = false,
            SavedEndTime = _endTime
        };
        for (int i = 0; i < PiggyBankData.Count(); i++)
        {
            SystemVariables.SetPiggyBankShowedPassMile(i, false);
            SystemVariables.SetPiggyBankShowedFull(i, false);
        }

        SaveData();
    }

    public static void CollectGems(int _multiplier = 1)
    {
        if (BoughtAtMax)
            return;
        Data.GemCollected += (PiggyBankData.GemPerEnemy(Data.LevelPiggy) * _multiplier);
        var _gemMaxAtLv = PiggyBankData.LevelData(LevelPiggy).MileStonesGem2;
        if (Data.GemCollected >= _gemMaxAtLv)
            Data.GemCollected = _gemMaxAtLv;
        SaveData();
    }

    #region IN GAME

    public static void OnResetInGame()
    {
        EnemyKilledOnMatch = 0;
    }

    public static void OnCollectGems()
    {
        if (BoughtAtMax)
            return;
        EnemyKilledOnMatch++;
    }

    public static void OnCalculateMatch()
    {
       // DPDebug.Log($"=======[Piggy Bank] ENEMY KILLED IN GAME:{EnemyKilledOnMatch}");
        CollectGemBySweep(EnemyKilledOnMatch);
    }

    #endregion


    public static void CollectGemBySweep(int _enemeyKilled)
    {
        if (BoughtAtMax)
            return;
        var _data = PiggyBankData.FactorEnemySweepCollect(Data.LevelPiggy);
        var _mile = IsOpened() ? _data._opened : _data._notOpen;
        var _gemAdd = (int)((float)_enemeyKilled / (float)_mile);
        Data.GemCollected += _gemAdd;
        var _gemMaxAtLv = PiggyBankData.LevelData(LevelPiggy).MileStonesGem2;
        if (Data.GemCollected >= _gemMaxAtLv)
            Data.GemCollected = _gemMaxAtLv;
        SaveData();
    }

    public static void OnBoughtIAPAndUpLevel()
    {
        if (IsMax)
            Data.BoughtAtMax = true;
        LevelPiggy++;
        if (LevelPiggy >= 2)
            LevelPiggy = 2;
        Player.AddGem(GemCollected);
        Data.GemCollected = 0;

        SaveData();
    }

    private static void SaveData()
    {
        Player.PiggyBankProcess = Data;
    }

    [Serializable]
    public class Process
    {
        public int LevelPiggy;
        public long SavedEndTimeTicks;
        public int GemCollected;
        public bool BoughtAtMax;

        [NonSerialized] private DateTime _cachedEndTime;

        public DateTime SavedEndTime
        {
            get
            {
                if (_cachedEndTime == default)
                    _cachedEndTime = new DateTime(SavedEndTimeTicks, DateTimeKind.Utc);
                return _cachedEndTime;
            }
            set
            {
                SavedEndTimeTicks = value.Ticks;
                _cachedEndTime = value;
            }
        }
    }
}