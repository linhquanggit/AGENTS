using DP.Utilities;
using EnhancedUI.EnhancedScroller;
using UnityEngine;
using UnityEngine.UI;

public class RowRewardMail : EnhancedScrollerCellView
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private Text txtCount;
    [SerializeField] private Image imgFrameIapPack;
    [SerializeField] private Text textPackName;
    public void SetData(RewardCount data)
    {
        bool _isIapPack = data.Type == REWARD_TYPE.IAP_PRODUCT;
        imgIcon.gameObject.SetActive(!_isIapPack);
        txtCount.gameObject.SetActive(!_isIapPack);
        imgFrameIapPack.gameObject.SetActive(_isIapPack);
        textPackName.gameObject.SetActive(_isIapPack);
        if (_isIapPack)
        {
            textPackName.text = IapPackRewardConfig.GetName(data.iapProductID).ToUpper();
        }
        else
        {
            imgIcon.sprite = data.GetSprRewardShow();
            txtCount.text = data.Count.ShortToString();
        }
    }
}

