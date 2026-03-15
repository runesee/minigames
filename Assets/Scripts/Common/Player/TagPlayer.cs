using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class TagPlayer : BoostPlayer
{
    protected NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected NetworkVariable<bool> isFrozenNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    protected NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected NetworkVariable<double> timeSpentTaggedNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    protected NetworkVariable<double> lastTagTimeNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public AudioSource tagAudioSource;
    public AudioClip tagClip;
    protected InputAction attackAction;
    protected InputAction interactAction;
    protected bool isPunching;
    protected bool isTaunting;
    protected bool canTaunt;
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -13f;
    public float maxZ = 12f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        attackAction = InputSystem.actions.FindAction("Attack");
        interactAction = InputSystem.actions.FindAction("Interact");
        attackAction.Enable();
        interactAction.Enable();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
    }

    protected virtual void HandleTaunting()
    {
        // Lastly, if neither moving or tagging, check if taunting.
        // Sets both trigger and bool value in Animator.
        // Limits animation to loop once if key is held down,
        // otherwise cancels on other actions or letting go of key
        canTaunt = !isWalkingNet.Value && !isSprintingNet.Value && !isPunching;
        if (isTaunting && canTaunt)
        {
            animator.SetTrigger("isTauntingTrigger");
            isTauntingNet.Value = true;
        }
        else if (isTaunting && canTaunt && !isTauntingNet.Value)
        {
            isTauntingNet.Value = true;
        }
        if (!isTaunting || !canTaunt || interactAction.WasReleasedThisFrame())
        {
            isTauntingNet.Value = false;
        }
    }

    protected virtual (Vector3 joystickOffset, Vector3 input) ParseInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 joystickOffset = new Vector3(input.x, 0, input.y);
        isTaunting = (interactAction.ReadValue<float>() > 0f && interactAction.WasPressedThisFrame()) || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isPunching = attackAction.WasPerformedThisFrame() || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);
        joystickOffset = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ?
            new Vector3((-1) * PlayPulse.Input.Input.JoystickX, 0, (-1) * PlayPulse.Input.Input.JoystickY) : joystickOffset;
        return (joystickOffset, input);
    }

    protected (float pedalSpeed, float animationSpeed) GetSmoothedPedalSpeed()
    {
        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        float inputSpeed = Math.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, inputSpeed, smoothing);
        float pedalSpeed = MinigameManager.USING_PLAYPULSE ? smoothedPedalSpeed : 0.4f;
        float pedalAnimationSpeed = MinigameManager.USING_PLAYPULSE ? 1.6f * pedalSpeed : 1f;
        return (pedalSpeed, pedalAnimationSpeed);
    }

    protected void HandleMovement(Vector3 joystickOffset, float moveSpeed, float pedalSpeed, float pedalAnimationSpeed, bool updateAnimations)
    {
        if (updateAnimations) UpdateMovementAnimations(pedalSpeed);
        Quaternion lastRotation = Quaternion.LookRotation(joystickOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
        animator.speed = pedalAnimationSpeed;

        Vector3 newPosition = rb.position + moveSpeed * Time.deltaTime * joystickOffset.normalized;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        rb.MovePosition(newPosition);
    }

    protected void HandleMovement()
    {
        isWalkingNet.Value = false;
        isSprintingNet.Value = false;
        animator.speed = 1.0f;
        isShowingBoostParticlesNet.Value = false;
    }

    protected virtual void UpdateMovementAnimations(float inputSpeed)
    {
        // Play running animation if movement speed above threshold
        isSprintingNet.Value = inputSpeed > sprintSpeedThreshold;
        isWalkingNet.Value = !isSprintingNet.Value;
        isShowingBoostParticlesNet.Value = isSprintingNet.Value;
    }

    [ServerRpc]
    protected virtual void UnfreezePlayerServerRpc()
    {
        isFrozenNet.Value = false;
    }

    [ClientRpc]
    protected virtual void StopAnimationsClientRpc()
    {
        if (!IsOwner) return;
        isWalkingNet.Value = false;
        isSprintingNet.Value = false;
        isTauntingNet.Value = false;
        isPunchingNet.Value = false;
    }
}
