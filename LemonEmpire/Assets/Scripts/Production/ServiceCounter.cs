using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class ServiceCounter : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [Header("Counter Settings")]
        [SerializeField] private Transform placementPoint;

        private ItemBase placedDrink;

        public ItemBase PlacedDrink => placedDrink;

        public void ClearPlacedDrink()
        {
            placedDrink = null;
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
                    if (item != null && item.ItemType == ItemType.BottledLemonade)
                    {
                        if (item.IsCrate)
                        {
                            if (item.Amount > 0 && placedDrink == null)
                            {
                                return $"[E] Поставить {item.DrinkName} на прилавок";
                            }
                        }
                        else
                        {
                            if (placedDrink == null)
                            {
                                return $"[E] Поставить {item.DrinkName} на прилавок";
                            }
                        }
                    }
                    return "";
                }
                else
                {
                    if (placedDrink != null)
                    {
                        return $"[E] Взять {placedDrink.DrinkName} с прилавка";
                    }
                    else
                    {
                        return "Прилавок (пусто)";
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
                        if (item.Amount < 6 && placedDrink != null)
                        {
                            return $"[F] Забрать {placedDrink.DrinkName} в коробку";
                        }
                    }
                }
                return "";
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
                    if (item.Amount < 6 && placedDrink != null)
                    {
                        // Pull back into crate
                        var bottleToTake = placedDrink;
                        placedDrink = null;
                        item.SetupDrink(bottleToTake.DrinkName, bottleToTake.Sugar, bottleToTake.Carbonation, bottleToTake.Alcohol, bottleToTake.Packaging, item.Amount + 1);
                        Destroy(bottleToTake.gameObject);
                    }
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
                if (item != null && item.ItemType == ItemType.BottledLemonade)
                {
                    if (item.IsCrate)
                    {
                        if (item.Amount > 0 && placedDrink == null)
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
                                PlaceBottleOnCounter(bottleToPlace);
                            }
                        }
                    }
                    else
                    {
                        if (placedDrink == null)
                        {
                            var bottleToPlace = context.PlayerCarry.TakeItem();
                            if (bottleToPlace != null)
                            {
                                PlaceBottleOnCounter(bottleToPlace);
                            }
                        }
                    }
                }
            }
            else
            {
                if (placedDrink != null)
                {
                    if (context.PlayerCarry.TryPickup(placedDrink))
                    {
                        placedDrink = null;
                    }
                }
            }
        }

        private void PlaceBottleOnCounter(ItemBase bottle)
        {
            placedDrink = bottle;

            // Disable physics/colliders
            var rb = placedDrink.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            var col = placedDrink.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Set parent and local position/rotation
            Transform parent = placementPoint != null ? placementPoint : transform;
            placedDrink.transform.SetParent(parent);
            placedDrink.transform.localPosition = Vector3.zero;
            placedDrink.transform.localRotation = Quaternion.identity;
        }
    }
}
