using DG.Tweening;
using DP.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RtfSpawnReward : MonoBehaviour
{
    [SerializeField] protected float childScale = 1f;
    [SerializeField] float delaySpawn = 0f;
    //
    protected List<RewardCount> rewards = new List<RewardCount>();
    protected List<ItemReward> itemsSpawned = new List<ItemReward>();
    protected bool isReceived;
    BarItems barItem;
    System.Action onShowCompleted;
    //
    public virtual void OnDestroyAllChild()
    {
        onShowCompleted = null;
        barItem = null;
        StopAllCoroutines();
        rewards.Clear();
        itemsSpawned.Clear();
        this.transform.DestroyAllChild();
    }
    public virtual void Show(List<RewardCount> _rewards, bool _isReceived, System.Action _onCompleted = null)
    {
        onShowCompleted = _onCompleted;
        barItem = Manager.Instance.BarItem;
        rewards = new List<RewardCount>(_rewards);
        isReceived = _isReceived;
        if (rewards == null || rewards.Count <= 0) return;
        //
        StartCoroutine(IESpawn());
    }
    IEnumerator IESpawn()
    {
        foreach (var _item in rewards)
        {
            Spawn(_item);
            yield return new WaitForSeconds(delaySpawn);
        }
        if (onShowCompleted != null)
            onShowCompleted.Invoke();
    }
    protected virtual void Spawn(RewardCount _reward)
    {
        //DPDebug.Log($"Spawn reward type--[{_reward.Type}]");
        if (_reward.Type == REWARD_TYPE.REMOVE_ADS) return;
        if (_reward.Type == REWARD_TYPE.IAP_PRODUCT) return;
        //
        var _prefab = Rewards.GetData(_reward.Type).PrefabShow;
        if (_prefab == null) return;
        //
        var _obj = Instantiate(_prefab, this.transform);
        ItemReward _item = _obj.GetComponent<ItemReward>();
        _item.Show(_reward.Count, isReceived, 0f, _scale: childScale);
        itemsSpawned.Add(_item);
    }
    public virtual void OnBornEffects()
    {
        if (itemsSpawned.Count <= 0) return;
        //
        UIManager.Instance.GetPopup<UIItemGatherPos>(PopupId.ItemGatherPos).ShowInventory();
        foreach (var _item in itemsSpawned)
        {
            _item.OnBornEffect();
        }
    }
    public void PlayX2Effect()
    {
        foreach (var _item in itemsSpawned)
        {
            _item.X2Effect();
        }
    }
}
