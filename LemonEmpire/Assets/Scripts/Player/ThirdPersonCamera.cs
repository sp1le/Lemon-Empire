using UnityEngine;

namespace LemonEmpire.Player
{

    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [SerializeField] private float distance = 6f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float zoomSpeed = 3f;
        [SerializeField] private Vector2 orbitSpeed = new Vector2(0.8f, 0.4f);
        [SerializeField] private float orbitSmoothTime = 0.08f;

        [Header("Vertical Limits")]
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Header("Initial Angles")]
        [SerializeField] private float initialYaw = 0f;
        [SerializeField] private float initialPitch = 30f;

        [Header("Fading")]
        [SerializeField] private float fadeDistance = 1.0f;

        private float _yaw, _pitch;
        private float _targetYaw, _targetPitch;
        private float _yawVelocity, _pitchVelocity;
        private float _targetDistance;
        private float _currentDistance;

        private Vector2 _lookInput;

        private Renderer[] _playerRenderers;

        private void Start()
        {
            _yaw = _targetYaw = initialYaw;
            _pitch = _targetPitch = initialPitch;
            _targetDistance = _currentDistance = distance;

            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    target = player.CameraTarget;
            }

            if (target != null)
            {
                _playerRenderers = target.root.GetComponentsInChildren<Renderer>();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateOrbit();
            UpdateZoom();
            ApplyPosition();
        }

        #region Input Setters

        public void SetLookInput(Vector2 input) => _lookInput = input;

        public void SetScrollInput(float scroll)
        {
            _targetDistance -= scroll * zoomSpeed * 0.1f;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
        }

        #endregion

        private void UpdateOrbit()
        {

            _targetYaw += _lookInput.x * orbitSpeed.x;
            _targetPitch -= _lookInput.y * orbitSpeed.y;
            _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);

            if (Mathf.Abs(_targetYaw) > 3600f)
            {
                _targetYaw %= 360f;
                _yaw %= 360f;
            }

            _yaw = Mathf.SmoothDamp(_yaw, _targetYaw, ref _yawVelocity, orbitSmoothTime);
            _pitch = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVelocity, orbitSmoothTime);
        }

        private void UpdateZoom()
        {
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * 10f);
        }

        private void ApplyPosition()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 direction = -(rotation * Vector3.forward);

            float safeDistance = _currentDistance;
            RaycastHit[] hits = Physics.SphereCastAll(target.position, collisionRadius, direction, _currentDistance, collisionLayers);

            float minSafeDist = _currentDistance;

            foreach(var hit in hits)
            {
                if (hit.collider.isTrigger) continue;
                if (hit.collider.transform.root == target.root) continue;

                if (hit.collider.GetComponentInParent<LemonEmpire.Core.ItemBase>() != null) continue;

                float hitSafeDist = Mathf.Max(hit.distance - collisionRadius, 0.5f);
                if (hitSafeDist < minSafeDist)
                {
                    minSafeDist = hitSafeDist;
                }
            }
            safeDistance = minSafeDist;

            Vector3 finalPos = target.position + direction * safeDistance;
            transform.position = finalPos;
            transform.LookAt(target.position);

            if (_playerRenderers != null && _playerRenderers.Length > 0)
            {
                bool isVisible = safeDistance > fadeDistance;
                foreach (var r in _playerRenderers)
                {
                    if (r != null) r.enabled = isVisible;
                }
            }
        }
    }
}
