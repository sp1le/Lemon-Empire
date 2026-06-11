using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Trading
{

    public class TradeStand : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [Header("Stand Settings")]
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private float pricePerBottle = 1.00f;
        [SerializeField] private float priceStep = 0.10f;
        [SerializeField] private float minPrice = 0.10f;
        [SerializeField] private float maxPrice = 5.00f;

        [Header("Display")]
        [SerializeField] private Transform displayArea;
        [SerializeField] private GameObject bottlePrefab;

        private List<float> _stockQualities = new();
        private List<GameObject> _dummyBottles = new();
        private List<float> _dummyYOffsets = new();

        public int StockedCount => _stockQualities.Count;
        public float Price => pricePerBottle;
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
                var carry = GetLocalPlayerCarry();
                if (carry != null && carry.IsCarrying)
                {
                    var item = carry.CarriedItem;
                    if (item.ItemType == ItemType.BottledLemonade)
                    {
                        if (item.IsCrate)
                        {
                            if (item.Amount > 0 && _stockQualities.Count < maxSlots)
                            {
                                return $"[E] Выложить {item.DrinkName} на стенд ({item.Amount} шт в коробке)";
                            }
                        }
                        else
                        {
                            if (_stockQualities.Count < maxSlots)
                            {
                                return $"[E] Поставить {item.DrinkName} на стенд";
                            }
                        }
                    }
                    return "";
                }
                else
                {
                    if (_stockQualities.Count > 0)
                    {
                        return $"[E] Взять лимонад (${pricePerBottle:F2}) | Товар: {_stockQualities.Count}/{maxSlots}";
                    }
                    else
                    {
                        return $"Стенд (${pricePerBottle:F2}) (Пусто)";
                    }
                }
            }
        }

        public string SecondaryInteractionPrompt
        {
            get
            {
                var carry = GetLocalPlayerCarry();
                if (carry != null && carry.IsCarrying)
                {
                    var item = carry.CarriedItem;
                    if (item != null && item.ItemType == ItemType.BottledLemonade && item.IsCrate)
                    {
                        if (item.Amount < 6 && _stockQualities.Count > 0)
                        {
                            return $"[F] Забрать {item.DrinkName} в коробку";
                        }
                    }
                    return "";
                }
                else
                {
                    return $"[F] Установить цену (Текущая: ${pricePerBottle:F2})";
                }
            }
        }

        public bool CanSecondaryInteract => true;

        public void SecondaryInteract(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item != null && item.ItemType == ItemType.BottledLemonade && item.IsCrate)
                {
                    if (item.Amount < 6 && _stockQualities.Count > 0)
                    {
                        int spaceInCrate = 6 - item.Amount;
                        int toTake = Mathf.Min(spaceInCrate, _stockQualities.Count);
                        
                        float lastQuality = _stockQualities[_stockQualities.Count - 1];
                        
                        item.Amount += toTake;
                        for (int i = 0; i < toTake; i++)
                        {
                            _stockQualities.RemoveAt(_stockQualities.Count - 1);
                        }
                        
                        item.SetupDrink(item.DrinkName, item.Sugar, item.Carbonation, item.Alcohol, item.Packaging, item.Amount);
                        item.Quality = lastQuality;
                        
                        UpdateDisplay();
                    }
                }
            }
            else
            {
                if (LemonEmpire.UI.GameHUD.Instance != null)
                {
                    LemonEmpire.UI.GameHUD.Instance.ShowPriceInputPanel("Стенд", pricePerBottle, (newPrice) => {
                        SetPrice(newPrice);
                    });
                }
            }
        }

        private PlayerCarry GetLocalPlayerCarry()
        {
            return FindFirstObjectByType<PlayerCarry>();
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item.ItemType == ItemType.BottledLemonade)
                {
                    if (item.IsCrate)
                    {
                        if (item.Amount > 0 && _stockQualities.Count < maxSlots)
                        {
                            int space = maxSlots - _stockQualities.Count;
                            int toTake = Mathf.Min(space, item.Amount);
                            item.Amount -= toTake;

                            for (int i = 0; i < toTake; i++)
                            {
                                _stockQualities.Add(item.Quality);
                            }
                            UpdateDisplay();
                        }
                    }
                    else
                    {
                        if (_stockQualities.Count < maxSlots)
                        {
                            _stockQualities.Add(item.Quality);
                            context.PlayerCarry.TakeItem().Consume();
                            UpdateDisplay();
                        }
                    }
                }
            }
            else
            {
                if (_stockQualities.Count > 0)
                {
                    var bottlePrefabToUse = bottlePrefab;
                    GameObject bottleGo = null;
                    if (bottlePrefabToUse != null)
                    {
                        bottleGo = Instantiate(bottlePrefabToUse);
                        bottleGo.name = bottlePrefabToUse.name;
                    }
                    else
                    {
                        bottleGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        bottleGo.name = "LemonadeBottle";
                    }

                    var bottleItem = bottleGo.GetComponent<ItemBase>();
                    if (bottleItem == null) bottleItem = bottleGo.AddComponent<ItemBase>();

                    float qualityToUse = _stockQualities[_stockQualities.Count - 1];
                    _stockQualities.RemoveAt(_stockQualities.Count - 1);

                    bottleItem.SetupDrink("Лимонад", 50f, 50f, 0f, PackagingType.Plastic, 1);
                    bottleItem.Quality = qualityToUse;
                    bottleItem.RetailPrice = pricePerBottle;

                    context.PlayerCarry.TryPickup(bottleItem);
                    UpdateDisplay();
                }
            }
        }

        private void UpdateDisplay()
        {
            Transform parent = displayArea != null ? displayArea : transform;

            while (_dummyBottles.Count < _stockQualities.Count)
            {
                GameObject go;
                if (bottlePrefab != null)
                {
                    // 1. Instantiate without parent to get clean world scale
                    go = Instantiate(bottlePrefab);
                    go.name = "DummyBottle";
                    // 2. Set to prefab's original local scale
                    go.transform.localScale = bottlePrefab.transform.localScale;
                    // 3. Parent it while maintaining world scale (Unity will auto-adjust localScale to counteract any parent stretching)
                    go.transform.SetParent(parent, true);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = "DummyBottle";
                    go.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
                    go.transform.SetParent(parent, true);

                    var rend = go.GetComponent<Renderer>();
                    if (rend != null) rend.material.color = Color.yellow;
                }

                // Calculate how much to lift the bottle so its bottom is at Y=0
                float yOffset = 0f;
                var col = go.GetComponentInChildren<Collider>();
                if (col != null)
                {
                    Physics.SyncTransforms();
                    yOffset = go.transform.position.y - col.bounds.min.y;
                }
                else if (bottlePrefab == null)
                {
                    yOffset = 0.4f;
                }
                _dummyYOffsets.Add(yOffset);

                // Remove colliders from dummies so they don't mess with physics
                foreach (var c in go.GetComponentsInChildren<Collider>())
                {
                    Destroy(c);
                }

                _dummyBottles.Add(go);
            }

            while (_dummyBottles.Count > _stockQualities.Count)
            {
                int lastIdx = _dummyBottles.Count - 1;
                var go = _dummyBottles[lastIdx];
                _dummyBottles.RemoveAt(lastIdx);
                _dummyYOffsets.RemoveAt(lastIdx);
                Destroy(go);
            }

            for (int i = 0; i < _dummyBottles.Count; i++)
            {
                float x = (i % 3) * 0.4f - 0.4f;
                float z = (i / 3) * 0.4f;
                float yIdx = _dummyYOffsets[i];

                _dummyBottles[i].transform.localPosition = new Vector3(x, yIdx, z);
            }
        }

        public bool SellOne()
        {
            return SellMultiple(1, pricePerBottle);
        }

        public bool SellMultiple(int count, float revenue)
        {
            if (_stockQualities.Count < count) return false;

            for (int i = 0; i < count; i++)
            {
                _stockQualities.RemoveAt(0);
            }

            if (EconomyManager.Instance != null && revenue > 0f)
                EconomyManager.Instance.Earn(revenue);

            UpdateDisplay();
            return true;
        }

        public void SetPrice(float newPrice)
        {
            pricePerBottle = newPrice;
        }

        public void AdjustPrice(float direction)
        {
            pricePerBottle += direction * priceStep;
            pricePerBottle = Mathf.Clamp(pricePerBottle, minPrice, maxPrice);
        }
    }
}
