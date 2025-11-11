using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTutorial.Manager;

namespace UnityTutorial.PlayerControl
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float AnimBlendSpeed = 8.9f;
        [SerializeField] private Camera playerCamera;
        private Coroutine fovCoroutine = null; // Track current FOV coroutine



        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float BottomLimit = 70f;
        [SerializeField] private float MouseSensitivity = 21.9f;

        [SerializeField] private float JumpFactor = 260f;
        [SerializeField] private float Dis2Ground = 0.8f;
        [SerializeField] private float AirResistance = 0.8f;
        [SerializeField] private LayerMask GroundCheck;

        //FOV for Dash
        [SerializeField] private float dashFOV = 80f; // FOV during dash
        [SerializeField] private float normalFOV = 60f; // Regular FOV (set to match your camera default)
        [SerializeField] private float fovChangeSpeed = 10f; // Adjust for how fast FOV changes


        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private Animator _animator;
        private bool _grounded;
        private bool _hasAnimator;
        private bool _isCrouching;
        private bool _crouchPressed;
        private int _xVelHash;
        private int _yVelHash;
        private int _zVelHash;
        private int _jumpHash;
        private int _groundedHash;
        private int _fallingHash;
        private int _crouchHash;

        private float _xRotation;

        private const float _walkSpeed = 2f;
        private const float _runSpeed = 6f;
        private Vector2 _currentVelocity;

        // Dash variables
        [SerializeField] private float dashForce = 20f;
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private float dashDuration = 0.15f;

        private bool isDashing = false;
        private float lastDashTime = -999f;

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
        }

        private void FixedUpdate()
        {
            // Dash input (Left Shift)
            if (_inputManager.Dash && Time.time >= lastDashTime + dashCooldown && _grounded && !isDashing)
            {
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
            if (!_hasAnimator || isDashing) return; // Don't move normally while dashing

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

            // Toggle crouch on button press
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
            Debug.Log(_grounded);
            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
            return;
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_fallingHash, !_grounded);
            _animator.SetBool(_groundedHash, _grounded);
        }

        // DASH COROUTINE (Smooth!)
        private IEnumerator ChangeFOV(float targetFOV)
        {
            while (Mathf.Abs(playerCamera.fieldOfView - targetFOV) > 0.5f)
            {
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
                yield return null;
            }
            playerCamera.fieldOfView = targetFOV;
        }

        private IEnumerator Dash()
        {
            isDashing = true;
            lastDashTime = Time.time;
            if (fovCoroutine != null)
                StopCoroutine(fovCoroutine);
            fovCoroutine = StartCoroutine(ChangeFOV(dashFOV));


            Vector3 dashDirection = transform.forward;
            if (_inputManager.Move != Vector2.zero)
            {
                dashDirection = (transform.right * _inputManager.Move.x + transform.forward * _inputManager.Move.y).normalized;
            }

            float startTime = Time.time;
            Vector3 start = transform.position;

            // Cast to see how far you can actually dash without hitting
            float dashDistance = dashForce; // dashForce should be your dash distance now
            RaycastHit hit;
            if (Physics.CapsuleCast(
                start + Vector3.up * 0.5f,         // start of capsule (feet level, Y might need tuning)
                start + Vector3.up * 1.5f,         // end of capsule (head level)
                0.4f,                              // radius (match your player's collider)
                dashDirection, out hit, dashDistance, LayerMask.GetMask("Default"))) // or your collision layers
            {
                dashDistance = hit.distance - 0.01f; // stop just before collision
            }

            Vector3 end = start + dashDirection * dashDistance;

            // Lerp position (stops before wall)
            while (Time.time < startTime + dashDuration)
            {
                float t = (Time.time - startTime) / dashDuration;
                _playerRigidbody.MovePosition(Vector3.Lerp(start, end, t));
                yield return null;
            }

            _playerRigidbody.MovePosition(end);
            if (fovCoroutine != null)
                StopCoroutine(fovCoroutine);
            fovCoroutine = StartCoroutine(ChangeFOV(normalFOV));

            isDashing = false;
        }

    }
}
