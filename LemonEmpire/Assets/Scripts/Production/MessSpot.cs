using System.Collections;
using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.Production
{
    public class MessSpot : MonoBehaviour, IInteractable
    {
        private bool _isCleaning;
        private ProgressUI _progressUI;
        private Transform _cleaningPlayer;
        private float _cleaningProgress = 0f;
        private char _nextRequiredKey = 'A';

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

        private BroomTool _carriedBroom;
        private GameObject _visualBroomGo;

        public void Interact(PlayerInteractionContext context)
        {
            if (_isCleaning) return;

            var broom = GetCarriedBroom(context);
            if (broom != null)
            {
                StartCleaning(context.PlayerTransform, broom);
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

        private void StartCleaning(Transform playerTransform, BroomTool carriedBroom)
        {
            _isCleaning = true;
            _cleaningPlayer = playerTransform;
            _carriedBroom = carriedBroom;
            _cleaningProgress = 0f;
            _nextRequiredKey = 'A';

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = true;
            }

            // Hide carried broom
            if (_carriedBroom != null)
            {
                _carriedBroom.gameObject.SetActive(false);
            }

            // Spawn visual dummy broom hovering above the dirt
            Vector3 basePos = transform.position + Vector3.up * 0.05f;
            var visualBroom = BroomTool.SpawnBroom(basePos);
            _visualBroomGo = visualBroom.gameObject;

            // Strip functional components
            var rb = _visualBroomGo.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            var col = _visualBroomGo.GetComponent<BoxCollider>();
            if (col != null) Destroy(col);
            var script = _visualBroomGo.GetComponent<BroomTool>();
            if (script != null) Destroy(script);

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
            if (_progressUI != null)
            {
                _progressUI.UpdateProgress(0f);
                _progressUI.SetLabelText("Жми [A]!");
            }

            // Setup initial position (slid to the right, waiting for 'A' key)
            Vector3 basePos = transform.position + Vector3.up * 0.05f;
            Vector3 targetPos = basePos + _cleaningPlayer.right * 0.35f;
            float tiltAngle = -20f;
            Quaternion playerRot = Quaternion.LookRotation(_cleaningPlayer.forward, Vector3.up);
            Quaternion targetRot = playerRot * Quaternion.Euler(0f, 0f, tiltAngle);

            if (_visualBroomGo != null)
            {
                _visualBroomGo.transform.position = targetPos;
                _visualBroomGo.transform.rotation = targetRot;
            }

            bool instantClean = false;
            if (LemonEmpire.Player.PlayerStatusEffects.Instance != null && LemonEmpire.Player.PlayerStatusEffects.Instance.IsSanitationPanicActive)
            {
                instantClean = true;
            }

            if (instantClean)
            {
                _cleaningProgress = 1.0f;
                if (_progressUI != null)
                {
                    _progressUI.UpdateProgress(1.0f);
                    _progressUI.SetLabelText("Готово!");
                }
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                while (_cleaningProgress < 1.0f)
                {
                    // Safety check
                    if (_cleaningPlayer == null)
                    {
                        CancelCleaning();
                        yield break;
                    }

                    // Smoothly animate the dummy broom towards target
                    if (_visualBroomGo != null)
                    {
                        _visualBroomGo.transform.position = Vector3.Lerp(_visualBroomGo.transform.position, targetPos, Time.deltaTime * 12f);
                        _visualBroomGo.transform.rotation = Quaternion.Slerp(_visualBroomGo.transform.rotation, targetRot, Time.deltaTime * 12f);
                    }

                    if (UnityEngine.InputSystem.Keyboard.current != null)
                    {
                        if (_nextRequiredKey == 'A' && UnityEngine.InputSystem.Keyboard.current.aKey.wasPressedThisFrame)
                        {
                            _cleaningProgress += 0.2f;
                            _nextRequiredKey = 'D';
                            
                            // Sweep left!
                            targetPos = basePos - _cleaningPlayer.right * 0.35f;
                            tiltAngle = 20f;
                            playerRot = Quaternion.LookRotation(_cleaningPlayer.forward, Vector3.up);
                            targetRot = playerRot * Quaternion.Euler(0f, 0f, tiltAngle);

                            if (_progressUI != null)
                            {
                                _progressUI.UpdateProgress(_cleaningProgress);
                                _progressUI.SetLabelText(_cleaningProgress < 1.0f ? "Жми [D]!" : "Готово!");
                            }
                        }
                        else if (_nextRequiredKey == 'D' && UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame)
                        {
                            _cleaningProgress += 0.2f;
                            _nextRequiredKey = 'A';
                            
                            // Sweep right!
                            targetPos = basePos + _cleaningPlayer.right * 0.35f;
                            tiltAngle = -20f;
                            playerRot = Quaternion.LookRotation(_cleaningPlayer.forward, Vector3.up);
                            targetRot = playerRot * Quaternion.Euler(0f, 0f, tiltAngle);

                            if (_progressUI != null)
                            {
                                _progressUI.UpdateProgress(_cleaningProgress);
                                _progressUI.SetLabelText(_cleaningProgress < 1.0f ? "Жми [A]!" : "Готово!");
                            }
                        }
                    }

                    yield return null;
                }
            }

            CompleteCleaning();
        }

        private void CancelCleaning()
        {
            _isCleaning = false;
            _cleaningPlayer = null;
            
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = false;
            }

            // Restore carried broom
            if (_carriedBroom != null)
            {
                _carriedBroom.gameObject.SetActive(true);
                _carriedBroom = null;
            }

            // Cleanup dummy visual
            if (_visualBroomGo != null)
            {
                Destroy(_visualBroomGo);
                _visualBroomGo = null;
            }

            if (_progressUI != null) _progressUI.Hide();
            Debug.Log("[MessSpot] Cleaning cancelled");
        }

        private void CompleteCleaning()
        {
            _isCleaning = false;
            
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = false;
            }

            // Restore carried broom
            if (_carriedBroom != null)
            {
                _carriedBroom.gameObject.SetActive(true);
                _carriedBroom = null;
            }

            // Cleanup dummy visual
            if (_visualBroomGo != null)
            {
                Destroy(_visualBroomGo);
                _visualBroomGo = null;
            }

            if (_progressUI != null) _progressUI.Hide();

            if (ShopCleanlinessManager.Instance != null)
            {
                ShopCleanlinessManager.Instance.IncreaseCleanliness(15f);
            }

            Debug.Log("[MessSpot] Mud spot cleaned!");
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_isCleaning && PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = false;
            }

            if (_carriedBroom != null)
            {
                _carriedBroom.gameObject.SetActive(true);
            }

            if (_visualBroomGo != null)
            {
                Destroy(_visualBroomGo);
            }
        }
    }
}
