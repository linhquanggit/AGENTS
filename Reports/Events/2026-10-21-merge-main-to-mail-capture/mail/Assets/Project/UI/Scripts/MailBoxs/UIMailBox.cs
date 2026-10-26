using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using I2.Loc;
using UnityEngine;

[Serializable]
public class DataMail
{
    public string id;
    public string userid;
    public string messagetype;
    public string content;
    public bool isread;
    public bool isrewardclaimed;
    public RewardPayload rewarddetails;
    public string createdat;
    public string updatedat;
    public string title;

    public bool IsHaveNotify
    {
        get
        {
            if (rewarddetails.IsEmpty)
                return !isread;
            else
                return !isread || !isrewardclaimed;
        }
    }
}


public class MailBoxCallback
{
    public string message;
    public List<DataMail> data;
}

public class UIMailBox : PopupBase, IEnhancedScrollerDelegate
{
    List<DataMail> lstData = new();

    [SerializeField] EnhancedScroller scroller;

    [SerializeField] private ItemMailBoxView itemMailPrefab;

    [SerializeField] ItemInternetStatus itemInternetStatus;

    [SerializeField] LocalizedString noMail;
    [SerializeField] LocalizedString noReward;
    [SerializeField] LocalizedString tryAgain;

    // Start is called before the first frame update
    void Start()
    {
        scroller.Delegate = this;
    }


    protected override void Init()
    {
        base.Init();
        lstData = HomeController.Instance.lstDataMail;
        if (lstData.Count == 0)
        {
            itemInternetStatus.SetActive(true);
            itemInternetStatus.ShowStatus(noMail.ToString());
        }
        else
            itemInternetStatus.SetActive(false);

        scroller.ReloadData();
    }
    protected override void OnAddListener()
    {
        base.OnAddListener();
        EventDispatcher.Instance.AddListener(EventName.OnSyncMail, OnSyncMailListener);
        EventDispatcher.Instance.AddListener(EventName.OnSyncMailError, OnSyncMailErrorListener);
        EventDispatcher.Instance.AddListener(EventName.OnSyncMailSuccess, OnSyncMailSuccessListener);
    }

    protected override void OnRemoveListener()
    {
        base.OnRemoveListener();
        EventDispatcher.Instance.RemoveListener(EventName.OnSyncMail, OnSyncMailListener);
        EventDispatcher.Instance.RemoveListener(EventName.OnSyncMailError, OnSyncMailErrorListener);
        EventDispatcher.Instance.RemoveListener(EventName.OnSyncMailSuccess, OnSyncMailSuccessListener);
    }

    private void OnSyncMailListener(EventName key, object data)
    {
        itemInternetStatus.SetActive(true);
        itemInternetStatus.ShowLoading();
        lstData.Clear();
        scroller.ReloadData();
    }
    private void OnSyncMailSuccessListener(EventName key, object data)
    {
        itemInternetStatus.SetActive(false);
        Init();
    }

    private void OnSyncMailErrorListener(EventName key, object data)
    {
        Init();
        itemInternetStatus.ShowStatus((string)data);
    }

    #region Web Request
    #endregion


    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return lstData.Count;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 133;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        var _item = this.scroller.GetCellView(itemMailPrefab) as ItemMailBoxView;
        _item.SetData(lstData[dataIndex]);
        return _item;
    }

    public void _OnDeleteAllClick()
    {
        if (lstData.Count == 0)
        {
            UIManager.Instance.GetPopup(PopupId.Messenger).Show(noMail.ToString());
            return;
        }

        UIManager.Instance.GetPopup(PopupId.LoadingInternet).Show();
        DHS.DeleteAllMailBox(this, Player.Id, _OnRequestFinish);
        void _OnRequestFinish(bool _isSuccess, string _response)
        {
            UIManager.Instance.GetPopup(PopupId.LoadingInternet).Hide();
            HomeController.Instance.OnSyncMail();
            if (!_isSuccess)
                _RequestError(_response);
            else
                _RequestSuccess(_response);

            void _RequestError(string _error)
            {
                UIManager.Instance.GetPopup(PopupId.Messenger).Show(_error + "\r\n" + tryAgain);
            }

            void _RequestSuccess(string _response)
            {
                //Refresh();
            }
        }
    }

    // Claim All tạm ẩn (object nút đã inactive trên scene) — bỏ comment để mở lại, Button.onClick vẫn giữ tên hàm _OnClaimAllClick trong prefab
    //public void _OnClaimAllClick()
    //{
        //if (lstData.Count == 0)
        //{
            //UIManager.Instance.GetPopup(PopupId.Messenger).Show(noReward.ToString());
            //return;
        //}

        //UIManager.Instance.GetPopup(PopupId.LoadingInternet).Show();

        //DHS.ClaimAllMail(this, Player.Id, _OnRequestFinish);
        //void _OnRequestFinish(bool _isSuccess, string _response)
        //{
            //UIManager.Instance.GetPopup(PopupId.LoadingInternet).Hide();
            //if (!_isSuccess)
                //_RequestError(_response);
            //else
                //_RequestSuccess(_response);
            //HomeController.Instance.OnSyncMail();

            //void _RequestError(string _error)
            //{
                //UIManager.Instance.GetPopup(PopupId.Messenger).Show(_error + "\r\n" + tryAgain);
            //}

            //void _RequestSuccess(string _response)
            //{
                ////var _dataReceived = JsonUtility.FromJson<MailBoxCallback>(_response);
                //_CalculateReward();
                ////Refresh();

                //void _CalculateReward()
                //{
                    //// Gộp reward theo (Type, iapProductID) — currency cùng loại cộng dồn,
                    //// pack khác nhau giữ riêng từng dòng. Generic theo mọi REWARD_TYPE, không hard-code field.
                    //var _merged = new Dictionary<(REWARD_TYPE, IAP_Product), RewardCount>();
                    //foreach (var _item in lstData)
                    //{
                        //if (_item == null || _item.isrewardclaimed) continue;

                        //var _rewards = _item.rewarddetails.ToRewardCountList();
                        //UIMailInformation.FlowLogClaim(_item, _rewards);
                        //foreach (var _r in _rewards)
                        //{
                            //var _key = (_r.Type, _r.iapProductID);
                            //if (_merged.TryGetValue(_key, out var _exist))
                                //_exist.Count += _r.Count;
                            //else
                                //_merged[_key] = (RewardCount)_r.Clone();
                        //}
                    //}

                    //if (_merged.Count == 0)
                    //{
                        //UIManager.Instance.GetPopup(PopupId.Messenger).Show(noReward.ToString());
                        //return;
                    //}

                    //List<RewardCount> rewardCountList = new(_merged.Values);
                    //Rewards.AddReward(rewardCountList, _whereLogReport: "MAIL_REWARD", _isAntiHack: false);
                    //UIManager.Instance.GetPopup(PopupId.ContainerOpeningV2).Show(rewardCountList);
                //}
            //}
        //}
    //}
}