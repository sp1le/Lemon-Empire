using UnityEngine;
using UnityEngine.Rendering;

namespace LemonEmpire.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("FPS Settings")]
        [SerializeField] private Vector2 sensitivity = new Vector2(0.15f, 0.15f);
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float eyeHeight = 1.6f;

        private Transform _playerTransform;
        private float _yaw, _pitch;
        private Vector2 _lookInput;

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
                _yaw = _playerTransform.eulerAngles.y;
            }
            else
            {
                Debug.LogError("[FPSCamera] No PlayerController found!");
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (_playerTransform == null) return;

            _yaw += _lookInput.x * sensitivity.x;
            _pitch -= _lookInput.y * sensitivity.y;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            Vector3 eyePos = _playerTransform.position + Vector3.up * eyeHeight;
            transform.position = eyePos;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        #region Input Setters

        public void SetLookInput(Vector2 input) => _lookInput = input;
        public void SetScrollInput(float scroll) { }

        #endregion
    }
}
