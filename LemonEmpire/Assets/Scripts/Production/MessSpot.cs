using System.Collections;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class MessSpot : MonoBehaviour, IInteractable
    {
        private bool _isCleaning;
        private float _cleaningTimer;
        private ProgressUI _progressUI;
        private Transform _cleaningPlayer;

        public string InteractionPrompt
        {
            get
            {
                if (_isCleaning)
                    return "Уборка...";

                var carry = FindFirstObjectByType<PlayerCarry>();
                if (carry != null && carry.IsCarrying && carry.CarriedItem.GetComponent<BroomTool>() != null)
                {
                    return "[E] Вытереть грязь шваброй";
                }
                return "Пятно грязи (нужна швабра)";
            }
        }

        public bool CanInteract
        {
            get
            {
                if (_isCleaning) return false;
                var carry = FindFirstObjectByType<PlayerCarry>();
                return carry != null && carry.IsCarrying && carry.CarriedItem.GetComponent<BroomTool>() != null;
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (_isCleaning) return;

            var broom = GetCarriedBroom(context);
            if (broom != null)
            {
                StartCleaning(context.PlayerTransform);
            }
        }

        private BroomTool GetCarriedBroom(PlayerInteractionContext context)
        {
            if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying)
            {
                return context.PlayerCarry.CarriedItem.GetComponent<BroomTool>();
            }
            return null;
        }

        private void StartCleaning(Transform playerTransform)
        {
            _isCleaning = true;
            _cleaningPlayer = playerTransform;
            _cleaningTimer = 0f;

            if (_progressUI == null)
            {
                var uiGo = new GameObject("ProgressUI");
                uiGo.transform.SetParent(transform);
                uiGo.transform.localPosition = Vector3.up * 0.5f;
                _progressUI = uiGo.AddComponent<ProgressUI>();
            }

            _progressUI.Show();
            StartCoroutine(CleaningCoroutine());
        }

        private IEnumerator CleaningCoroutine()
        {
            float duration = 2.0f;
            if (LemonEmpire.Player.PlayerStatusEffects.Instance != null && LemonEmpire.Player.PlayerStatusEffects.Instance.IsSanitationPanicActive)
            {
                duration = 0f;
            }
            while (_cleaningTimer < duration)
            {
                // Range check - if player walks away, cancel
                if (_cleaningPlayer == null || Vector3.Distance(_cleaningPlayer.position, transform.position) > 3.0f)
                {
                    CancelCleaning();
                    yield break;
                }

                _cleaningTimer += Time.deltaTime;
                if (_progressUI != null)
                {
                    _progressUI.UpdateProgress(_cleaningTimer / duration);
                }
                yield return null;
            }

            CompleteCleaning();
        }

        private void CancelCleaning()
        {
            _isCleaning = false;
            _cleaningPlayer = null;
            if (_progressUI != null) _progressUI.Hide();
            Debug.Log("[MessSpot] Cleaning cancelled - player moved too far");
        }

        private void CompleteCleaning()
        {
            _isCleaning = false;
            if (_progressUI != null) _progressUI.Hide();

            if (ShopCleanlinessManager.Instance != null)
            {
                ShopCleanlinessManager.Instance.IncreaseCleanliness(15f);
            }

            Debug.Log("[MessSpot] Mud spot cleaned!");
            Destroy(gameObject);
        }
    }
}
