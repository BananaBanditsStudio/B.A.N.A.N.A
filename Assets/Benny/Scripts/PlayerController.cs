using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Needed for Slider!
using UnityTutorial.Manager;

namespace UnityTutorial.PlayerControl
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float AnimBlendSpeed = 8.9f;
        [SerializeField] private Camera playerCamera; // Assign Main Camera (Camera component!) here
        [SerializeField] private Slider staminaBar;

        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float BottomLimit = 70f;
        [SerializeField] private float MouseSensitivity = 21.9f;

        [SerializeField] private float JumpFactor = 260f;
        [SerializeField] private float Dis2Ground = 0.8f;
        [SerializeField] private float AirResistance = 0.8f;
        [SerializeField] private LayerMask GroundCheck;

        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private Animator _animator;
        private bool _grounded;
        private bool _hasAnimator;
        private bool _isCrouching;
        private bool _crouchPressed;
        private int _xVelHash, _yVelHash, _zVelHash, _jumpHash, _groundedHash, _fallingHash, _crouchHash;
        private float _xRotation;
        private const float _walkSpeed = 2f;
        private const float _runSpeed = 6f;
        private Vector2 _currentVelocity;

        // Dash & FOV
        [SerializeField] private float dashForce = 7f; // This is dash distance, not a force now!
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashFOV = 80f;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float fovChangeSpeed = 10f; // higher = snappier

        private bool isDashing = false;
        private float lastDashTime = -999f;
        private Coroutine fovCoroutine = null;

        // Stamina system
        [SerializeField] private float maxStamina = 50f;
        [SerializeField] private float dashStaminaCost = 50f;
        [SerializeField] private float staminaRegenRate = 25f;
        private float currentStamina;

        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundedHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _crouchHash = Animator.StringToHash("Crouch");

            // Stamina bar setup
            currentStamina = maxStamina;
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;

            // On start set normal FOV
            if (playerCamera != null)
                playerCamera.fieldOfView = normalFOV;
        }

        private void Update()
        {
            // Regen stamina
            if (currentStamina < maxStamina)
                currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

            // Update bar UI
            staminaBar.value = currentStamina;
            // Optional: color shows ready
            staminaBar.fillRect.GetComponent<Image>().color = currentStamina >= dashStaminaCost ? Color.green : Color.red;
        }

        private void FixedUpdate()
        {
            // Dash only if enough stamina!
            if (_inputManager.Dash && Time.time >= lastDashTime + dashCooldown && _grounded && !isDashing && currentStamina >= dashStaminaCost)
            {
                currentStamina -= dashStaminaCost;
                StartCoroutine(Dash());
            }

            SampleGround();
            Move();
            HandleJump();
            HandleCrouch();
        }

        private void LateUpdate()
        {
            CamMovements();
        }

        private void Move()
        {
            if (!_hasAnimator || isDashing) return; // Block movement if dashing

            float targetSpeed = _inputManager.Run ? _runSpeed : _walkSpeed;
            if (_isCrouching) targetSpeed = 1.5f;
            if (_inputManager.Move == Vector2.zero) targetSpeed = 0;

            if (_grounded)
            {
                _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
                _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

                var xVelDifference = _currentVelocity.x - _playerRigidbody.linearVelocity.x;
                var zVelDifference = _currentVelocity.y - _playerRigidbody.linearVelocity.z;

                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0, zVelDifference)), ForceMode.VelocityChange);
            }
            else
            {
                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(_currentVelocity.x * AirResistance, 0, _currentVelocity.y * AirResistance)), ForceMode.VelocityChange);
            }

            _animator.SetFloat(_xVelHash, _currentVelocity.x);
            _animator.SetFloat(_yVelHash, _currentVelocity.y);
        }

        private void CamMovements()
        {
            if (!_hasAnimator) return;

            var Mouse_X = _inputManager.Look.x;
            var Mouse_Y = _inputManager.Look.y;
            Camera.position = CameraRoot.position;

            _xRotation -= Mouse_Y * MouseSensitivity * Time.smoothDeltaTime;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);
            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(0, Mouse_X * MouseSensitivity * Time.smoothDeltaTime, 0));
        }

        private void HandleCrouch()
        {
            if (!_hasAnimator) return;

            if (_inputManager.Crouch && !_crouchPressed)
            {
                _crouchPressed = true;
                _isCrouching = !_isCrouching;
                _animator.SetBool(_crouchHash, _isCrouching);
            }
            else if (!_inputManager.Crouch)
            {
                _crouchPressed = false;
            }
        }

        private void HandleJump()
        {
            if (!_hasAnimator) return;
            if (!_inputManager.Jump) return;
            _animator.SetTrigger(_jumpHash);
        }

        public void JumpAddForce()
        {
            _playerRigidbody.AddForce(_playerRigidbody.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            _playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            _animator.ResetTrigger(_jumpHash);
        }

        private void SampleGround()
        {
            if (!_hasAnimator) return;

            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hitInfo, Dis2Ground + 0.1f, GroundCheck))
            {
                _grounded = true;
                SetAnimationGrounding();
                return;
            }
            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_fallingHash, !_grounded);
            _animator.SetBool(_groundedHash, _grounded);
        }

        // FOV coroutine (ensures only one runs at a time)
        private IEnumerator ChangeFOV(float targetFOV)
        {
            while (Mathf.Abs(playerCamera.fieldOfView - targetFOV) > 0.5f)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
                yield return null;
            }
            playerCamera.fieldOfView = targetFOV;
        }

        // DASH coroutine with hit check and FOV
        private IEnumerator Dash()
        {
            isDashing = true;
            lastDashTime = Time.time;

            // Smooth FOV effect, only one coroutine at a time!
            if (fovCoroutine != null) StopCoroutine(fovCoroutine);
            fovCoroutine = StartCoroutine(ChangeFOV(dashFOV));

            // Get dash path and stop if you hit something
            Vector3 dashDirection = transform.forward;
            if (_inputManager.Move != Vector2.zero)
                dashDirection = (transform.right * _inputManager.Move.x + transform.forward * _inputManager.Move.y).normalized;

            float startTime = Time.time;
            Vector3 start = transform.position;
            float dashDistance = dashForce;

            RaycastHit hit;
            if (Physics.CapsuleCast(
                start + Vector3.up * 0.5f,
                start + Vector3.up * 1.5f,
                0.4f, dashDirection, out hit, dashDistance, LayerMask.GetMask("Default")))
            {
                dashDistance = hit.distance - 0.01f;
            }

            Vector3 end = start + dashDirection * dashDistance;

            while (Time.time < startTime + dashDuration)
            {
                float t = (Time.time - startTime) / dashDuration;
                _playerRigidbody.MovePosition(Vector3.Lerp(start, end, t));
                yield return null;
            }
            _playerRigidbody.MovePosition(end);

            // Smooth FOV reset after dash
            if (fovCoroutine != null) StopCoroutine(fovCoroutine);
            fovCoroutine = StartCoroutine(ChangeFOV(normalFOV));

            isDashing = false;
        }
    }
}
