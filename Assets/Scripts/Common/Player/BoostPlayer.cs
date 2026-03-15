using System;
using Unity.Netcode;
using UnityEngine;

public abstract class BoostPlayer : MovementPlayer
{
    protected NetworkVariable<bool> isShowingBoostParticlesNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public ParticleSystem sprintParticleEffect;
    protected float smoothedPedalSpeed = 0f;
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -13f;
    public float maxZ = 12f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (sprintParticleEffect != null)
        {
            var main = sprintParticleEffect.main;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            sprintParticleEffect.Stop();
        }
        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
    }

    public virtual void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
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

    protected virtual (Vector3 joystickOffset, Vector3 input) ParseInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 joystickOffset = new Vector3(input.x, 0, input.y);
        joystickOffset = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ?
            new Vector3((-1) * PlayPulse.Input.Input.JoystickX, 0, (-1) * PlayPulse.Input.Input.JoystickY) : joystickOffset;
        return (joystickOffset, input);
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
}
