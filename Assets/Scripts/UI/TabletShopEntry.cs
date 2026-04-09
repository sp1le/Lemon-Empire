using UnityEngine;
using UnityEngine.UI;
using LemonEmpire.Core;

namespace LemonEmpire.UI
{

    public class TabletShopEntry : MonoBehaviour
    {
        private TabletUI _tablet;
        private ShopItemData _item;
        private Text _qtyText;
        private int _quantity = 1;

        public void Setup(TabletUI tablet, ShopItemData item,
            Text qtyText, Button minusBtn, Button plusBtn, Button orderBtn)
        {
            _tablet = tablet;
            _item = item;
            _qtyText = qtyText;

            minusBtn.onClick.AddListener(Decrease);
            plusBtn.onClick.AddListener(Increase);
            orderBtn.onClick.AddListener(Order);

            UpdateDisplay();
        }

        public void UpdateAffordability(int balance)
        {

            var orderBtn = transform.Find("OrderBtn");
            if (orderBtn != null)
            {
                var img = orderBtn.GetComponent<Image>();
                if (img != null)
                {
                    bool canAfford = balance >= _item.price * _quantity;
                    img.color = canAfford
                        ? new Color(0.2f, 0.55f, 0.2f)
                        : new Color(0.45f, 0.25f, 0.25f);
                }
            }
        }

        private void Increase()
        {
            _quantity = Mathf.Min(_quantity + 1, 10);
            UpdateDisplay();
        }

        private void Decrease()
        {
            _quantity = Mathf.Max(_quantity - 1, 1);
            UpdateDisplay();
        }

        private void Order()
        {
            _tablet.OrderItem(_item, _quantity);
        }

        private void UpdateDisplay()
        {
            if (_qtyText != null)
                _qtyText.text = _quantity.ToString();

            int balance = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0;
            UpdateAffordability(balance);
        }
    }
}
