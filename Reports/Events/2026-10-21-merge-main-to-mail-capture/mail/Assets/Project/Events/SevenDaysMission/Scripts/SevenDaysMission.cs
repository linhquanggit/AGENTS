using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using GameAnalyticsSdk;
using Games;
using UnityEngine;
using UnityEngine.Networking;

public static class SevenDaysMission
{
    private static DateTime eventStartTime, changeGoldByChapterTime;
    
    static SevenDaysMission()
    {
        process = Player.SevenDaysMissionProcess;
        var _isCreatData = process == null || process.DayInfos == null || process.DayInfos.Count <= 0
            || process.MilestoneRewardClaimed == null || process.MilestoneRewardClaimed.Count <= 0;
        if (_isCreatData)
            CreateData();
    }

    public static DateTime GetEndTime()
    {
        return GetStartTime().AddDays(SevenDaysMissionData.TotalDays);
    }

    public static TimeSpan GetTimeRemaining()
    {
        return GetEndTime() - DPUtils.GetUtcNow;
    }

    public static bool IsActive()
    {
        return GetTimeRemaining() > TimeSpan.Zero;
    }
    
    // public static bool IsUnlocked = Level.LevelMax >= SevenDaysMissionData.LevelUnlock;

    //public static DateTime EventTime => EventEndTime;

    private static Process process;
    
    //

    public static void Refresh()
    {
        process = Player.SevenDaysMissionProcess;
        //if (process.EventPoint != _processSave.EventPoint)
        //{
        //    process = _processSave;
        //    return;
        //}
        //foreach (var _status in process.MilestoneClaimStatus)
        //{
        //    foreach (var _statusSave in _processSave.MilestoneClaimStatus)
        //    {
        //        if (_status != _statusSave) 
        //        {
        //            process = _processSave;
        //            return;
        //        }
        //    }
        //}
    }
    
    public static int GetEventPoint()
    {
        return process.EventPoint;
    }

    public static bool IsUnlock(out int _lvUnlock)
    {
        _lvUnlock = SevenDaysMissionData.LevelUnlock;
        return Games.Level.LevelMax >= _lvUnlock;
    }
    
    public static DateTime GetStartTime()
    {
        if (eventStartTime == DateTime.MinValue)
        {
            if (string.IsNullOrEmpty(process.EventStartTime))
            {
                process.EventStartTime = DPUtils.GetUtcNow.Date.ToString();
                SaveData(process);
            }
            eventStartTime = DateTimeOffset.Parse(process.EventStartTime).UtcDateTime;
        }
        return  eventStartTime;
    }
    
    // public static DateTime GetChangeGoldByChapterTime()
    // {
    //     if (changeGoldByChapterTime == DateTime.MinValue)
    //     {
    //         if(string.IsNullOrEmpty(process.ChangeGoldByChapterTime))
    //             process.ChangeGoldByChapterTime = DPUtils.GetUtcNow.Date.ToString();
    //         changeGoldByChapterTime = DateTimeOffset.Parse(process.ChangeGoldByChapterTime).UtcDateTime;
    //     }
    //     return  changeGoldByChapterTime;
    // }
    
    private static void SaveData(Process _process)
    {
        Player.SevenDaysMissionProcess = _process;
    }

    public static bool GetTaskDoneStatus(int _dayId, int _taskId)
    {
        // var _process = GetProcess();
        var _dayInfos = process.DayInfos;
        if (_dayId < 0 || _dayId >= _dayInfos.Count) return false;
        var _task = SevenDaysMissionData.GetTask(_dayId, _taskId);
        if (_task == null) return false;
        return GetProgressCount(_task.Type) >= _task.Count;
    }
    
    public static bool GetRewardClaimStatus(int _dayId, int _taskId)
    {
        var _dayInfos = process.DayInfos;
        if (_dayId < 0 || _dayId >= _dayInfos.Count) return false;
        if (_taskId < 0 || _taskId >= _dayInfos[_dayId].RewardClaimStatus.Count) return false;
        return _dayInfos[_dayId].RewardClaimStatus[_taskId];
    }

    public static void SetRewardClaimStatus(int _dayId, int _taskId)
    {
        // var _process = GetProcess();
        var _dayInfos = process.DayInfos;
        if (_dayId < 0 || _dayId >= _dayInfos.Count) return;
        if (_taskId < 0 || _taskId >= _dayInfos[_dayId].RewardClaimStatus.Count) return;
        
        _dayInfos[_dayId].RewardClaimStatus[_taskId] =  true;
        SaveData(process);
    }

    public static bool GetIapBoughtStatus(int _dayId, int _iapId)
    {
        var _dayInfos = process.DayInfos;
        if (_dayId < 0 || _dayId >= _dayInfos.Count) return false;
        if (_iapId < 0 || _iapId >= _dayInfos[_dayId].IapBoughtStatus.Count) return false;
        return _dayInfos[_dayId].IapBoughtStatus[_iapId];
    }
    
    public static void SetIapBoughtStatus(int _dayId, int _iapId)
    {
        // var _process = GetProcess();
        var _dayInfos = process.DayInfos;
        if (_dayId < 0 || _dayId >= _dayInfos.Count) return;
        if (_iapId < 0 || _iapId >= _dayInfos[_dayId].IapBoughtStatus.Count) return;
        
        _dayInfos[_dayId].IapBoughtStatus[_iapId] = true;
        SaveData(process);
    }

    public static bool GetMilestoneClaimStatus(int _milestoneId)
    {
        // var _process = GetProcess();
        if (_milestoneId < 0 || _milestoneId >= process.MilestoneClaimStatus.Count) return false;
        return process.MilestoneClaimStatus[_milestoneId];
    }
    
    public static SEVEN_DAYS_MISSION_RW GetMilestoneRewardClaimed(int _milestoneId)
    {
        // var _process = GetProcess();
        if (_milestoneId < 0 || _milestoneId >= process.MilestoneRewardClaimed.Count) return SEVEN_DAYS_MISSION_RW.NONE;
        return process.MilestoneRewardClaimed[_milestoneId];
    }
    
    public static void SetMilestoneClaimStatus(int _milestoneId, SEVEN_DAYS_MISSION_RW _rewardType)
    {
        // var _process = GetProcess();
        if (_milestoneId < 0 || _milestoneId >= process.MilestoneClaimStatus.Count) return;
        if (_milestoneId < 0 || _milestoneId >= process.MilestoneRewardClaimed.Count) return;
        process.MilestoneClaimStatus[_milestoneId] = true;
        process.MilestoneRewardClaimed[_milestoneId] = _rewardType;
        SaveData(process);
    }

    public static int GetProgressCount(TYPE_MISSION _task)
    {
        var _progress = GetTaskProgress(_task);
        if(_progress == null) return 0;
        return _progress.Count;
    }

    private static TaskProgress GetTaskProgress(TYPE_MISSION _task)
    {
        // var _process = GetProcess();
        foreach (var _progress in process.TaskProgresses)
        {
            if (_progress.Type == _task) return _progress;
        }
        return null;
    }

    public static void HandleEdgeCaseMission()
    {
        var _levelReach = GetProgressCount(TYPE_MISSION.CompleteLevel); // ng choi cu da qua lv can thiet cho task complete lv
        if (_levelReach != Level.LevelMax)
        {
            var _delta = Level.LevelMax - _levelReach;
            UpdateTaskProgress(TYPE_MISSION.CompleteLevel, _delta);
        }

        var _loginCount = GetProgressCount(TYPE_MISSION.Login); // TH ng choi mo khoa su kien nhung ko dc tinh login
        if (_loginCount == 0)
            UpdateTaskProgress(TYPE_MISSION.Login);
    }
    
    public static void UpdateTaskProgress(TYPE_MISSION _task, int _addCount = 1)
    {
        if (!IsUnlock(out _)) return;//

        // var _process = GetProcess();
        GetTaskProgress(_task).Count += _addCount;
        SaveData(process);
    }

    public static int CountTaskDoneInDay(int _dayId)
    {
        int _count = 0;
        var _taskCount = SevenDaysMissionData.GetTasks(_dayId).Count;
        for (int i = 0; i < _taskCount; i++)
        {
            var _isDone = GetTaskDoneStatus(_dayId, i);
            if (_isDone) _count++;
        }
        return _count;
    }

    public static bool GetDayUnlockedStatus(int _dayId)
    {
        var _dayPass = GetStartTime().AddDays(_dayId);
        return DPUtils.GetUtcNow >= _dayPass;
    }
    
    public static void OnBuyIap(IAP_Product _iapProduct)
    {
        var _iap = SevenDaysMissionData.GetIap(_iapProduct);
        if(_iap == null) return;
        foreach (var _rw in _iap.Rewards)
        {
            OnAddReward(_rw);
        }
    }

    private static UISevenDaysMission uiSevenDay;
    
    public static void OnAddReward(SevenDaysMissionReward _reward)
    {
        RewardCount _rwCount = new RewardCount();
        switch (_reward.Type)
        {
            default:
                _rwCount.Type = ConvertRewardType(_reward.Type);
                _rwCount.Count = _reward.AmountByLv;
                Rewards.AddReward(_rwCount, _whereLogReport: AnalyticsWhere.SevenDaysMissionIapReward);
                // UIManager.Instance.GetPopup(PopupId.Messenger).
                break;
                
            case SEVEN_DAYS_MISSION_RW.FIGHTER_6:
                var _fighterProcess = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_6);
                Fighter.Unlock(_fighterProcess);
                UIManager.Instance.GetPopup<UICharacterClaim>(PopupId.CharacterClaim)
                    .Show(_fighterID: FIGHTER_TYPE.FIGHTER_6);
                break;
            
            case SEVEN_DAYS_MISSION_RW.WINGMAN_4:
                var _wingmanProcess = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_4);
                Wingman.Unlock(_wingmanProcess);
                UIManager.Instance.GetPopup<UICharacterClaim>(PopupId.CharacterClaim)
                    .Show(_wingmanID: WINGMAN_TYPE.WINGMAN_4);
                break;
            
            case SEVEN_DAYS_MISSION_RW.EVENT_POINT:
                AddEventPoint(_reward.AmountByLv);
                if (uiSevenDay == null)
                    uiSevenDay = UIManager.Instance.GetPopup<UISevenDaysMission>(PopupId.SevenDaysMission);
                uiSevenDay.ConfigMilestoneProgress();
                break;
        }
    }

    public static void GrantFromMail(IAP_Product _iapProduct)
    {
        var _iap = SevenDaysMissionData.GetIap(_iapProduct);
        if (_iap == null) return;
        CheckGoldByChapter();
        foreach (var _rw in _iap.Rewards)
        {
            OnAddRewardSilent(_rw);
        }
    }

    public static void OnAddRewardSilent(SevenDaysMissionReward _reward)
    {
        RewardCount _rwCount = new RewardCount();
        switch (_reward.Type)
        {
            default:
                _rwCount.Type = ConvertRewardType(_reward.Type);
                _rwCount.Count = _reward.AmountByLv;
                Rewards.AddReward(_rwCount, _whereLogReport: AnalyticsWhere.SevenDaysMissionIapReward);
                break;

            case SEVEN_DAYS_MISSION_RW.FIGHTER_6:
                var _fighterProcess = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_6);
                Fighter.Unlock(_fighterProcess);
                break;

            case SEVEN_DAYS_MISSION_RW.WINGMAN_4:
                var _wingmanProcess = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_4);
                Wingman.Unlock(_wingmanProcess);
                break;

            case SEVEN_DAYS_MISSION_RW.EVENT_POINT:
                AddEventPoint(_reward.AmountByLv);
                if (UIManager.Instance.IsHasPopup<UISevenDaysMission>(PopupId.SevenDaysMission, out var _uiSevenDay))
                    _uiSevenDay.ConfigMilestoneProgress();
                break;
        }
    }

    public static REWARD_TYPE ConvertRewardType(SEVEN_DAYS_MISSION_RW _reward)
    {
        switch (_reward)
        {
            default: return REWARD_TYPE.NONE;
            case SEVEN_DAYS_MISSION_RW.GOLD: return REWARD_TYPE.GOLD;
            case SEVEN_DAYS_MISSION_RW.GEM: return REWARD_TYPE.GEM;
            case SEVEN_DAYS_MISSION_RW.CHIP_TICKET_NORMAL: return REWARD_TYPE.CHIP_TICKET_NORMAL;
            case SEVEN_DAYS_MISSION_RW.CHIP_TICKET_SPECIAL: return REWARD_TYPE.CHIP_TICKET_SPECIAL;
            case SEVEN_DAYS_MISSION_RW.AWARD_COMMON_RANDOM: return REWARD_TYPE.AWARD_COMMON_RANDOM;
            case SEVEN_DAYS_MISSION_RW.AWARD_RARE_RANDOM: return REWARD_TYPE.AWARD_RARE_RANDOM;
            case SEVEN_DAYS_MISSION_RW.AWARD_EPIC_RANDOM: return REWARD_TYPE.AWARD_EPIC_RANDOM;
            case SEVEN_DAYS_MISSION_RW.AWARD_LEGEND_RANDOM: return REWARD_TYPE.AWARD_LEGEND_RANDOM;
            case SEVEN_DAYS_MISSION_RW.AWARD_MYTHIC_RANDOM: return REWARD_TYPE.AWARD_MYTHIC_RANDOM;
            case SEVEN_DAYS_MISSION_RW.FIGHTER_SPECIAL_PIECES: return REWARD_TYPE.FIGHTER_SPECIAL_PIECES;
            case SEVEN_DAYS_MISSION_RW.FIGHTER_ULTRA_PIECES: return REWARD_TYPE.FIGHTER_ULTRA_PIECES;
            case SEVEN_DAYS_MISSION_RW.ENERGY: return REWARD_TYPE.ENERGY;
            case SEVEN_DAYS_MISSION_RW.BF_TICKET: return REWARD_TYPE.BF_TICKET;
            case SEVEN_DAYS_MISSION_RW.WINGMAN_SPECIAL_PIECES: return REWARD_TYPE.WINGMAN_SPECIAL_PIECES;
        }
    }

    public static void AddEventPoint(int _addCount)
    {
        // var _process = GetProcess();
        process.EventPoint += _addCount;
        SaveData(process);
    }
    
    private static void CreateData()
    {
        process = new Process();
        
        var _dayInfos = process.DayInfos;
        for (int i = 0; i < SevenDaysMissionData.Days.Count; i++)
        {
            _dayInfos.Add(new DayInfo());
            var _tasks = SevenDaysMissionData.GetTasks(i);
            var _iaps = SevenDaysMissionData.GetIaps(i);
            for (int j = 0; j < _tasks.Count; j++)
            {
                _dayInfos[i].RewardClaimStatus.Add(false);
            }
            for (int k = 0; k < _iaps.Count; k++)
            {
                _dayInfos[i].IapBoughtStatus.Add(false);
            }
        }
        
        var _milestoneClaimStatus = process.MilestoneClaimStatus;
        var _alternateMilestoneRwStatus = process.MilestoneRewardClaimed;
        for (int i = 0; i < SevenDaysMissionData.Milestones.Count; i++)
        {
            _milestoneClaimStatus.Add(false);
            _alternateMilestoneRwStatus.Add(SEVEN_DAYS_MISSION_RW.NONE);
        }
        
        var _taskProgress = process.TaskProgresses;
        foreach (var _taskDict in SevenDaysMissionData.TaskDict)
        {
            var _progress = new TaskProgress
            {
                Type = _taskDict.Type,
                Count = 0
            };
            _taskProgress.Add(_progress);
        }

        if (process.TypeEvent == TYPE_7DAYS_EVENT.NONE)
            process.TypeEvent = TYPE_7DAYS_EVENT.SEVENDAYS_EVENT_1;
        else
        {
            
        }
        
        SaveData(process);
    }

    public static void CheckGoldByChapter()
    {
        // var _days = (GetChangeGoldByChapterTime() - GetStartTime()).TotalDays;
        if (process.GoldByChapter == -1 || DPUtils.GetUtcNow > GetEndTime())
        {
            SetGoldByChapter();
        }
    }

    public static int GetGoldByChapter()
    {
        return process.GoldByChapter;
    }

    public static void SetGoldByChapter()
    {
        process.GoldByChapter = GoldByChapter.GetGoldByChapter(GOLD_BY_CHAPTER_ID.SEVEN_DAYS_MISSION);
        SaveData(process);
        // process.ChangeGoldByChapterTime = DPUtils.GetUtcNow.Date.ToString();
    }

    public static bool GetFirstOpen()
    {
        return process.IsFirstOpen;
    }
    
    public static void SetFirstOpen()
    {
        process.IsFirstOpen =  true;
        SaveData(process);
    }

    public static bool IsHack
    {
        get => PlayerPrefs.GetInt("SevenDaysMission_IsHack", 0) == 1;
        set => PlayerPrefs.SetInt("SevenDaysMission_IsHack", value ? 1 : 0);
    }

    private static Action callbackSuccess;
    private static Action callbackError;
    public static void OnCheckInternet(Action _callbackSuccess, Action _callbackError)
    {
        callbackSuccess = _callbackSuccess;
        callbackError = _callbackError;
        UIManager.Instance.GetPopup(PopupId.LoadingInternet).Show();
        // DHS.GetAllEvent(Manager.Instance, _callback: OnCompleteRequest);
        Manager.Instance.StartCoroutine(PingServer());
    }
    
    private static IEnumerator PingServer()
    {
        // Use a HEAD request to minimize data transfer
        using (UnityWebRequest request = UnityWebRequest.Head("https://www.google.com"))
        {
            request.timeout = 3; // Keep the timeout short to avoid hanging
            yield return request.SendWebRequest();
            OnCompleteRequest(request.result == UnityWebRequest.Result.Success, "");
        }
    }
    
    public static void OnCompleteRequest(bool _isSuccess, string _result)
    {
        UIManager.Instance.GetPopup(PopupId.LoadingInternet).Hide();
        if (!_isSuccess)
        {
            callbackError?.Invoke();
            var _popupMission = UIManager.Instance.GetPopup<UISevenDaysMission>(PopupId.SevenDaysMission);
            _popupMission?.OnShowNoInternet();
        }
        else
        {
            callbackSuccess?.Invoke();
        }
    }

    [Serializable]
    public class Process
    {
        public TYPE_7DAYS_EVENT TypeEvent;
        
        public string EventStartTime;

        // public string ChangeGoldByChapterTime;
        
        public int EventPoint;
        
        public List<TaskProgress> TaskProgresses;

        public List<DayInfo> DayInfos;

        public List<bool> MilestoneClaimStatus;
        
        public List<SEVEN_DAYS_MISSION_RW> MilestoneRewardClaimed;

        public int GoldByChapter;

        public bool IsFirstOpen;

        public Process()
        {
            DayInfos = new List<DayInfo>();

            MilestoneClaimStatus = new List<bool>();

            TaskProgresses = new List<TaskProgress>();

            MilestoneRewardClaimed = new List<SEVEN_DAYS_MISSION_RW>();

            GoldByChapter = -1;

            TypeEvent = TYPE_7DAYS_EVENT.NONE;
        }
    }
    
    [Serializable]
    public class DayInfo
    {
        public List<bool> RewardClaimStatus; // check reward nhiem vu: da nhan = true, chua nhan = false
        public List<bool> IapBoughtStatus; // check goi iap: da mua = true, chua mua = false

        public DayInfo()
        {
            RewardClaimStatus = new List<bool>();
            IapBoughtStatus = new List<bool>();
        }
    }

    [Serializable]
    public class TaskProgress
    {
        public TYPE_MISSION Type;
        public int Count;
    }
}

public enum TYPE_7DAYS_EVENT
{
    NONE = 0,
    SEVENDAYS_EVENT_1 = 1,
    SEVENDAYS_EVENT_2 = 2,
}
