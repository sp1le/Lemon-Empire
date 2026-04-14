using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Player
{

    public class PlayerCarry : MonoBehaviour
    {
        [Header("Carry Settings")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private float holdHeight = 1.0f;
        [SerializeField] private float holdForward = 0.7f;
        [SerializeField] private float dropDistance = 1.5f;
        [SerializeField] private float dropHeight = 0.5f;

        private ItemBase _carriedItem;

        public bool IsCarrying => _carriedItem != null;
        public ItemBase CarriedItem => _carriedItem;

        private void Awake()
        {
            EnsureHoldPoint();
        }

        private void EnsureHoldPoint()
        {
            if (holdPoint != null) return;

            var go = new GameObject("HoldPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, holdHeight, holdForward);
            holdPoint = go.transform;
        }

        public bool TryPickup(ItemBase item)
        {
            if (_carriedItem != null)
            {

                return false;
            }

            _carriedItem = item;
            _carriedItem.OnPickedUp(holdPoint);
            return true;
        }

        public void DropItem()
        {
            if (_carriedItem == null) return;

            _carriedItem.OnDropped(holdPoint.position);
            _carriedItem = null;
        }

        public ItemBase TakeItem()
        {
            if (_carriedItem == null) return null;

            var item = _carriedItem;
            _carriedItem = null;
            return item;
        }
    }
}
