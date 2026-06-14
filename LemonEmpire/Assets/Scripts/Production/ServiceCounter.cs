using UnityEngine;
using System.Collections.Generic;
using LemonEmpire.Core;
using LemonEmpire.Player;
using LemonEmpire.Trading;

namespace LemonEmpire.Production
{
    public class ServiceCounter : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [Header("Counter Settings")]
        [SerializeField] private Transform placementPoint;

        private List<NPCBuyer> queue = new List<NPCBuyer>();

        public bool HasActiveCustomer => queue.Count > 0 && queue[0] != null;
        public NPCBuyer ActiveCustomer => HasActiveCustomer ? queue[0] : null;

        public List<NPCBuyer> GetWaitingCustomers()
        {
            List<NPCBuyer> waiting = new List<NPCBuyer>();
            foreach (var npc in queue)
            {
                if (npc != null && (npc.BarState == NPCBuyer.BarCustomerState.WaitingForOrder || npc.BarState == NPCBuyer.BarCustomerState.WaitingForDrink))
                {
                    waiting.Add(npc);
                }
            }
            return waiting;
        }

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
                var carryDefault = GetLocalPlayerCarry();
                if (carryDefault != null && carryDefault.IsCarrying)
                {
                    var item = carryDefault.CarriedItem;
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
                if (HasActiveCustomer) return "";

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

        public bool CanSecondaryInteract => !HasActiveCustomer;

        public void SecondaryInteract(PlayerInteractionContext context)
        {
            if (HasActiveCustomer) return;
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

        public void RegisterCustomer(NPCBuyer npc)
        {
            if (!queue.Contains(npc))
            {
                queue.Add(npc);
                UpdateQueuePositions();
            }
        }

        public void UnregisterCustomer(NPCBuyer npc)
        {
            if (queue.Contains(npc))
            {
                queue.Remove(npc);
                UpdateQueuePositions();
            }
        }

        public void UpdateQueuePositions()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i] != null)
                {
                    Vector3 targetPos = GetQueuePosition(i);
                    queue[i].SetQueueDestination(targetPos, i == 0);
                }
            }
        }

        public Vector3 GetQueuePosition(int index)
        {
            Vector3 queueStart = transform.position - transform.forward * 0.9f;
            return queueStart - transform.forward * (index * 0.8f);
        }

        public bool IsPlayerBehindCounter()
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player == null) return false;

            Vector3 counterPos = transform.position;
            Vector3 fwd = transform.forward;

            float playerProj = Vector3.Dot(player.transform.position - counterPos, fwd);
            float playerDist = Vector3.Distance(player.transform.position, counterPos);

            return playerProj > 0f && playerDist <= 3.5f;
        }

        public void TakeOrder(NPCBuyer npc)
        {
            npc.TransitionToWaitingForDrink();
            Debug.Log($"[ServiceCounter] Taken order from {npc.gameObject.name}: {TranslateArchetype(npc.Archetype)}");
        }

        public void DeliverDrink(ItemBase drink, NPCBuyer npc)
        {
            int basePrice = 15;
            float totalEarning = 0f;

            bool matchesSugar = drink.Sugar >= npc.MinSugar && drink.Sugar <= npc.MaxSugar;
            bool matchesAlcohol = drink.Alcohol >= npc.MinAlcohol && drink.Alcohol <= npc.MaxAlcohol;
            bool matchesCarbonation = drink.Carbonation >= npc.MinCarbonation && drink.Carbonation <= npc.MaxCarbonation;
            
            bool matchesPkg = true;
            if (npc.PreferredPackaging.HasValue)
            {
                matchesPkg = drink.Packaging == npc.PreferredPackaging.Value;
            }

            float multiplier = 1.0f;
            float tip = 0f;

            if (matchesSugar && matchesAlcohol && matchesCarbonation && matchesPkg)
            {
                multiplier = 1.5f;
                tip = 20f;
            }
            else
            {
                int matchCount = 0;
                if (matchesSugar) matchCount++;
                if (matchesAlcohol) matchCount++;
                if (matchesCarbonation) matchCount++;
                if (matchesPkg) matchCount++;

                multiplier = 0.5f + (matchCount * 0.25f);
                tip = matchCount * 4f;
            }

            totalEarning = (basePrice * multiplier) + tip;

            if (EconomyManager.Instance != null && totalEarning > 0f)
            {
                EconomyManager.Instance.Earn(totalEarning);
                Debug.Log($"[ServiceCounter] Bar customer bought drink for ${totalEarning:F2}");
            }

            if (PlayerVitals.Instance != null)
            {
                PlayerVitals.Instance.OnDealSuccess();
            }

            PlaceBottleOnCounter(drink);
            npc.SetDrinkingState(drink);
        }

        public string TranslateArchetype(NPCArchetype archetype)
        {
            switch (archetype)
            {
                case NPCArchetype.Kids: return "Ребенок";
                case NPCArchetype.Athletes: return "Спортсмен";
                case NPCArchetype.Hipsters: return "Хипстер";
                case NPCArchetype.PartyAnimals: return "Тусовщик";
                default: return "Покупатель";
            }
        }

        private string TranslatePackaging(PackagingType? pkg)
        {
            if (!pkg.HasValue) return "Любая тара";
            switch (pkg.Value)
            {
                case PackagingType.Plastic: return "Пластик";
                case PackagingType.Can: return "Жестянка";
                case PackagingType.Glass: return "Стекло";
                default: return "Любая тара";
            }
        }
    }
}
