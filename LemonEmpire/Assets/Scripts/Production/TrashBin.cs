using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class TrashBin : MonoBehaviour, IInteractable
    {
        public bool CanInteract => true;

        public string InteractionPrompt
        {
            get
            {
                var carry = FindFirstObjectByType<PlayerCarry>();
                if (carry != null && carry.IsCarrying && IsBoxOrCrate(carry.CarriedItem))
                {
                    return "[E] Выбросить мусорную коробку в бак";
                }
                return "Мусорный бак для коробок";
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying)
            {
                var carriedItem = context.PlayerCarry.CarriedItem;
                if (IsBoxOrCrate(carriedItem))
                {
                    var item = context.PlayerCarry.TakeItem();
                    if (item != null)
                    {
                        item.Consume();

                        if (ShopCleanlinessManager.Instance != null)
                        {
                            ShopCleanlinessManager.Instance.IncreaseCleanliness(10f);
                        }

                        Debug.Log("[TrashBin] Thrown away empty crate or box.");
                    }
                }
            }
        }

        private bool IsBoxOrCrate(ItemBase item)
        {
            if (item == null) return false;

            // Check if EmptyCrate
            if (item.GetComponent<EmptyCrate>() != null) return true;

            // Check item types
            if (item.ItemType == ItemType.LemonCrate ||
                item.ItemType == ItemType.BottlePack ||
                item.ItemType == ItemType.EquipmentBox) return true;

            // Name check fallback
            string nameLower = item.name.ToLower();
            if (nameLower.Contains("crate") ||
                nameLower.Contains("box") ||
                nameLower.Contains("bag") ||
                nameLower.Contains("pack")) return true;

            return false;
        }
    }
}
