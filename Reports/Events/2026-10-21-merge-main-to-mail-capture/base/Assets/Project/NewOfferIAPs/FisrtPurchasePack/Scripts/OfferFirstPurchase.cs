using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using GameAnalyticsSdk;
using UnityEngine;

public class OfferFirstPurchase
{
    private static Process Data;

    public static bool IsShowedOnHome = false;

    static OfferFirstPurchase()
    {
        Data = Player.FirstPurchaseProcess;
        if (Data == null)
            OnRefresh();
        else
            CheckAndUpdateDays();
    }

    public static void Refresh()
    {
        Data = Player.FirstPurchaseProcess;
        if (Data == null)
            OnRefresh();
    }

    static void OnRefresh()
    {
        Data = new Process();
        SaveData();
    }

    private static void SaveData()
    {
        Player.FirstPurchaseProcess = Data;
    }

    // Kiểm tra và cập nhật ngày hiện tại dựa trên thời gian đã trôi qua kể từ lần kiểm tra cuối cùng
    private static void CheckAndUpdateDays()
    {
        var _hasUpdate = false;

        foreach (var _packData in Data.Packs)
        {
            if (!_packData.IsPurchased) continue;

            var _lastCheck = new DateTime(_packData.LastCheckTime);
            var _now = DPUtils.GetUtcNow;

            int _daysPassed = (_now.Date - _lastCheck.Date).Days;

            if (_daysPassed > 0)
            {
                // Cập nhật ngày hiện tại (tối đa là day 2 vì chỉ có 3 ngày: 0,1,2)
                _packData.CurrentDay = Mathf.Min(_packData.CurrentDay + _daysPassed, 2);
                _packData.LastCheckTime = _now.Ticks;
                _hasUpdate = true;

                DPDebug.Log($"Pack {_packData.PackType}: Cập nhật sang ngày {_packData.CurrentDay + 1}");
            }
        }

        if (_hasUpdate)
            SaveData();
    }

    public static bool IsCanShow()
    {
        return Data.Packs.Any(t => !t.IsPurchased || !t.IsAllRewardsClaimed());
    }

    // Kiểm tra xem có thể nhận quà ngày dayIndex của packType không
    public static bool CanClaimReward(OfferFirstPurchases.PackType _packType, int _dayIndex)
    {
        var _packData = Data.GetPackData(_packType);

        if (!_packData.IsPurchased) return false;
        if (_dayIndex is < 0 or >= 3) return false;
        if (_dayIndex > _packData.CurrentDay) return false;
        return !_packData.DayClaimStatus[_dayIndex];
    }

    // Lấy pack nên hiển thị đầu tiên
    public static OfferFirstPurchases.PackType PackShowFirst()
    {
        // ưu tiên pack có rewardschưa nhận
        var _unclaimedPack = Data.Packs.FirstOrDefault(t => t.HasUnclaimedReward());
        if (_unclaimedPack != null)
        {
            DPDebug.Log($"=====> PackShowFirst: {_unclaimedPack.PackType} (has unclaimed reward)");
            return _unclaimedPack.PackType;
        }
        // ưu tiên pack đã mua nhưng chưa hoàn thành nhận quà 
        var _purchasedIncomplete = Data.Packs.FirstOrDefault(t => t.IsPurchased && !t.IsAllRewardsClaimed());
        if (_purchasedIncomplete != null)
        {
            DPDebug.Log($"=====> PackShowFirst: {_purchasedIncomplete.PackType} (purchased, waiting for next day)");
            return _purchasedIncomplete.PackType;
        }
        // pack chưa mua
        var _notPurchased = Data.Packs.FirstOrDefault(t => !t.IsPurchased);
        if (_notPurchased != null)
        {
            DPDebug.Log($"=====> PackShowFirst: {_notPurchased.PackType} (not purchased)");
            return _notPurchased.PackType;
        }
        //Tất cả đã mua và nhận hết quà -> Không show hoặc show pack cuối
        return OfferFirstPurchases.PackType.Deluxe;

    }

    public static bool IsPurchased(OfferFirstPurchases.PackType _packType)
    {
        return Data.GetPackData(_packType).IsPurchased;
    }

    public static int GetCurrentDay(OfferFirstPurchases.PackType _packType)
    {
        return Data.GetPackData(_packType).CurrentDay;
    }

    public static PackProgress GetPackProgress(OfferFirstPurchases.PackType _packType)
    {
        return Data.GetPackData(_packType);
    }

    //
    public static bool ClaimReward(OfferFirstPurchases.PackType _packType, int _dayIndex)
    {
        var _ = Data.GetPackData(_packType);

        if (!_.IsPurchased)
        {
            DPDebug.LogWarning($"Chưa mua gói {_packType}!");
            return false;
        }

        if (_dayIndex < 0 || _dayIndex >= 3)
        {
            DPDebug.LogWarning($"Day index không hợp lệ: {_dayIndex}");
            return false;
        }

        if (_dayIndex > _.CurrentDay)
        {
            DPDebug.LogWarning($"Chưa đến ngày {_dayIndex + 1}!");
            return false;
        }

        if (_.DayClaimStatus[_dayIndex])
        {
            DPDebug.LogWarning($"Đã nhận quà ngày {_dayIndex + 1} rồi!");
            return false;
        }

        // Nhận quà
        var dayReward = OfferFirstPurchases.GetDayReward(_packType, _dayIndex);
        if (dayReward != null)
        {
            Rewards.AddReward(dayReward.Rewards, _whereLogReport: AnalyticsWhere.OfferFirstPurchaseDailyClaim);
            UIManager.Instance.GetPopup(PopupId.ContainerOpening).Show(dayReward.Rewards);
        }

        _.DayClaimStatus[_dayIndex] = true;

        SaveData();

        DPDebug.Log($"Đã nhận quà ngày {_dayIndex + 1} của gói {_packType}");
        return true;
    }


    // Mua gói
    public static void PurchasePack(OfferFirstPurchases.PackType _packType)
    {
        var _packData = Data.GetPackData(_packType);

        if (_packData.IsPurchased)
        {
            DPDebug.Log($"Pack {_packType} đã được mua rồi!");
            return;
        }

        _packData.IsPurchased = true;
        _packData.PurchaseTime = DPUtils.GetUtcNow.Ticks;
        _packData.LastCheckTime = DPUtils.GetUtcNow.Ticks;
        _packData.CurrentDay = 0;

        _packData.DayClaimStatus = new List<bool> { false, false, false };
        SaveData();

        DPDebug.Log($"Đã mua gói {_packType}");
    }

    [Serializable]
    public class Process
    {
        public List<PackProgress> Packs = new();

        public Process()
        {
            Packs.Add(new PackProgress(OfferFirstPurchases.PackType.Basic));
            Packs.Add(new PackProgress(OfferFirstPurchases.PackType.Premium));
            Packs.Add(new PackProgress(OfferFirstPurchases.PackType.Deluxe));
        }

        // Lấy data pack
        public PackProgress GetPackData(OfferFirstPurchases.PackType packType)
        {
            return Packs.FirstOrDefault(x => x.PackType == packType);
        }
    }


    [Serializable]
    public class PackProgress
    {
        public OfferFirstPurchases.PackType PackType;
        public bool IsPurchased;
        public long PurchaseTime;
        public long LastCheckTime;
        public int CurrentDay;
        public List<bool> DayClaimStatus;

        public PackProgress()
        {
        }

        public PackProgress(OfferFirstPurchases.PackType packType)
        {
            PackType = packType;
            IsPurchased = false;
            PurchaseTime = 0;
            LastCheckTime = 0;
            CurrentDay = 0;
            DayClaimStatus = new List<bool> { false, false, false };
        }

        public bool IsClaimedAtDay(int _day)
        {
            return DayClaimStatus != null && DayClaimStatus[_day];
        }

        // Kiểm tra còn quà chưa nhận không
        public bool HasUnclaimedReward()
        {
            if (!IsPurchased) return false;
            if (DayClaimStatus == null) return false;

            for (int i = 0; i <= CurrentDay && i < DayClaimStatus.Count; i++)
            {
                if (!DayClaimStatus[i]) // Ngày đã mở nhưng chưa nhận
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAllRewardsClaimed()
        {
            return DayClaimStatus != null && DayClaimStatus.All(x => x);
        }

        public int ClaimedDaysCount()
        {
            return DayClaimStatus?.Count(x => x) ?? 0;
        }

        public DateTime GetPurchaseDateTime()
        {
            return PurchaseTime > 0 ? new DateTime(PurchaseTime) : DateTime.MinValue;
        }

        public DateTime GetLastCheckDateTime()
        {
            return LastCheckTime > 0 ? new DateTime(LastCheckTime) : DateTime.MinValue;
        }
    }
}