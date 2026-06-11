using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class VendingMachine : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => "[E] Купить батончик ($1.50) | Сытость +35";

        public bool CanInteract => UpgradeManager.HasSnackVending && PlayerVitals.Instance != null && EconomyManager.Instance != null && EconomyManager.Instance.Balance >= 1.5f;

        public void Interact(PlayerInteractionContext context)
        {
            if (EconomyManager.Instance == null || PlayerVitals.Instance == null) return;

            // Balance in EconomyManager is stored as float, so we can charge exactly $1.50
            if (EconomyManager.Instance.TrySpend(1.5f))
            {
                PlayerVitals.Instance.ReplenishSatiety(35f);
                Debug.Log("[VendingMachine] Bought a candy bar. Satiety +35, Cash spent: $1.50");
            }
        }
    }
}
