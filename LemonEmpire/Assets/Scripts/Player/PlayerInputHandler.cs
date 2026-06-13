using UnityEngine;
using UnityEngine.InputSystem;
using LemonEmpire.Core;

namespace LemonEmpire.Player
{

    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ThirdPersonCamera thirdPersonCamera;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private LemonEmpire.UI.TabletUI tabletUI;

        [Header("Price Hold Settings")]
        [SerializeField] private float holdDelay = 0.4f;
        [SerializeField] private float holdRepeatRate = 0.08f;

        private bool _priceUpHeld;
        private bool _priceDownHeld;
        private float _priceHoldTimer;
        private bool _priceHoldActive;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (playerInteraction == null)
                playerInteraction = GetComponent<PlayerInteraction>();
            if (thirdPersonCamera == null)
                thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
            if (tabletUI == null)
                tabletUI = FindFirstObjectByType<LemonEmpire.UI.TabletUI>();
        }

        private void Update()
        {
            if (IsInputBlocked || (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildModeActive)) return;

            // Handle drink consumption! If holding a drink or looking at one, press R to drink it
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                var carry = playerController.GetComponent<PlayerCarry>();
                
                // Case A: Player is carrying a single bottle in hand
                if (carry != null && carry.IsCarrying && carry.CarriedItem != null && carry.CarriedItem.ItemType == ItemType.BottledLemonade)
                {
                    var drink = carry.CarriedItem;
                    if (drink.IsCrate)
                    {
                        Debug.LogWarning("[PlayerInputHandler] Нельзя выпить коробку! Вытащите бутылку на полку или выпейте прямо из лежащей коробки.");
                    }
                    else
                    {
                        carry.TakeItem();
                        ConsumeSingleBottle(drink, destroyOnConsume: true);
                    }
                }
                // Case B: Player is looking at a bottle/crate lying on the ground
                else if (playerInteraction != null && playerInteraction.CurrentTarget is ItemBase targetItem && targetItem.ItemType == ItemType.BottledLemonade)
                {
                    if (targetItem.IsCrate)
                    {
                        if (targetItem.Amount > 0)
                        {
                            ConsumeSingleBottle(targetItem, destroyOnConsume: false);
                            targetItem.Amount--;
                        }
                    }
                    else
                    {
                        ConsumeSingleBottle(targetItem, destroyOnConsume: true);
                    }
                }
                // Case C: Player is looking at a service counter that has a drink placed on it
                else if (playerInteraction != null && playerInteraction.CurrentTarget is LemonEmpire.Production.ServiceCounter counter && counter.PlacedDrink != null && counter.PlacedDrink.ItemType == ItemType.BottledLemonade && !counter.PlacedDrink.IsCrate)
                {
                    var placedItem = counter.PlacedDrink;
                    counter.ClearPlacedDrink();
                    ConsumeSingleBottle(placedItem, destroyOnConsume: true);
                }
            }

            if (_priceUpHeld || _priceDownHeld)
            {
                _priceHoldTimer -= Time.unscaledDeltaTime;
                if (_priceHoldTimer <= 0f)
                {
                    _priceHoldTimer = _priceHoldActive ? holdRepeatRate : holdDelay;
                    _priceHoldActive = true;

                    int dir = _priceUpHeld ? 1 : -1;
                    var stand = playerInteraction?.CurrentTarget as LemonEmpire.Trading.TradeStand;
                    stand?.AdjustPrice(dir);
                }
            }
        }

        private bool IsInputBlocked => Cursor.visible;
        private bool IsGameplayBlocked => IsInputBlocked || (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildModeActive);

        private void OnMove(InputValue value)
        {
            if (IsInputBlocked) { playerController?.SetMoveInput(Vector2.zero); return; }
            playerController?.SetMoveInput(value.Get<Vector2>());
        }

        private void OnLook(InputValue value)
        {
            if (IsInputBlocked) { thirdPersonCamera?.SetLookInput(Vector2.zero); return; }
            thirdPersonCamera?.SetLookInput(value.Get<Vector2>());
        }

        private void OnSprint(InputValue value)
        {
            if (IsInputBlocked) { playerController?.SetSprintInput(false); return; }
            playerController?.SetSprintInput(value.isPressed);
        }

        private void OnInteract(InputValue value)
        {
            if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildModeActive)
            {
                if (value.isPressed)
                    BuildModeManager.Instance.OnLeftClick();
                return;
            }
            if (IsGameplayBlocked) return;
            playerInteraction?.TriggerInteraction();
        }

        private void OnSecondaryInteract(InputValue value)
        {
            if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildModeActive)
            {
                if (value.isPressed)
                    BuildModeManager.Instance.OnRightClick();
                return;
            }
            if (IsGameplayBlocked) return;
            playerInteraction?.TriggerSecondaryInteraction();
        }

        private void OnJump(InputValue value)
        {
            if (IsGameplayBlocked) return;
            playerController?.TriggerJump();
        }

        private void OnScrollZoom(InputValue value)
        {
            if (IsGameplayBlocked) return;
            Vector2 scroll = value.Get<Vector2>();
            if (Mathf.Abs(scroll.y) > 0.01f)
                thirdPersonCamera?.SetScrollInput(scroll.y);
        }

        private void OnTablet(InputValue value)
        {
            if (tabletUI != null && tabletUI.IsOpen)
            {
                tabletUI.Close();
                return;
            }

            if (IsGameplayBlocked) return;
            tabletUI?.Toggle();
        }

        private void OnCancel(InputValue value)
        {
            if (tabletUI != null && tabletUI.IsOpen)
            {
                tabletUI.Close();
                return;
            }
        }

        private void OnPriceUp(InputValue value)
        {
            if (IsGameplayBlocked) return;
            if (value.isPressed)
            {
                _priceUpHeld = true;
                _priceHoldTimer = 0f;
                _priceHoldActive = false;
            }
            else
            {
                _priceUpHeld = false;
                _priceHoldActive = false;
            }
        }

        private void OnPriceDown(InputValue value)
        {
            if (IsGameplayBlocked) return;
            if (value.isPressed)
            {
                _priceDownHeld = true;
                _priceHoldTimer = 0f;
                _priceHoldActive = false;
            }
            else
            {
                _priceDownHeld = false;
                _priceHoldActive = false;
            }
        }

        private void ConsumeSingleBottle(ItemBase drink, bool destroyOnConsume = true)
        {
            float sugarContent = drink.Sugar;
            float carbonation = drink.Carbonation;
            float alcohol = drink.Alcohol;

            if (destroyOnConsume)
            {
                drink.Consume();
            }

            // Replenish Satiety + Morale
            if (PlayerVitals.Instance != null)
            {
                PlayerVitals.Instance.ReplenishSatiety(25f);
                PlayerVitals.Instance.ReplenishMorale(20f);
            }

            // Sugar Rush Trigger tracking - ONLY if sugar content is > 75%
            if (sugarContent > 75f)
            {
                if (PlayerStatusEffects.Instance != null)
                {
                    PlayerStatusEffects.Instance.ConsumeSweetSoda();
                }
                Debug.Log($"[PlayerInputHandler] Drank sweet soda (Sugar: {sugarContent:F0}%). Satiety +25, Morale +20! Progressing Sugar Rush.");
            }
            else
            {
                Debug.Log($"[PlayerInputHandler] Drank soda (Sugar: {sugarContent:F0}%). Satiety +25, Morale +20! (requires > 75% for Sugar Rush).");
            }

            // Carbonation Overload check (Gas > 75%)
            if (carbonation > 75f)
            {
                if (PlayerStatusEffects.Instance != null)
                {
                    PlayerStatusEffects.Instance.ConsumeCarbonatedSoda();
                }
                Debug.Log($"[PlayerInputHandler] Drank carbonated soda (Gas: {carbonation:F0}%). Satiety +25, Morale +20! Progressing Carbonation Overload.");
            }

            // Alcohol Intoxication check (Alcohol > 15%)
            if (alcohol > 15f)
            {
                if (PlayerStatusEffects.Instance != null)
                {
                    PlayerStatusEffects.Instance.ConsumeAlcoholicSoda();
                }
                Debug.Log($"[PlayerInputHandler] Drank alcoholic soda (Alcohol: {alcohol:F0}%). Satiety +25, Morale +20! Progressing Alcohol Intoxication.");
            }
        }
    }
}
