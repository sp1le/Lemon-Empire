using UnityEngine;

namespace LemonEmpire.Player
{

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float rotationSmoothTime = 0.12f;
        [SerializeField] private float speedChangeRate = 10f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float groundCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Camera Target")]
        [Tooltip("Point where the camera looks at. Auto-created if empty.")]
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraTargetHeight = 1.5f;

        private CharacterController _controller;
        private Animator _animator;
        
        private float _currentSpeed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private bool _isGrounded;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private bool _jumpRequested;
        
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;

        public bool IsGrounded => _isGrounded;
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
            GroundCheck();
            ApplyGravity();
            Move();
        }

        #region Input Setters

        public void SetMoveInput(Vector2 input) => _moveInput = input;
        public void SetSprintInput(bool sprinting) => _isSprinting = sprinting;
        public void TriggerJump() => _jumpRequested = true;

        #endregion

        private void EnsureCameraTarget()
        {
            if (cameraTarget != null) return;

            var go = new GameObject("CameraTarget");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, cameraTargetHeight, 0f);
            cameraTarget = go.transform;
        }

        private void GroundCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            _isGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
            
            if (_animator != null)
                _animator.SetBool(_animIDGrounded, _isGrounded);
        }

        private void ApplyGravity()
        {
            if (_isGrounded)
            {
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_jumpRequested)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    _jumpRequested = false;
                    
                    if (_animator != null)
                        _animator.SetTrigger(_animIDJump);
                }
            }
            
            _verticalVelocity += gravity * Time.deltaTime;
        }

        private void Move()
        {
            float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;

            if (_moveInput.sqrMagnitude < 0.01f)
                targetSpeed = 0f;

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
            if (_currentSpeed < 0.01f) _currentSpeed = 0f;
            
            if (_animator != null)
                _animator.SetFloat(_animIDSpeed, _currentSpeed);

            if (_moveInput.sqrMagnitude > 0.01f)
            {

                Camera cam = Camera.main;
                if (cam == null) return;

                Vector3 camForward = cam.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cam.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

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
