using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class DisplayStand : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [Header("Shelf Settings")]
        [SerializeField] protected int maxCapacity = 12;
        [SerializeField] protected float retailPrice = 15f;
        [SerializeField] protected Transform[] shelfSlots;

        [Header("Visual Alignment")]
        [SerializeField] protected float bottleRotationOffset = 90f;
        [SerializeField] protected float bottleVerticalOffset = 0.135f;

        protected List<ItemBase> shelfItems = new List<ItemBase>();

        public IReadOnlyList<ItemBase> ShelfItems => shelfItems;
        public virtual int MaxCapacity => UpgradeManager.HasPremiumShelves ? 36 : maxCapacity;

        public float RetailPrice => retailPrice;

        public virtual bool CanInteract => true;

        public virtual string InteractionPrompt
        {
            get
            {
                var carry = GetLocalPlayerCarry();
                if (carry != null && carry.IsCarrying)
                {
                    var item = carry.CarriedItem;
                    if (item != null && (item.ItemType == ItemType.BottledLemonade || item.ItemType == ItemType.BottlePack))
                    {
                        if (item.IsCrate)
                        {
                            if (item.Amount > 0 && shelfItems.Count < MaxCapacity)
                            {
                                return $"[E] Выгрузить {item.DrinkName} на полку ({item.Amount} шт в коробке)";
                            }
                        }
                        else
                        {
                            if (shelfItems.Count < MaxCapacity)
                            {
                                return $"[E] Поставить {item.DrinkName} на полку";
                            }
                        }
                    }
                    return "";
                }
                else
                {
                    if (shelfItems.Count > 0)
                    {
                        var lastItem = shelfItems[shelfItems.Count - 1];
                        return $"[E] Взять {lastItem.DrinkName}";
                    }
                    return "";
                }
            }
        }

        public virtual string SecondaryInteractionPrompt
        {
            get
            {
                var carry = GetLocalPlayerCarry();
                if (carry != null && carry.IsCarrying)
                {
                    var item = carry.CarriedItem;
                    if (item != null && (item.ItemType == ItemType.BottledLemonade || item.ItemType == ItemType.BottlePack) && item.IsCrate)
                    {
                        if (item.Amount < 6 && shelfItems.Count > 0)
                        {
                            return $"[F] Забрать {shelfItems[shelfItems.Count - 1].DrinkName} в коробку";
                        }
                    }
                    return "";
                }
                else
                {
                    return $"[F] Изменить цену стеллажа (Текущая: ${retailPrice:F2})";
                }
            }
        }

        public virtual bool CanSecondaryInteract => true;

        public virtual void SecondaryInteract(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item != null && (item.ItemType == ItemType.BottledLemonade || item.ItemType == ItemType.BottlePack) && item.IsCrate)
                {
                    if (item.Amount < 6 && shelfItems.Count > 0)
                    {
                        // Pull back into crate
                        var bottleToTake = shelfItems[shelfItems.Count - 1];
                        shelfItems.RemoveAt(shelfItems.Count - 1);
                        item.SetupDrink(bottleToTake.DrinkName, bottleToTake.Sugar, bottleToTake.Carbonation, bottleToTake.Alcohol, bottleToTake.Packaging, item.Amount + 1);
                        Destroy(bottleToTake.gameObject);
                        UpdateBottlePositions();
                    }
                }
            }
            else
            {
                if (LemonEmpire.UI.GameHUD.Instance != null)
                {
                    LemonEmpire.UI.GameHUD.Instance.ShowPriceInputPanel("Стеллаж", retailPrice, (newPrice) => {
                        SetPrice(newPrice);
                    });
                }
            }
        }

        protected virtual void Start()
        {
            // Optional initialization
        }

        protected virtual void OnDestroy()
        {
            // Optional cleanup
        }

        private PlayerCarry GetLocalPlayerCarry()
        {
            return FindFirstObjectByType<PlayerCarry>();
        }

        public virtual void Interact(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item != null && (item.ItemType == ItemType.BottledLemonade || item.ItemType == ItemType.BottlePack))
                {
                    if (item.IsCrate)
                    {
                        if (item.Amount > 0 && shelfItems.Count < MaxCapacity)
                        {
                            ItemBase bottleToPlace = null;
                            var bottlePrefab = Resources.Load<GameObject>("LemonadeBottle");
                            if (bottlePrefab != null)
                            {
                                var cloneGo = Instantiate(bottlePrefab);
                                bottleToPlace = cloneGo.GetComponent<ItemBase>();
                            }
                            else
                            {
                                var cloneGo = Instantiate(item.gameObject);
                                bottleToPlace = cloneGo.GetComponent<ItemBase>();
                            }
                            bottleToPlace.SetupDrink(item.DrinkName, item.Sugar, item.Carbonation, item.Alcohol, item.Packaging, 1);
                            item.Amount--;

                            if (bottleToPlace != null)
                            {
                                PlaceBottleOnShelf(bottleToPlace);
                            }
                        }
                    }
                    else
                    {
                        if (shelfItems.Count < MaxCapacity)
                        {
                            var bottleToPlace = context.PlayerCarry.TakeItem();
                            if (bottleToPlace != null)
                            {
                                PlaceBottleOnShelf(bottleToPlace);
                            }
                        }
                    }
                }
            }
            else
            {
                // If NOT carrying anything
                if (shelfItems.Count > 0)
                {
                    var bottle = shelfItems[shelfItems.Count - 1];
                    if (context.PlayerCarry.TryPickup(bottle))
                    {
                        shelfItems.RemoveAt(shelfItems.Count - 1);
                        UpdateBottlePositions();
                    }
                }
            }
        }

        private void PlaceBottleOnShelf(ItemBase bottle)
        {
            var rb = bottle.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            var col = bottle.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            bottle.transform.SetParent(this.transform);
            bottle.RetailPrice = retailPrice;

            shelfItems.Add(bottle);
            UpdateBottlePositions();
        }

        public void SetPrice(float newPrice)
        {
            retailPrice = newPrice;

            // Sync all items currently on the shelf
            foreach (var item in shelfItems)
            {
                if (item != null)
                {
                    item.RetailPrice = retailPrice;
                }
            }
        }

        protected void UpdateBottlePositions()
        {
            for (int i = 0; i < shelfItems.Count; i++)
            {
                if (shelfItems[i] == null) continue;

                if (shelfSlots != null && i < shelfSlots.Length && shelfSlots[i] != null)
                {
                    shelfItems[i].transform.SetParent(shelfSlots[i]);
                    shelfItems[i].transform.localPosition = new Vector3(0f, bottleVerticalOffset, 0f);
                    shelfItems[i].transform.rotation = this.transform.rotation * Quaternion.Euler(0f, bottleRotationOffset, 0f);
                }
                else
                {
                    // Fallback to dynamic grid positioning
                    shelfItems[i].transform.SetParent(transform);
                    // Arrange bottles in a grid layout fallback
                    float x = (i % 4) * 0.2f - 0.3f;
                    float y = (i / 4) * 0.4f + 0.2f;
                    float z = 0f;
                    shelfItems[i].transform.localPosition = new Vector3(x, y + bottleVerticalOffset, z);
                    shelfItems[i].transform.rotation = this.transform.rotation * Quaternion.Euler(0f, bottleRotationOffset, 0f);
                }
            }
        }
    }
}
