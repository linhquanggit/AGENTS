using System;
using DP.Utilities;
using GameAnalyticsSdk;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Games;
using I2.Loc;

[CreateAssetMenu(fileName = "Rewards", menuName = "Data/Rewards")]
public class Rewards : ScriptableObject
{
    const string REWARD_RESOURECE_FOLDER = "Data/Rewards";
    //
    private static readonly ResourceAsset<Rewards> asset = new ResourceAsset<Rewards>(REWARD_RESOURECE_FOLDER);
    //
    [SerializeField] List<RewardDataInit> rewardsDataList = new List<RewardDataInit>();
    [SerializeField] LimitRewardData[] limitRewardsDataList;
    //
    public static int GetLimitRewardCount(LimitRewardData.LIMIT_REWARD_TYPE _id)
    {
        LimitRewardData _data = Array.Find(asset.Value.limitRewardsDataList, x => x.Id == _id);
        if (_data != null) return _data.LimitCount;
        return 0;
    }
    public static LimitRewardData GetLimitRewardData(LimitRewardData.LIMIT_REWARD_TYPE _id)
    {
        LimitRewardData _data = Array.Find(asset.Value.limitRewardsDataList, x => x.Id == _id);
        return _data;
    }
    public static RewardDataInit GetData(REWARD_TYPE _type)
    {
        RewardDataInit _data = asset.Value.rewardsDataList.Find(x => x.Type == _type);
        return _data;
    }
    public static void AddReward(List<RewardCount> _rewards, bool _isUpdateShow = true, System.Action _onCompleted = null,
        string _whereLogReport = "", int _level = -1, bool _isAntiHack = true)
    {
        if (_rewards == null || _rewards.Count <= 0) return;
        //
        foreach (var _reward in _rewards)
        {
            AddReward(_reward, _isUpdateShow, _whereLogReport, _level, _isAntiHack: _isAntiHack);
        }
        if (_onCompleted != null)
            _onCompleted.Invoke();
    }
    public static void HackAddReward(RewardCount _reward)
    {
        switch (_reward.Type)
        {
            case REWARD_TYPE.NONE: break;
            case REWARD_TYPE.REWARD_CONTAINER_FIGHTER: Currency.AddRewardContainerFighter(_reward.Count); break;
            case REWARD_TYPE.REWARD_CONTAINER_WINGMAN: Currency.AddRewardContainerWingman(_reward.Count); break;
            case REWARD_TYPE.HOLIDAY_NOEL_POINT: Currency.AddHolidayEventPoint(_reward.Count, EVENT_NAME.NOEL); break;
            case REWARD_TYPE.AIR_DEFENSE_ENERGY: AirDefense.OnAddEnergy(_reward.Count); break;
            case REWARD_TYPE.CAMPAIGN_REVIVE_TICKET: Campaign.AddReviveTicket(_reward.Count); break;
            case REWARD_TYPE.CAMPAIGN_EXTRATIME_TICKET: Campaign.AddExtraTimeTicket(_reward.Count); break;
            // passiveDataList
            case REWARD_TYPE.GOLD:
                Player.HackAddGold(_reward.Count);
                break;
            case REWARD_TYPE.GEM:
                Player.HackAddGem(_reward.Count);
                break;
            case REWARD_TYPE.ENERGY:
                Energy.AddEnergyCurrent(_reward.Count, true);
                break;
            case REWARD_TYPE.AWARD_COMMON_RANDOM:
                Award.AddTicket(AWARD_RARITY.COMMON, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_RARE_RANDOM:
                Award.AddTicket(AWARD_RARITY.RARE, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_EPIC_RANDOM:
                Award.AddTicket(AWARD_RARITY.EPIC, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_LEGEND_RANDOM:
                Award.AddTicket(AWARD_RARITY.LEGEND, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_MYTHIC_RANDOM:
                Award.AddTicket(AWARD_RARITY.MYTHIC, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_LEGEND_EVENT_RANDOM:
                Award.AddEventLegendTicket(_reward.Count);
                break;
            case REWARD_TYPE.AWARD_MYTHIC_EVENT_RANDOM:
                Award.AddEventMythicTicket(_reward.Count);
                break;
            default:
                PIECES_TYPE _pieceType = Pieces.Convert(_reward.Type);
                Piece.HackAddPiece(_pieceType, _reward.Count);
                break;

        }
    }
    public static void AddReward(RewardCount _reward, bool _isUpdateShow = true, string _whereLogReport = "", int _level = -1
        , System.Action _onCompleted = null, bool _isAntiHack = true)
    {
        //DPDebug.Log($"Add reward[{_reward.Type}]--count[{_reward.Count}]");
        if (_level == -1)
            _level = Level.LevelMax;
        switch (_reward.Type)
        {
            // passiveDataList
            case REWARD_TYPE.NONE: break;
            case REWARD_TYPE.GOLD:
                Player.AddGold(_reward.Count, _isUpdateShow);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.Gold,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.Gold.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.Gold));
                break;
            case REWARD_TYPE.GEM:
                Player.AddGem(_reward.Count, _isUpdateShow, _isAntiHack, _whereLogReport);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.Gem,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.Gem.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.Gem));
                break;
            case REWARD_TYPE.ENERGY:
                Energy.AddEnergyCurrent(_reward.Count, _isUpdateShow);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.Energy,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.Energy.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.Energy));
                break;
            case REWARD_TYPE.REWARD_CONTAINER_FIGHTER:
                Currency.AddRewardContainerFighter(_reward.Count);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.Currency,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.Currency.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.Currency));
                break;
            case REWARD_TYPE.REWARD_CONTAINER_WINGMAN:
                Currency.AddRewardContainerWingman(_reward.Count);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.Currency,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.Currency.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.Currency));
                break;
            case REWARD_TYPE.BF_TICKET:
                Player.AddTicket(_reward.Count);
                // log report
                // OGNetWork cũ — giữ lại để tham khảo, không dùng nữa. Đã chuyển sang GameAnalyticsSdk bên dưới.
                // LogManager.LogResource(
                //     flowType: FlowType.source,
                //     currency: CurrencyType.BossFightTicket,
                //     amount: _reward.Count,
                //     where: _whereLogReport,
                //     level: _level);
                Analytics.TrackCurrencySource(CurrencyType.BossFightTicket.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.BossFightTicket));
                break;
            case REWARD_TYPE.CHIP_TICKET_NORMAL:
                ChipStores.AddTicket(true, _reward.Count);
                break;
            case REWARD_TYPE.CHIP_TICKET_SPECIAL:
                ChipStores.AddTicket(false, _reward.Count);
                break;
            case REWARD_TYPE.CAMPAIGN_REVIVE_TICKET:
                Campaign.AddReviveTicket(_reward.Count);
                Analytics.TrackCurrencySource(CurrencyType.CampaignReviveTicket.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.CampaignReviveTicket));
                break;
            case REWARD_TYPE.CAMPAIGN_EXTRATIME_TICKET:
                Campaign.AddExtraTimeTicket(_reward.Count);
                Analytics.TrackCurrencySource(CurrencyType.CampaignExtraTimeTicket.ToString(), _whereLogReport, _reward.Count, AnalyticsCurrencyBalance.Current(CurrencyType.CampaignExtraTimeTicket));
                break;
            case REWARD_TYPE.FIGHTER:
                Fighter.Process _fighterProcess = Fighter.GetProcess(_reward.FighterID);
                Fighter.Unlock(_fighterProcess);
                break;
            case REWARD_TYPE.WINGMAN:
                Wingman.Process _wingmanProcess = Wingman.GetProcess(_reward.WingmanID);
                Wingman.Unlock(_wingmanProcess);
                break;
            case REWARD_TYPE.FIGHTER_10:
                Fighter.Process _fighterProcess10 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_10);
                Fighter.Unlock(_fighterProcess10);
                break;
            case REWARD_TYPE.WINGMAN_9:
                Wingman.Process _wingmanProcess9 = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_9);
                Wingman.Unlock(_wingmanProcess9);
                break;
            case REWARD_TYPE.HOLIDAY_NOEL_POINT:
                Currency.AddHolidayEventPoint(_reward.Count, EVENT_NAME.NOEL);
                break;
            case REWARD_TYPE.FIGHTER_11:
                Fighter.Process _fighterProcess11 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_11);
                Fighter.Unlock(_fighterProcess11);
                break;
            case REWARD_TYPE.FIGHTER_12:
                Fighter.Process _fighterProcess12 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_12);
                Fighter.Unlock(_fighterProcess12);
                break;
            case REWARD_TYPE.FIGHTER_13:
                Fighter.Process _fighterProcess13 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_13);
                Fighter.Unlock(_fighterProcess13);
                break;
            case REWARD_TYPE.FIGHTER_14:
                Fighter.Process _fighterProcess14 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_14);
                Fighter.Unlock(_fighterProcess14);
                break;
            case REWARD_TYPE.FIGHTER_15:
                Fighter.Process _fighterProcess15 = Fighter.GetProcess(FIGHTER_TYPE.FIGHTER_15);
                Fighter.Unlock(_fighterProcess15);
                break;
            case REWARD_TYPE.WINGMAN_10:
                Wingman.Process _wingmanProcess10 = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_10);
                Wingman.Unlock(_wingmanProcess10);
                break;
            case REWARD_TYPE.WINGMAN_11:
                Wingman.Process _wingmanProcess11 = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_11);
                Wingman.Unlock(_wingmanProcess11);
                break;
            case REWARD_TYPE.WINGMAN_12:
                Wingman.Process _wingmanProcess12 = Wingman.GetProcess(WINGMAN_TYPE.WINGMAN_12);
                Wingman.Unlock(_wingmanProcess12);
                break;
            case REWARD_TYPE.AIR_DEFENSE_ENERGY:
                AirDefense.OnAddEnergy(_reward.Count);
                break;
            case REWARD_TYPE.AIR_DEFENSE_CURRENCY:
                AirDefense.OnAddCurrency(_reward.Count);
                break;
            case REWARD_TYPE.AIR_DEFENSE_EXP:
                AirDefense.OnAddExp(_reward.Count);
                break;
            case REWARD_TYPE.AWARD_COMMON_RANDOM:
                Award.AddTicket(AWARD_RARITY.COMMON, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_RARE_RANDOM:
                Award.AddTicket(AWARD_RARITY.RARE, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_EPIC_RANDOM:
                Award.AddTicket(AWARD_RARITY.EPIC, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_LEGEND_RANDOM:
                Award.AddTicket(AWARD_RARITY.LEGEND, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_MYTHIC_RANDOM:
                Award.AddTicket(AWARD_RARITY.MYTHIC, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_LEGEND_EVENT_RANDOM:
                Award.AddEventLegendTicket(_reward.Count);
                break;
            case REWARD_TYPE.AWARD_MYTHIC_EVENT_RANDOM:
                Award.AddEventMythicTicket(_reward.Count);
                break;
            case REWARD_TYPE.CHIP_MUL_SHIELD:
                Chip.AddChip(28, _reward.Count);
                break;
            case REWARD_TYPE.CHIP_MUL_ENDHANCE:
                Chip.AddChip(29, _reward.Count);
                break;
            case REWARD_TYPE.CHIP_MUL_REVIVE:
                Chip.AddChip(30, _reward.Count);
                break;
            case REWARD_TYPE.CHIP_RANDOM:
            case REWARD_TYPE.CHIP_ID:
                DPDebug.Log($"Reward added chip--ID[{_reward.ChipID}]--Count[{_reward.Count}]");
                Chip.AddChip(_reward.ChipID, _reward.Count);
                break;
            case REWARD_TYPE.AWARD_RANDOM:
            case REWARD_TYPE.AWARD_ID:
                DPDebug.Log($"Reward added award--ID[{_reward.AwardID}]--Count[{_reward.Count}]");
                Award.AddQuantity(_reward.AwardID, _reward.Count);
                break;
            case REWARD_TYPE.BT_ENERGY:
                BattleTeam.AddEnergy(_reward.Count);
                break;
            case REWARD_TYPE.BT_CURRENCY:
                BattleTeam.OnAddCurrency(_reward.Count);
                break;
            default:
                PIECES_TYPE _pieceType = Pieces.Convert(_reward.Type);
                Piece.AddPiece(_pieceType, _reward.Count, _isAntiHack);
                break;
        }
    }
    public static REWARD_TYPE Convert(PIECES_TYPE _pieceType)
    {
        switch (_pieceType)
        {
            // fighter
            case PIECES_TYPE.FIGHTER_1: return REWARD_TYPE.FIGHTER_1_PIECES;
            case PIECES_TYPE.FIGHTER_2: return REWARD_TYPE.FIGHTER_2_PIECES;
            case PIECES_TYPE.FIGHTER_3: return REWARD_TYPE.FIGHTER_3_PIECES;
            case PIECES_TYPE.FIGHTER_4: return REWARD_TYPE.FIGHTER_4_PIECES;
            case PIECES_TYPE.FIGHTER_5: return REWARD_TYPE.FIGHTER_5_PIECES;
            case PIECES_TYPE.FIGHTER_6: return REWARD_TYPE.FIGHTER_6_PIECES;
            case PIECES_TYPE.FIGHTER_7: return REWARD_TYPE.FIGHTER_7_PIECES;
            case PIECES_TYPE.FIGHTER_8: return REWARD_TYPE.FIGHTER_8_PIECES;
            case PIECES_TYPE.FIGHTER_9: return REWARD_TYPE.FIGHTER_9_PIECES;
            case PIECES_TYPE.FIGHTER_10: return REWARD_TYPE.FIGHTER_10_PIECES;
            case PIECES_TYPE.FIGHTER_11: return REWARD_TYPE.FIGHTER_11_PIECES;
            case PIECES_TYPE.FIGHTER_12: return REWARD_TYPE.FIGHTER_12_PIECES;
            case PIECES_TYPE.FIGHTER_13: return REWARD_TYPE.FIGHTER_13_PIECES;
            case PIECES_TYPE.FIGHTER_14: return REWARD_TYPE.FIGHTER_14_PIECES;
            case PIECES_TYPE.FIGHTER_15: return REWARD_TYPE.FIGHTER_15_PIECES;
            case PIECES_TYPE.FIGHTER_16: return REWARD_TYPE.FIGHTER_16_PIECES;
            case PIECES_TYPE.SPECIAL_FIGHTER: return REWARD_TYPE.FIGHTER_SPECIAL_PIECES;
            case PIECES_TYPE.ULTRA_FIGHTER: return REWARD_TYPE.FIGHTER_ULTRA_PIECES;
            // wingman
            case PIECES_TYPE.WINGMAN_1: return REWARD_TYPE.WINGMAN_1_PIECES;
            case PIECES_TYPE.WINGMAN_2: return REWARD_TYPE.WINGMAN_2_PIECES;
            case PIECES_TYPE.WINGMAN_3: return REWARD_TYPE.WINGMAN_3_PIECES;
            case PIECES_TYPE.WINGMAN_4: return REWARD_TYPE.WINGMAN_4_PIECES;
            case PIECES_TYPE.WINGMAN_5: return REWARD_TYPE.WINGMAN_5_PIECES;
            case PIECES_TYPE.WINGMAN_6: return REWARD_TYPE.WINGMAN_6_PIECES;
            case PIECES_TYPE.WINGMAN_7: return REWARD_TYPE.WINGMAN_7_PIECES;
            case PIECES_TYPE.WINGMAN_8: return REWARD_TYPE.WINGMAN_8_PIECES;
            case PIECES_TYPE.WINGMAN_9: return REWARD_TYPE.WINGMAN_9_PIECES;
            case PIECES_TYPE.WINGMAN_10: return REWARD_TYPE.WINGMAN_10_PIECES;
            case PIECES_TYPE.WINGMAN_11: return REWARD_TYPE.WINGMAN_11_PIECES;
            case PIECES_TYPE.WINGMAN_12: return REWARD_TYPE.WINGMAN_12_PIECES;
            case PIECES_TYPE.WINGMAN_13: return REWARD_TYPE.WINGMAN_13_PIECES;
            case PIECES_TYPE.SPECIAL_WINGMAN: return REWARD_TYPE.WINGMAN_SPECIAL_PIECES;
            case PIECES_TYPE.ULTRA_WINGMAN: return REWARD_TYPE.WINGMAN_ULTRA_PIECES;
            //
            default: return REWARD_TYPE.NONE;
        }
    }
    public static ITEM_TYPE Convert(REWARD_TYPE _type)
    {
        switch (_type)
        {
            case REWARD_TYPE.GOLD: return ITEM_TYPE.GOLD;
            case REWARD_TYPE.GEM: return ITEM_TYPE.GEM;
            // fighter
            case REWARD_TYPE.FIGHTER_SPECIAL_PIECES: return ITEM_TYPE.PIECE_FIGHTER_SPECIAL;
            case REWARD_TYPE.FIGHTER_ULTRA_PIECES: return ITEM_TYPE.PIECE_FIGHTER_ULTRA;
            case REWARD_TYPE.FIGHTER_1_PIECES: return ITEM_TYPE.PIECE_FIGHTER1;
            case REWARD_TYPE.FIGHTER_2_PIECES: return ITEM_TYPE.PIECE_FIGHTER2;
            case REWARD_TYPE.FIGHTER_3_PIECES: return ITEM_TYPE.PIECE_FIGHTER3;
            case REWARD_TYPE.FIGHTER_4_PIECES: return ITEM_TYPE.PIECE_FIGHTER4;
            case REWARD_TYPE.FIGHTER_5_PIECES: return ITEM_TYPE.PIECE_FIGHTER5;
            case REWARD_TYPE.FIGHTER_6_PIECES: return ITEM_TYPE.PIECE_FIGHTER6;
            case REWARD_TYPE.FIGHTER_7_PIECES: return ITEM_TYPE.PIECE_FIGHTER7;
            case REWARD_TYPE.FIGHTER_8_PIECES: return ITEM_TYPE.PIECE_FIGHTER8;
            case REWARD_TYPE.FIGHTER_9_PIECES: return ITEM_TYPE.PIECE_FIGHTER9;
            case REWARD_TYPE.FIGHTER_10_PIECES: return ITEM_TYPE.PIECE_FIGHTER10;
            case REWARD_TYPE.FIGHTER_11_PIECES: return ITEM_TYPE.PIECE_FIGHTER11;
            case REWARD_TYPE.FIGHTER_12_PIECES: return ITEM_TYPE.PIECE_FIGHTER12;
            case REWARD_TYPE.FIGHTER_13_PIECES: return ITEM_TYPE.PIECE_FIGHTER13;
            case REWARD_TYPE.FIGHTER_14_PIECES: return ITEM_TYPE.PIECE_FIGHTER14;
            case REWARD_TYPE.FIGHTER_15_PIECES: return ITEM_TYPE.PIECE_FIGHTER15;
            case REWARD_TYPE.FIGHTER_16_PIECES: return ITEM_TYPE.PIECE_FIGHTER16;
            //wingman
            case REWARD_TYPE.WINGMAN_SPECIAL_PIECES: return ITEM_TYPE.PIECE_WINGMAN_SPECIAL;
            case REWARD_TYPE.WINGMAN_ULTRA_PIECES: return ITEM_TYPE.PIECE_WINGMAN_ULTRA;
            case REWARD_TYPE.WINGMAN_1_PIECES: return ITEM_TYPE.PIECE_WINGMAN1;
            case REWARD_TYPE.WINGMAN_2_PIECES: return ITEM_TYPE.PIECE_WINGMAN2;
            case REWARD_TYPE.WINGMAN_3_PIECES: return ITEM_TYPE.PIECE_WINGMAN3;
            case REWARD_TYPE.WINGMAN_4_PIECES: return ITEM_TYPE.PIECE_WINGMAN4;
            case REWARD_TYPE.WINGMAN_5_PIECES: return ITEM_TYPE.PIECE_WINGMAN5;
            case REWARD_TYPE.WINGMAN_6_PIECES: return ITEM_TYPE.PIECE_WINGMAN6;
            case REWARD_TYPE.WINGMAN_7_PIECES: return ITEM_TYPE.PIECE_WINGMAN7;
            case REWARD_TYPE.WINGMAN_8_PIECES: return ITEM_TYPE.PIECE_WINGMAN8;
            case REWARD_TYPE.WINGMAN_9_PIECES: return ITEM_TYPE.PIECE_WINGMAN9;
            case REWARD_TYPE.WINGMAN_10_PIECES: return ITEM_TYPE.PIECE_WINGMAN10;
            case REWARD_TYPE.WINGMAN_12_PIECES: return ITEM_TYPE.PIECE_WINGMAN12;
            case REWARD_TYPE.WINGMAN_13_PIECES: return ITEM_TYPE.PIECE_WINGMAN13;
            //
            case REWARD_TYPE.REWARD_CONTAINER_FIGHTER: return ITEM_TYPE.REWARD_CONTAINER_FIGHTER;
            case REWARD_TYPE.REWARD_CONTAINER_WINGMAN: return ITEM_TYPE.REWARD_CONTAINER_WINGMAN;
            case REWARD_TYPE.BF_TICKET: return ITEM_TYPE.BOSS_FIGHT_TICKET;
            case REWARD_TYPE.CHIP_TICKET_NORMAL: return ITEM_TYPE.TICKET_SHOP_NORMAL;
            case REWARD_TYPE.CHIP_TICKET_SPECIAL: return ITEM_TYPE.TICKET_SHOP_SPECIAL;
            case REWARD_TYPE.CAMPAIGN_REVIVE_TICKET: return ITEM_TYPE.CAMPAIGN_REVIVE_TICKET;
            case REWARD_TYPE.CAMPAIGN_EXTRATIME_TICKET: return ITEM_TYPE.CAMPAIGN_EXTRATIME_TICKET;
            case REWARD_TYPE.ENERGY: return ITEM_TYPE.ENERGY;
            case REWARD_TYPE.HOLIDAY_NOEL_POINT: return ITEM_TYPE.HOLIDAY_NOEL_POINT;
            case REWARD_TYPE.AIR_DEFENSE_CURRENCY: return ITEM_TYPE.AIR_DEFENSE_CURRENCY;
            case REWARD_TYPE.AIR_DEFENSE_EXP: return ITEM_TYPE.AIR_DEFENSE_EXP;
            case REWARD_TYPE.AIR_DEFENSE_ENERGY: return ITEM_TYPE.AIR_DEFENSE_ENERGY;
            case REWARD_TYPE.AWARD_COMMON_RANDOM: return ITEM_TYPE.AWARD_RANDOM;
            case REWARD_TYPE.AWARD_RARE_RANDOM: return ITEM_TYPE.AWARD_RANDOM;
            case REWARD_TYPE.AWARD_EPIC_RANDOM: return ITEM_TYPE.AWARD_RANDOM;
            case REWARD_TYPE.AWARD_LEGEND_RANDOM: return ITEM_TYPE.AWARD_RANDOM;
            case REWARD_TYPE.AWARD_MYTHIC_RANDOM: return ITEM_TYPE.AWARD_RANDOM;
            case REWARD_TYPE.AWARD_LEGEND_EVENT_RANDOM: return ITEM_TYPE.AWARD_RANDOM_EVENT_LEGEND;
            case REWARD_TYPE.AWARD_MYTHIC_EVENT_RANDOM: return ITEM_TYPE.AWARD_RANDOM_EVENT_MYTHIC;
            case REWARD_TYPE.BT_ENERGY: return ITEM_TYPE.BT_ENERGY;
            case REWARD_TYPE.BT_CURRENCY: return ITEM_TYPE.BT_CURRENCY;
            case REWARD_TYPE.CHIP_ID:// cả 2 đều trả về CHIP_ID để khi claim sẽ check tiếp chipID có hợp lệ hay không
            case REWARD_TYPE.CHIP_RANDOM:
                return ITEM_TYPE.CHIP_ID;
            case REWARD_TYPE.AWARD_ID:// cả 2 đều trả về AWARD_ID để khi claim sẽ check tiếp awardID có hợp lệ hay không
            case REWARD_TYPE.AWARD_RANDOM:
                return ITEM_TYPE.AWARD_ID;
            default: return ITEM_TYPE.NONE;
        }
    }

    public bool IsPieceReward(REWARD_TYPE _type)
    {
        return _type == REWARD_TYPE.FIGHTER_SPECIAL_PIECES ||
               _type == REWARD_TYPE.FIGHTER_ULTRA_PIECES ||
               (_type >= REWARD_TYPE.FIGHTER_RANK_N && _type <= REWARD_TYPE.FIGHTER_RANK_S) ||
               (_type >= REWARD_TYPE.FIGHTER_1_PIECES && _type <= REWARD_TYPE.FIGHTER_10_PIECES) ||
               _type == REWARD_TYPE.WINGMAN_SPECIAL_PIECES ||
               _type == REWARD_TYPE.WINGMAN_ULTRA_PIECES ||
                (_type >= REWARD_TYPE.WINGMAN_RANK_N && _type <= REWARD_TYPE.WINGMAN_RANK_S) ||
               (_type >= REWARD_TYPE.WINGMAN_1_PIECES && _type <= REWARD_TYPE.WINGMAN_9_PIECES);
    }

    public static CONTAINER_TYPE ConvertContainer(REWARD_TYPE _type)
    {
        switch (_type)
        {
            case REWARD_TYPE.REWARD_CONTAINER_FIGHTER: return CONTAINER_TYPE.REWARD_FIGHTER;
            case REWARD_TYPE.REWARD_CONTAINER_WINGMAN: return CONTAINER_TYPE.REWARD_WINGMAN;
            default: return CONTAINER_TYPE.NONE;
        }
    }

}
[System.Serializable]
public class LimitRewardData
{
    public enum LIMIT_REWARD_TYPE
    {
        NONE = 0,
        ENERGY,
        LV_BONUS_RANDOM_LOOT,
        CHIP_ON_SHOP,
        SPIN_MONEY,
        SPIN_PIECE,
        LUCKY_WHEEL,
        PREMIUM_LUCKY_WHEEL,
        RANDOM_LOOT,
    }
    public LIMIT_REWARD_TYPE Id;
    public int LimitCount;
}
[System.Serializable]
public class RewardCount : ICloneable
{
    public REWARD_TYPE Type;
    //
    bool IsChipType => Type == REWARD_TYPE.CHIP_ID || Type == REWARD_TYPE.CHIP_RANDOM;
    bool IsAwardType => Type == REWARD_TYPE.AWARD_ID || Type == REWARD_TYPE.AWARD_RANDOM;
    //
    [SerializeField] int[] chipsRandom;
    public int chipID;
    [SerializeField] int[] awardsRandom;
    public int awardID = -1;
    public FIGHTER_TYPE fighterID = FIGHTER_TYPE.NONE;
    public WINGMAN_TYPE wingmanID = WINGMAN_TYPE.NONE;
    //
    public int Count;
    //
    public FIGHTER_TYPE FighterID
    {
        get
        {
            if (Type == REWARD_TYPE.FIGHTER) return fighterID;
            return FIGHTER_TYPE.NONE;
        }
    }
    public WINGMAN_TYPE WingmanID
    {
        get
        {
            if (Type == REWARD_TYPE.WINGMAN) return wingmanID;
            return WINGMAN_TYPE.NONE;
        }
    }
    public int RandomChipID()
    {
        if (Type == REWARD_TYPE.CHIP_RANDOM)
            chipID = chipsRandom[UnityEngine.Random.Range(0, chipsRandom.Length)];
        else
            chipID = -1;
        return chipID;
    }
    public int ChipID
    {
        get
        {
            switch (Type)
            {
                case REWARD_TYPE.CHIP_RANDOM:
                case REWARD_TYPE.CHIP_ID:
                    return chipID;
            }
            return -1;
        }
    }
    public int RandomAwardID()
    {
        if (Type == REWARD_TYPE.AWARD_RANDOM)
            awardID = awardsRandom[UnityEngine.Random.Range(0, awardsRandom.Length)];
        else
            awardID = -1;
        return awardID;
    }
    public int AwardID
    {
        get
        {
            switch (Type)
            {
                case REWARD_TYPE.AWARD_RANDOM:
                case REWARD_TYPE.AWARD_ID:
                    return awardID;
            }
            return -1;
        }
    }
    public override string ToString()
    {
        return $"Reward: [Type: {Type}--Count: {Count}]";
    }
    public Sprite GetSprRewardShow()
    {
        DPDebug.Log($"GetSprRewardShow--Type[{Type}]--ChipID[{ChipID}]--AwardID[{AwardID}]");
        int _id = IsChipType ? ChipID : IsAwardType ? AwardID : -1;
        return Rewards.GetData(Type)?.GetSprRewardShow(_id);
    }
    public string GetName() => Rewards.GetData(Type)?.GetName(AwardID);
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

[System.Serializable]
public class RewardDataInit
{
    public REWARD_TYPE Type;
    bool IsAwardType => Type == REWARD_TYPE.AWARD_ID || Type == REWARD_TYPE.AWARD_RANDOM;
    bool IsChipType => Type == REWARD_TYPE.CHIP_ID || Type == REWARD_TYPE.CHIP_RANDOM;
    //
    public string Name;
    public LocalizedString LocalizedName;
    public string Description;
    public LocalizedString LocalizedDescription;
    public GameObject PrefabShow;
    public GameObject PrefabInforShow;
    public GameObject PrefabClaimShow;
    public GameObject PrefabBossFightShow;
    public GameObject PrefabInventoryShow;
    public ItemWeeklyEventReward PrefabWeeklyEvent;
    public ItemRewardEndless PrefabEndlessShow;
    public ItemRandomLoot PrefabRandomLoot;
    public ItemReward PrefabLootGathered;
    public Sprite SprRewardShow;
    public ItemReward PrefabSweep;
    public ItemReward PrefabBlackMarket;
    public ItemPrizes PrefabPrizes;
    public ItemReward PrefabPVP;
    public ItemRewardAirDefense PrefabAirDefense;
    public GameObject PrefabBattleTeam;
    public ItemReward PrefabBossTower;
    //
    public Sprite GetSprRewardShow(int _id = -1)
    {
        if (IsAwardType) return Awards.GetAwardSprite(_id);
        if (IsChipType) return Chips.GetAvatarChip(_id);
        return SprRewardShow;
    }
    public string GetName(int _id = -1)
    {
        if (IsAwardType) return Awards.GetAwardName(_id);
        return Name;
    }

    //Description
    public LocalizedString UseLocalizedDescription;
    public Sprite IconRewardInfo;
    //

}
[System.Serializable]
public class AwardRewardData
{
    public int Id;
    public Sprite SprRewardShow;
}
public enum REWARD_TYPE
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    //(FIGHTER(3)-STT(00 -> 99)-RANK(1 -> 9)-MANH(1))
    FIGHTER_SPECIAL_PIECES = 30001,
    FIGHTER_ULTRA_PIECES = 30002,
    FIGHTER_1_PIECES = 30111,
    FIGHTER_2_PIECES = 30211,
    FIGHTER_3_PIECES = 30321,
    FIGHTER_4_PIECES = 30421,
    FIGHTER_5_PIECES = 30531,
    FIGHTER_6_PIECES = 30641,
    FIGHTER_7_PIECES = 30731,
    FIGHTER_8_PIECES = 30821,
    FIGHTER_9_PIECES = 30941,
    FIGHTER_10_PIECES = 31041,
    FIGHTER_11_PIECES = 31141,
    FIGHTER_12_PIECES = 31241,
    FIGHTER_13_PIECES = 31341,
    FIGHTER_14_PIECES = 31441,
    FIGHTER_15_PIECES = 31541,
    FIGHTER_16_PIECES = 31641,
    FIGHTER_RANK_N = 30011,
    FIGHTER_RANK_R = 30021,
    FIGHTER_RANK_E = 30031,
    FIGHTER_RANK_S = 30041,
    //(WINGMAN(4)-STT(00 -> 99)-RANK(1 -> 9)-MANH(1))
    WINGMAN_SPECIAL_PIECES = 40001,
    WINGMAN_ULTRA_PIECES = 40002,
    WINGMAN_1_PIECES = 40111,
    WINGMAN_2_PIECES = 40211,
    WINGMAN_3_PIECES = 40321,
    WINGMAN_4_PIECES = 40421,
    WINGMAN_5_PIECES = 40531,
    WINGMAN_6_PIECES = 40641,
    WINGMAN_7_PIECES = 40731,
    WINGMAN_8_PIECES = 40821,
    WINGMAN_9_PIECES = 40941,
    WINGMAN_10_PIECES = 41041,
    WINGMAN_11_PIECES = 41141,
    WINGMAN_12_PIECES = 41241,
    WINGMAN_13_PIECES = 41341,
    WINGMAN_RANK_N = 40011,
    WINGMAN_RANK_R = 40021,
    WINGMAN_RANK_E = 40031,
    WINGMAN_RANK_S = 40041,
    //(CONTAINER(5)-ID(ALL(0),FIGHTER(3),WINGMAN(4))
    CONTAINER_FIGHTER = 53,
    CONTAINER_WINGMAN = 54,
    CONTAINER_ALL = 50,
    //
    ENERGY = 6,
    REMOVE_ADS = 7,
    BF_TICKET = 8,// Boss fight ticket
    PROMOTE_MEDAL = 9,
    //
    REWARD_CONTAINER_FIGHTER = 55,
    REWARD_CONTAINER_WINGMAN = 56,
    CHIP_TICKET_NORMAL = 57,
    CHIP_TICKET_SPECIAL = 58,

    //holiday noel 1000x
    HOLIDAY_NOEL_POINT = 10000,
    BOSS_FIGHT_HOLIDAY_NOEL = 10001,
    FIGHTER_10 = 10002,
    WINGMAN_9 = 10003,
    //AIR DEFENSE 101xx
    AIR_DEFENSE_CURRENCY = 10101,
    AIR_DEFENSE_EXP = 100102,
    AIR_DEFENSE_ENERGY = 10103,

    //other event 102xx

    // khung avatar
    FRAME_AVATAR_1 = 50001,
    FRAME_AVATAR_2 = 50002,
    FRAME_AVATAR_3 = 50003,
    FRAME_AVATAR_4 = 50004,
    FRAME_AVATAR_5 = 50005,
    FRAME_AVATAR_6 = 50006,
    FRAME_AVATAR_7 = 50007,
    FRAME_AVATAR_8 = 50008,
    FRAME_AVATAR_9 = 50009,
    FRAME_AVATAR_10 = 500010,
    //
    FIGHTER_11 = 59,
    WINGMAN_10 = 60,
    AWARD_COMMON_RANDOM = 61,
    AWARD_RARE_RANDOM = 62,
    AWARD_EPIC_RANDOM = 63,
    AWARD_LEGEND_RANDOM = 64,
    AWARD_MYTHIC_RANDOM = 65,
    AWARD_LEGEND_EVENT_RANDOM = 66,
    AWARD_MYTHIC_EVENT_RANDOM = 67,
    FIGHTER_12 = 68,
    WINGMAN_11 = 69,
    BT_ENERGY = 70,// điểm tuyển quân
    BT_CURRENCY = 71,
    CHIP_MUL_SHIELD = 72,
    CHIP_MUL_ENDHANCE = 73,
    CHIP_MUL_REVIVE = 74,
    AWARD_RANDOM = 75,
    AWARD_ID = 76,
    CHIP_RANDOM = 77,
    CHIP_ID = 78,
    FIGHTER = 79,
    WINGMAN = 80,
    FIGHTER_13 = 81,
    FIGHTER_14 = 82,
    CAMPAIGN_REVIVE_TICKET = 83, // vé hồi sinh - Popup Rescue (Campaign.AddReviveTicket)
    CAMPAIGN_EXTRATIME_TICKET = 84, // vé +time - Abyss timeout (Campaign.AddExtraTimeTicket)
    // DANH SÁCH REWARD BY PROGRESS mới. tất cả các reward mới sẽ + offset: 1_000_000
    BP_GOLD = 1000001,
    BP_GEM = 1000002,
    BP_FIGHTER_SPECIAL_PIECES = 1030001,
    BP_FIGHTER_ULTRA_PIECES = 1030002,
    BP_WINGMAN_SPECIAL_PIECES = 1040001,
    BP_WINGMAN_ULTRA_PIECES = 1040002,
    BP_ENERGY = 1000006,
    BP_BF_TICKET = 1000008,
    BP_PROMOTE_MEDAL = 1000009,
    BP_REWARD_CONTAINER_FIGHTER = 1000055,
    BP_REWARD_CONTAINER_WINGMAN = 1000056,
    BP_CHIP_TICKET_NORMAL = 1000057,
    BP_CHIP_TICKET_SPECIAL = 1000058,
    BP_AWARD_COMMON_RANDOM = 1000061,
    BP_AWARD_RARE_RANDOM = 1000062,
    BP_AWARD_EPIC_RANDOM = 1000063,
    BP_AWARD_LEGEND_RANDOM = 1000064,
    BP_AWARD_MYTHIC_RANDOM = 1000065,
    BP_AWARD_LEGEND_EVENT_RANDOM = 1000066,
    BP_AWARD_MYTHIC_EVENT_RANDOM = 1000067,
    BP_AWARD_RANDOM = 1000075,
    BP_CHIP_RANDOM = 1000077,
    WINGMAN_12 = 85,
    FIGHTER_15 = 86,
}
