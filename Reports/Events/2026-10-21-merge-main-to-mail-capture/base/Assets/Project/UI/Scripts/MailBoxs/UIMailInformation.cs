using DP.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMailInformation : PopupBase
{
    [SerializeField] private Text txtContent;
    [SerializeField] private Text txtTitle;

    [SerializeField] GameObject objBtnClaim;
    [SerializeField] GameObject objRewardDecor;

    [SerializeField] Transform containerReward;
    [SerializeField] RowRewardMail cellRewardPrefab;

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

        containerReward.DestroyAllChild();
        foreach (var _reward in rewards)
        {
            var _cell = Instantiate(cellRewardPrefab, containerReward);
            _cell.SetData(_reward);
        }
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

            Rewards.AddReward(rewards, _whereLogReport: "MAIL_REWARD", _isAntiHack: false);

            UIItemGatherPos _uIItemGatherPos = UIManager.Instance.GetPopup<UIItemGatherPos>(PopupId.ItemGatherPos);
            _uIItemGatherPos.ShowInventory();

            BarItems _bar = _uIItemGatherPos.BarItems;
            Vector3 _piecesPos = _uIItemGatherPos.PiecesGatherPos;
            Vector3 _originPos = objRewardDecor.transform.position;

            foreach (var _reward in rewards)
            {
                var _itemType = Rewards.Convert(_reward.Type);
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