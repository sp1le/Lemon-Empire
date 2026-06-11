using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class InteractiveDoor : MonoBehaviour, IInteractable
    {
        [Header("Door References")]
        [SerializeField] private Transform hinge;

        [Header("Door Settings")]
        [SerializeField] private float openAngle = -90f; // -90 for opening outwards/inwards depending on setup
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private string displayName = "Дверь";
        [SerializeField] private bool requiresUpgrade = false;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Quaternion targetRotation;
        private UnityEngine.AI.NavMeshObstacle navObstacle;

        public string InteractionPrompt
        {
            get
            {
                if (requiresUpgrade && !UpgradeManager.IsLeftHallUnlocked)
                {
                    return $"[Закрыто] Требуется {displayName.ToLower()} (купите расширение зала)";
                }
                return isOpen ? $"[E] Закрыть {displayName.ToLower()}" : $"[E] Открыть {displayName.ToLower()}";
            }
        }

        public bool CanInteract
        {
            get
            {
                if (requiresUpgrade && !UpgradeManager.IsLeftHallUnlocked)
                {
                    return false;
                }
                return true;
            }
        }

        private void Start()
        {
            if (hinge == null)
            {
                hinge = transform.Find("Hinge");
            }

            if (hinge != null)
            {
                closedRotation = hinge.localRotation;
                openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
                targetRotation = isOpen ? openRotation : closedRotation;
            }
            else
            {
                Debug.LogWarning($"InteractiveDoor ({name}): Hinge child transform not found!");
            }

            navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (navObstacle == null) navObstacle = GetComponentInChildren<UnityEngine.AI.NavMeshObstacle>();
            UpdateObstacle();
        }

        private void Update()
        {
            if (hinge != null)
            {
                hinge.localRotation = Quaternion.Slerp(hinge.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
        }

        private void UpdateObstacle()
        {
            if (navObstacle != null)
            {
                bool anyOpen = false;
                var doors = GetComponents<InteractiveDoor>();
                foreach (var d in doors)
                {
                    if (d.isOpen) anyOpen = true;
                }
                navObstacle.enabled = !anyOpen;
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (hinge == null) return;

            isOpen = !isOpen;
            targetRotation = isOpen ? openRotation : closedRotation;
            UpdateObstacle();
        }
    }
}
