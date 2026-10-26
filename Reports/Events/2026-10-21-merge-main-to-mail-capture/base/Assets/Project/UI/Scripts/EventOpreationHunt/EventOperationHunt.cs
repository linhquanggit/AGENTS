using System;
using System.Collections;
using System.Collections.Generic;
using DP.Utilities;
using GameAnalyticsSdk;
using UnityEngine;

public static class EventOperationHunt
{
    private static DateTime EventStartTime;

    public static DateTime EventEndTime;

    public static TimeSpan TimeRemaining => EventEndTime - DPUtils.GetUtcNow;

    static EventOperationHunt()
    {
        var _process = Player.OperationHuntEventProcess;
        if (_process != null && _process.SavedEndTimeTicks != 0)
        {
            EventEndTime = _process.SavedEndTime;
            EventStartTime = EventEndTime.AddDays(-7);
            return;
        }

        OnRefresh();
    }

    public static void CheckAndReset(Action<bool, List<RewardCount>> _onClaimAction = null)
    {
        if (DPUtils.GetUtcNow < EventEndTime)
        {
            _onClaimAction?.Invoke(false, null);
            return;
        }

        var _isHaveRewards = false;
        // Đã hết tuần → auto claim các mốc còn lại( nếu có )
        AutoClaimAllBeforeReset(_onClaimAction, out _isHaveRewards);

        // Sau khi claim xong thì tạo tuần mới
        OnRefresh();
        // nếu ko có rewards gọi end -> show popup
        if (!_isHaveRewards)
            _onClaimAction?.Invoke(true, null); //
    }

    private static void InitEventTime()
    {
        DateTime now = DPUtils.GetUtcNow;
        DateTime lastReset = GetLastThursdayReset(now);
        DateTime nextReset = lastReset.AddDays(7);

        EventStartTime = lastReset;
        EventEndTime = nextReset;
    }

    private static void OnRefresh()
    {
        InitEventTime();

        var _process = new Process()
        {
            Index = -1,
            EnemyKillCount = 0,
            SavedEndTime = EventEndTime // lưu endTime tuần này
        };

        SaveData(_process);
    }

    public static void ClaimNextReward(Action<List<RewardCount>> _onClaimReward = null)
    {
        var _process = Player.OperationHuntEventProcess;
        if (_process == null) return;

        int _nextIndex = _process.Index + 1;
        if (_nextIndex >= EventOperationHuntData.Count) return;

        var _cfg = EventOperationHuntData.GetConfigByIndex(_nextIndex);
        if (_process.EnemyKillCount < _cfg.targetEnemyKillCount) return;
        _onClaimReward?.Invoke(_cfg.rewards);
        Rewards.AddReward(_cfg.rewards, _whereLogReport: AnalyticsWhere.EventOperationHuntClaimNext);
        _process.Index = _nextIndex;
        SaveData(_process);
    }


    private static void AutoClaimAllBeforeReset(Action<bool, List<RewardCount>> _onClaimAuto, out bool isHaveRewards)
    {
        isHaveRewards = false;
        var _process = Player.OperationHuntEventProcess;
        if (_process == null) return;

        var _idx = _process.Index;

        List<RewardCount> _finalRewards = new List<RewardCount>();

        while (_idx + 1 < EventOperationHuntData.Count)
        {
            var _cfg = EventOperationHuntData.GetConfigByIndex(_idx + 1);
            if (_cfg == null) break;

            if (_process.EnemyKillCount < _cfg.targetEnemyKillCount)
                break;

            if (!string.IsNullOrEmpty(_cfg.payID))
                break;

            // Claim reward free
            AddRewardsToList(_finalRewards, _cfg.rewards);
            _idx++;
        }

        _process.Index = _idx;
        if (_finalRewards.Count > 0)
        {
            isHaveRewards = true;
            Rewards.AddReward(_finalRewards, _whereLogReport: AnalyticsWhere.EventOperationHuntAutoClaimReset);
            _onClaimAuto?.Invoke(true, _finalRewards);
        }

        SaveData(_process);
    }

    private static void AddRewardsToList(List<RewardCount> rewardList, List<RewardCount> rewards)
    {
        foreach (var r in rewards)
        {
            AddRewardToList(rewardList, r);
        }
    }

    private static void AddRewardToList(List<RewardCount> rewardList, RewardCount reward)
    {
        var existing = rewardList.Find(r => r.Type == reward.Type);
        if (existing != null)
            existing.Count += reward.Count;
        else
            rewardList.Add((RewardCount)reward.Clone());
    }


    public static int IndexCurrent => Player.OperationHuntEventProcess.Index;
    public static int EnemyKillCount => Player.OperationHuntEventProcess.EnemyKillCount;

    private static DateTime GetLastThursdayReset(DateTime now)
    {
        // Tìm thứ 5 gần nhất
        int daysSinceThursday = (7 + (now.DayOfWeek - DayOfWeek.Thursday)) % 7;
        DateTime lastThursday = now.Date.AddDays(-daysSinceThursday).AddHours(8).AddMinutes(30);

        if (now < lastThursday)
            lastThursday = lastThursday.AddDays(-7);

        return lastThursday;
    }

    public static void AddEnemyKillCount(int _count = 1)
    {
        var _process = Player.OperationHuntEventProcess;
        if (_process == null)
            return;
        if (TimeRemaining.TotalSeconds <= 0) return;
        _process.EnemyKillCount += _count;
        SaveData(_process);
    }

    private static void SaveData(EventOperationHunt.Process _process)
    {
        Player.OperationHuntEventProcess = _process;
    }

    [Serializable]
    public class Process
    {
        public int Index = -1;
        public int EnemyKillCount;
        public long SavedEndTimeTicks; // lưu dưới dạng long

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