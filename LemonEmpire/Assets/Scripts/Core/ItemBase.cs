using UnityEngine;
using LemonEmpire.Player;

namespace LemonEmpire.Core
{

    public enum PackagingType
    {
        Plastic,
        Can,
        Glass
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ItemBase : MonoBehaviour, IInteractable
    {
        [Header("Item Settings")]
        [SerializeField] private ItemType itemType;
        [SerializeField] private string displayName = "Предмет";

        [Header("Quality (set by machines)")]
        [SerializeField] private float quality = 50f;

        [Header("Capacity")]
        [SerializeField] private int amount = 1;

        [Header("Custom Drink Stats")]
        [SerializeField] private string drinkName;
        [SerializeField] private float sugar;
        [SerializeField] private float carbonation;
        [SerializeField] private float alcohol;
        [SerializeField] private float temperature = 20f;
        [SerializeField] private float retailPrice = 15f;
        [SerializeField] private PackagingType packaging = PackagingType.Plastic;

        private Rigidbody _rb;
        private Collider _collider;
        private bool _isBeingCarried;

        public ItemType ItemType => itemType;
        public string DisplayName
        {
            get
            {
                if (IsCrate && !string.IsNullOrEmpty(drinkName))
                {
                    return amount > 1 ? $"Коробка с \"{drinkName}\" ({amount} шт)" : $"Коробка с \"{drinkName}\"";
                }
                return amount > 1 ? $"{displayName} ({amount} шт)" : displayName;
            }
        }
        public bool IsBeingCarried => _isBeingCarried;
        
        private Vector3 _originalScale = Vector3.one;
        public Vector3 OriginalScale
        {
            get => _originalScale;
            set => _originalScale = value;
        }

        public int Amount
        {
            get => amount;
            set
            {
                amount = value;
                UpdateCrateVisuals();
            }
        }

        public bool IsCrate => itemType == ItemType.BottlePack || (itemType == ItemType.BottledLemonade && (transform.Find("CardboardBox") != null || transform.Find("Bottles") != null));

        public void UpdateCrateVisuals()
        {
            if (!IsCrate) return;

            var bottleChildren = new System.Collections.Generic.List<Transform>();
            
            // Check direct children starting with "Bottle" but not "Bottles" or the object itself
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Bottle") && child.name != name && child.name != "Bottles")
                {
                    bottleChildren.Add(child);
                }
            }

            // If none found, look inside a child named "Bottles"
            if (bottleChildren.Count == 0)
            {
                var bottlesParent = transform.Find("Bottles");
                if (bottlesParent != null)
                {
                    foreach (Transform child in bottlesParent)
                    {
                        if (child.name.StartsWith("Bottle"))
                        {
                            bottleChildren.Add(child);
                        }
                    }
                }
            }

            // If still no children exist (like in the EmptyCrate prefab), spawn 6 bottle slot positions dynamically!
            if (bottleChildren.Count == 0)
            {
                var bottlePrefab = Resources.Load<GameObject>("LemonadeBottle");
                if (bottlePrefab != null)
                {
                    Vector3[] localPositions = new Vector3[]
                    {
                        new Vector3(-0.19f, 0.015f, -0.22f),
                        new Vector3(-0.19f, 0.015f, -0.075f),
                        new Vector3(-0.19f, 0.015f, 0.075f),
                        new Vector3(-0.19f, 0.015f, 0.22f),
                        new Vector3(0f,     0.015f, -0.22f),
                        new Vector3(0f,     0.015f, -0.075f),
                        new Vector3(0f,     0.015f, 0.075f),
                        new Vector3(0f,     0.015f, 0.22f),
                        new Vector3(0.19f,  0.015f, -0.22f),
                        new Vector3(0.19f,  0.015f, -0.075f),
                        new Vector3(0.19f,  0.015f, 0.075f),
                        new Vector3(0.19f,  0.015f, 0.22f)
                    };

                    for (int i = 0; i < localPositions.Length; i++)
                    {
                        var bottleGo = Instantiate(bottlePrefab, transform);
                        bottleGo.name = $"Bottle_{i}";
                        bottleGo.transform.localPosition = localPositions[i];
                        bottleGo.transform.localRotation = Quaternion.identity;
                        bottleGo.transform.localScale = Vector3.one;

                        var col = bottleGo.GetComponent<Collider>();
                        if (col != null) col.enabled = false;
                        var rb = bottleGo.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = true;

                        bottleChildren.Add(bottleGo.transform);
                    }
                }
            }

            // Show only first 'amount' bottles, hide the rest
            for (int i = 0; i < bottleChildren.Count; i++)
            {
                if (bottleChildren[i] != null)
                {
                    bottleChildren[i].gameObject.SetActive(i < amount);
                }
            }
        }

        public float Quality
        {
            get => quality;
            set => quality = Mathf.Clamp(value, 0f, 100f);
        }

        public string DrinkName => drinkName;
        public float Sugar => sugar;
        public float Carbonation => carbonation;
        public float Alcohol => alcohol;
        public float Temperature
        {
            get => temperature;
            set => temperature = value;
        }
        public float RetailPrice
        {
            get => retailPrice;
            set => retailPrice = value;
        }
        public PackagingType Packaging => packaging;

        public virtual string InteractionPrompt => _isBeingCarried ? "" : $"E — Взять {DisplayName}";
        public virtual bool CanInteract => !_isBeingCarried;

        public void Interact(PlayerInteractionContext context)
        {

            var carry = context.PlayerTransform.GetComponent<PlayerCarry>();
            if (carry != null)
            {
                carry.TryPickup(this);
            }
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _originalScale = transform.localScale;
        }

        protected virtual void Start()
        {
            UpdateCrateVisuals();
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

        public void OnPlaced(Transform newParent, Vector3 newPosition, Quaternion newRotation)
        {
            _isBeingCarried = false;

            transform.SetParent(newParent);
            transform.position = newPosition;
            transform.rotation = newRotation;
            transform.localScale = OriginalScale; // Force original scale

            _collider.enabled = true;
            _rb.isKinematic = true;
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
            UpdateCrateVisuals();
        }

        public void SetupDrink(string name, float sug, float carb, float alc, PackagingType pkg, int itemAmount = 1)
        {
            itemType = ItemType.BottledLemonade;
            displayName = name;
            drinkName = name;
            sugar = Mathf.Clamp(sug, 0f, 100f);
            carbonation = Mathf.Clamp(carb, 0f, 100f);
            alcohol = Mathf.Clamp(alc, 0f, 100f);
            packaging = pkg;
            amount = itemAmount;
            quality = 100f; // Custom crafted drink starts at max quality
            temperature = 20f;
            retailPrice = 15f;
            UpdateCrateVisuals();
        }
    }
}
