using UnityEngine;

namespace LemonEmpire.Player
{
    public enum JumpState { Grounded, Rising, Falling }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float speedChangeRate = 10f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15f;

        [Header("Camera Target")]
        [Tooltip("Auto-created at head height if empty.")]
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraTargetHeight = 1.6f;

        private CharacterController _controller;
        private Animator _animator;
        
        private float _currentSpeed;
        private float _verticalVelocity;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private bool _jumpRequested;
        private JumpState _jumpState = JumpState.Grounded;
        
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;

        public bool IsGrounded => _jumpState == JumpState.Grounded;
        public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
        public float CurrentSpeed => _currentSpeed;
        public Transform CameraTarget => cameraTarget;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("IsGrounded");
            _animIDJump = Animator.StringToHash("Jump");

            EnsureCameraTarget();
        }

        private void Update()
        {
            UpdateJumpState();
            ApplyGravity();
            Move();

            if (cameraTarget != null)
                cameraTarget.localPosition = new Vector3(0f, cameraTargetHeight, 0f);
        }

        #region Input Setters

        public void SetMoveInput(Vector2 input) => _moveInput = input;
        public void SetSprintInput(bool sprinting) => _isSprinting = sprinting;
        public void TriggerJump()
        {
            if (_jumpState == JumpState.Grounded)
                _jumpRequested = true;
        }

        #endregion

        private void EnsureCameraTarget()
        {
            if (cameraTargetHeight < 0.1f)
                cameraTargetHeight = 1.6f;

            if (cameraTarget == null)
            {
                var existing = transform.Find("CameraTarget");
                if (existing != null)
                    cameraTarget = existing;
                else
                {
                    var go = new GameObject("CameraTarget");
                    go.transform.SetParent(transform);
                    cameraTarget = go.transform;
                }
            }

            cameraTarget.localPosition = new Vector3(0f, cameraTargetHeight, 0f);
        }

        private void UpdateJumpState()
        {
            bool controllerGrounded = _controller.isGrounded;

            switch (_jumpState)
            {
                case JumpState.Grounded:
                    if (_animator != null)
                        _animator.SetBool(_animIDGrounded, true);
                    
                    if (_verticalVelocity < 0f)
                        _verticalVelocity = -2f;
                    
                    if (_jumpRequested)
                    {
                        _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                        _jumpRequested = false;
                        _jumpState = JumpState.Rising;
                        
                        if (_animator != null)
                        {
                            _animator.SetBool(_animIDGrounded, false);
                            _animator.SetTrigger(_animIDJump);
                        }
                    }
                    break;

                case JumpState.Rising:
                    if (_verticalVelocity <= 0f)
                        _jumpState = JumpState.Falling;
                    break;

                case JumpState.Falling:
                    if (controllerGrounded && _verticalVelocity <= 0f)
                    {
                        _jumpState = JumpState.Grounded;
                        _jumpRequested = false;
                    }
                    
                    if (_animator != null)
                        _animator.SetBool(_animIDGrounded, controllerGrounded && _verticalVelocity <= 0f);
                    break;
            }
        }

        private void ApplyGravity()
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        private void Move()
        {
            float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;

            // Apply hunger speed penalty
            var vitals = LemonEmpire.Core.PlayerVitals.Instance;
            if (vitals != null)
                targetSpeed *= vitals.SpeedMultiplier;

            if (_moveInput.sqrMagnitude < 0.01f)
                targetSpeed = 0f;

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
            if (_currentSpeed < 0.01f) _currentSpeed = 0f;
            
            if (_animator != null)
                _animator.SetFloat(_animIDSpeed, _currentSpeed);

            Camera cam = Camera.main;
            if (cam == null) return;

            float camYaw = cam.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, camYaw, 0f);

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 camForward = cam.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cam.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

                Vector3 movement = moveDir * (_currentSpeed * Time.deltaTime);
                movement.y = _verticalVelocity * Time.deltaTime;
                _controller.Move(movement);
            }
            else
            {
                _controller.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));
            }
        }
    }
}
