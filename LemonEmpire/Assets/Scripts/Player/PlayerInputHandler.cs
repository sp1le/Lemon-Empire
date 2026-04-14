using UnityEngine;
using UnityEngine.InputSystem;

namespace LemonEmpire.Player
{

    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ThirdPersonCamera thirdPersonCamera;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private LemonEmpire.UI.TabletUI tabletUI;

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

        private bool IsInputBlocked => Cursor.visible;

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
            if (IsInputBlocked) return;
            playerInteraction?.TriggerInteraction();
        }

        private void OnSecondaryInteract(InputValue value)
        {
            if (IsInputBlocked) return;
            playerInteraction?.TriggerSecondaryInteraction();
        }

        private void OnJump(InputValue value)
        {
            if (IsInputBlocked) return;
            playerController?.TriggerJump();
        }

        private void OnScrollZoom(InputValue value)
        {
            if (IsInputBlocked) return;
            Vector2 scroll = value.Get<Vector2>();
            if (Mathf.Abs(scroll.y) > 0.01f)
                thirdPersonCamera?.SetScrollInput(scroll.y);
        }

        private void OnTablet(InputValue value)
        {
            tabletUI?.Toggle();
        }

        private void OnPriceUp(InputValue value)
        {
            if (IsInputBlocked) return;
            var stand = playerInteraction?.CurrentTarget as LemonEmpire.Trading.TradeStand;
            stand?.AdjustPrice(1);
        }

        private void OnPriceDown(InputValue value)
        {
            if (IsInputBlocked) return;
            var stand = playerInteraction?.CurrentTarget as LemonEmpire.Trading.TradeStand;
            stand?.AdjustPrice(-1);
        }
    }
}
