using System;
using System.Collections.Generic;
using DP.Utilities;
using Networking.Models;
using UnityEngine;
using static WorldCupSO;

public static class WorldCup
{
    static Process process;
    static WorldCup() => EnsureProcess();
    public static void Refresh() => EnsureProcess();
    // đảm bảo process tồn tại, nếu chưa có thì khởi tạo mới và lưu lại (persist qua Player, pattern BattleTeam)
    static void EnsureProcess()
    {
        process = Player.WorldCupEventProcess;
        if (process == null || IsOutdatedVersion(process.Version))
            ResetProcess();
    }
    public static readonly string CurrentVersion = "0.1";// phiên bản hiện tại
    // reset tiến trình về mặc định (dùng khi update lớn cần reset tiến trình của user)
    static void ResetProcess()
    {
        process = new Process { Version = CurrentVersion };
        SaveData();
    }
    // true nếu version đã lưu thấp hơn CurrentVersion --> reset tiến trình event
    static bool IsOutdatedVersion(string _savedVersion)
    {
        if (string.IsNullOrEmpty(_savedVersion)) return false;
        if (!System.Version.TryParse(_savedVersion, out var _saved)) return false;
        if (!System.Version.TryParse(CurrentVersion, out var _current)) return false;
        return _current > _saved;
    }
    public static Process GetProcess() => process;
    // reset toàn bộ tiến trình + runtime test về mặc định (dùng cho TestingTools)
    public static void HackReset()
    {
        ResetProcess();
#if UNITY_EDITOR
        ClearStateOverride();// [TEST-OVERRIDE]
        responseTimeOverride = false;// [TEST-OVERRIDE]
#endif
        winningTeamId = "";
        result = PREDICTION_RESULT.NONE;
    }
    public static event Action OnChanged;// UI subscribe để tự refresh khi trạng thái user đổi
    // persist xuống save file (Player.SaveData) + bắn event refresh - 1 cửa duy nhất
    static void SaveData()
    {
        Player.WorldCupEventProcess = process;
        OnChanged?.Invoke();
    }
    public static FinalPredictionData GetData() => WorldCupSO.GetData().FinalPrediction;
    public static string Name => "EVENT_WORLD_CUP";
    //
    // trạng thái module theo bảng Unlock & Hiển thị (mục 4)
    public enum MODULE_STATE
    {
        HIDDEN = 0,
        PREDICTION_OPEN = 1,
        PREDICTION_SUBMITTED = 2,
        PREDICTION_LOCKED = 3,
        RESULT_AVAILABLE = 4,
        OFFER_ACTIVE = 5,
        ENDED = 6,
    }
    //========================================================================================================================================================================
    // PROGRESS (lưu của user) - route qua process
    public static string SelectedTeamId
    {
        get => process.SelectedTeamId;
        set
        {
            process.SelectedTeamId = value;
            SaveData();
        }
    }
    public static bool IsSubmitted
    {
        get => process.IsSubmitted;
        set
        {
            process.IsSubmitted = value;
            SaveData();
        }
    }
    public static bool IsConfirmed
    {
        get => process.IsConfirmed;
        set
        {
            process.IsConfirmed = value;
            SaveData();
        }
    }
    public static int AdsChangeUsed
    {
        get => process.AdsChangeUsed;
        set
        {
            process.AdsChangeUsed = value;
            SaveData();
        }
    }
    public static bool IsRewardClaimed
    {
        get => process.IsRewardClaimed;
        set
        {
            process.IsRewardClaimed = value;
            SaveData();
        }
    }
    public static bool IsFreeVoucherClaimed
    {
        get => process.IsFreeVoucherClaimed;
        set
        {
            process.IsFreeVoucherClaimed = value;
            SaveData();
        }
    }
    public static bool IsExtraVoucherUnlocked
    {
        get => process.IsExtraVoucherUnlocked;
        set
        {
            process.IsExtraVoucherUnlocked = value;
            SaveData();
        }
    }
    public static bool IsMythicPackPurchased
    {
        get => process.IsMythicPackPurchased;
        set
        {
            process.IsMythicPackPurchased = value;
            SaveData();
        }
    }
    public static bool IsLegendPackPurchased
    {
        get => process.IsLegendPackPurchased;
        set
        {
            process.IsLegendPackPurchased = value;
            SaveData();
        }
    }
    //
    // HELPERS suy ra từ process
    public static bool CanChangeByAds => CanCommitAdsChange && process.AdsChangeUsed < GetData().MaxChangeByAds;
    // đủ điều kiện commit đổi đội bằng ads (KHÔNG xét số lượt - lượt đã tiêu lúc xem ads xong ở OnWatchAdsChange)
    static bool CanCommitAdsChange => process.IsSubmitted && !process.IsConfirmed && IsBeforeAdsLock;
    public static int ChangeRemaining => GetData().MaxChangeByAds - process.AdsChangeUsed;
    // teamId có phải 1 trong 2 đội chung kết hay không
    public static bool IsFinalist(string _teamId) => !string.IsNullOrEmpty(_teamId) && (_teamId == finalistTeamIdA || _teamId == finalistTeamIdB);
    //========================================================================================================================================================================
    // ACTIONS - PREDICTION FLOW (nhóm A)
    // chọn đội (chưa submit hoặc đang đổi). Chỉ đổi được khi chưa chốt cuối
    public static bool OnSelectTeam(string _teamId)
    {
        if (State != MODULE_STATE.PREDICTION_OPEN)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSelectTeam thất bại: sai trạng thái - state {State}");
            return false;
        }
        if (process.IsConfirmed)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSelectTeam thất bại: đã chốt cuối, không đổi được - teamId {_teamId}");
            return false;
        }
        if (!IsFinalist(_teamId))
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSelectTeam thất bại: teamId không hợp lệ - teamId {_teamId}");
            return false;
        }
        SelectedTeamId = _teamId;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Chọn đội - teamId {_teamId}");
        return true;
    }
    // submit dự đoán lần đầu
    public static bool OnSubmit()
    {
        if (State != MODULE_STATE.PREDICTION_OPEN)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSubmit thất bại: sai trạng thái - state {State}");
            return false;
        }
        if (process.IsSubmitted)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSubmit thất bại: đã submit rồi (one active prediction)");
            return false;
        }
        if (!IsFinalist(process.SelectedTeamId))
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnSubmit thất bại: chưa chọn đội hợp lệ - teamId {process.SelectedTeamId}");
            return false;
        }
        IsSubmitted = true;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Submit dự đoán - teamId {process.SelectedTeamId}");
        WorldCupLog.Fire(WorldCupLog.WC_PREDICTION);
        return true;
    }
    // tiêu 1 lượt đổi-bằng-ads NGAY khi xem ads xong (không chờ confirm). false nếu hết lượt / ngoài cửa đổi
    public static bool OnWatchAdsChange()
    {
        if (State != MODULE_STATE.PREDICTION_SUBMITTED)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnWatchAdsChange thất bại: sai trạng thái - state {State}");
            return false;
        }
        if (!CanChangeByAds)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnWatchAdsChange thất bại: không còn được đổi - submitted {process.IsSubmitted} confirmed {process.IsConfirmed} used {process.AdsChangeUsed}");
            return false;
        }
        AdsChangeUsed++;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Xem ads xong - tiêu 1 lượt đổi {process.AdsChangeUsed}/{GetData().MaxChangeByAds}");
        WorldCupLog.Fire(WorldCupLog.WC_ADS_CHANGE);
        return true;
    }
    // đổi lựa chọn bằng rewarded ads (gọi lúc confirm; lượt đã tiêu lúc xem ads xong ở OnWatchAdsChange)
    public static bool OnChangeByAds(string _newTeamId)
    {
        if (State != MODULE_STATE.PREDICTION_SUBMITTED)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnChangeByAds thất bại: sai trạng thái - state {State}");
            return false;
        }
        if (!CanCommitAdsChange)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnChangeByAds thất bại: không đổi được - submitted {process.IsSubmitted} confirmed {process.IsConfirmed}");
            return false;
        }
        if (!IsFinalist(_newTeamId))
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnChangeByAds thất bại: teamId không hợp lệ - teamId {_newTeamId}");
            return false;
        }
        SelectedTeamId = _newTeamId;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Đổi dự đoán bằng ads - teamId {_newTeamId} đã đổi {process.AdsChangeUsed}/{GetData().MaxChangeByAds}");
        return true;
    }
    // xác nhận lần cuối --> khóa lựa chọn
    public static bool OnConfirm()
    {
        if (!process.IsSubmitted)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnConfirm thất bại: chưa submit dự đoán");
            return false;
        }
        if (process.IsConfirmed)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnConfirm thất bại: đã chốt cuối rồi");
            return false;
        }
        IsConfirmed = true;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Chốt cuối dự đoán - teamId {process.SelectedTeamId}");
        return true;
    }
    //========================================================================================================================================================================
    // ACTIONS - RESULT & REWARD (nhóm B)
    // LiveOps publish kết quả: set đội thắng và tính đúng/sai
    public static void OnPublishResult(string _winningTeamId)
    {
        WinningTeamId = _winningTeamId;
        EvaluateResult();
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Publish kết quả - winningTeamId {_winningTeamId} result {result}");
    }
    // tính kết quả dự đoán dựa trên đội thắng. Chưa submit --> NONE (không nhận thưởng)
    public static PREDICTION_RESULT EvaluateResult()
    {
        if (!process.IsSubmitted || !IsFinalist(winningTeamId))
            Result = PREDICTION_RESULT.NONE;
        else
            Result = process.SelectedTeamId == winningTeamId ? PREDICTION_RESULT.WIN : PREDICTION_RESULT.LOSE;
        return result;
    }
    // bảng thưởng tương ứng kết quả. NONE --> null
    public static RewardTable GetRewardTable()
    {
        switch (result)
        {
            case PREDICTION_RESULT.WIN: return GetData().Correct;
            case PREDICTION_RESULT.LOSE: return GetData().Wrong;
            default: return null;
        }
    }
    // vàng thưởng trong event = vàng cơ bản theo level × hệ số (giá trị GOLD cấu hình trong WorldCup.asset CHÍNH LÀ hệ số nhân, không phải số tuyệt đối) - giống GridEventBoard.EventGold
    public static int EventGold(int _multiplier)
        => _multiplier <= 0 ? 0 : GoldByChapter.GetGoldByChapterWithMultiply(GOLD_BY_CHAPTER_ID.WORLD_CUP_PREDICT, _multiplier);
    // BaseReward theo kết quả, đã quy đổi GOLD theo level by chapter - dùng ở cả grant lẫn UI để số hiện = số nhận
    public static List<RewardCount> GetResultRewards()
    {
        var _table = GetRewardTable();
        return _table?.BaseReward == null ? null : ResolveGold(_table.BaseReward);
    }
    // Contents của option pack (theo trạng thái unlock Title), đã quy đổi GOLD theo level by chapter - dùng ở cả grant lẫn UI
    public static List<RewardCount> GetPackRewards(VOUCHER_TYPE _voucher)
    {
        var _option = GetIapOption(_voucher);
        return _option?.Contents == null ? null : ResolveGold(_option.Contents);
    }
    // clone từng item + quy đổi riêng item GOLD (Count trong asset dùng thẳng làm hệ số nhân) - RewardCount là class -> phải Clone, không sửa đè lên asset
    static List<RewardCount> ResolveGold(RewardCount[] _rewards)
    {
        var _result = new List<RewardCount>(_rewards.Length);
        foreach (var _r in _rewards)
        {
            var _clone = (RewardCount)_r.Clone();
            if (_clone.Type == REWARD_TYPE.GOLD) _clone.Count = EventGold(_clone.Count);
            _result.Add(_clone);
        }
        return _result;
    }
    // voucher miễn phí tương ứng kết quả
    public static VOUCHER_TYPE GetFreeVoucher()
    {
        var _table = GetRewardTable();
        return _table == null ? VOUCHER_TYPE.NONE : _table.FreeVoucher;
    }
    // claim base reward sau khi có kết quả
    public static bool OnClaimReward()
    {
        if (State != MODULE_STATE.RESULT_AVAILABLE && State != MODULE_STATE.OFFER_ACTIVE)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimReward thất bại: chưa tới lúc claim - state {State}");
            return false;
        }
        if (result == PREDICTION_RESULT.NONE)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimReward thất bại: chưa có kết quả hợp lệ");
            return false;
        }
        if (process.IsRewardClaimed)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimReward thất bại: đã claim rồi");
            return false;
        }
        Rewards.AddReward(GetResultRewards(), _whereLogReport: Name);
        IsRewardClaimed = true;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Claim base reward - result {result}");
        WorldCupLog.Fire(WorldCupLog.WC_CLAIM);
        return true;
    }
    // nhận voucher miễn phí theo kết quả
    public static bool OnClaimFreeVoucher()
    {
        if (State != MODULE_STATE.RESULT_AVAILABLE && State != MODULE_STATE.OFFER_ACTIVE)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimFreeVoucher thất bại: chưa tới lúc claim - state {State}");
            return false;
        }
        if (result == PREDICTION_RESULT.NONE)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimFreeVoucher thất bại: chưa có kết quả hợp lệ");
            return false;
        }
        if (process.IsFreeVoucherClaimed)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnClaimFreeVoucher thất bại: đã nhận rồi");
            return false;
        }
        IsFreeVoucherClaimed = true;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Nhận voucher miễn phí - voucher {GetFreeVoucher()}");
        WorldCupLog.Fire(WorldCupLog.WC_VOUCHER_FREE);
        return true;
    }
    //========================================================================================================================================================================
    // ACTIONS - CROSS VOUCHER & IAP (nhóm D)
    // voucher nhánh đối diện (mua bằng gem): đoán đúng mở Legend, đoán sai mở Mythic
    public static VOUCHER_TYPE GetExtraVoucherType()
    {
        switch (result)
        {
            case PREDICTION_RESULT.WIN: return VOUCHER_TYPE.LEGEND;
            case PREDICTION_RESULT.LOSE: return VOUCHER_TYPE.MYTHIC;
            default: return VOUCHER_TYPE.NONE;
        }
    }
    // số title được chọn khi mua pack (UIWorldCupOfferDetail)
    public static int TitleSelectCount => GetData().TitleSelectCount;
    // giá gem để mở voucher còn lại
    public static int GetExtraVoucherGemCost()
    {
        switch (GetExtraVoucherType())
        {
            case VOUCHER_TYPE.MYTHIC: return GetData().GemCostMythicVoucher;
            case VOUCHER_TYPE.LEGEND: return GetData().GemCostLegendVoucher;
            default: return 0;
        }
    }
    // dùng gem mở voucher nhánh đối diện
    public static bool OnUnlockExtraVoucher()
    {
        if (!IsInOfferWindow || result == PREDICTION_RESULT.NONE)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnUnlockExtraVoucher thất bại: ngoài cửa sổ offer hoặc chưa có kết quả - state {State}");
            return false;
        }
        if (process.IsExtraVoucherUnlocked)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnUnlockExtraVoucher thất bại: đã mở rồi");
            return false;
        }
        var _cost = GetExtraVoucherGemCost();
        if (Player.GemCurrent < _cost)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnUnlockExtraVoucher thất bại: thiếu gem - cần {_cost} có {Player.GemCurrent}");
            return false;
        }
        Player.MinusGem(_cost);
        IsExtraVoucherUnlocked = true;
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Mở voucher bằng gem - voucher {GetExtraVoucherType()} gem {_cost}");
        WorldCupLog.Fire(WorldCupLog.WC_VOUCHER_EXTRA);
        return true;
    }
    // data gói IAP theo loại voucher
    public static IapPackData GetIapPackData(VOUCHER_TYPE _voucher)
    {
        switch (_voucher)
        {
            case VOUCHER_TYPE.MYTHIC: return GetData().MythicPredictorPack;
            case VOUCHER_TYPE.LEGEND: return GetData().LegendSupporterPack;
            default: return null;
        }
    }
    // chọn option của gói theo trạng thái unlock Title System (so flag, không hardcode level)
    public static IapOption GetIapOption(VOUCHER_TYPE _voucher)
    {
        var _pack = GetIapPackData(_voucher);
        if (_pack == null || _pack.Options == null || _pack.Options.Length <= 0) return null;
        //
        foreach (var _option in _pack.Options)
        {
            if (_option.RequireTitleUnlocked == IsTitleSystemUnlocked) return _option;
        }
        return null;
    }
    // % giảm giá 1 option, tính từ GIÁ STORE THẬT (Android/iOS/vùng khác nhau): lọc chữ số 2 chuỗi giá gốc & sau voucher
    // (cùng máy = cùng tiền tệ = cùng số lẻ -> tỉ lệ chuẩn). Store chưa sẵn sàng / giá lỗi -> fallback SaleValue cấu hình
    public static int GetDiscountPercent(IapOption _option)
    {
        if (_option == null) return 0;
        if (CC_IAP.current == null) return _option.SaleValue;
        long _origin = PriceDigits(CC_IAP.current.Localized(_option.IDProductOrigin.ToString()));
        long _now = PriceDigits(CC_IAP.current.Localized(_option.IDProduct.ToString()));
        if (_origin <= 0 || _now <= 0 || _now >= _origin) return _option.SaleValue;
        return (int)Math.Round((_origin - _now) / (double)_origin * 100);
    }
    // gom mọi chữ số trong chuỗi giá localized (bỏ ký hiệu tiền tệ + dấu phân cách); char.IsDigit bắt cả digit non-ASCII (Ả Rập, Ba Tư...)
    static long PriceDigits(string _s)
    {
        if (string.IsNullOrEmpty(_s)) return 0;
        long _v = 0;
        foreach (char _c in _s) if (char.IsDigit(_c)) _v = _v * 10 + (long)char.GetNumericValue(_c);
        return _v;
    }
    // gói đã mua hay chưa
    public static bool IsPackPurchased(VOUCHER_TYPE _voucher)
    {
        switch (_voucher)
        {
            case VOUCHER_TYPE.MYTHIC: return process.IsMythicPackPurchased;
            case VOUCHER_TYPE.LEGEND: return process.IsLegendPackPurchased;
            default: return false;
        }
    }
    // đánh dấu đã mua gói (gọi sau khi IAP success)
    public static bool OnPurchasePack(VOUCHER_TYPE _voucher)
    {
        if (!IsInOfferWindow)
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnPurchasePack thất bại: ngoài cửa sổ offer 48h - voucher {_voucher}");
            return false;
        }
        if (IsPackPurchased(_voucher))
        {
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnPurchasePack thất bại: đã mua rồi - voucher {_voucher}");
            return false;
        }
        switch (_voucher)
        {
            case VOUCHER_TYPE.MYTHIC: IsMythicPackPurchased = true; break;
            case VOUCHER_TYPE.LEGEND: IsLegendPackPurchased = true; break;
            default:
                DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnPurchasePack thất bại: voucher không hợp lệ - voucher {_voucher}");
                return false;
        }
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Mua gói thành công - voucher {_voucher}");
        return true;
    }
    // gọi sau khi IAP SUCCESS (CC_Interface.PurchaseSuccessCallback): set cờ qua OnPurchasePack rồi grant nội dung gói
    public static bool OnPurchasePackSuccess(VOUCHER_TYPE _voucher)
    {
        if (!OnPurchasePack(_voucher)) return false;// guard offer-window + chống mua lại + set cờ
        var _rewards = GetPackRewards(_voucher);
        if (_rewards != null) Rewards.AddReward(_rewards, _whereLogReport: Name);
        return true;
    }
    //========================================================================================================================================================================
    // SERVER/RUNTIME - nhận từ ApiManager.Instance.WorldCup.Connect (WorldCupConnectData); test local qua TabTest (Apply Times / Apply As ConnectResponse)
    static WorldCupConnectData connectResponse;
    public static WorldCupConnectData ConnectResponseCurrent => connectResponse;
    // giờ chuẩn của module: anchor server_time lúc connect rồi đếm tăng bằng realtime - KHÔNG tin giờ máy (máy lệch giờ hết ảnh hưởng lock/offer window)
    static DateTime? serverUtcAtSync;// server_time lúc connect gần nhất
    static float realtimeAtSync;     // Time.realtimeSinceStartup tại thời điểm đó
    static DateTime UtcNow => serverUtcAtSync.HasValue
        ? serverUtcAtSync.Value.AddSeconds(Time.realtimeSinceStartup - realtimeAtSync)
        : DPUtils.GetUtcNow;// fallback khi chưa từng connect (offline/test local)
    public static string ServerPhase { get; private set; }// phase server tính lúc connect - CHỈ để log đối chiếu, KHÔNG drive UI (nguồn state duy nhất = ResolveState)
    // apply data server 1 cửa duy nhất. Gọi từ UIMilestoneProgression.OnWorldCupConnectCallback
    public static void OnConnectResponse(WorldCupConnectData _res)
    {
        connectResponse = _res;
        if (_res == null || !_res.success || _res.EVENT_STATUS != Games.Events.EVENT_STATUS.ACTIVE.ToString())
        {
            // không apply, không xoá data cũ (kill-switch mềm) -> chưa từng có data thì ResolveState tự ra HIDDEN
            DPDebug.Log($"<color=#ffd900>[{Name}]</color> OnConnectResponse bỏ qua - success {_res?.success} status {_res?.EVENT_STATUS}");
            return;
        }
        // anchor giờ server (luôn làm, kể cả khi override time test - giờ là giờ, mốc là mốc)
        var _serverNow = ParseTimeOrNull(_res.server_time);
        if (_serverNow.HasValue)
        {
            serverUtcAtSync = _serverNow;
            realtimeAtSync = Time.realtimeSinceStartup;
        }
#if UNITY_EDITOR
        if (!responseTimeOverride)// [TEST-OVERRIDE] true = giữ 5 mốc time local từ TabTest, chỉ apply finalists/winner/process
#endif
        {
            ModuleShowTime = _res.module_show_time;// setter sẵn có: parse 1 lần + cache Utc
            PredictionOpenTime = _res.prediction_open_time;
            PredictionLockTime = _res.prediction_lock_time;
            MatchEndTime = _res.match_end_time;
            ModuleHideTime = _res.module_hide_time;
        }
        FinalistTeamIdA = _res.finalist_team_a;
        FinalistTeamIdB = _res.finalist_team_b;
        WinningTeamId = _res.winning_team_id;// "" = LiveOps chưa publish
        // server là nguồn sự thật cho tiến trình prediction (khôi phục khi đổi máy/reinstall)
        if (!string.IsNullOrEmpty(_res.my_prediction))
        {
            process.SelectedTeamId = _res.my_prediction;
            process.IsSubmitted = true;
        }
        else
        {
            process.SelectedTeamId = "";
            process.IsSubmitted = false;
        }
        if (_res.claimed) process.IsRewardClaimed = true;// suy ra 1 chiều - KHÔNG set false đè local (server chưa kịp ghi claimed thì vẫn giữ đã-claim, tránh mở lại nút Claim -> dupe reward)
        if (_res.claimed) process.IsFreeVoucherClaimed = true;// claim + nhận voucher free là CÙNG 1 hành động (_OnContinueClick) -> suy ra 1 chiều để khôi phục khi mất local/đổi máy (không set false đè local)
        if (IsResultPublished) EvaluateResult();// khôi phục result (in-memory) sau khi mở lại game
        ServerPhase = _res.phase;
        SaveData();// persist + OnChanged -> RefreshBanner/card tự refresh
        // đối chiếu phase server với state local (cùng input nên phải khớp; lệch = config/parse sai)
        DPDebug.Log($"<color=#4aff21>[{Name}]</color> Apply connect - finalists {finalistTeamIdA}/{finalistTeamIdB} winner {winningTeamId} state {State} (server phase {ServerPhase})");
    }
    public static MODULE_STATE State
    {
        get
        {
#if UNITY_EDITOR
            if (stateOverride.HasValue) return stateOverride.Value;// [TEST-OVERRIDE]
#endif
            return ResolveState();
        }
    }
    // các mốc thời gian do server/LiveOps điều khiển. Parse 1 lần khi set, cache DateTime? để ResolveState không parse lại mỗi repaint (tránh spam log RemoteConfig)
    static string moduleShowTime;
    static DateTime? moduleShowUtc;
    public static string ModuleShowTime { get => moduleShowTime; set { moduleShowTime = value; moduleShowUtc = ParseTimeOrNull(value); } }
    static string predictionOpenTime;
    static DateTime? predictionOpenUtc;
    public static string PredictionOpenTime { get => predictionOpenTime; set { predictionOpenTime = value; predictionOpenUtc = ParseTimeOrNull(value); } }
    static string predictionLockTime;// trước thời điểm trận đấu bắt đầu 30 phút
    static DateTime? predictionLockUtc;
    public static string PredictionLockTime { get => predictionLockTime; set { predictionLockTime = value; predictionLockUtc = ParseTimeOrNull(value); } }
    static string moduleHideTime;// mốc hide module = mốc kết thúc offer luôn (backend tự tính lúc publish, KHÔNG dựa match_end_time)
    static DateTime? moduleHideUtc;
    public static string ModuleHideTime { get => moduleHideTime; set { moduleHideTime = value; moduleHideUtc = ParseTimeOrNull(value); } }
    static string matchEndTime;// thời điểm trận chung kết kết thúc (chỉ dùng cho GetTimeToMatchEnd)
    static DateTime? matchEndUtc;
    public static string MatchEndTime { get => matchEndTime; set { matchEndTime = value; matchEndUtc = ParseTimeOrNull(value); } }
    // hai đội vào chung kết
    static string finalistTeamIdA;
    public static string FinalistTeamIdA { get => finalistTeamIdA; set => finalistTeamIdA = value; }
    static string finalistTeamIdB;
    public static string FinalistTeamIdB { get => finalistTeamIdB; set => finalistTeamIdB = value; }
    // đội thắng do LiveOps publish
    static string winningTeamId;
    public static string WinningTeamId { get => winningTeamId; set => winningTeamId = value; }
    // đã unlock Title System chưa (chọn option IAP). Title System = Award System
    public static bool IsTitleSystemUnlocked
    {
        get
        {
#if UNITY_EDITOR
            if (titleUnlockedOverride.HasValue) return titleUnlockedOverride.Value;// [TEST-OVERRIDE]
#endif
            return Award.IsAvailable();
        }
    }
    // kết quả dự đoán đúng/sai
    static PREDICTION_RESULT result = PREDICTION_RESULT.NONE;
    public static PREDICTION_RESULT Result { get => result; set => result = value; }
    //========================================================================================================================================================================
    // STATE MACHINE & TIME (nhóm C) - đọc cache time local + GetUtcNow, KHÔNG fetch server
    // parse 1 lần khi set (log RemoteConfig chỉ 1 lần/lần set). null nếu chưa set / parse fail
    static DateTime? ParseTimeOrNull(string _value)
    {
        if (string.IsNullOrEmpty(_value)) return null;
        return DPUtils.TryParseRemoteDateTime(_value, out var _t) ? _t : (DateTime?)null;
    }
    // đã qua mốc thời gian (time có set và now >= mốc)
    static bool Reached(DateTime? _t) => _t.HasValue && UtcNow >= _t.Value;
    // thời điểm offer/voucher hết hạn = module_hide_time (backend tự set lúc publish, KHÔNG cộng thêm OfferWindowHours)
    static bool TryGetOfferEndTime(out DateTime _end)
    {
        _end = DateTime.MinValue;
        if (!moduleHideUtc.HasValue) return false;
        _end = moduleHideUtc.Value;
        return true;
    }
    // còn trong cửa sổ đổi-bằng-ads (trước lock trừ safety window). True nếu chưa set lock để test local
    static bool IsBeforeAdsLock
    {
        get
        {
            if (!predictionLockUtc.HasValue) return true;
            return UtcNow < predictionLockUtc.Value.AddMinutes(-GetData().AdsSafetyWindowMinutes);
        }
    }
    // thời gian còn lại tới các mốc cho UI countdown (Zero nếu mốc null/đã qua)
    public static TimeSpan GetTimeToPredictionOpen() => RemainUntil(predictionOpenUtc);
    public static TimeSpan GetTimeToPredictionLock() => RemainUntil(predictionLockUtc);
    public static TimeSpan GetTimeToMatchEnd() => RemainUntil(matchEndUtc);
    public static TimeSpan GetTimeToEventClose() => TryGetOfferEndTime(out var _end) ? RemainUntil(_end) : TimeSpan.Zero;
    static TimeSpan RemainUntil(DateTime? _t) => _t.HasValue ? RemainUntil(_t.Value) : TimeSpan.Zero;
    static TimeSpan RemainUntil(DateTime _t)
    {
        var _remain = _t - UtcNow;
        return _remain > TimeSpan.Zero ? _remain : TimeSpan.Zero;
    }
    public static bool HasTwoFinalists => !string.IsNullOrEmpty(finalistTeamIdA) && !string.IsNullOrEmpty(finalistTeamIdB);
    public static bool HasValidVoucher => process.IsFreeVoucherClaimed || process.IsExtraVoucherUnlocked;
    public static bool IsResultPublished => IsFinalist(winningTeamId);
    public static bool IsInOfferWindow => !TryGetOfferEndTime(out var _end) || UtcNow <= _end;
    // card WC có được hiện trên banner không (mục 6 GD):
    // ẩn khi chưa tới module_show hoặc đã ENDED; HIDDEN sau module_show = Coming Soon -> vẫn hiện
    public static bool IsModuleAvailable
    {
        get
        {
            if (moduleShowUtc.HasValue && UtcNow < moduleShowUtc.Value) return false;
            return State != MODULE_STATE.ENDED;
        }
    }
    // tính trạng thái module từ time + process (mục 6 GD)
    public static MODULE_STATE ResolveState()
    {
        var _now = UtcNow;
        if (moduleShowUtc.HasValue && _now < moduleShowUtc.Value) return MODULE_STATE.HIDDEN;
        if (Reached(moduleHideUtc)) return MODULE_STATE.ENDED;
        if (TryGetOfferEndTime(out var _offerEnd) && _now > _offerEnd) return MODULE_STATE.ENDED;
        if (IsResultPublished)
        {
            if (HasValidVoucher && IsInOfferWindow) return MODULE_STATE.OFFER_ACTIVE;
            return MODULE_STATE.RESULT_AVAILABLE;
        }
        if (process.IsRewardClaimed) return MODULE_STATE.ENDED;// đã claim mà data không có winner (bất nhất - vd time override khi test) -> phần chơi đã xong, không quay lại PREDICTION_*
        if (Reached(predictionLockUtc)) return MODULE_STATE.PREDICTION_LOCKED;
        if (process.IsSubmitted) return MODULE_STATE.PREDICTION_SUBMITTED;
        if (Reached(predictionOpenUtc) && HasTwoFinalists) return MODULE_STATE.PREDICTION_OPEN;
        return MODULE_STATE.HIDDEN;
    }
    //========================================================================================================================================================================
#if UNITY_EDITOR
    // ===== [TEST-OVERRIDE] CHỈ editor — set tay để test. Golive: xoá nguyên block này + 2 nhánh #if [TEST-OVERRIDE] trong State/IsTitleSystemUnlocked + dòng trong HackReset =====
    static MODULE_STATE? stateOverride;
    public static void SetStateOverride(MODULE_STATE _state) => stateOverride = _state;
    public static void ClearStateOverride() => stateOverride = null;
    public static MODULE_STATE ResolvedState => ResolveState();
    static bool? titleUnlockedOverride;
    public static void SetTitleSystemUnlockedOverride(bool _value) => titleUnlockedOverride = _value;
    public static void ClearTitleUnlockedOverride() => titleUnlockedOverride = null;
    static bool responseTimeOverride;// true = OnConnectResponse KHÔNG apply 5 mốc time từ server (giữ time local set bằng TabTest), finalists/winner vẫn apply
    public static bool ResponseTimeOverride => responseTimeOverride;
    public static void SetResponseTimeOverride(bool _value) => responseTimeOverride = _value;
#endif
    [Serializable]
    public class Process
    {
        public string SelectedTeamId;// countryID đội user đang chọn ("" = chưa chọn). Set khi tap đội, còn đổi được khi chưa IsConfirmed
        public bool IsSubmitted;// user đã submit dự đoán lần đầu hay chưa (vẫn được đổi bằng ads cho tới khi IsConfirmed)
        public bool IsConfirmed;// user đã xác nhận LẦN CUỐI --> khóa lựa chọn, không cho đổi bằng ads nữa dù còn lượt/còn thời gian
        public int AdsChangeUsed;// số lần đã đổi lựa chọn bằng rewarded ads, so với MaxChangeByAds(=2) để chặn đổi tiếp
        public bool IsRewardClaimed;// đã claim reward đúng/sai sau khi LiveOps publish kết quả hay chưa --> chặn claim 2 lần
        public bool IsFreeVoucherClaimed;// đã nhận voucher MIỄN PHÍ theo kết quả (đúng=Mythic, sai=Legend) hay chưa
        public bool IsExtraVoucherUnlocked;// đã DÙNG GEM mở voucher CÒN LẠI (voucher nhánh đối diện) hay chưa
        public bool IsMythicPackPurchased;// đã mua gói Mythic Predictor Pack chưa --> ẩn/sold out offer
        public bool IsLegendPackPurchased;// đã mua gói Legend Supporter Pack chưa --> ẩn/sold out offer
        public string Version = "0.1";// phiên bản của event
    }
}
