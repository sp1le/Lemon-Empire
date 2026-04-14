using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{

    public class BottlerMachine : MachineBase
    {
        [Header("Bottler Inputs")]
        [SerializeField] private ItemType inputA = ItemType.LemonadeContainer;
        [SerializeField] private ItemType inputB = ItemType.BottlePack;

        private bool _hasInputA;
        private bool _hasInputB;

        public override bool HasPendingIngredients() => _hasInputA || _hasInputB;

        protected override string GetIdlePrompt()
        {
            if (!_hasInputA && !_hasInputB)
                return $"[E] Нужны: {inputA} и {inputB}";
            if (!_hasInputA)
                return $"[E] Нужен: {inputA}";
            if (!_hasInputB)
                return $"[E] Нужен: {inputB}";

            return "[E] Запустить";
        }

        protected override void TryAcceptItem(PlayerInteractionContext context)
        {
            if (context.PlayerCarry == null || !context.PlayerCarry.IsCarrying)
                return;

            var itemType = context.PlayerCarry.CarriedItem.ItemType;

            if (!_hasInputA && itemType == inputA)
            {
                _hasInputA = true;
                context.PlayerCarry.TakeItem().Consume();
            }
            else if (!_hasInputB && itemType == inputB)
            {
                _hasInputB = true;
                context.PlayerCarry.TakeItem().Consume();
            }
            else
            {
                Debug.Log($"Bottler: Doesn't need {itemType} right now.");
                return;
            }

            if (_hasInputA && _hasInputB)
            {
                _hasInputA = false;
                _hasInputB = false;
                StartQtePhase();
            }
        }
    }
}
