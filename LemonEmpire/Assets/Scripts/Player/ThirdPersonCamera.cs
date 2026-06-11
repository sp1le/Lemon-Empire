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
        [SerializeField] private float eyeHeight = 1.4f;

        private Transform _playerTransform;
        private float _yaw, _pitch;
        private Vector2 _lookInput;
        private float _nextNullLogTime = 0f;

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
                _yaw = _playerTransform.eulerAngles.y;
                Debug.Log($"[ThirdPersonCamera] Player found in Start: {player.name} at {player.transform.position}");
            }
            else
            {
                Debug.LogWarning("[ThirdPersonCamera] No PlayerController found in Start! Will attempt dynamic resolution in LateUpdate.");
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private float _currentRoll = 0f;
        private float _currentHeightMultiplier = 1f;
        private float _hiccupKickOffset = 0f;

        public void TriggerHiccupKick()
        {
            _hiccupKickOffset = 5f; // Sudden upward pitch kick of 5 degrees
        }

        private void LateUpdate()
        {
            // Dynamic resolution from Singleton
            if (PlayerController.Instance != null)
            {
                if (_playerTransform != PlayerController.Instance.transform)
                {
                    _playerTransform = PlayerController.Instance.transform;
                    _yaw = _playerTransform.eulerAngles.y;
                    Debug.Log($"[ThirdPersonCamera] Player resolved dynamically from Instance: {PlayerController.Instance.name} at {_playerTransform.position}");
                }
            }
            else
            {
                _playerTransform = null;
            }

            if (_playerTransform == null)
            {
                if (Time.time >= _nextNullLogTime)
                {
                    Debug.LogWarning("[ThirdPersonCamera] PlayerController.Instance is null in LateUpdate!");
                    _nextNullLogTime = Time.time + 1f;
                }
                return;
            }

            _yaw += _lookInput.x * sensitivity.x;
            _pitch -= _lookInput.y * sensitivity.y;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            // Interpolate sleep roll
            float targetRoll = 0f;
            if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.IsSleeping)
            {
                targetRoll = 15f; // 15 degrees tilt during sleep
            }
            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, Time.deltaTime * 5f);

            // Interpolate slouch height multiplier
            float targetHeightMult = 1f;
            if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.IsFinancialDepressionActive)
            {
                targetHeightMult = 0.75f; // Slouch: lower camera by 25%
            }
            _currentHeightMultiplier = Mathf.Lerp(_currentHeightMultiplier, targetHeightMult, Time.deltaTime * 4f);

            // Decay hiccup kick offset smoothly
            _hiccupKickOffset = Mathf.Lerp(_hiccupKickOffset, 0f, Time.deltaTime * 6f);

            // Apply alcohol look sway
            float swayYaw = 0f;
            float swayPitch = 0f;
            if (PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.IsAlcoholIntoxicationActive)
            {
                swayYaw = Mathf.Sin(Time.time * 1.5f) * 2.5f;
                swayPitch = Mathf.Cos(Time.time * 1.1f) * 1.8f;
            }

            float finalYaw = _yaw + swayYaw;
            float finalPitch = Mathf.Clamp(_pitch - _hiccupKickOffset + swayPitch, minPitch, maxPitch);

            Vector3 playerPos = _playerTransform.position;
            Vector3 eyePos = playerPos + Vector3.up * (eyeHeight * _currentHeightMultiplier);

            if (playerPos.x == 0f && playerPos.z == 0f)
            {
                Debug.LogWarning($"[ThirdPersonCamera] Player position is (0,0,0)! Camera position set to {eyePos}. StackTrace: {System.Environment.StackTrace}");
            }

            transform.position = eyePos;
            transform.rotation = Quaternion.Euler(finalPitch, finalYaw, _currentRoll);
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        #region Input Setters

        public void SetLookInput(Vector2 input) => _lookInput = input;
        public void SetScrollInput(float scroll) { }

        #endregion
    }
}
