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

    private int targetLevel = 5;

    private Tuple<Sprite, int> chipInfo;

    private bool isShowGiftFly = false;

    private bool isChanged = false;


    private void OnEnable()
    {
        isChanged = false;
    }

    public void OnPurchaseSuccess()
    {
        var _chiprd = Chips.RandomChipBySlot(6, CHIP_TIER_TYPE.Tier3);
        var _process = Chip.GetProcess(_chiprd.Id);
        if (_process == null)
        {
            Chip.AddChip(_chiprd.Id, 0);
            Chip.UpLevelChip(_chiprd.Id, 5);
            chipInfo = new Tuple<Sprite, int>(Chips.GetAvatarChip(_chiprd.Id), 0);
        }
        else
        {
            var _currentLv = _process.Level;
            var _chipsToAdd = 0;
            var _toLevel = _currentLv <= targetLevel ? _currentLv : targetLevel;
            for (int i = 0; i < _toLevel; i++)
            {
                var (_chipRequired, _) = Chips.GetRequiredForUpgradeLevel(_chiprd.Id, i);
                _chipsToAdd += _chipRequired;
            }

            if (_currentLv <= targetLevel)
                Chip.UpLevelChip(_chiprd.Id, targetLevel);
            Chip.AddChip(_chiprd.Id, _chipsToAdd);
            chipInfo = new Tuple<Sprite, int>(Chips.GetAvatarChip(_chiprd.Id), _chipsToAdd);
        }

        ChipStores.SetBoughtOfferSlot6();
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