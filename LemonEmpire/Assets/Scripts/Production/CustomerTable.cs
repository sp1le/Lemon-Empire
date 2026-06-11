using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;
using LemonEmpire.Trading;
using LemonEmpire.UI;

namespace LemonEmpire.Production
{
    public enum TableState
    {
        Empty,
        WaitingForOrder,
        WaitingForDrink,
        Eating,
        Complete
    }

    public class CustomerTable : MonoBehaviour, IInteractable
    {
        [Header("Chairs / Seating")]
        [SerializeField] private Transform[] chairPoints = new Transform[2];

        [Header("State")]
        [SerializeField] private TableState state = TableState.Empty;

        private List<NPCBuyer> seatedNPCs = new List<NPCBuyer>();
        private float eatingTimer = 0f;
        private string activeOrderSpecifications = "";
        private float pendingEarnings = 0f;

        [Header("Patience Settings")]
        [SerializeField] private float baseOrderPatience = 45f;
        [SerializeField] private float baseDrinkPatience = 60f;
        private float tablePatienceTimer = 0f;
        private bool tablePatienceInitialized = false;

        public TableState State => state;
        public bool IsEmpty => state == TableState.Empty && seatedNPCs.Count == 0;
        public int SeatedCount => seatedNPCs.Count;

        public bool CanInteract => state == TableState.WaitingForOrder || state == TableState.WaitingForDrink;

        public string InteractionPrompt
        {
            get
            {
                if (state == TableState.WaitingForOrder)
                {
                    return "[E] Принять заказ у стола";
                }
                else if (state == TableState.WaitingForDrink)
                {
                    var carry = GetLocalPlayerCarry();
                    if (carry != null && carry.IsCarrying && carry.CarriedItem.ItemType == ItemType.BottledLemonade)
                    {
                        return $"[E] Подать {carry.CarriedItem.DisplayName} гостям";
                    }
                    else
                    {
                        return "Столик ожидает напиток";
                    }
                }
                return "";
            }
        }

        private PlayerCarry GetLocalPlayerCarry()
        {
            return FindFirstObjectByType<PlayerCarry>();
        }

        private void Update()
        {
            if (state == TableState.Eating)
            {
                eatingTimer -= Time.deltaTime;
                if (eatingTimer <= 0f)
                {
                    CompleteEating();
                }
            }
            else if (state == TableState.WaitingForOrder || state == TableState.WaitingForDrink)
            {
                if (!tablePatienceInitialized)
                {
                    float basePatience = state == TableState.WaitingForOrder ? baseOrderPatience : baseDrinkPatience;
                    float cleanliness = 100f;
                    if (LemonEmpire.Core.ShopCleanlinessManager.Instance != null)
                    {
                        cleanliness = LemonEmpire.Core.ShopCleanlinessManager.Instance.Cleanliness;
                    }
                    float patienceMultiplier = Mathf.Clamp(cleanliness / 100f, 0.1f, 1f);
                    tablePatienceTimer = basePatience * patienceMultiplier;
                    tablePatienceInitialized = true;
                }

                tablePatienceTimer -= Time.deltaTime;
                if (tablePatienceTimer <= 0f)
                {
                    HandleTableTimeout();
                }
            }
        }

        private void HandleTableTimeout()
        {
            Debug.Log($"[CustomerTable] Table timed out waiting for {(state == TableState.WaitingForOrder ? "order" : "drink")}!");

            // Clear order details
            activeOrderSpecifications = "";
            if (GameHUD.ActiveOrderText == activeOrderSpecifications)
            {
                GameHUD.ActiveOrderText = "";
            }

            // Make NPCs leave angry/disappointed
            foreach (var npc in seatedNPCs)
            {
                if (npc != null)
                {
                    npc.ForceLeave();
                }
            }

            seatedNPCs.Clear();
            pendingEarnings = 0;
            state = TableState.Empty;
            tablePatienceInitialized = false;
        }

        public bool AssignNPC(NPCBuyer npc)
        {
            if (seatedNPCs.Count >= 2 || (state != TableState.Empty && state != TableState.WaitingForOrder))
                return false;

            seatedNPCs.Add(npc);
            
            // Get chair point (fallback if not enough chairPoints assigned)
            Transform chair = null;
            if (chairPoints != null && chairPoints.Length > seatedNPCs.Count - 1)
            {
                chair = chairPoints[seatedNPCs.Count - 1];
            }
            
            npc.AssignTable(this, chair);

            if (state == TableState.Empty)
            {
                state = TableState.WaitingForOrder;
                tablePatienceInitialized = false;
            }

            return true;
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (state == TableState.WaitingForOrder)
            {
                TakeOrder();
            }
            else if (state == TableState.WaitingForDrink)
            {
                if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying)
                {
                    var item = context.PlayerCarry.CarriedItem;
                    if (item != null && item.ItemType == ItemType.BottledLemonade)
                    {
                        ItemBase drink = context.PlayerCarry.TakeItem();
                        if (drink != null)
                        {
                            DeliverDrink(drink);
                        }
                    }
                }
            }
        }

        private void TakeOrder()
        {
            if (seatedNPCs.Count == 0) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Заказ стола: ");
            for (int i = 0; i < seatedNPCs.Count; i++)
            {
                var npc = seatedNPCs[i];
                sb.Append($"{TranslateArchetype(npc.Archetype)} (Сахар: {npc.MinSugar:F0}-{npc.MaxSugar:F0}%, {TranslatePackaging(npc.PreferredPackaging)})");
                if (i < seatedNPCs.Count - 1) sb.Append(", ");
            }

            activeOrderSpecifications = sb.ToString();
            GameHUD.ActiveOrderText = activeOrderSpecifications; // Update active order on HUD

            state = TableState.WaitingForDrink;
            tablePatienceInitialized = false;
            Debug.Log($"[CustomerTable] Принят заказ: {activeOrderSpecifications}");
        }

        private string TranslateArchetype(NPCArchetype archetype)
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

        private void DeliverDrink(ItemBase drink)
        {
            // Evaluate how matches specifications
            int basePrice = 15;
            float totalEarning = 0f;

            foreach (var npc in seatedNPCs)
            {
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
                    multiplier = 1.5f; // Perfect drink!
                    tip = 20f;
                }
                else
                {
                    // Partial match evaluation
                    int matchCount = 0;
                    if (matchesSugar) matchCount++;
                    if (matchesAlcohol) matchCount++;
                    if (matchesCarbonation) matchCount++;
                    if (matchesPkg) matchCount++;

                    multiplier = 0.5f + (matchCount * 0.25f);
                    tip = matchCount * 4f;
                }

                totalEarning += (basePrice * multiplier) + tip;
            }

            pendingEarnings = totalEarning;

            // Consume/Destroy the drink
            drink.Consume();

            // Transition to Eating
            state = TableState.Eating;
            tablePatienceInitialized = false;
            eatingTimer = 5f;

            Debug.Log($"[CustomerTable] Напиток подан! Гости едят. Переход в Eating на 5 сек.");
        }

        private void CompleteEating()
        {
            state = TableState.Complete;

            if (EconomyManager.Instance != null && pendingEarnings > 0f)
            {
                EconomyManager.Instance.Earn(pendingEarnings);
                Debug.Log($"[CustomerTable] Гости закончили есть и заплатили ${pendingEarnings:F2}");
            }

            // Remove order note
            activeOrderSpecifications = "";
            GameHUD.ActiveOrderText = "";

            // Make NPCs exit
            foreach (var npc in seatedNPCs)
            {
                if (npc != null)
                {
                    npc.ForceLeave();
                }
            }

            seatedNPCs.Clear();
            pendingEarnings = 0f;
            state = TableState.Empty;
            Debug.Log("[CustomerTable] Стол пуст, гости ушли.");
        }
    }
}
