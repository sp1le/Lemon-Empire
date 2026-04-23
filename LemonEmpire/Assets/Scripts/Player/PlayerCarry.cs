using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Player
{

    public class PlayerCarry : MonoBehaviour
    {
        [Header("Carry Settings")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private float holdDistance = 0.6f;
        [SerializeField] private float holdDown = 0.4f;
        [SerializeField] private float dropDistance = 1.5f;
        [SerializeField] private float dropHeight = 0.5f;
        [SerializeField] private float carryScale = 0.6f;

        [Header("Camera Follow")]
        [SerializeField] private float followSmooth = 8f;

        private ItemBase _carriedItem;
        private Vector3 _originalScale;
        private Vector3 _smoothVelocity;

        public bool IsCarrying => _carriedItem != null;
        public ItemBase CarriedItem => _carriedItem;

        private void Awake()
        {
            EnsureHoldPoint();
        }

        private void LateUpdate()
        {
            if (_carriedItem == null || holdPoint == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 targetPos = cam.transform.position
                + cam.transform.forward * holdDistance
                + cam.transform.up * -holdDown;

            holdPoint.position = Vector3.SmoothDamp(
                holdPoint.position, targetPos, ref _smoothVelocity,
                1f / followSmooth);

            holdPoint.rotation = Quaternion.Slerp(
                holdPoint.rotation, cam.transform.rotation,
                Time.deltaTime * followSmooth);
        }

        private void EnsureHoldPoint()
        {
            if (holdPoint != null) return;

            var go = new GameObject("HoldPoint");
            go.transform.SetParent(null);
            holdPoint = go.transform;
        }

        public bool TryPickup(ItemBase item)
        {
            if (_carriedItem != null)
                return false;

            _carriedItem = item;
            _originalScale = _carriedItem.transform.localScale;
            _carriedItem.OnPickedUp(holdPoint);
            _carriedItem.transform.localScale = _originalScale * carryScale;

            _smoothVelocity = Vector3.zero;
            return true;
        }

        public void DropItem()
        {
            if (_carriedItem == null) return;

            _carriedItem.transform.localScale = _originalScale;
            _carriedItem.OnDropped(transform.position + transform.forward * dropDistance + Vector3.up * dropHeight);
            _carriedItem = null;
        }

        public ItemBase TakeItem()
        {
            if (_carriedItem == null) return null;

            var item = _carriedItem;
            item.transform.localScale = _originalScale;
            _carriedItem = null;
            return item;
        }
    }
}
