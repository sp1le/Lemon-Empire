using UnityEngine;

namespace LemonEmpire.Core
{

    [System.Serializable]
    public class ShopItemData
    {
        public string name = "Предмет";
        public ItemType itemType;
        public int price = 50;
        public int defaultAmount = 1;
        public GameObject prefab;

        [Tooltip("Delivery time in seconds")]
        public float deliveryTime = 5f;
    }
}
