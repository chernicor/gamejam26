using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Dany
{
public class SlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Image highlightImage;

    public void UpdateSlot(InventoryItem item, int count)
    {
        if (item != null && count > 0)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            countText.text = (count > 1) ? count.ToString() : "";
            countText.enabled = (count > 1);
        }
        else
        {
            iconImage.enabled = false;
            countText.enabled = false;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (highlightImage != null)
        {
            highlightImage.enabled = isSelected;
        }
    }
}
}