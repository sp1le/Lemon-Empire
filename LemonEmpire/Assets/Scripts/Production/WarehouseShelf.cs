using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class WarehouseShelf : MonoBehaviour, IInteractable
    {
        [Header("Warehouse Shelf Settings")]
        [SerializeField] private int maxCapacity = 12;
        [SerializeField] private Transform[] shelfSlots;

        private List<ItemBase> storedItems = new List<ItemBase>();

        public bool CanInteract => true;

        public string InteractionPrompt
        {
            get
            {
                var carry = GetLocalPlayerCarry();
                if (carry != null && carry.IsCarrying)
                {
                    var item = carry.CarriedItem;
                    if (item != null && IsValidItem(item))
                    {
                        if (storedItems.Count < maxCapacity)
                        {
                            return $"[E] Поставить {item.DisplayName} на складской стеллаж ({storedItems.Count}/{maxCapacity})";
                        }
                    }
                    return "";
                }
                else
                {
                    if (storedItems.Count > 0)
                    {
                        var lastItem = storedItems[storedItems.Count - 1];
                        return $"[E] Взять {lastItem.DisplayName} со стеллажа ({storedItems.Count}/{maxCapacity})";
                    }
                    return "Складской стеллаж (Пусто)";
                }
            }
        }

        private PlayerCarry GetLocalPlayerCarry()
        {
            return FindFirstObjectByType<PlayerCarry>();
        }

        private bool IsValidItem(ItemBase item)
        {
            // Allow placing any crate, box, bag or empty crate. Basically anything except single bottles.
            return item.ItemType != ItemType.BottledLemonade || item.IsCrate;
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null) return;

            if (context.PlayerCarry.IsCarrying)
            {
                var item = context.PlayerCarry.CarriedItem;
                if (item != null && IsValidItem(item) && storedItems.Count < maxCapacity)
                {
                    var takenItem = context.PlayerCarry.TakeItem();
                    if (takenItem != null)
                    {
                        PlaceItemOnShelf(takenItem);
                    }
                }
            }
            else
            {
                if (storedItems.Count > 0)
                {
                    var item = storedItems[storedItems.Count - 1];
                    if (context.PlayerCarry.TryPickup(item))
                    {
                        storedItems.RemoveAt(storedItems.Count - 1);
                    }
                }
            }
        }

        private void PlaceItemOnShelf(ItemBase item)
        {
            var rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            var col = item.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            int slotIdx = storedItems.Count;
            Transform parent = (shelfSlots != null && slotIdx < shelfSlots.Length && shelfSlots[slotIdx] != null) ? shelfSlots[slotIdx] : transform;

            item.transform.SetParent(parent);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            storedItems.Add(item);
        }
    }
}
