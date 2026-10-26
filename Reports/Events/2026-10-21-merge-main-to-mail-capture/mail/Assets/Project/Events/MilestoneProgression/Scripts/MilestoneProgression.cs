using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using Games;
using UnityEngine;

public static class MilestoneProgression
{
    static MilestoneProgression() => EnsureProcess();
    public static void Refresh() => EnsureProcess();
    // đảm bảo process tồn tại, nếu chưa có hoặc version cũ thì reset mới và lưu lại
    static void EnsureProcess()
    {
        process = Player.MilestoneProgressionProcess1;
        if (process == null || process.IsOutdatedVersion(CurrentVersion))
        {
            // lên phiên bản mới reset toàn bộ tiến trình cũ của user
            ResetProcess();
        }
        OnNewDay();
    }
    static Process process;
    public static Process GetProcess() => process;
    static void SaveData()
    {
        Player.MilestoneProgressionProcess1 = process;
    }
    public static readonly string CurrentVersion = "0.2";// phiên bản hiện tại
    // event_index đang active (từ connect response): dùng để chọn bộ milestone trong SO
    static int activeEventIndex;
    public static int ActiveEventIndex => activeEventIndex;
    public static void SetActiveEventIndex(int _index)
    {
        activeEventIndex = _index;
        if (process == null) return;
        // LastEventIndex == 0: save cũ / chưa mở popup lần nào → data (nếu có, tích từ gameplay) coi như thuộc phase đầu (index 1)
        int _lastIndex = process.LastEventIndex == 0 ? 1 : process.LastEventIndex;
        // đổi sang event khác (index luôn khác cho event kế tiếp) → reset sạch tiến trình cho phase mới
        if (_lastIndex != _index)
        {
            ResetProcess();// TotalPoint=0, clear Milestones/PointAdditionals, IsX2/X4=false, intro hiện lại
            DPDebug.Log($"<color=#ffd900>[MilestoneProgression]</color> đổi event {_lastIndex} -> {_index}, reset tiến trình cho phase mới");
        }
        process.LastEventIndex = _index;
        SaveData();
    }
    // hàm reset tiến trình event về mặc định (dùng khi có update lớn cần reset tiến trình của user)
    static void ResetProcess()
    {
        process = new Process { Version = CurrentVersion };
        SaveData();
    }
    // end event
    static bool isEndEvent = false;
    public static bool IsEndEvent
    {
        get { return isEndEvent; }
        set { isEndEvent = value; }
    }
    public static readonly int LevelUnlock = 25;
    public static bool IsUnlocked
    {
        get
        {
            if (Level.LevelMax >= LevelUnlock) return true;
            return false;
        }
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
        if (!DPUtils.IsNewDay(LastOpen)) return;
        // sang ngày mới
        process.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
        // sang ngày mới → cho intro hiện lại, bỏ lựa chọn "không hiển thị nữa" của ngày hôm trước
        process.IsChooseDontShowIntro = false;
        // Reset tiến trình của các mốc về trạng thái chưa hoàn thành
        ResetDailyPointAdditionals();
        //
        SaveData();
    }
    public static bool ICanClaim
    {
        get
        {
            foreach (var _item in GetAllMilestoneProcess())
            {
                if (_item.Stats == REWARD_STATS.CLAIM)
                {
                    return true;
                }
            }
            return false;
        }
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
    public static void HackNewDay()
    {
        if (process == null) return;
        DPDebug.Log($"[HACK] Simulating new day. Old DayOfYear: {process.LastOpen}");
        process.LastOpen = DPUtils.GetUtcNow.AddDays(-1).Ticks.ToString(); // Set to an arbitrary different value to force OnNewDay to trigger
        SaveData();
        OnNewDay();
        DPDebug.Log($"[HACK] New day simulated. New DayOfYear: {process.LastOpen}");
    }
    public static void HackAddPoint(int _pointAdd)
    {
        process.TotalPoint += _pointAdd;
        SaveData();
        DPDebug.Log($"Milestone progression hack add point toltal[{process.TotalPoint}]---add[{_pointAdd}]");
    }
    // reset toàn bộ tiến trình event về mặc định (dùng cho testing)
    public static void HackReset()
    {
        ResetProcess();
        DPDebug.Log($"[HACK] Milestone progression reset. TotalPoint[{process.TotalPoint}]");
    }
    // Tổng điểm hiện tại của người chơi
    public static int TotalPoint
    {
        get
        {
            DPDebug.Log($"MilestoneProgression tổng điểm hiện tại của người chơi: {process.TotalPoint}");
            return process.TotalPoint;
        }
    }
    // GOLD_BY_CHAPTER_ID dùng chung cho mọi milestone GOLD quy đổi qua RewardByProgression.
    const GOLD_BY_CHAPTER_ID ProgressionGoldByChapterId = GOLD_BY_CHAPTER_ID.GOLD_BY_CHAPTER_MILESTONE;
    public static RewardCount GetReward(int _id)
    {
        var _rw = MilestoneProgressionSO.GetData().Milestones.FirstOrDefault(x => x.Id == _id);
        var _reward = _rw.Reward;

        // Reward không có alias BP_* (vd FIGHTER_13/FIGHTER_14: cấp thẳng 1 phi công cụ thể) -> Count cố định, không quy đổi theo tiến độ.
        if (!RewardByProgressionSystem.IsProgressionReward(_reward.Type))
        {
            return new RewardCount
            {
                Type = _reward.Type,
                Count = Mathf.RoundToInt(_reward.Multiplier),
                chipID = _reward.chipID,
                awardID = _reward.awardID,
                fighterID = _reward.fighterID,
                wingmanID = _reward.wingmanID,
            };
        }

        // Progression reward: đã chốt snapshot 1 lần lúc record được tạo (lần đầu mở event) -> luôn trả về snapshot, không quy đổi lại theo tiến độ hiện tại.
        var _milestone = GetMilestoneProcess(_id);
        return new RewardCount { Type = _milestone.RewardType, Count = _milestone.RewardCountValue };
    }
    // Quy đổi reward theo tiến độ hiện tại và chốt vào record - chỉ gọi lúc record vừa được tạo (lần đầu mở event), không gọi lại sau đó.
    static void ResolveRewardSnapshot(MilestoneProcess _milestone, RewardMultiplier _reward)
    {
        if (!RewardByProgressionSystem.IsProgressionReward(_reward.Type)) return;
        var _resolved = RewardByProgressionSystem.GetRewardCount(_reward.Type, _reward.Multiplier);
        if (_resolved.Type == REWARD_TYPE.GOLD && _resolved.Count > 0)
            _resolved.Count = GoldByChapter.GetGoldByChapterWithMultiply(ProgressionGoldByChapterId, _resolved.Count);
        _milestone.RewardType = _resolved.Type;
        _milestone.RewardCountValue = _resolved.Count;
    }
    // Nhận phần thưởng của mốc
    public static void ClaimReward(int _id)
    {
        var _milestone = GetMilestoneProcess(_id);
        if (_milestone != null && _milestone.Stats == REWARD_STATS.CLAIM)
        {
            _milestone.Stats = REWARD_STATS.CLAIMED;// Cập nhật trạng thái phần thưởng của mốc này thành đã nhận phần thưởng
            SaveData();
        }
        // nhận phần thưởng tương ứng với mốc này
        RewardCount _rw = GetReward(_id);
        DPDebug.Log($"Claim reward for milestone ID={_id}. Reward: {_rw.Type} x {_rw.Count}");
        Rewards.AddReward(_reward: _rw, _whereLogReport: "MILESTONE_PROGRESSION_CLAIM_REWARD");
    }
    // Hoàn thành mốc
    public static void CompleteMilestone(int _id)
    {
        var _milestone = GetMilestoneProcess(_id);
        if (_milestone != null)
        {
            _milestone.Stats = REWARD_STATS.CLAIM;// Cập nhật trạng thái phần thưởng của mốc này thành có thể nhận phần thưởng
            SaveData();
        }
    }
    // Lấy tiến trình của mốc, nếu chưa có thì tạo mới
    public static MilestoneProcess GetMilestoneProcess(int _id)
    {
        var _milestone = process.Milestones.FirstOrDefault(x => x.Id == _id);
        var _milestoneData = MilestoneProgressionSO.GetData().Milestones.FirstOrDefault(x => x.Id == _id);
        if (_milestone == null)
        {
            _milestone = new MilestoneProcess
            {
                Id = _id,
                Stats = process.TotalPoint >= _milestoneData.PointRequire ? REWARD_STATS.CLAIM : REWARD_STATS.LOCK
            };
            ResolveRewardSnapshot(_milestone, _milestoneData.Reward);
            process.Milestones.Add(_milestone);
            SaveData();
        }
        // nếu có rồi thì kiểm tra xem đã đủ điều kiện claim không
        if (_milestone.Stats == REWARD_STATS.LOCK)
        {
            if (process.TotalPoint >= _milestoneData.PointRequire)
            {
                _milestone.Stats = REWARD_STATS.CLAIM;
                SaveData();
            }
        }
        // CLAIM nhưng thực ra chưa đủ điểm theo data phase đang active (VD: bị set sai lúc ActiveEventIndex chưa kịp sync) → hạ lại LOCK
        else if (_milestone.Stats == REWARD_STATS.CLAIM && process.TotalPoint < _milestoneData.PointRequire)
        {
            _milestone.Stats = REWARD_STATS.LOCK;
            SaveData();
            DPDebug.Log($"<color=#ff4a4a>[MilestoneProgression]</color> milestone ID={_id} bị CLAIM sai (TotalPoint={process.TotalPoint} < PointRequire={_milestoneData.PointRequire}), hạ lại LOCK");
        }
        return _milestone;
    }
    // Lấy tất cả tiến trình của các mốc, sắp xếp theo Id
    public static List<MilestoneProcess> GetAllMilestoneProcess()
    {
        if (process.Milestones.Count <= 0)
        {
            // Nếu chưa có tiến trình nào thì khởi tạo tiến trình cho tất cả các mốc, chốt snapshot reward theo tiến độ hiện tại (lần đầu mở event)
            process.Milestones = MilestoneProgressionSO.GetData().Milestones
            .Select(x =>
            {
                var _milestone = new MilestoneProcess { Id = x.Id, Stats = REWARD_STATS.LOCK };
                ResolveRewardSnapshot(_milestone, x.Reward);
                return _milestone;
            })
            .OrderBy(x => x.Id)
            .ToList();
            //
            SaveData();
            return process.Milestones;
        }
        // kiểm tra xem có mốc nào đã đạt mới hay không
        MilestoneProgressionSO.MilestoneData _milestoneData;
        foreach (var _process in process.Milestones)
        {
            if (_process.Stats == REWARD_STATS.CLAIMED) continue;
            //
            _milestoneData = MilestoneProgressionSO.GetData().Milestones.FirstOrDefault(x => x.Id == _process.Id);
            bool _isEnough = process.TotalPoint >= _milestoneData.PointRequire;
            if (_process.Stats == REWARD_STATS.LOCK)
            {
                if (_isEnough)
                {
                    // đã đạt mốc mới, chuyển đổi trạng thái cho mốc này
                    _process.Stats = REWARD_STATS.CLAIM;
                    SaveData();
                }
                else
                {
                    // nếu mốc đầu check mà đã không đạt thì k cần check những mốc sau
                    break;
                }
            }
            else if (!_isEnough)
            {
                // CLAIM nhưng thực ra chưa đủ điểm theo data phase đang active (VD: bị set sai lúc ActiveEventIndex chưa kịp sync) → hạ lại LOCK
                _process.Stats = REWARD_STATS.LOCK;
                SaveData();
                DPDebug.Log($"<color=#ff4a4a>[MilestoneProgression]</color> milestone ID={_process.Id} bị CLAIM sai (TotalPoint={process.TotalPoint} < PointRequire={_milestoneData.PointRequire}), hạ lại LOCK");
            }
        }
        return process.Milestones.OrderBy(x => x.Id).ToList();
    }
    // Reset tiến trình của các mốc về trạng thái chưa hoàn thành, giữ nguyên điểm hiện tại của người chơi hang ngày
    public static List<MilestonePointAdditionalProcess> ResetDailyPointAdditionals()
    {
        if (process.PointAdditionals.Count <= 0)
        {
            // Nếu chưa có tiến trình nào thì khởi tạo tiến trình cho tất cả các mốc
            process.PointAdditionals = MilestoneProgressionSO.GetData().PointAdditional
            .Select(x => new MilestonePointAdditionalProcess
            {
                ID = x.ID,
                CurrentPoint = 0,
                // Nếu đang trong trạng thái x4 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân 4, nếu đang trong trạng thái x2 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân đôi, ngược lại sẽ giữ nguyên giá trị gốc
                PointAdditionalMax = x.PointAdditionalMax * FactorPointAdditional
            })
            .ToList();
            //
            SaveData();
            return process.PointAdditionals;
        }
        // Reset điểm thưởng thêm hiện tại về 0, giữ nguyên điểm thưởng thêm tối đa
        foreach (var _milestonePointAdditional in process.PointAdditionals)
        {
            _milestonePointAdditional.CurrentPoint = 0;
            _milestonePointAdditional.PointAdditionalMax = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _milestonePointAdditional.ID)
            // Nếu đang trong trạng thái x4 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân 4, nếu đang trong trạng thái x2 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân đôi, ngược lại sẽ giữ nguyên giá trị gốc
            .PointAdditionalMax * FactorPointAdditional;
        }
        SaveData();
        DPDebug.Log($"Reset daily point additionals. TotalPoint: {process.TotalPoint}, PointAdditionals count: {process.PointAdditionals.Count}");
        return process.PointAdditionals;
    }
    // Lấy tiến trình của điểm thưởng thêm, nếu chưa có thì tạo mới
    public static List<MilestonePointAdditionalProcess> GetAllMilestonePointAdditional()
    {
        if (process.PointAdditionals.Count <= 0)
        {
            // Lấy hệ số điểm thưởng thêm, nếu đang trong trạng thái x4 điểm thì hệ số sẽ là 4, nếu đang trong trạng thái x2 điểm thì hệ số sẽ là 2, ngược lại sẽ là 1
            // Nếu chưa có tiến trình nào thì khởi tạo tiến trình cho tất cả các mốc
            process.PointAdditionals = MilestoneProgressionSO.GetData().PointAdditional
            .Select(x => new MilestonePointAdditionalProcess
            {
                ID = x.ID,
                CurrentPoint = 0,
                PointAdditionalMax = x.PointAdditionalMax * FactorPointAdditional
            })
            .ToList();
            //
            SaveData();
            return process.PointAdditionals;
        }
        return process.PointAdditionals;
    }
    // Lấy tiến trình của điểm thưởng thêm theo ID, nếu chưa có thì tạo mới
    public static MilestonePointAdditionalProcess GetMilestonePointAdditionalProcess(MilestoneProgressionSO.MILESTONE_POINT _id)
    {
        var _milestonePointAdditional = process.PointAdditionals.Find(x => x.ID == _id);
        if (_milestonePointAdditional == null)
        {
            // Lấy hệ số điểm thưởng thêm, nếu đang trong trạng thái x4 điểm thì hệ số sẽ là 4, nếu đang trong trạng thái x2 điểm thì hệ số sẽ là 2, ngược lại sẽ là 1
            _milestonePointAdditional = new MilestonePointAdditionalProcess
            {
                ID = _id,
                CurrentPoint = 0,
                PointAdditionalMax = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _id).PointAdditionalMax * FactorPointAdditional
            };
            process.PointAdditionals.Add(_milestonePointAdditional);
            SaveData();
        }
        return _milestonePointAdditional;
    }
    // đồng bộ lại PointAdditionalMax theo phase đang active (gọi sau khi biết event_index từ server), giữ nguyên CurrentPoint
    public static void SyncPointAdditionalMax()
    {
        foreach (var _milestonePointAdditional in GetAllMilestonePointAdditional())
        {
            var _data = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _milestonePointAdditional.ID);
            _milestonePointAdditional.PointAdditionalMax = _data.PointAdditionalMax * FactorPointAdditional;
        }
        SaveData();
        DPDebug.Log($"<color=#4aff21>[MilestoneProgression]</color> SyncPointAdditionalMax theo EventIndex {ActiveEventIndex}");
    }
    // mua goi x2 điểm thưởng thêm
    public static void SetIsX2Point(bool _isX2Point)
    {
        process.IsX2Point = _isX2Point;
        // Nếu đang trong trạng thái x2 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân đôi, ngược lại sẽ giữ nguyên giá trị gốc
        foreach (var _milestonePointAdditional in GetAllMilestonePointAdditional())
        {
            var _data = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _milestonePointAdditional.ID);
            _milestonePointAdditional.PointAdditionalMax = _data.PointAdditionalMax * 2;
        }
        DPDebug.Log($"Mua thành công gói x2 điểm thưởng thêm. IsX2Point: {process.IsX2Point}, PointAdditionalMax đã được cập nhật tương ứng.");
        SaveData();
    }
    public static void SetIsX4Point(bool _isX4Point)
    {
        process.IsX4Point = _isX4Point;
        // Nếu đang trong trạng thái x4 điểm thì điểm thưởng thêm tối đa cũng sẽ được nhân 4, ngược lại sẽ giữ nguyên giá trị gốc
        foreach (var _milestonePointAdditional in GetAllMilestonePointAdditional())
        {
            var _data = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _milestonePointAdditional.ID);
            _milestonePointAdditional.PointAdditionalMax = _data.PointAdditionalMax * 4;
        }
        DPDebug.Log($"Mua thành công gói x4 điểm thưởng thêm. IsX4Point: {process.IsX4Point}, PointAdditionalMax đã được cập nhật tương ứng.");
        SaveData();
    }
    // Cộng điểm thưởng thêm khi thỏa mãn điều kiện của mốc, nếu điểm thưởng thêm vượt quá điểm thưởng thêm tối đa thì sẽ không được cộng thêm điểm nữa
    public static void AddPointAdditional(MilestoneProgressionSO.MILESTONE_POINT _id, int _value, bool _isStats = false)
    {
        if (!IsUnlocked) return;
        // 
        var _process = GetMilestonePointAdditionalProcess(_id);
        var _currentPoint = _process.CurrentPoint;
        var _pointAdditionalMax = process.PointAdditionals.Find(x => x.ID == _id)?.PointAdditionalMax ?? 0;
        if (_currentPoint >= _pointAdditionalMax)
        {
            // Nếu điểm thưởng thêm hiện tại đã đạt hoặc vượt quá điểm thưởng thêm tối đa thì sẽ không được cộng thêm điểm nữa
            DPDebug.Log($"Current additional point của ID={_id} đã đạt hoặc vượt quá điểm thưởng thêm tối đa ({_currentPoint} >= {_pointAdditionalMax}), không được cộng thêm điểm nữa.");
            return;
        }
        // Lấy thông tin điểm thưởng thêm của mốc tương ứng
        var _pointAdditionalData = MilestoneProgressionSO.GetData().PointAdditional.FirstOrDefault(x => x.ID == _id);
        var _pointAdd = _value * (_pointAdditionalData.IsStates
        ? (_isStats ? _pointAdditionalData.PointAdditionalTrue : _pointAdditionalData.PointAdditionalFalse) : _pointAdditionalData.PointAdditional);
        // Nếu đang trong trạng thái x4 điểm thì điểm thưởng thêm cũng sẽ được nhân 4, nếu đang trong trạng thái x2 điểm thì điểm thưởng thêm cũng sẽ được nhân đôi, ngược lại sẽ giữ nguyên giá trị gốc
        _pointAdd *= FactorPointAdditional;

        // Điểm thực tế có thể cộng bị giới hạn bởi điểm thưởng thêm tối đa
        var _actualPointAdd = Mathf.Min(_pointAdd, _pointAdditionalMax - _currentPoint);
        _currentPoint += _actualPointAdd;
        _process.CurrentPoint = _currentPoint;
        process.TotalPoint += _actualPointAdd;
        DPDebug.Log($"Adding additional point: ID={_id}, Value={_value}, IsStats={_isStats}, PointAdd={_pointAdd}, ActualPointAdd={_actualPointAdd}, CurrentPoint={_currentPoint}, PointAdditionalMax={_pointAdditionalMax}");
        // cập nhật tổng điểm của người chơi
        SaveData();
    }
    // Hệ số điểm thưởng thêm, nếu đang trong trạng thái x4 điểm thì hệ số sẽ là 4, nếu đang trong trạng thái x2 điểm thì hệ số sẽ là 2, ngược lại sẽ là 1
    public static int FactorPointAdditional { get { return process.IsX4Point ? 4 : process.IsX2Point ? 2 : 1; } }
    // số lượt đã mua của gói point trong phase hiện tại
    public static int GetGoalPackBoughtCount(MilestoneProgressionSO.GOAL_POINT_PACK _id)
    {
        return process.GoalPointPacks.Find(x => x.ID == _id)?.BoughtCount ?? 0;
    }
    // còn lượt mua hay không (limit theo phase, reset cùng ResetProcess)
    public static bool CanBuyGoalPack(MilestoneProgressionSO.GOAL_POINT_PACK _id)
    {
        var _packs = MilestoneProgressionSO.GetData().GoalPointPacks;
        if (_packs == null) return false;
        var _data = _packs.FirstOrDefault(x => x.ID == _id);
        return GetGoalPackBoughtCount(_id) < _data.Limit;
    }
    // mua gói point: cộng thẳng vào TotalPoint, KHÔNG qua daily cap, KHÔNG nhân factor x2/x4
    public static void BuyGoalPointPack(MilestoneProgressionSO.GOAL_POINT_PACK _id)
    {
        var _packs = MilestoneProgressionSO.GetData().GoalPointPacks;
        if (_packs == null) return;
        var _data = _packs.FirstOrDefault(x => x.ID == _id);
        var _bought = process.GoalPointPacks.Find(x => x.ID == _id);
        if (_bought == null)
        {
            _bought = new GoalPointPackProcess { ID = _id, BoughtCount = 0 };
            process.GoalPointPacks.Add(_bought);
        }
        if (_bought.BoughtCount >= _data.Limit)
        {
            DPDebug.Log($"<color=#ffd900>[MilestoneProgression]</color> goal pack {_id} hết lượt mua ({_bought.BoughtCount}/{_data.Limit})");
            return;
        }
        process.TotalPoint += _data.Point;
        _bought.BoughtCount++;
        SaveData();
        DPDebug.Log($"<color=#4aff21>[MilestoneProgression]</color> mua goal pack {_id} +{_data.Point} point, bought {_bought.BoughtCount}/{_data.Limit}, total {process.TotalPoint}");
    }
    // Mail bù IAP thật đã trả tiền: goi thang BuyGoalPointPack, khong can bypass gi vi ham nay
    // von da khong co gate nao khac ngoai Limit/BoughtCount (dung guard chong double-grant co san).
    public static bool GrantFromMail(IAP_Product _product)
    {
        var _packs = MilestoneProgressionSO.GetData().GoalPointPacks;
        if (_packs == null) return false;

        var _data = _packs.FirstOrDefault(x => x.Product == _product);
        if (_data.ID == MilestoneProgressionSO.GOAL_POINT_PACK.NONE) return false;

        if (!CanBuyGoalPack(_data.ID)) return false;

        BuyGoalPointPack(_data.ID);
        return true;
    }
    //
    [System.Serializable]
    public class Process : EventProcessBase
    {
        public int TotalPoint = 0;// Tổng điểm đã đạt được, bao gồm cả điểm thưởng thêm
        public string LastOpen = string.Empty;
        public List<MilestoneProcess> Milestones = new();
        public List<MilestonePointAdditionalProcess> PointAdditionals = new();
        public bool IsX2Point = false;// Có đang trong trạng thái x2 điểm hay không
        public bool IsX4Point = false;// Có đang trong trạng thái x4 điểm hay không
        public bool IsChooseDontShowIntro = false; // Người chơi chọn không hiển thị intro nữa
        public int LastEventIndex = 0; // event_index của phase gần nhất user tham gia, để phát hiện đổi phase và reset
        public List<GoalPointPackProcess> GoalPointPacks = new();// số lượt đã mua từng gói point trong phase, reset cùng ResetProcess
    }
    [System.Serializable]
    public class MilestoneProcess
    {
        public int Id;
        public REWARD_STATS Stats;// Thông tin phần thưởng của mốc này, sẽ được cập nhật khi hoàn thành mốc
        public REWARD_TYPE RewardType = REWARD_TYPE.NONE;// Loại reward progression đã quy đổi, chốt 1 lần lúc record được tạo (lần đầu mở event)
        public int RewardCountValue = 0;// Số lượng reward progression đã quy đổi, chốt cùng lúc với RewardType
    }
    [System.Serializable]
    public class MilestonePointAdditionalProcess
    {
        public MilestoneProgressionSO.MILESTONE_POINT ID;
        public int CurrentPoint = 0;// Điểm thưởng thêm hiện tại
        public int PointAdditionalMax = 0;// Điểm thưởng thêm tối đa khi hoàn thành mốc này, nếu điểm thưởng thêm vượt quá giá trị này thì sẽ không được cộng thêm điểm nữa
    }
    [System.Serializable]
    public class GoalPointPackProcess
    {
        public MilestoneProgressionSO.GOAL_POINT_PACK ID;
        public int BoughtCount = 0;// số lượt đã mua gói này trong phase hiện tại
    }
}
