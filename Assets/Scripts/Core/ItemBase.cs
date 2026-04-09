using UnityEngine;
using LemonEmpire.Player;

namespace LemonEmpire.Core
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ItemBase : MonoBehaviour, IInteractable
    {
        [Header("Item Settings")]
        [SerializeField] private ItemType itemType;
        [SerializeField] private string displayName = "Предмет";

        [Header("Quality (set by machines)")]
        [SerializeField] private float quality = -1f;

        [Header("Capacity")]
        [SerializeField] private int amount = 1;

        private Rigidbody _rb;
        private Collider _collider;
        private bool _isBeingCarried;

        public ItemType ItemType => itemType;
        public string DisplayName => amount > 1 ? $"{displayName} ({amount} шт)" : displayName;
        public bool IsBeingCarried => _isBeingCarried;

        public int Amount
        {
            get => amount;
            set => amount = value;
        }

        public float Quality
        {
            get => quality;
            set => quality = Mathf.Clamp(value, 0f, 100f);
        }

        public virtual string InteractionPrompt => _isBeingCarried ? "" : $"E — Взять {displayName}";
        public virtual bool CanInteract => !_isBeingCarried;

        public void Interact(PlayerInteractionContext context)
        {

            var carry = context.PlayerTransform.GetComponent<PlayerCarry>();
            if (carry != null)
            {
                carry.TryPickup(this);
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        public void OnPickedUp(Transform holdPoint)
        {
            _isBeingCarried = true;

            _rb.isKinematic = true;
            _collider.enabled = false;

            transform.SetParent(holdPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnDropped(Vector3 dropPosition)
        {
            _isBeingCarried = false;

            transform.SetParent(null);
            transform.position = dropPosition;

            _collider.enabled = true;
            _rb.isKinematic = false;
        }

        public void Consume()
        {
            Destroy(gameObject);
        }

        public void Setup(ItemType type, string name, float itemQuality = -1f, int itemAmount = 1)
        {
            itemType = type;
            displayName = name;
            quality = itemQuality;
            amount = itemAmount;
        }
    }
}
