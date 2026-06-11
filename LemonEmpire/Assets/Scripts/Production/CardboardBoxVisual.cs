using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    [RequireComponent(typeof(ItemBase))]
    public class CardboardBoxVisual : MonoBehaviour
    {
        [Header("Flap Hinges")]
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private Transform frontHinge;
        [SerializeField] private Transform backHinge;

        [Header("Bottles Slide")]
        [SerializeField] private Transform bottlesContainer;
        [SerializeField] private float closedBottlesY = -0.12f;
        [SerializeField] private float openBottlesY = 0.02f;

        [Header("Smooth Speed")]
        [SerializeField] private float openSpeed = 6f;

        private ItemBase _itemBase;
        private float _currentOpenFactor = 0f; // 0 = closed, 1 = open

        private void Awake()
        {
            _itemBase = GetComponent<ItemBase>();
        }

        private void Start()
        {
            if (_itemBase == null) _itemBase = GetComponent<ItemBase>();
            if (_itemBase != null)
            {
                _currentOpenFactor = _itemBase.IsBeingCarried ? 1f : 0f;
                ApplyVisuals(_currentOpenFactor);
            }
        }

        private void Update()
        {
            if (_itemBase == null) return;

            // Target state: open when carried, closed when on ground/shelves
            float targetFactor = _itemBase.IsBeingCarried ? 1f : 0f;
            
            if (Mathf.Abs(_currentOpenFactor - targetFactor) > 0.001f)
            {
                _currentOpenFactor = Mathf.MoveTowards(_currentOpenFactor, targetFactor, Time.deltaTime * openSpeed);
                ApplyVisuals(_currentOpenFactor);
            }
        }

        private void ApplyVisuals(float factor)
        {
            // Apply rotations:
            // Closed (0): Left Z = -90, Right Z = 90, Front X = 90, Back X = -90
            // Open (1): Left Z = 45, Right Z = -45, Front X = -45, Back X = 45
            
            if (leftHinge != null)
                leftHinge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-90f, 45f, factor));
            if (rightHinge != null)
                rightHinge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(90f, -45f, factor));
            if (frontHinge != null)
                frontHinge.localRotation = Quaternion.Euler(Mathf.Lerp(90f, -45f, factor), 0f, 0f);
            if (backHinge != null)
                backHinge.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, 45f, factor), 0f, 0f);

            // Apply bottles slide
            if (bottlesContainer != null)
            {
                Vector3 pos = bottlesContainer.localPosition;
                pos.y = Mathf.Lerp(closedBottlesY, openBottlesY, factor);
                bottlesContainer.localPosition = pos;
            }
        }
    }
}
