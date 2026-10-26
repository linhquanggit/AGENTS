using UnityEngine;
using UnityEngine.UI;

public class RowRewardMail : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private Text txtCount;

    public void SetData(RewardCount data)
    {
        imgIcon.sprite = data.GetSprRewardShow();
        txtCount.text = data.Count.ToString();
    }
}
