using System;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ChipStores
{
    #region Constants & Static Constructor

    public const int DefaultTimeRefreshStoresByGold = 3;
    public static int DefaultTimeRefreshStoresFree
    {
        get
        {
            var _limit = LimitAds.GetLimitAdsCount(LIMIT_ADS.CHIP_ON_SHOP_REFRESH);
            return _limit;
        }
    }
    public static int DefaultTimesOpenByAds
    {
        get
        {
            var _limit = LimitAds.GetLimitAdsCount(LIMIT_ADS.CHIP_ON_SHOP_FREE_SLOT);
            return _limit;
        }
    }

    public const int CostToRefresh = 50000; // cost to refresh store random

    public const int TicketOfferAdd = 100;

    public static bool IsShowEffectOfferTicket1 = true;
    public static bool IsShowEffectOfferTicket2 = true;

    private const int PackageCount = 6;

    private const int MaximumSlot = 6;

    private const int PurchaseChipAmount = 5; // pruchase on package chip


    static ChipStores()
    {
        OnCheckNewDay();
    }

    #endregion

    #region Process & Data Management

    public static Process GetProcess()
    {
        return Player.GetChipStoresProcess();
    }

    public static void Refresh()
    {
        OnCheckNewDay();
    }

    private static void SaveData(Process _process)
    {
        Player.UpdateChipStores(_process);
    }

    private static void OnCheckNewDay()
    {
        var _process = GetProcess();
        //if (_process.DaySaved != DateTime.Now.DayOfYear)
        if (DPUtils.IsNewDay(LastOpen))
        {
            _process.RefreshStoreAttemptsByGold = DefaultTimeRefreshStoresByGold;
            _process.OpenedByAds = DefaultTimesOpenByAds;
            _process.RefreshStoreAttemptsFree = DefaultTimeRefreshStoresFree;
            //_process.DaySaved = DateTime.Now.DayOfYear;
            _process.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
            OnRandomChipLuckyInSlot(_process);
            OnRefreshStores();
        }

        SaveData(_process);
    }
    static string LastOpen
    {
        get
        {
            var _process = GetProcess();
            if (string.IsNullOrEmpty(_process.LastOpen))
            {
                _process.LastOpen = DPUtils.GetUtcNow.AddDays(-1).Ticks.ToString();
                SaveData(_process);
            }
            else
            {
                if (!DPUtils.ParseTicks(_process.LastOpen, out var _))
                {
                    _process.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
                    SaveData(_process);
                }
            }
            return _process.LastOpen;
        }
    }
    public static bool IsBoughtOfferSlot6()
    {
        var _process = GetProcess();
        return _process.IsBoughtOfferSlot6;
    }

    public static void SetBoughtOfferSlot6()
    {
        var _process = GetProcess();
        _process.IsBoughtOfferSlot6 = true;
        SaveData(_process);
    }

    // Tach ra tu PopupUpgradeStraightChip.OnPurchaseSuccess() de dung chung duoc voi mail-grant
    // (popup đóng lúc claim mail nên không gọi qua instance popup được). Logic giữ nguyên y hệt bản gốc.
    public static Tuple<Sprite, int> GrantPremiumChipPack()
    {
        var _chiprd = Chips.RandomChipBySlot(6, CHIP_TIER_TYPE.Tier3);
        var _process = Chip.GetProcess(_chiprd.Id);
        Tuple<Sprite, int> _chipInfo;
        if (_process == null)
        {
            Chip.AddChip(_chiprd.Id, 0);
            Chip.UpLevelChip(_chiprd.Id, 5);
            _chipInfo = new Tuple<Sprite, int>(Chips.GetAvatarChip(_chiprd.Id), 0);
        }
        else
        {
            var _currentLv = _process.Level;
            var _chipsToAdd = 0;
            var _toLevel = _currentLv <= 5 ? _currentLv : 5;
            for (int i = 0; i < _toLevel; i++)
            {
                var (_chipRequired, _) = Chips.GetRequiredForUpgradeLevel(_chiprd.Id, i);
                _chipsToAdd += _chipRequired;
            }

            if (_currentLv <= 5)
                Chip.UpLevelChip(_chiprd.Id, 5);
            Chip.AddChip(_chiprd.Id, _chipsToAdd);
            _chipInfo = new Tuple<Sprite, int>(Chips.GetAvatarChip(_chiprd.Id), _chipsToAdd);
        }

        SetBoughtOfferSlot6();
        return _chipInfo;
    }

    private static void OnRandomChipLuckyInSlot(Process _process)
    {
        _process.slotIdsLucky = new List<SlotIdLucky>();
        for (int i = 0; i < MaximumSlot; i++)
        {
            var _tier = i == MaximumSlot - 1 ? StoresChipData.RandomTierSlotSpecial() : CHIP_TIER_TYPE.NONE;
            var _chip = Chips.RandomChipBySlot(i + 1, _tier);
            _process.slotIdsLucky.Add(new SlotIdLucky()
            {
                Slot = i + 1,
                IdChip = _chip.Id
            });
        }
    }

    #endregion

    #region Store Refresh & Randomization

    public static void OnRefreshStores()
    {
        var _process = GetProcess();
        _process.randomStores = new List<SlotProcessStores>();
        for (var i = 0; i < PackageCount; i++)
        {
            var rd = i == 0 ? StoresChipData.OnTicket() : StoresChipData.RandomItem();

            var _itemType = i == 0 ? StoreItemType.Ticket : rd.TypeItem;
            var _idItem = _itemType != StoreItemType.Chip ? (int)rd.Ticket : rd.Chip.Id;
            var _rd = Random.Range(0, 2);
            _process.randomStores.Add(new SlotProcessStores
            {
                Index = i,
                Item = new ItemStore
                {
                    Type = _itemType,
                    IdItem = _idItem
                },
                IsUpAmountRandom = _rd == 1,
                Price = rd.Price
            });
        }



        SaveData(_process);
    }

    public static bool TryRefreshStoresByGold(out bool isNotEnoughCoin, out bool isLimit)
    {
        isNotEnoughCoin = false;
        isLimit = false;
        var _process = GetProcess();
        if (_process.RefreshStoreAttemptsByGold <= 0 && !Hack.IsUnlimitRefreshShop)
        {
            isLimit = true;
            return false;
        }

        if (Player.GoldCurrent < CostToRefresh)
        {
            isNotEnoughCoin = false;
            return false;
        }


        Player.MinusGold(CostToRefresh);
        OnRefreshStores();
        _process.RefreshStoreAttemptsByGold--;
        SaveData(_process);
        return true;
    }

    public static bool CanRefreshByAds()
    {
        var _process = GetProcess();
        return _process.RefreshStoreAttemptsFree > 0;
    }

    public static bool TryRefreshStoryByAds()
    {
        var _process = GetProcess();
        if (_process.RefreshStoreAttemptsFree <= 0)
            return false;
        OnRefreshStores();
        _process.RefreshStoreAttemptsFree--;
        SaveData(_process);
        return true;
    }

    #endregion

    #region Store Purchase & Opening

    public static int GetPurchaseAmount(int _index)
    {
        var _process = GetProcess();
        return _process.randomStores[_index].IsUpAmountRandom ? PurchaseChipAmount * 2 : PurchaseChipAmount;
    }

    public static bool TryBuyItem(int _index)
    {
        var _slot = GetProcessPackage(_index);
        var _process = GetProcess();
        if (_slot == null || _slot.IsBought) return false;

        var _amount = _slot.Item.Type == StoreItemType.Ticket ? 1 : GetPurchaseAmount(_index);
        var _cost = _slot.Price.Cost * _amount;
        var _resType = _slot.Price.ResourceType;

        var _hasEnough = _resType switch
        {
            RESOURCE_TYPE.Gold => Player.GoldCurrent >= _cost,
            RESOURCE_TYPE.Gem => Player.GemCurrent >= _cost,
            _ => false
        };

        if (!_hasEnough) return false;

        switch (_resType)
        {
            case RESOURCE_TYPE.Gold: Player.MinusGold(_cost); break;
            case RESOURCE_TYPE.Gem: Player.MinusGem(_cost); break;
        }

        OnOpenItem(_index);
        return true;
    }

    public static void OnOpenItemByAds(int _index)
    {
        var _process = GetProcess();
        _process.OpenedByAds--;
        OnOpenItem(_index);
        SaveData(_process);
    }

    public static bool OnCheckCanOpenAds()
    {
        var _process = GetProcess();
        return _process.OpenedByAds > 0;
    }

    public static void AddTicket(bool _isNormal, int _count)
    {
        Player.AddTicketChipStore(_isNormal, _count);
    }

    private static void OnOpenItem(int _index)
    {
        var _process = GetProcess();
        var _slot = GetProcessPackage(_index);
        var _amount = _index == 0 || _slot.Item.Type == StoreItemType.Ticket ? 1 : GetPurchaseAmount(_index);
        if (_slot.Item.Type == StoreItemType.Chip)
            Chip.AddChip(_slot.Item.IdItem, _amount);
        else
            Player.AddTicketChipStore((RESOURCE_TYPE)_slot.Item.IdItem == RESOURCE_TYPE.TicketChipNormal, _amount);

        if (_slot.Index != 0)
            _slot.IsBought = true;
        _process.randomStores[_index] = _slot;
        SaveData(_process);
    }

    public static int GetIdChipLucky(int _slot)
    {
        var _process = GetProcess();
        return _process.slotIdsLucky.FirstOrDefault(x => x.Slot == _slot).IdChip;
    }

    #endregion

    #region Research Chips

    public static bool TryResearch(bool _isUseTicket, int _slot, bool _isTen, out Dictionary<int, int> _data)
    {
        var _process = GetProcess();
        _data = new Dictionary<int, int>();
        var _summonCount = _isTen ? 10 : 1;

        var _isSpecialSlot = _slot == 6;

        if (_isUseTicket)
        {
            ref var _ticket = ref _slot == 6 ? ref _process.TicketSpecial : ref _process.TicketNormal;
            _ticket -= _summonCount;
        }
        else
        {
            var _cost = !_isTen ? StoresChipData.GemCostOnce(!_isSpecialSlot) : StoresChipData.GemCostMulti(!_isSpecialSlot);
            var _canPay = Player.GemCurrent >= _cost;
            if (!_canPay) return false;

            Player.MinusGem(_cost);
        }

        for (var i = 0; i < _summonCount; i++)
        {
            var _chip = _slot < 6
                ? Chips.RandomChipBySlot(_slot)
                : (_slot == 6 ? Chips.RandomChipBySlot(_slot, StoresChipData.RandomTierSlotSpecial()) : null);
            if (_chip == null) continue;
            if (!_data.TryAdd(_chip.Id, 1))
                _data[_chip.Id]++;
        }

        SevenDaysMission.UpdateTaskProgress(TYPE_MISSION.ResearchChip);
        
        return true;
    }

    #endregion

    #region Utilities

    public static SlotProcessStores GetProcessPackage(int _index)
    {
        var _process = GetProcess();
        return _process.randomStores[_index];
    }

    #endregion

    #region Nested Classes

    [Serializable]
    public class Process
    {
        public int TicketNormal = 0;
        public int TicketSpecial = 0;
        //public int DaySaved = -1;
        public string LastOpen;
        public int OpenedByAds = 2;
        public int RefreshStoreAttemptsByGold = 2;
        public int RefreshStoreAttemptsFree = 2;
        public bool IsBoughtOfferSlot6 = false;
        public List<SlotProcessStores> randomStores = new List<SlotProcessStores>();
        public List<SlotIdLucky> slotIdsLucky = new();
    }

    [Serializable]
    public class SlotProcessStores
    {
        public int Index;
        public ItemStore Item;
        public StoresChipData.Price Price;
        public bool IsUpAmountRandom = false;
        public bool IsBought;
    }

    [Serializable]
    public class ItemStore
    {
        public StoreItemType Type;
        public int IdItem;
    }

    [Serializable]
    public struct SlotIdLucky
    {
        public int Slot;
        public int IdChip;
    }

    #endregion
}