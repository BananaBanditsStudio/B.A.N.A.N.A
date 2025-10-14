using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource walkSound;
    public AudioSource sprintSound;
    public AudioSource jumpSound;

    [Header("Movement Settings")]
    public float moveThreshold = 0.1f; // how much movement counts as "walking"
    public float crouchVolumeMultiplier = 0.5f;
    public float crouchPitchMultiplier = 0.85f;

    private CharacterController controller;
    private bool isMoving;
    private bool wasGrounded;
    private bool isCrouching;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        wasGrounded = controller ? controller.isGrounded : true;
    }

    void Update()
    {
        HandleCrouchToggle();
        HandleMovementSounds();
        HandleJumpSound();
    }

    void HandleCrouchToggle()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }
    }

    void HandleMovementSounds()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        isMoving = Mathf.Abs(horizontal) > moveThreshold || Mathf.Abs(vertical) > moveThreshold;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // ✅ Only play movement sounds if grounded
        bool isGrounded = controller ? controller.isGrounded : true;

        if (isGrounded && isMoving)
        {
            if (isSprinting && !isCrouching)
            {
                if (!sprintSound.isPlaying)
                {
                    walkSound.Stop();
                    sprintSound.Play();
                }
                sprintSound.volume = 1f;
                sprintSound.pitch = 1f;
            }
            else
            {
                if (!walkSound.isPlaying)
                {
                    sprintSound.Stop();
                    walkSound.Play();
                }

                if (isCrouching)
                {
                    walkSound.volume = 1f * crouchVolumeMultiplier;
                    walkSound.pitch = 1f * crouchPitchMultiplier;
                }
                else
                {
                    walkSound.volume = 1f;
                    walkSound.pitch = 1f;
                }
            }
        }
        else
        {
            // stop footsteps if not grounded or not moving
            if (walkSound.isPlaying) walkSound.Stop();
            if (sprintSound.isPlaying) sprintSound.Stop();
        }
    }

    void HandleJumpSound()
    {
        if (controller)
        {
            // play jump sound when player leaves the ground
            if (!controller.isGrounded && wasGrounded)
            {
                jumpSound.Play();
            }

            wasGrounded = controller.isGrounded;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpSound.Play();
            }
        }
    }
}
