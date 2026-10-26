using System;
using System.Collections;
using System.Collections.Generic;
using DP.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class PopupUpgradeStraightChip : PopupBase
{
    [SerializeField] private GameObject objPopup, objGift;

    [SerializeField] private Image imgChipShow;

    [SerializeField] private AirportSlot slotGift;

    private Tuple<Sprite, int> chipInfo;

    private bool isShowGiftFly = false;

    private bool isChanged = false;


    private void OnEnable()
    {
        isChanged = false;
    }

    public void OnPurchaseSuccess()
    {
        chipInfo = ChipStores.GrantPremiumChipPack();
        isChanged = true;
        OnGift();
    }

    void OnGift()
    {
        objPopup.SetActive(false);
        objGift.SetActive(true);
        if (chipInfo.Item1 != null)
            imgChipShow.sprite = chipInfo.Item1;
    }

    public override void _OnCloseClick()
    {
        if (!isChanged)
            actionOnClose = null;
        base._OnCloseClick();
    }

    public void _OnClickBuy()
    {
        SoundUI.OnButtonClick();
#if UNITY_EDITOR
        OnPurchaseSuccess();
        return;
#elif UNITY_ANDROID
         CC_Interface.current.PurchaseItem(IAP_Product.aa_chippremiumpack);
#elif UNITY_IOS
          CC_Interface.current.PurchaseItem(IAP_Product.aa_gempack8);
#endif
    }

    public void _OnClickContinue()
    {
        SoundUI.OnButtonClick();
        if (chipInfo.Item2 > 0)
        {
            if (isShowGiftFly)
                return;
            isShowGiftFly = true;
            StartCoroutine(IEShowAddGift());
        }
        else
        {
            objGift.SetActive(false);
            this.Hide();
        }
    }

    IEnumerator IEShowAddGift()
    {
        yield return new WaitForSeconds(0.1f);
        UIManager.Instance.GetPopup<UIItemGatherPos>(PopupId.ItemGatherPos).ShowInventory();
        SpawnItems.Instance.Show(ITEM_TYPE.CHIP, imgChipShow.transform.position,
            _count: Mathf.Clamp(chipInfo.Item2, 1, 1), _sprIcon: chipInfo.Item1);
        yield return new WaitForSeconds(1.5f);
        objGift.SetActive(false);
        this.Hide();
    }
}