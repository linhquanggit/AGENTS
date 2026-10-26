using DP.Utilities;
using EnhancedUI.EnhancedScroller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMailInformation : PopupBase, IEnhancedScrollerDelegate
{
    [SerializeField] private Text txtContent;
    [SerializeField] private Text txtTitle;

    [SerializeField] GameObject objBtnClaim;
    [SerializeField] GameObject objRewardDecor;

    [SerializeField] EnhancedScroller scrollerReward;
    [SerializeField] RowRewardMail cellRewardPrefab;
    [SerializeField] float cellRewardSize = 146.71f;

    [SerializeField] ButtonTranslate btnTranslate;

    private DataMail dataMail;
    private List<RewardCount> rewards = new();

    protected override void InitData()
    {
        base.InitData();

        dataMail = (DataMail)data;
        txtContent.text = dataMail.content;
        txtTitle.text = dataMail.title;
        btnTranslate.Init(new List<Text>()
        {
            txtContent,
            txtTitle
        });
        rewards = dataMail.rewarddetails.ToRewardCountList();
        objBtnClaim.SetActive(rewards.Count > 0 && !dataMail.isrewardclaimed);
        objRewardDecor.SetActive(rewards.Count > 0);
    }

    protected override void Init()
    {
        base.Init();
        scrollerReward.Delegate = this;
        float _contentSize = rewards.Count * cellRewardSize + Mathf.Max(0, rewards.Count - 1) * scrollerReward.spacing;
        int _pad = Mathf.Max(0, Mathf.FloorToInt((scrollerReward.ScrollRectSize - _contentSize) * 0.5f));
        scrollerReward.padding.left = _pad;
        scrollerReward.padding.right = _pad;
        scrollerReward.ReloadData();
    }

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return rewards.Count;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellRewardSize;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        var _cell = scrollerReward.GetCellView(cellRewardPrefab) as RowRewardMail;
        _cell.SetData(rewards[dataIndex]);
        return _cell;
    }

    public static void FlowLogClaim(DataMail _mail, List<RewardCount> _rewards)
    {
#if UNITY_EDITOR
        var _parts = new List<string>();
        foreach (var _reward in _rewards)
            _parts.Add(_reward.Type == REWARD_TYPE.IAP_PRODUCT ? $"IAP {_reward.iapProductID}" : $"{_reward.Type} {_reward.Count}");
        FlowCaptureLogger.Write($"[MAIL] {_mail.id} \"{_mail.title}\" | {string.Join(" | ", _parts)}");
#endif
    }

    public void _OnClickClaim()
    {
        UIManager.Instance.GetPopup(PopupId.LoadingInternet).Show();
        DHS.OnMailBox(this, MailBoxType.ClaimReward, dataMail.id, dataMail.userid, OnSendComplete);
    }

    void OnSendComplete(bool _isSuccess, string _response)
    {
        UIManager.Instance.GetPopup(PopupId.LoadingInternet).Hide();

        if (!_isSuccess)
            _GetDataFail(_response);
        else
            _GetDataSuccess(_response);

        HomeController.Instance.OnSyncMail();
        void _GetDataFail(string _content)
        {
            UIManager.Instance.GetPopup(PopupId.Messenger).Show(_content);
        }

        void _GetDataSuccess(string _response)
        {
            objBtnClaim.SetActive(false);

            IapProductRewards.LastGrantFailures.Clear();
            FlowLogClaim(dataMail, rewards);
            Rewards.AddReward(rewards, _whereLogReport: "MAIL_REWARD", _isAntiHack: false);
            if (IapProductRewards.LastGrantFailures.Count > 0)
                UIManager.Instance.GetPopup(PopupId.Messenger).Show("Một số phần thưởng chưa cấp được, vui lòng liên hệ hỗ trợ.");

            var _flyItemTypes = new List<ITEM_TYPE>();
            foreach (var _reward in rewards)
            {
                ITEM_TYPE? _itemType = _reward.Type == REWARD_TYPE.IAP_PRODUCT
                    ? IapProductRewards.GetFlyableItemType(_reward.iapProductID)
                    : Rewards.Convert(_reward.Type);
                if (_itemType.HasValue)
                    _flyItemTypes.Add(_itemType.Value);
            }

            if (_flyItemTypes.Count == 0) return; // không có gì bay được -> không mở hòm

            UIItemGatherPos _uIItemGatherPos = UIManager.Instance.GetPopup<UIItemGatherPos>(PopupId.ItemGatherPos);
            _uIItemGatherPos.ShowInventory();

            BarItems _bar = _uIItemGatherPos.BarItems;
            Vector3 _piecesPos = _uIItemGatherPos.PiecesGatherPos;
            Vector3 _originPos = objRewardDecor.transform.position;

            foreach (var _itemType in _flyItemTypes)
            {
                var _target = _itemType switch
                {
                    ITEM_TYPE.GOLD => _bar.FrameGold.TargetPosition,
                    ITEM_TYPE.GEM => _bar.FrameGem.TargetPosition,
                    ITEM_TYPE.ENERGY => _bar.FrameEnergy.TargetPosition,
                    _ => _piecesPos,
                };
                SpawnItems.Instance.Show(_itemType, _originPos, _target);
            }
        }
    }
}