using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Player
{

    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float detectionRadius = 0.4f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private IInteractable _currentTarget;
        private bool _interactRequested;

        public event System.Action<string> OnPromptChanged;
        public event System.Action OnPromptCleared;

        public IInteractable CurrentTarget => _currentTarget;

        private void Update()
        {
            DetectInteractable();
            ProcessInteraction();
        }

        public void TriggerInteraction()
        {
            _interactRequested = true;
        }

        private void DetectInteractable()
        {
            IInteractable best = null;
            float bestDist = float.MaxValue;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                if (Physics.SphereCast(ray, detectionRadius, out RaycastHit hit,
                    interactionRange * 2f, interactionLayers, QueryTriggerInteraction.Collide))
                {
                    var interactable = hit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null && interactable.CanInteract)
                    {
                        float dist = Vector3.Distance(transform.position, hit.collider.transform.position);
                        if (dist <= interactionRange)
                        {
                            best = interactable;
                            bestDist = dist;
                        }
                    }
                }
            }

            if (best == null)
            {
                Collider[] colliders = Physics.OverlapSphere(
                    transform.position + Vector3.up * 0.8f,
                    interactionRange * 0.6f,
                    interactionLayers,
                    QueryTriggerInteraction.Collide);

                foreach (var col in colliders)
                {
                    var interactable = col.GetComponentInParent<IInteractable>();
                    if (interactable == null || !interactable.CanInteract) continue;

                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = interactable;
                    }
                }
            }

            if (best != _currentTarget)
            {
                _currentTarget = best;

                if (_currentTarget != null)
                    OnPromptChanged?.Invoke(_currentTarget.InteractionPrompt);
                else
                    OnPromptCleared?.Invoke();
            }
        }

        private void ProcessInteraction()
        {
            if (!_interactRequested) return;
            _interactRequested = false;

            var carry = GetComponent<PlayerCarry>();

            if (carry != null && carry.IsCarrying)
            {

                if (IsTargetValid && !(_currentTarget is ItemBase) && _currentTarget.CanInteract)
                {
                    var ctx = new PlayerInteractionContext
                    {
                        PlayerTransform = transform,
                        PlayerCarry = carry
                    };
                    _currentTarget.Interact(ctx);
                    return;
                }

                carry.DropItem();
                return;
            }

            if (IsTargetValid && _currentTarget.CanInteract)
            {
                var ctx = new PlayerInteractionContext
                {
                    PlayerTransform = transform,
                    PlayerCarry = carry
                };
                _currentTarget.Interact(ctx);
            }
        }

        public void TriggerSecondaryInteraction()
        {
            var carry = GetComponent<PlayerCarry>();

            if (_currentTarget is LemonEmpire.Production.MachineBox box &&
                box.GetComponent<Rigidbody>() != null &&
                !box.GetComponent<Rigidbody>().isKinematic)
            {
                box.Unpack();
                return;
            }

            if (_currentTarget is LemonEmpire.Production.MachineBase machine &&
                (carry == null || !carry.IsCarrying) && machine.CanBePacked)
            {
                machine.PackToBox();
                return;
            }
        }

        private bool IsTargetValid => _currentTarget != null && (_currentTarget as UnityEngine.Object) != null;

        public string GetCurrentPrompt()
        {
            var carry = GetComponent<PlayerCarry>();

            if (carry != null && carry.IsCarrying)
            {
                if (IsTargetValid && !(_currentTarget is ItemBase) && _currentTarget.CanInteract)
                    return _currentTarget.InteractionPrompt;

                return $"E — Положить {carry.CarriedItem.DisplayName}";
            }

            if (IsTargetValid && _currentTarget.CanInteract)
                return _currentTarget.InteractionPrompt;

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, interactionRange * 0.6f);
        }
    }
}
