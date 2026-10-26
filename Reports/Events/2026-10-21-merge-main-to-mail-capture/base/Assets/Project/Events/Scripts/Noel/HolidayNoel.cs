using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;
using DP.Utilities;
using GameAnalyticsSdk;
using System.Linq;
using static DailyMission;
using GoogleMobileAds.Api;
using Games;

public static class HolidayNoel
{

    public static bool IsShowOnHome = false;

    // Remote config time (lấy từ server)
    public static DateTime RemoteStartTime;
    public static DateTime RemoteEndTime;

    private static HashSet<TypeBoxLuckyWheel> _bossItems = null;
    public static TimeSpan TimeRemaining => RemoteEndTime - DPUtils.GetUtcNow;

    public static bool IsEventActive => Player.HolidayNoelProcess?.IsEventActive ?? false;

    public static bool IsPassedLevelRequireOpen => Games.Level.LevelMax >= 35;

    public static bool IsRemoteShowing => RemoteConfigManager.Config.data.Holiday_Noel_IsActive;

    public static bool IsCycle2 => Player.HolidayNoelProcess.IsCycle2;

    private static List<HolidayNoels.HolidayNoelMilestoneReward> milestonesByProgress;
    private static List<LuckyWheelData.LuckyWheelConfig> wheelConfigsByProgress, wheelConfigsByProgressCycle2;

    static HolidayNoel()
    {
        InitializeFromRemote();
        // OnEventStart();

    }

    public static void Refresh()
    {
        InitializeFromRemote();
    }

    public static void InitializeFromRemote()
    {
        if (!IsPassedLevelRequireOpen)
            return;
        var _timeStartRemote = RemoteConfigManager.Config.data.Mid_Autumn_EventStart;
        DPDebug.Log($"[HolidayNoel] Remote config event start time: {_timeStartRemote}");
        // var _timeStartRemote = "2026-03-01T00:00:00Z";
        if (DPUtils.TryParseRemoteDateTime(_timeStartRemote, out DateTime _startTime))
        {
            RemoteStartTime = _startTime;
            RemoteEndTime = _startTime.AddDays(32);
            DPDebug.Log($"[HolidayNoel] Remote Start Time: {RemoteStartTime}, End Time: {RemoteEndTime}, Current Time: {DPUtils.GetUtcNow}");
            CheckAndActivateEvent();
        }
    }

    /// <summary>
    /// Kiểm tra và kích hoạt event nếu trong thời gian
    /// </summary>
    private static void CheckAndActivateEvent()
    {
        DateTime now = DPUtils.GetUtcNow;
        bool isInEventTime = now >= RemoteStartTime && now < RemoteEndTime;

        var _process = Player.HolidayNoelProcess;

        // Trường hợp 1: Event đang trong thời gian VÀ chưa active -> Kích hoạt
        if (isInEventTime && (_process == null || !_process.IsEventActive))
        {
            // Nếu process tồn tại và cùng EventStartTimeTicks -> thiết bị bị lỗi giờ tạm thời,
            // chỉ reactivate lại mà KHÔNG reset data
            if (_process != null && _process.EventStartTimeTicks == RemoteStartTime.Ticks)
            {
                DPDebug.Log("🎄 Event Holiday Noel Reactivated (same event, time error recovered)!");
                _process.IsEventActive = true;
                InitializeMissions(_process);
                InitlizeGiftPack(_process);
                SaveData(_process);
                return;
            }
            DPDebug.Log("🎄 Event Holiday Noel Started!");
            OnEventStart();
            return;
        }

        // Trường hợp 2: Event hết thời gian VÀ đang active -> Kết thúc
        if (!isInEventTime && _process != null && _process.IsEventActive)
        {
            DPDebug.Log("🎄 Event Holiday Noel Ended!");
            OnEventEnd();
            return;
        }

        // Trường hợp 3: Event đang active và vẫn trong thời gian -> Không làm gì
        if (isInEventTime && _process != null && _process.IsEventActive)
        {
            DPDebug.Log("🎄 Event Holiday Noel is running...");
            // Đảm bảo EventStartTimeTicks được set (cho player cũ chưa có field này)
            if (_process.EventStartTimeTicks == 0)
            {
                _process.EventStartTimeTicks = RemoteStartTime.Ticks;
            }
            InitializeMissions(_process);
            InitlizeGiftPack(_process);
            // DPDebug.Log("[CheckEventStatus] GIFT PACK LIST CHECK ----> " + _process.lstGiftPackProcess.Count);
            
            // sang ngay moi -> set daily point earn
            if (DPUtils.GetUtcNow.Date > GetCurrentDate())
            {
                SetCurrentDate(_process);
                SetDailyPointEarnNonBoss(0);
            }

            if (_process.Group == PROGRESSION_GROUP.NONE) SetProgressionGroup();
            
            SaveData(_process);
        }
    }

    private static void SetProgressionGroup()
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;
        _process.Group = RewardByProgressionSystem.CurrentGroup;
    }

    public static List<HolidayNoels.HolidayNoelMilestoneReward> GetMilestone()
    {
        if(milestonesByProgress != null) return milestonesByProgress;
        var _milestones = new List<HolidayNoels.HolidayNoelMilestoneReward>();
        foreach (var _milestone in HolidayNoels.MilestoneRewards)
        {
            _milestones.Add(new HolidayNoels.HolidayNoelMilestoneReward
            {
                Mile = _milestone.Mile,
                RewardType = _milestone.RewardType,
                Count = GetRewardBpCount(_milestone.RewardType, _milestone.Count),
            });
        }

        milestonesByProgress = _milestones;
        return _milestones;
    }

    public static List<LuckyWheelData.LuckyWheelConfig> GetWheelConfig()
    {
        bool _isCycle2 = Player.HolidayNoelProcess.IsCycle2;
        if (_isCycle2)
        {
            if(wheelConfigsByProgressCycle2 != null) return wheelConfigsByProgressCycle2;
        }
        else if (wheelConfigsByProgress != null)
        {
            return wheelConfigsByProgress;
        }
        
        var _configs = new List<LuckyWheelData.LuckyWheelConfig>();
        foreach (var _config in HolidayNoels.Config(_isCycle2))
        {
            _configs.Add(new LuckyWheelData.LuckyWheelConfig
            {
                Level = _config.Level,
                Probability =  _config.Probability,
            });
            var _levelConfigs = new List<LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig>();
            foreach (var _lvConfig in _config.LevelConfig)
            {
                var _newLvConfig = new LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig
                {
                    Type = _lvConfig.Type,
                    Probability = _lvConfig.Probability,
                    Rewards = new List<RewardCount>(),
                };
                foreach (var _reward in _lvConfig.Rewards)
                {
                    _newLvConfig.Rewards.Add(new RewardCount
                    {
                        Type = _reward.Type,
                        Count =  GetRewardBpCount(_reward.Type, _reward.Count),
                    });
                }
                _levelConfigs.Add(_newLvConfig);
            }
            _configs[^1].LevelConfig =  _levelConfigs.ToArray();
        }

        if(_isCycle2) wheelConfigsByProgressCycle2 = _configs;
        else wheelConfigsByProgress = _configs;

        return _configs;
    }

    private static int GetRewardBpCount(REWARD_TYPE _type, int _count)
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return _count;
        var _group = _process.Group;
        if (_group == PROGRESSION_GROUP.NONE)
            _group = RewardByProgressionSystem.CurrentGroup;
        var _typeBP = GetRewardTypeBP(_type);
        if (RewardByProgressionSystem.IsProgressionReward(_typeBP))
        {
            var _rw = RewardByProgressionSystem.GetRewardCount(_typeBP, _count, _group);
            if (_typeBP == REWARD_TYPE.BP_GOLD)
            {
                return GoldByChapter.GetGoldByChapterWithMultiply(GOLD_BY_CHAPTER_ID.EVENT_CHRISTMAS, _rw.Count);
            }

            return _rw.Count;
        }

        return _count;
    }

    private static REWARD_TYPE GetRewardTypeBP(REWARD_TYPE _type)
    {
        var _typeBP = _type switch
        {
            REWARD_TYPE.GOLD => REWARD_TYPE.BP_GOLD,
            REWARD_TYPE.GEM => REWARD_TYPE.BP_GEM,
            REWARD_TYPE.FIGHTER_SPECIAL_PIECES => REWARD_TYPE.BP_FIGHTER_SPECIAL_PIECES,
            REWARD_TYPE.FIGHTER_ULTRA_PIECES => REWARD_TYPE.BP_FIGHTER_ULTRA_PIECES,
            REWARD_TYPE.WINGMAN_SPECIAL_PIECES => REWARD_TYPE.BP_WINGMAN_SPECIAL_PIECES,
            REWARD_TYPE.WINGMAN_ULTRA_PIECES => REWARD_TYPE.BP_WINGMAN_ULTRA_PIECES,
            REWARD_TYPE.REWARD_CONTAINER_WINGMAN => REWARD_TYPE.BP_REWARD_CONTAINER_WINGMAN,
            REWARD_TYPE.REWARD_CONTAINER_FIGHTER => REWARD_TYPE.BP_REWARD_CONTAINER_FIGHTER,
            REWARD_TYPE.ENERGY => REWARD_TYPE.BP_ENERGY,
            REWARD_TYPE.CHIP_TICKET_NORMAL => REWARD_TYPE.BP_CHIP_TICKET_NORMAL,
            REWARD_TYPE.CHIP_TICKET_SPECIAL => REWARD_TYPE.BP_CHIP_TICKET_SPECIAL,
            REWARD_TYPE.AWARD_COMMON_RANDOM => REWARD_TYPE.BP_AWARD_COMMON_RANDOM,
            REWARD_TYPE.AWARD_RARE_RANDOM => REWARD_TYPE.BP_AWARD_RARE_RANDOM,
            REWARD_TYPE.AWARD_EPIC_RANDOM => REWARD_TYPE.BP_AWARD_EPIC_RANDOM,
            REWARD_TYPE.AWARD_LEGEND_RANDOM => REWARD_TYPE.BP_AWARD_LEGEND_RANDOM,
            REWARD_TYPE.AWARD_MYTHIC_RANDOM => REWARD_TYPE.BP_AWARD_MYTHIC_RANDOM,
            REWARD_TYPE.AWARD_LEGEND_EVENT_RANDOM => REWARD_TYPE.BP_AWARD_LEGEND_EVENT_RANDOM,
            REWARD_TYPE.AWARD_MYTHIC_EVENT_RANDOM => REWARD_TYPE.BP_AWARD_MYTHIC_EVENT_RANDOM,
            _ => _type,
        };
        return  _typeBP;
    }

    /// <summary>
    /// Khi event bắt đầu -> Reset all data
    /// </summary>
    private static void OnEventStart()
    {
        var _cacheAllSpin = 0;
        var _processOld = Player.HolidayNoelProcess;
        if (_processOld != null)
            _cacheAllSpin = GetProcess().SpinAllCount;

        var _process = new Process()
        {
            IsEventActive = true,
            EventStartTimeTicks = RemoteStartTime.Ticks,
            SpinedLimitBoxIndexes = new List<int>(),
            SpinCount = 0,
            SpinAllCount = 0,
            SpinAllCountCache = _cacheAllSpin,
            IsChooseDontShowIntro = false,
            ClaimedMilestoneIndexes = new List<int>(),
            lstGiftPackProcess = new List<GiftPackProcess>(),
            lstMissionProcess = new List<MissionProcess>(),
            IsCycle2 = false,
            DailyPointEarnNonBoss = 0,
        };
        _process.Group = RewardByProgressionSystem.CurrentGroup;
        InitializeMissions(_process);
        InitlizeGiftPack(_process);
        Currency.ResetHolidayEventPoint(EVENT_NAME.NOEL);

        SetCurrentDate(_process);
        
        SaveData(_process);
    }

    /// <summary>
    /// Khi event kết thúc -> Auto claim tất cả rewards còn lại
    /// </summary>
    private static void OnEventEnd(Action<List<RewardCount>> _onAutoClaimRewards = null)
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null || !_process.IsEventActive) return;

        // // Lấy tất cả milestone có thể claim
        // List<int> claimableIndexes = GetAllClaimableMilestoneIndexes(_process.SpinCount);

        // // Filter milestone chưa claim
        // List<int> unclaimedIndexes = claimableIndexes
        //     .Where(index => !_process.ClaimedMilestoneIndexes.Contains(index))
        //     .ToList();

        // if (unclaimedIndexes.Count > 0)
        // {
        //     List<RewardCount> _finalRewards = new List<RewardCount>();

        //     foreach (int index in unclaimedIndexes)
        //     {
        //         var _milestone = HolidayNoels.MilestoneRewards[index];

        //         AddRewardToList(_finalRewards, new RewardCount
        //         {
        //             Type = _milestone.RewardType,
        //             Count = _milestone.Count
        //         });

        //         _process.ClaimedMilestoneIndexes.Add(index);
        //     }

        //     if (_finalRewards.Count > 0)
        //     {
        //         Rewards.AddReward(_finalRewards);
        //         _onAutoClaimRewards?.Invoke(_finalRewards);
        //     }
        // }

        // Đánh dấu event đã kết thúc
        _process.IsEventActive = false;
        // Currency.ResetHolidayEventPoint(EVENT_NAME.NOEL);
        SaveData(_process);
    }

    #region MILESTONE REWARDS

    /// <summary>
    /// Lấy tất cả milestones
    /// </summary>
    // public static List<HolidayNoels.HolidayNoelMilestoneReward> GetAllMilestones()
    // {
    //     return HolidayNoels.MilestoneRewards;
    // }

    /// <summary>
    /// Lấy milestone tại index
    /// </summary>
    public static HolidayNoels.HolidayNoelMilestoneReward GetMilestoneAtIndex(int index)
    {
        var _milestones = GetMilestone();//Player.HolidayNoelProcess.milestones;
        if (index < 0 || index >= _milestones.Count) return null;
        return _milestones[index];
    }

    /// <summary>
    /// Lấy reward tại milestone index
    /// </summary>
    public static RewardCount GetRewardAtMilestoneIndex(int index)
    {
        var milestone = GetMilestoneAtIndex(index);
        if (milestone == null) return null;

        return new RewardCount
        {
            Type = milestone.RewardType,
            Count = milestone.Count
        };
    }
    public static void CheckAndDownLoadAsset()
    {
        if (Level.LevelLimited < 25) return;
        //
        AddressableManager.instance.CheckAndDownload("Noel");
    }

    public static void ClaimedMilestoneIndex(int index, out RewardCount reward)
    {
        reward = null;
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;

        if (!_process.ClaimedMilestoneIndexes.Contains(index))
        {
            _process.ClaimedMilestoneIndexes.Add(index);
            var _reward = GetRewardAtMilestoneIndex(index);
            // if (_reward.Type == REWARD_TYPE.GOLD)
            // {
            //     // lấy số vàng theo level by chapter
            //     _reward.Count = GoldByChapter.GetGoldByChapter(GOLD_BY_CHAPTER_ID.EVENT_CHRISTMAS_MILESTONE);
            // }
            Rewards.AddReward(new List<RewardCount>() { _reward }, _whereLogReport: "Holiday_Noel_MilestoneReward");
            SaveData(_process);
            reward = _reward;
        }
    }

    /// <summary>
    /// Tìm tất cả milestone indexes có thể claim (dựa trên spinCount)
    /// </summary>
    private static List<int> GetAllClaimableMilestoneIndexes(int spinCount)
    {
        var _milestones = GetMilestone();//Player.HolidayNoelProcess.milestones;
        List<int> claimableIndexes = new List<int>();

        for (int i = 0; i < _milestones.Count; i++)
        {
            if (spinCount >= _milestones[i].Mile)
            {
                claimableIndexes.Add(i);
            }
        }

        return claimableIndexes;
    }


    /// <summary>
    /// Claim tất cả milestone có thể claim (từ ClaimedMilestoneIndex + 1 đến max)
    /// </summary>
    public static bool ClaimAllAvailableMilestones(Action<List<RewardCount>> _onClaimRewards = null)
    {
        if (!IsEventActive)
        {
            Debug.LogWarning("Event is not active!");
            return false;
        }

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        // Lấy tất cả milestone có thể claim dựa trên SpinCount
        List<int> claimableIndexes = GetAllClaimableMilestoneIndexes(_process.SpinAllCount);

        // Filter ra những milestone chưa claim
        List<int> unclaimedIndexes = claimableIndexes
            .Where(index => !_process.ClaimedMilestoneIndexes.Contains(index))
            .ToList();

        if (unclaimedIndexes.Count == 0)
        {
            Debug.Log("No new milestones to claim!");
            return false;
        }

        List<RewardCount> _allRewards = new List<RewardCount>();

        // Claim tất cả milestone chưa claimed
        foreach (int index in unclaimedIndexes)
        {
            var _milestones = GetMilestone()[index];//Player.HolidayNoelProcess.milestones[index];

            AddRewardToList(_allRewards, new RewardCount
            {
                Type = _milestones.RewardType,
                Count = _milestones.Count
            });

            // Thêm vào danh sách đã claim
            _process.ClaimedMilestoneIndexes.Add(index);
        }

        if (_allRewards.Count > 0)
        {
            _onClaimRewards?.Invoke(_allRewards);
            Rewards.AddReward(_allRewards, _whereLogReport: AnalyticsWhere.HolidayNoelMilestoneClaimAll);

            SaveData(_process);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra milestone tại index đã claim chưa
    /// </summary>
    public static bool IsMilestoneClaimedAtIndex(int index)
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        return _process.ClaimedMilestoneIndexes.Contains(index);
    }

    /// <summary>
    /// Kiểm tra có thể claim milestone tại index không
    /// </summary>
    public static bool CanClaimMilestoneAtIndex(int index)
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        var _milestones = GetMilestone();//Player.HolidayNoelProcess.milestones;
        // Kiểm tra index hợp lệ
        if (index < 0 || index >= _milestones.Count) return false;

        // Đã claim rồi -> không thể claim
        if (_process.ClaimedMilestoneIndexes.Contains(index)) return false;

        // Kiểm tra đủ số lượt quay chưa
        var _milestone = _milestones[index];
        return _process.SpinAllCount >= _milestone.Mile;
    }


    /// <summary>
    /// Lấy tổng số milestone
    /// </summary>
    // public static int GetTotalMilestoneCount()
    // {
    //     return HolidayNoels.MilestoneRewards.Count;
    // }

    public static int GetAvailableMilestoneIndex()
    {
        if (!IsEventActive) return 0;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return 0;

        int totalMilestones = GetMilestone().Count;//_process.milestones.Count;
        if (totalMilestones == 0) return 0;

        // Lấy tất cả milestone có thể claim dựa trên SpinCount
        List<int> claimableIndexes = GetAllClaimableMilestoneIndexes(_process.SpinAllCount);

        // Tìm milestone CÓ THỂ CLAIM (chưa claim)
        var unclaimedIndexes = claimableIndexes
            .Where(index => !_process.ClaimedMilestoneIndexes.Contains(index))
            .OrderBy(index => index)
            .ToList();

        // Trường hợp 1: Có milestone mới để claim -> Trả về index đầu tiên có thể claim
        if (unclaimedIndexes.Count > 0)
        {
            int availableIndex = unclaimedIndexes[0];
            DPDebug.Log($"[Milestone] Available to claim at index: {availableIndex}");
            return availableIndex;
        }

        // Trường hợp 2: Không có milestone mới -> Tìm index cao nhất đã claim
        if (_process.ClaimedMilestoneIndexes.Count > 0)
        {
            int highestClaimedIndex = _process.ClaimedMilestoneIndexes.Max();

            // Nếu đã claim hết tất cả -> Trả về index cuối cùng
            if (highestClaimedIndex >= totalMilestones - 1)
            {
                DPDebug.Log($"[Milestone] All claimed! Return last index: {totalMilestones - 1}");
                return totalMilestones - 1;
            }

            // Chưa claim hết -> Trả về index cao nhất đã claim
            DPDebug.Log($"[Milestone] Highest claimed index: {highestClaimedIndex}");
            return highestClaimedIndex;
        }

        // Trường hợp 3: Chưa claim gì cả -> Trả về index đầu tiên
        DPDebug.Log($"[Milestone] No claimed yet! Return first index: 0");
        return 0;
    }

    /// <summary>
    /// Kiểm tra có milestone mới để claim không
    /// </summary>
    public static bool HasAvailableMilestones()
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        // Lấy tất cả milestone có thể claim
        List<int> claimableIndexes = GetAllClaimableMilestoneIndexes(_process.SpinAllCount);

        // Kiểm tra xem có milestone nào chưa claim không
        return claimableIndexes.Any(index => !_process.ClaimedMilestoneIndexes.Contains(index));
    }

    /// <summary>
    /// Lấy info đầy đủ về milestone hiện tại
    /// </summary>
    public static (int index, bool canClaim, bool isCompleted) GetCurrentMilestoneInfo()
    {
        int index = GetAvailableMilestoneIndex();
        bool canClaim = CanClaimMilestoneAtIndex(index);
        bool isCompleted = IsMilestoneClaimedAtIndex(index);

        return (index, canClaim, isCompleted);
    }


    #endregion

    #region LIMIT BOXES (Các ô đặc biệt trong vòng quay)

    /// <summary>
    /// Thêm limit box đã quay vào (khi quay trúng ô limit)
    /// </summary>
    public static bool AddSpinedLimitBox(int limitBoxIndex)
    {
        if (!IsEventActive)
        {
            DPDebug.LogWarning("Event is not active!");
            return false;
        }

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        TypeBoxLuckyWheel boxType = (TypeBoxLuckyWheel)limitBoxIndex;

        // Kiểm tra đã quay đủ số lần chưa
        int maxSpins = GetLimitBoxMaxSpinCount(boxType);
        int currentSpins = CountLimitBoxSpinned(boxType, _process.SpinedLimitBoxIndexes);

        if (currentSpins >= maxSpins)
        {
            DPDebug.LogWarning($"Limit box {boxType} đã quay đủ {maxSpins} lần!");
            return false;
        }
        // Thêm vào danh sách đã quay (có thể trùng)
        _process.SpinedLimitBoxIndexes.Add(limitBoxIndex);

        DPDebug.Log($"✅ Added limit box {boxType} (Spin {currentSpins + 1}/{maxSpins})");

        SaveData(_process);
        return true;
    }

    /// <summary>
    /// Kiểm tra limit box đã quay ĐỦ số lần tối đa chưa
    /// </summary>
    public static bool IsLimitBoxFullySpined(TypeBoxLuckyWheel type)
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        return IsLimitBoxFullySpinned(type, _process.SpinedLimitBoxIndexes);
    }


    /// <summary>
    /// Lấy danh sách index các limit box đã quay
    /// </summary>
    public static List<int> GetSpinedLimitBoxIndexes()
    {
        if (!IsEventActive) return new List<int>();
        return Player.HolidayNoelProcess?.SpinedLimitBoxIndexes ?? new List<int>();
    }

    /// <summary>
    /// Lấy số lần đã quay của một limit box
    /// </summary>
    public static int GetLimitBoxSpinCount(TypeBoxLuckyWheel type)
    {
        if (!IsEventActive) return 0;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return 0;

        return CountLimitBoxSpinned(type, _process.SpinedLimitBoxIndexes);
    }

    #endregion

    public static Process GetProcess()
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null)
        {
            _process = new Process
            {
                IsEventActive = false,
                ClaimedMilestoneIndexes = new List<int>(),
                SpinCount = 0,
                SpinedLimitBoxIndexes = new List<int>()
            };
            SaveData(_process);
        }

        return _process;
    }

    #region SPIN COUNT

    /// <summary>
    /// Lấy số lượt quay hiện tại
    /// </summary>
    public static int SpinCount
    {
        get
        {
            if (!IsEventActive) return 0;
            return Player.HolidayNoelProcess?.SpinCount ?? 0;
        }
    }

    /// <summary>
    /// Lấy số lượt quay tổng
    /// </summary>
    public static int SpinAllCount
    {
        get
        {
            if (!IsEventActive) return 0;
            return Player.HolidayNoelProcess?.SpinAllCount ?? 0;
        }
    }

    public static int SpinCountRecycle
    {
        get
        {
            if (!IsEventActive) return 0;
            return Player.HolidayNoelProcess?.SpinCountRecycle ?? 0;
        }
    }

    /// <summary>
    /// Thêm số lượt quay (mỗi lần quay vòng quay)
    /// </summary>
    public static void AddSpinCount(int _count = 1)
    {
        if (!IsEventActive)
        {
            Debug.LogWarning("Event is not active!");
            return;
        }

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;

        _process.SpinCount += _count;
        _process.SpinAllCount += _count;
        _process.SpinCountRecycle += _count;

        //DPDebug.Log($"[SpinCount] Added {_count} spin(s). Current SpinCount: {_process.SpinCount}, Mile: {HolidayNoels.MilePrizeUltimateValue}");
        SaveData(_process);
    }

    private static void ResetSpinCycle()
    {
        var _processData = Player.HolidayNoelProcess;
        _processData.SpinCount = -1;
        if (IsTier3FullySpinned())
        {
            _processData.IsCycle2 = true;
            _processData.SpinCountRecycle = -1;
            _processData.SpinedLimitBoxIndexes = new List<int>();
            DPDebug.Log("[SpinCycle] All Tier 3 done! Reset SpinedLimitBoxIndexes.");
        }
        else
        {
            DPDebug.Log("[SpinCycle] Tier 3 not fully done yet, keep SpinedLimitBoxIndexes.");
        }

        SaveData(_processData);
    }

    #endregion

    #region  Random Lucky LuckyWheel

    /// <summary>
    /// Quay vòng quay - hỗ trợ quay 1 lần hoặc nhiều lần (x10)
    /// </summary>
    /// <param name="spinAmount">Số lần quay (1 hoặc 10)</param>
    /// <returns>Danh sách kết quả quay</returns>
    public static List<SpinResult> SpinWheel(int spinAmount = 1)
    {
        if (!IsEventActive)
        {
            DPDebug.LogWarning("Event is not active!");
            return new List<SpinResult>();
        }

        if (spinAmount != 1 && spinAmount != 10)
        {
            DPDebug.LogError($"Invalid spin amount: {spinAmount}. Only 1 or 10 allowed!");
            return new List<SpinResult>();
        }

        List<SpinResult> results = new List<SpinResult>();
        int currentSpin = SpinCount;

        bool hasBoss = false;
        if (_bossItems == null) BuildBossCache();

        for (int i = 0; i < spinAmount; i++)
        {
            currentSpin++; // Tăng lên để check lần quay tiếp theo

            DPDebug.Log($"🎰 Spinning... Current: {currentSpin}/{spinAmount}");

            bool excludeBoss = (spinAmount > 1) && hasBoss;

            // Lấy kết quả với logic đặc biệt cho spin 100
            var (_type, _rewards, _tierIndex, _configIndex) = GetRandomLuckyItemForSpin(currentSpin, excludeBoss);

            if (_type == TypeBoxLuckyWheel.NONE)
            {
                DPDebug.LogError($"Failed to get result for spin {currentSpin}");
                continue;
            }

            if (spinAmount > 1 && !hasBoss && IsBossItem(_type))
            {
                hasBoss = true;
                DPDebug.Log($"🎯 Boss appeared at spin #{i + 1}");
            }

            results.Add(new SpinResult
            {
                Id = HolidayNoels.GetIndexInSort(_type),
                SpinNumber = currentSpin,
                Type = _type,
                Rewards = _rewards,
                TierIndex = _tierIndex,
                ConfigIndex = _configIndex,
                IsSpecialSpinIndex = currentSpin == HolidayNoels.MilePrizeLimitRewardValue
            });

            // Tăng SpinCount sau mỗi lần quay
            AddSpinCount(1);
        }

        return results;
    }

    /// <summary>
    /// Lấy kết quả random cho 1 lần quay cụ thể (xử lý logic spin 100)
    /// </summary>
    private static (TypeBoxLuckyWheel type, List<RewardCount> rewards, int tierIndex, int configIndex)
        GetRandomLuckyItemForSpin(int spinNumber, bool excludeBoss = false)
    {
        if (!HolidayNoel.IsEventActive)
        {
            DPDebug.LogWarning("Event is not active!");
            return (TypeBoxLuckyWheel.NONE, null, -1, -1);
        }

        var configs = GetWheelConfig();
        if (configs == null || configs.Count == 0)
        {
            DPDebug.LogError("No lucky wheel config found!");
            return (TypeBoxLuckyWheel.NONE, null, -1, -1);
        }

        var spinedLimitBoxes = GetSpinedLimitBoxIndexes();

        // ===== TRƯỜNG HỢP ĐẶC BIỆT: Spin thứ 100 -> Chắc chắn tier 3 =====
        if (spinNumber == HolidayNoels.MilePrizeLimitRewardValue && !IsTier3FullySpinned())
        {
            DPDebug.Log("🎯 Spin #100! Force select Tier 3 with special reward!");
            int tier3Index = configs.Count - 1;
            var tier3Config = configs[tier3Index];

            // Random trong tier 3 với forceLast = true
            var (configIndex, selectedConfig) = GetRandomTier3Special(tier3Config, spinedLimitBoxes, false);

            if (selectedConfig == null)
            {
                DPDebug.LogError("Cannot get Tier 3 config at spin 100!");
                return (TypeBoxLuckyWheel.NONE, null, -1, -1);
            }

            DPDebug.Log($"✅ Spin #100 Result: Config {configIndex} (Type: {selectedConfig.Type})");

            // Đánh dấu limit box đã quay
            HolidayNoel.AddSpinedLimitBox((int)selectedConfig.Type);
            //
            ResetSpinCycle();



            return (selectedConfig.Type, selectedConfig.Rewards, tier3Index, configIndex);
        }

        // ===== LOGIC THÔNG THƯỜNG (Spin khác 100) =====
        return GetRandomLuckyItemNormal(spinedLimitBoxes, excludeBoss);
    }

    private static (TypeBoxLuckyWheel type, List<RewardCount> rewards, int tierIndex, int configIndex) GetRandomLuckyItemNormal(List<int> spinedLimitBoxes, bool excludeBoss = false)
    {
        var configs = GetWheelConfig();
        var _process = Player.HolidayNoelProcess;
        // Bước 1: Tính tổng probability và filter tier hợp lệ
        List<int> validTierIndexes = new List<int>();
        List<float> validTierProbabilities = new List<float>();

        for (int i = 0; i < configs.Count; i++)
        {
            // Nếu là tier 3 (tier limit) - kiểm tra xem đã quay hết chưa
            if (IsTier3(i))
            {
                // Kiểm tra điều kiện unlock Tier 3
                if (_process.SpinCount < 100)
                {
                    DPDebug.Log($"[Tier3 Lock] SpinCount = {_process.SpinCount}/100. Tier 3 bị khóa!");
                    continue; // Bỏ qua Tier 3
                }

                // Đếm số limit box trong tier 3 đã quay
                int tier3Count = GetTier3LimitBoxCount(configs[i]);
                int spinedCount = CountSpinedLimitBoxesInTier(configs[i], spinedLimitBoxes);

                // Nếu đã quay hết tất cả limit box trong tier 3 -> bỏ qua tier này
                if (spinedCount >= tier3Count)
                {
                    DPDebug.Log($"Tier {i} (Tier 3) đã quay hết limit boxes, bỏ qua!");
                    continue;
                }
            }

            validTierIndexes.Add(i);
            validTierProbabilities.Add(configs[i].Probability);
        }
        if (validTierIndexes.Count == 0)
        {
            DPDebug.LogError("No valid tier available!");
            return (TypeBoxLuckyWheel.NONE, null, -1, -1);
        }

        // Bước 2: Random chọn tier
        int _selectedTierIndex = RandomTier(validTierIndexes, validTierProbabilities);
        // if (_process.SpinCount >= 115)
        //     _selectedTierIndex = configs.Length - 1; // ép chọn tier 3 từ spin 115 trở đi
        var _selectedTier = configs[_selectedTierIndex];
        DPDebug.Log($"Selected Tier: {_selectedTierIndex} (Probability: {_selectedTier.Probability})");

        // Bước 3: Random trong tier đã chọn
        var _levelConfigs = _selectedTier.LevelConfig;
        if (_levelConfigs == null || _levelConfigs.Length == 0)
        {
            DPDebug.LogError($"Tier {_selectedTierIndex} has no level config!");
            return (TypeBoxLuckyWheel.NONE, null, -1, -1);
        }

        // Nếu là tier 3, cần filter các limit box đã quay

        if (IsTier3(_selectedTierIndex))
        {
            // Tier 3: Filter limit box chưa quay + logic config cuối
            var (_configIndex, _selectedConfig) = GetRandomTier3Special(_selectedTier, spinedLimitBoxes, false);

            if (_selectedConfig == null)
            {
                DPDebug.LogError($"Tier {_selectedTierIndex} has no available limit boxes!");
                return (TypeBoxLuckyWheel.NONE, null, -1, -1);
            }
            DPDebug.Log($"Selected Config: {_configIndex} (Type: {_selectedConfig.Type})");

            HolidayNoel.AddSpinedLimitBox((int)_selectedConfig.Type);
            ResetSpinCycle();
            return (_selectedConfig.Type, _selectedConfig.Rewards, _selectedTierIndex, _configIndex);
        }

        // ===== XỬ LÝ TIER 1, 2 (TIER THƯỜNG) =====
        List<int> validConfigIndexes = new List<int>();
        // Tier thường, tất cả config đều hợp lệ
        for (int i = 0; i < _levelConfigs.Length; i++)
        {
            // Skip boss nếu đã có boss rồi
            if (excludeBoss && IsBossItem(_levelConfigs[i].Type))
            {
                continue;
            }
            validConfigIndexes.Add(i);
        }

        // Fallback nếu filter hết
        if (validConfigIndexes.Count == 0)
        {
            for (int i = 0; i < _levelConfigs.Length; i++)
            {
                validConfigIndexes.Add(i);
            }
        }

        // Random chọn config trong tier
        int selectedConfigIndex = RandomLevelConfig(_levelConfigs, validConfigIndexes);
        var selectedConfig = _levelConfigs[selectedConfigIndex];

        DPDebug.Log($"Selected Config: {selectedConfigIndex} (Type: {selectedConfig.Type}, Probability: {selectedConfig.Probability})");

        return (selectedConfig.Type, selectedConfig.Rewards, _selectedTierIndex, selectedConfigIndex);

    }

    /// <summary>
    /// Random chọn tier dựa trên probability
    /// </summary>
    private static int RandomTier(List<int> validIndexes, List<float> probabilities)
    {
        if (validIndexes.Count == 0)
            return -1;

        // Duyệt qua từng tier hợp lệ theo thứ tự
        for (int i = 0; i < validIndexes.Count; i++)
        {
            int tierIndex = validIndexes[i];
            float tierProbability = probabilities[i];

            // Nếu là tier cuối cùng -> Chắc chắn chọn (100%)
            if (i == validIndexes.Count - 1)
            {
                DPDebug.Log($"✅ Last tier! Tier {tierIndex} selected (100%)");
                return tierIndex;
            }

            // Random từ 0-100
            float randomValue = UnityEngine.Random.Range(0f, 100f);

            DPDebug.Log($"🎲 Tier {tierIndex}: Rolling {randomValue:F2} vs {tierProbability:F2}%");

            // Nếu random value < probability của tier này -> Chọn tier này
            if (randomValue < tierProbability)
            {
                DPDebug.Log($"✅ Tier {tierIndex} HIT! ({randomValue:F2} < {tierProbability:F2})");
                return tierIndex;
            }

            DPDebug.Log($"❌ Tier {tierIndex} MISS ({randomValue:F2} >= {tierProbability:F2}), continue to next tier...");
        }

        // Không bao giờ tới đây vì tier cuối luôn được chọn
        return validIndexes[validIndexes.Count - 1];
    }

    /// <summary>
    /// Random config trong Tier 3 với logic đặc biệt:
    /// - LevelConfig cuối cùng chỉ xuất hiện khi đã quay được 3 limit box trước
    /// </summary>
    private static (int configIndex, LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig selectedConfig)
        GetRandomTier3Special(LuckyWheelData.LuckyWheelConfig tier3Config, List<int> spinedLimitBoxes, bool forceLast)
    {
        var levelConfigs = tier3Config.LevelConfig;
        if (levelConfigs == null || levelConfigs.Length == 0)
            return (-1, null);

        List<int> validConfigIndexes = new List<int>();

        int _firstUnlockedConfigIndex = -1;


        for (int i = 0; i < levelConfigs.Length; i++)
        {
            var config = levelConfigs[i];
            int maxSpins = GetLimitBoxMaxSpinCount(config.Type);
            int currentSpins = CountLimitBoxSpinned(config.Type, spinedLimitBoxes);

            DPDebug.Log($"[Tier3] Config {i} ({config.Type}): {currentSpins}/{maxSpins} spins");

            // Nếu chưa quay đủ số lần → Đây là config đầu tiên chưa mở hết
            if (currentSpins < maxSpins)
            {
                _firstUnlockedConfigIndex = i;
                DPDebug.Log($"[Tier3] First unlocked config: {i} ({config.Type})");
                break;
            }
        }

        // Nếu đã mở hết tất cả → Return null
        if (_firstUnlockedConfigIndex == -1)
        {
            DPDebug.Log($"[Tier3] All configs fully spinned!");
            return (-1, null);
        }

        int lastConfigIndex = levelConfigs.Length - 1;
        // Nếu không ép chọn config cuối → Lấy tất cả config chưa mở hết trừ config cuối
        if (forceLast && _firstUnlockedConfigIndex == lastConfigIndex)
        {
            // Spin 100 VÀ config cuối chưa mở hết → Chọn config cuối
            DPDebug.Log($"🎁 Spin #100: Chọn config cuối (index {lastConfigIndex})");

            return (lastConfigIndex, levelConfigs[lastConfigIndex]);
        }

        // Trường hợp thông thường: CHỈ cho phép quay config đầu tiên chưa mở hết
        validConfigIndexes.Add(_firstUnlockedConfigIndex);

        DPDebug.Log($"[Tier3] Only allow config {_firstUnlockedConfigIndex} ({levelConfigs[_firstUnlockedConfigIndex].Type})");
        if (validConfigIndexes.Count == 0)
            return (-1, null);
        //  int selectedIndex = RandomLevelConfig(levelConfigs, validConfigIndexes);
        return (_firstUnlockedConfigIndex, levelConfigs[_firstUnlockedConfigIndex]);

    }

    /// <summary>
    /// Random chọn config trong tier dựa trên probability
    /// Theo thứ tự từ index 0, index cuối = 100%
    /// </summary>
    private static int RandomLevelConfig(LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig[] configs, List<int> validIndexes)
    {


        for (int i = 0; i < validIndexes.Count; i++)
        {
            // Random từ 0-100
            float random = UnityEngine.Random.Range(0f, 100f);
            int configIndex = validIndexes[i];

            // Index cuối cùng = 100%
            if (i == validIndexes.Count - 1)
            {
                return configIndex;
            }

            // Kiểm tra probability
            float probability = configs[configIndex].Probability;
            DPDebug.Log($"[RandomLevelConfig] Checking Config {configIndex} ({configs[configIndex].Type}): {random:F2} vs {probability:F2}%");
            if (random < probability)
            {
                return configIndex;
            }
        }

        // Fallback: trả về index cuối
        return validIndexes[validIndexes.Count - 1];
    }


    /// <summary>
    /// Kiểm tra xem có phải tier 3 (tier limit) không
    /// Thường tier 3 là tier cuối cùng hoặc có Level = 3
    /// </summary>
    private static bool IsTier3(int tierIndex)
    {
        var configs = GetWheelConfig();
        if (tierIndex < 0 || tierIndex >= configs.Count) return false;

        return tierIndex == configs.Count - 1;
    }

    /// <summary>
    /// Đếm số lượng limit box trong tier 3
    /// </summary>
    private static int GetTier3LimitBoxCount(LuckyWheelData.LuckyWheelConfig tier)
    {
        return tier.LevelConfig?.Length ?? 0;
    }

    /// <summary>
    /// Đếm số lượng limit box trong tier đã quay
    /// </summary>
    private static int CountSpinedLimitBoxesInTier(LuckyWheelData.LuckyWheelConfig tier, List<int> spinedIndexes)
    {
        int count = 0;

        if (tier.LevelConfig == null) return 0;

        foreach (var config in tier.LevelConfig)
        {
            if (IsLimitBoxFullySpinned(config.Type, spinedIndexes))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Kiểm tra limit box đã quay đủ số lần chưa
    /// </summary>
    private static bool IsLimitBoxFullySpinned(TypeBoxLuckyWheel type, List<int> spinedIndexes)
    {
        int maxSpinCount = GetLimitBoxMaxSpinCount(type);
        int currentSpinCount = CountLimitBoxSpinned(type, spinedIndexes);

        return currentSpinCount >= maxSpinCount;
    }

    /// <summary>
    /// Kiểm tra Tier 3 đã quay hết tất cả limit boxes chưa
    /// </summary>
    public static bool IsTier3FullySpinned()
    {
        if (!IsEventActive) return false;

        var configs = GetWheelConfig();
        if (configs == null || configs.Count == 0) return false;

        int tier3Index = configs.Count - 1;
        var tier3Config = configs[tier3Index];

        var spinedLimitBoxes = GetSpinedLimitBoxIndexes();

        int tier3TotalBoxes = GetTier3LimitBoxCount(tier3Config);
        int tier3SpinnedBoxes = CountSpinedLimitBoxesInTier(tier3Config, spinedLimitBoxes);

        bool isFullySpinned = tier3SpinnedBoxes >= tier3TotalBoxes;

        DPDebug.Log($"[Tier3 Check] Total: {tier3TotalBoxes}, Spinned: {tier3SpinnedBoxes}, Fully: {isFullySpinned}");

        return isFullySpinned;
    }

    /// <summary>
    /// Đếm số lần đã quay của một limit box cụ thể
    /// </summary>
    private static int CountLimitBoxSpinned(TypeBoxLuckyWheel type, List<int> spinedIndexes)
    {
        int count = 0;
        int typeInt = (int)type;

        foreach (int spinedType in spinedIndexes)
        {
            if (spinedType == typeInt)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Lấy số lần tối đa có thể quay cho mỗi limit box
    /// TypeBox11, TypeBox12 = 2 lần
    /// Các box khác = 1 lần
    /// </summary>
    public static int GetLimitBoxMaxSpinCount(TypeBoxLuckyWheel type)
    {
        switch (type)
        {
            case TypeBoxLuckyWheel.ItemLuckyWheel11:
                return 2;
            case TypeBoxLuckyWheel.ItemLuckyWheel12:
                return 4;
            default:
                return 1;
        }
    }

    #endregion

    #region  Boss Constraint
    private static void BuildBossCache()
    {
        if (_bossItems != null) return;

        _bossItems = new HashSet<TypeBoxLuckyWheel>();
        var configs = GetWheelConfig();

        if (configs != null)
        {
            foreach (var tier in configs)
            {
                if (tier.LevelConfig != null)
                {
                    foreach (var cfg in tier.LevelConfig)
                    {
                        if (cfg.Rewards != null && cfg.Rewards.Any(r => r.Type == REWARD_TYPE.BOSS_FIGHT_HOLIDAY_NOEL))
                        {
                            _bossItems.Add(cfg.Type);
                        }
                    }
                }
            }
        }
    }

    private static bool IsBossItem(TypeBoxLuckyWheel type)
    {
        if (_bossItems == null) BuildBossCache();
        return _bossItems.Contains(type);
    }

    #endregion
    public static void AddRewardToList(List<RewardCount> rewardList, RewardCount reward)
    {
        var existing = rewardList.Find(r => r.Type == reward.Type);
        if (existing != null)
            existing.Count += reward.Count;
        else
            rewardList.Add((RewardCount)reward.Clone());
    }

    public static void SetChooseDontShowIntro(bool dontShow)
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;

        _process.IsChooseDontShowIntro = dontShow;
        SaveData(_process);
    }

    public static bool GetChooseDontShowIntro()
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        return _process.IsChooseDontShowIntro;
    }

    #region  Mission

    public static void InitializeMissions(Process _process, bool _forceInit = false)
    {
        if (_process == null) return;

        // Kiểm tra nếu list rỗng hoặc count = 0
        if (_process.lstMissionProcess == null || _process.lstMissionProcess.Count == 0 || _forceInit)
        {
            DPDebug.Log("[Mission] Initializing mission list...");
            _process.lstMissionProcess = new List<MissionProcess>();

            // Lấy data mission từ ScriptableObject
            var missionData = HolidayNoels.MissionsHolidayNoel;
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
                SaveData(_process);
            DPDebug.Log($"[Mission] Initialized {_process.lstMissionProcess.Count} missions");
        }
    }

    /// <summary>
    /// Lấy thông tin mission process theo ID
    /// </summary>
    public static MissionProcess GetMissionProcess(int missionID)
    {
        if (!IsEventActive) return null;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return null;

        if (_process.lstMissionProcess == null || _process.lstMissionProcess.Count == 0)
            InitializeMissions(_process, true);

        return _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionID);
    }

    /// <summary>
    /// Cập nhật tiến độ mission
    /// </summary>
    public static void UpdateMissionProgress(TYPE_MISSION missionType, int incrementValue = 1, params int[] missionIDs)
    {
        if (!IsEventActive)
        {
            DPDebug.LogWarning("[Mission] Event is not active!");
            return;
        }

        if (!IsPassedLevelRequireOpen)
            return;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;

        var missionData = HolidayNoels.MissionsHolidayNoel;
        if (missionData == null) return;

        bool hasUpdated = false;
        bool filterByID = missionIDs != null && missionIDs.Length > 0;

        foreach (var missionConfig in missionData)
        {
            if (missionConfig.mission.Type != missionType) continue;
            if (filterByID && !missionIDs.Contains(missionConfig.ID)) continue;

            var missionProcess = _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionConfig.ID);
            if (missionProcess == null) continue;
            if (missionProcess.IsClaimed) continue;

            missionProcess.ProcessValue += incrementValue;

            if (missionProcess.ProcessValue >= missionConfig.mission.Value)
            {
                missionProcess.ProcessValue = missionConfig.mission.Value;
                DPDebug.Log($"[Mission] ✅ Mission {missionConfig.ID} completed!");
            }

            DPDebug.Log($"[Mission] Updated {missionType} (ID: {missionConfig.ID}): {missionProcess.ProcessValue}/{missionConfig.mission.Value}");
            hasUpdated = true;
        }

        if (hasUpdated)
            SaveData(_process);
    }

    public static bool ClaimMissionReward(int missionID, out RewardCount reward)
    {
        reward = null;

        if (!IsEventActive)
        {
            DPDebug.LogWarning("[Mission] Event is not active!");
            return false;
        }

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        // Tìm mission process
        var missionProcess = _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionID);
        if (missionProcess == null)
        {
            DPDebug.Log($"[Mission] Mission ID {missionID} not found!");
            return false;
        }

        var _missionData = HolidayNoels.GetMission(missionID);
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

        // Đánh dấu đã claim
        missionProcess.IsClaimed = true;

        // Tạo reward
        reward = new RewardCount
        {
            Type = _missionData.reward.Type,
            Count = _missionData.reward.Count
        };

        // Add reward
        Rewards.AddReward(new List<RewardCount> { reward }, _whereLogReport: "Holiday_Noel_Mission");

        SaveData(_process);

        DPDebug.Log($"[Mission] ✅ Claimed mission {missionID} - Reward: {reward.Type} x{reward.Count}");
        return true;
    }

    /// <summary>
    /// Kiểm tra mission có thể claim không
    /// </summary>
    public static bool CanClaimMission(int missionID)
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;

        var missionProcess = _process.lstMissionProcess.FirstOrDefault(x => x.IDMission == missionID);
        if (missionProcess == null) return false;
        var _missionData = HolidayNoels.GetMission(missionID);
        if (_missionData == null) return false;
        return missionProcess.ProcessValue >= _missionData.mission.Value && !missionProcess.IsClaimed;
    }

    /// <summary>
    /// Kiểm tra có mission nào có thể claim không
    /// </summary>
    public static bool HasClaimableMissions()
    {
        if (!IsEventActive) return false;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return false;
        for (int i = 0; i < _process.lstMissionProcess.Count; i++)
        {
            var missionProcess = _process.lstMissionProcess[i];
            var _missionData = HolidayNoels.GetMission(missionProcess.IDMission);
            if (_missionData == null) continue;

            if (missionProcess.ProcessValue >= _missionData.mission.Value && !missionProcess.IsClaimed)
                return true;
        }
        return false;
    }

    #endregion

    #region Gift Pack

    public static void InitlizeGiftPack(Process _process, bool _forceInit = false)
    {
        DPDebug.Log("[InitGiftPack] Event is active.");
        if (_process == null) return;

        DPDebug.Log("[InitGiftPack] Process is valid.");
        if (_process.lstGiftPackProcess == null)
            _process.lstGiftPackProcess = new List<GiftPackProcess>();

        DPDebug.Log("[InitGiftPack] GiftPackProcess list is valid.");

        var giftPacks = HolidayNoels.GiftPackNoels;
        if (giftPacks == null || giftPacks.Length == 0) return;
        DPDebug.Log("[InitGiftPack] Found gift pack data.");

        foreach (var giftPack in giftPacks)
        {
            if (!_process.lstGiftPackProcess.Any(gp => gp.GiftPackID == giftPack.ID))
            {
                _process.lstGiftPackProcess.Add(new GiftPackProcess
                {
                    GiftPackID = giftPack.ID,
                    IsPurchased = false
                });
            }
        }
        DPDebug.Log("[InitGiftPack] Gift Pack Process initialized.");

        if (_forceInit)
            SaveData(_process);
    }

    public static GiftPackProcess GetGiftPackProcess(int giftPackID)
    {
        if (!IsEventActive) return null;

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return null;

        if (_process.lstGiftPackProcess == null || _process.lstGiftPackProcess.Count == 0)
            InitlizeGiftPack(_process);

        return _process.lstGiftPackProcess.FirstOrDefault(gp => gp.GiftPackID == giftPackID);
    }

    public static void PurchaseGiftPack(int giftPackID)
    {
        if (!IsEventActive)
        {
            DPDebug.LogWarning("Event is not active!");
            return;
        }

        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;

        var giftPackProcess = _process.lstGiftPackProcess.FirstOrDefault(gp => gp.GiftPackID == giftPackID);
        if (giftPackProcess == null)
        {
            DPDebug.LogError($"Gift Pack ID {giftPackID} not found in process!");
            return;
        }

        var _giftData = HolidayNoels.GetGiftPackNoel(giftPackID);
        if (_giftData.IsLimitedPack)
            giftPackProcess.IsPurchased = true;
        var _reward = new RewardCount
        {
            Type = REWARD_TYPE.HOLIDAY_NOEL_POINT,
            Count = _giftData.Value
        };
        Rewards.AddReward(_reward, _whereLogReport: AnalyticsWhere.HolidayNoelGiftPackPurchase);
        SaveData(_process);

        DPDebug.Log($"✅ Gift Pack ID {giftPackID} marked as purchased.");
    }

    #endregion
    
    public static LuckyWheelData.LuckyWheelConfig.LuckyWheelLevelConfig GetLevelConfig(TypeBoxLuckyWheel _type)
    {
        var config = GetWheelConfig();
        if (config == null || config.Count == 0)
            return null;

        foreach (var tierCfg in config)
        {
            if (tierCfg.LevelConfig != null)
            {
                foreach (var itemCfg in tierCfg.LevelConfig)
                {
                    if (itemCfg.Type == _type)
                        return itemCfg;
                }
            }
        }
        return null;

    }

    public static int DailyPointEarnNonBossCache;
    public static void SetDailyPointEarnNonBoss(int _value)
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return;
        _value = Mathf.Clamp(_value, 0, HolidayNoels.CapRewardOutsideBossFight);
        _process.DailyPointEarnNonBoss = _value;
    }

    public static int GetDailyPointEarnNonBoss()
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return HolidayNoels.CapRewardOutsideBossFight;
        return _process.DailyPointEarnNonBoss;
    }

    public static DateTime GetCurrentDate()
    {
        var _process = Player.HolidayNoelProcess;
        if (_process == null) return DateTime.MinValue;
        if (string.IsNullOrEmpty(_process.CurrentDate)) SetCurrentDate(_process);
        return DateTime.Parse(_process.CurrentDate, CultureInfo.InvariantCulture);
    }

    public static void SetCurrentDate(Process _process)
    {
        if (_process == null) return;
        _process.CurrentDate = DPUtils.GetUtcNow.Date.ToString(CultureInfo.InvariantCulture);
    }

    private static void SaveData(Process _process)
    {
        Player.HolidayNoelProcess = _process;
    }

    [Serializable]
    public class Process
    {
        public bool IsEventActive = false; // Kiểm tra event đang active hay không
        public long EventStartTimeTicks = 0; // Ticks của RemoteStartTime khi event được khởi tạo (dùng để phân biệt cùng event hay event mới)
        public List<int> ClaimedMilestoneIndexes = new List<int>(); // Index của milestone đã claim (-1 = chưa claim gì)
        public int SpinCount;                     // Tổng số lượt quay to mile limit reset
        public bool IsChooseDontShowIntro = false; // Người chơi chọn không hiển thị intro nữa
        public int SpinAllCount;                  // Tổng số lượt quay không giới hạn
        public int SpinCountRecycle;                   // Số lượt quay đã reset về 0 sau mỗi khi nhận ultimate mile prize
        public int SpinAllCountCache;
        public List<int> SpinedLimitBoxIndexes = new List<int>(); // Danh sách index các hộp đã quay giới hạn

        public List<MissionProcess> lstMissionProcess = new();

        public List<GiftPackProcess> lstGiftPackProcess = new();
        
        public bool IsCycle2 = false;

        public int DailyPointEarnNonBoss;
        public string CurrentDate;
        public PROGRESSION_GROUP Group;
    }
}

[Serializable]
public class SpinResult
{
    public int Id;
    public int SpinNumber;              // Số thứ tự lần quay (1, 2, ..., 100)
    public TypeBoxLuckyWheel Type;      // Loại phần thưởng
    public List<RewardCount> Rewards;   // Danh sách phần thưởng
    public int TierIndex;               // Index tier (0, 1, 2)
    public int ConfigIndex;             // Index config trong tier
    public bool IsSpecialSpinIndex;           // Có phải spin #100 không
}

[Serializable]
public class GiftPackProcess
{
    public int GiftPackID;
    public bool IsPurchased;
}