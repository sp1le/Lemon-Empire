using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class VendingMachine : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => "[E] Купить батончик ($1.50) | Сытость +35";

        public bool CanInteract => PlayerVitals.Instance != null && EconomyManager.Instance != null && EconomyManager.Instance.Balance >= 1.5f;

        public void Interact(PlayerInteractionContext context)
        {
            if (EconomyManager.Instance == null || PlayerVitals.Instance == null) return;

            // Balance in EconomyManager is stored as integers (whole dollars), so we charge $2 (since $1.50 rounds up to $2).
            if (EconomyManager.Instance.TrySpend(2))
            {
                PlayerVitals.Instance.ReplenishSatiety(35f);
                Debug.Log("[VendingMachine] Bought a candy bar. Satiety +35, Cash spent: $2");
            }
        }
    }
}
