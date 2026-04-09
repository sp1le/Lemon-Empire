using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Trading
{

    public class TradeStand : MonoBehaviour, IInteractable
    {
        [Header("Stand Settings")]
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private int pricePerBottle = 100;
        [SerializeField] private int priceStep = 10;
        [SerializeField] private int minPrice = 10;
        [SerializeField] private int maxPrice = 500;

        [Header("Display")]
        [SerializeField] private Transform displayArea;

        private List<float> _stockQualities = new();
        private List<GameObject> _dummyBottles = new();

        public int StockedCount => _stockQualities.Count;
        public int Price => pricePerBottle;
        public bool HasStock => _stockQualities.Count > 0;
        public float AverageQuality
        {
            get
            {
                if (_stockQualities.Count == 0) return 0f;
                float sum = 0f;
                foreach (var q in _stockQualities)
                    sum += q;
                return sum / _stockQualities.Count;
            }
        }

        public bool CanInteract => true;
        public string InteractionPrompt
        {
            get
            {
                return $"[E] Стенд (${pricePerBottle}) | Товар: {_stockQualities.Count}/{maxSlots}\n[Q/R] Цена";
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item.ItemType == ItemType.BottledLemonade)
                {
                    int space = maxSlots - _stockQualities.Count;
                    if (space > 0)
                    {
                        int toTake = Mathf.Min(space, item.Amount);
                        item.Amount -= toTake;

                        for (int i = 0; i < toTake; i++)
                        {
                            _stockQualities.Add(item.Quality);
                        }

                        if (item.Amount <= 0)
                        {
                            context.PlayerCarry.TakeItem().Consume();
                        }

                        UpdateDisplay();
                    }
                    else
                    {
                        Debug.Log("TradeStand: Стенд заполнен!");
                    }
                }
                else
                {
                    Debug.Log("TradeStand: Сюда можно ставить только лимонад!");
                }
            }
        }

        private void UpdateDisplay()
        {
            Transform parent = displayArea != null ? displayArea : transform;

            while (_dummyBottles.Count < _stockQualities.Count)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "DummyBottle";
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);

                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.material.color = Color.yellow;

                Destroy(go.GetComponent<Collider>());
                _dummyBottles.Add(go);
            }

            while (_dummyBottles.Count > _stockQualities.Count)
            {
                var go = _dummyBottles[_dummyBottles.Count - 1];
                _dummyBottles.RemoveAt(_dummyBottles.Count - 1);
                Destroy(go);
            }

            for (int i = 0; i < _dummyBottles.Count; i++)
            {
                float x = (i % 3) * 0.4f - 0.4f;
                float z = (i / 3) * 0.4f;
                _dummyBottles[i].transform.localPosition = new Vector3(x, 0.4f, z);
            }
        }

        public bool SellOne()
        {
            if (_stockQualities.Count == 0) return false;

            _stockQualities.RemoveAt(0);

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Earn(pricePerBottle);

            UpdateDisplay();
            return true;
        }

        public void AdjustPrice(int direction)
        {
            pricePerBottle += direction * priceStep;
            pricePerBottle = Mathf.Clamp(pricePerBottle, minPrice, maxPrice);
        }
    }
}
