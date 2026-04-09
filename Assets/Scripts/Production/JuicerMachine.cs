using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{

    public class JuicerMachine : MachineBase
    {
        [Header("Juicer Input")]
        [SerializeField] private ItemType requiredInput = ItemType.LemonCrate;

        protected override string GetIdlePrompt()
        {
            return $"[E] Загрузить сырьё ({requiredInput})";
        }

        protected override void TryAcceptItem(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null || !context.PlayerCarry.IsCarrying)
            {
                Debug.Log($"Juicer: Player empty-handed. Need {requiredInput}.");
                return;
            }

            var item = context.PlayerCarry.CarriedItem;
            if (item.ItemType != requiredInput)
            {
                Debug.Log($"Juicer: Wrong item. Expected {requiredInput}, got {item.ItemType}.");
                return;
            }

            var consumedItem = context.PlayerCarry.TakeItem();

            consumedItem.Consume();

            StartQtePhase();
        }
    }
}
