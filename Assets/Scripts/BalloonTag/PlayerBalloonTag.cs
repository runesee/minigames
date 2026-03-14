using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class PlayerBalloonTag : MovementPlayer
{
    [Header("Map Boundaries")]
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -13f;
    public float maxZ = 12f;

    [Header("Audio settings")]
    public AudioSource tagAudioSource;
    public AudioClip tagClip;
    public AudioClip popClip;

    private NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> isShowingBoostParticlesNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public NetworkVariable<double> timeSpentTaggedNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<double> lastTagTimeNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<BalloonState> balloonsNet = new NetworkVariable<BalloonState>(
    new BalloonState(2, "#D6877F"),
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    public ParticleSystem sprintParticleEffect;
    public List<GameObject> BalloonPrefabs;
    private InputAction attackAction;
    private InputAction interactAction;

    private bool isPunching;
    private bool isTaunting;
    private bool canTaunt;
    private float smoothedPedalSpeed = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;

        // Configure sprint particle effect
        if (sprintParticleEffect != null)
        {
            var main = sprintParticleEffect.main;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            sprintParticleEffect.Stop();
        }

        // Init key bindings
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        attackAction = InputSystem.actions.FindAction("Attack");
        interactAction = InputSystem.actions.FindAction("Interact");
        moveAction.Enable();
        sprintAction.Enable();
        attackAction.Enable();
        interactAction.Enable();

        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;
        balloonsNet.OnValueChanged += OnBalloonsChanged;
        if (IsHost) StartCoroutine(WaitForPlayerConnect());
    }

    private System.Collections.IEnumerator WaitForPlayerConnect()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || BalloonTagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Running);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
        balloonsNet.OnValueChanged -= OnBalloonsChanged;
    }

    private void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
    }
    
    void OnBalloonsChanged(BalloonState previousValue, BalloonState newValue)
    {
        for (int i = 0; i < BalloonPrefabs.Count; i++)
        {
            BalloonPrefabs[i].SetActive(i < newValue.count);
            UnityEngine.ColorUtility.TryParseHtmlString(newValue.GetColor(i).ToString(), out var color);
            BalloonPrefabs[i].GetComponentInChildren<MeshRenderer>().material.color = color;
        }
    }

    /// <summary>
    /// Helper method for changing a player model's color.
    /// </summary>
    /// <param name="color">Updated hex-code color.</param>
    protected override void SetSkinColor(string color)
    {
        base.SetSkinColor(color);
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        BalloonPrefabs[0].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        BalloonPrefabs[1].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        if (IsOwner) InitializeBalloonsServerRpc(color);
    }

    public override PlayerData GetPlayerData()
    {
        PlayerData playerData = new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            balloonsNet.Value.count,
            balloonsNet.Value.count
        );
        return playerData;
    }

    private void Update()
    {
        if (BalloonTagGameState.Instance != null && BalloonTagGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;

        // Parse InputInteractions
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 joystickOffset = new Vector3(input.x, 0, input.y);
        isTaunting = (interactAction.ReadValue<float>() > 0f && interactAction.WasPressedThisFrame()) ||
            PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isTaunting = interactAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isPunching = attackAction.WasPerformedThisFrame() || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);
        isPunchingNet.Value = isPunching;

        if (isPunching && NetworkManager.Singleton.ServerTime.FixedTime - lastTagTimeNet.Value > 0.7)
        {
            PlayerBalloonTag target = PlayerUtils.FindClosestPlayerInRange<PlayerBalloonTag>(2.5f, this.gameObject, this.transform);
            tagAudioSource.pitch = 1f;
            tagAudioSource?.PlayOneShot(tagClip);
            if (target != null) TagPlayerServerRpc(target.NetworkObjectId);
            else TagServerRpc();
        }

        // Handle animations and update position based on input actions
        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        float inputSpeed = Math.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, inputSpeed, smoothing);
        float pedalSpeed = MinigameManager.USING_PLAYPULSE ? smoothedPedalSpeed : 0.5f;
        float pedalAnimationSpeed = MinigameManager.USING_PLAYPULSE ? 1.6f * pedalSpeed : 1f;
        joystickOffset = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ?
        new Vector3((-1) * PlayPulse.Input.Input.JoystickX, 0, (-1) * PlayPulse.Input.Input.JoystickY) : joystickOffset;

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            // Play running animation if movement speed above threshold
            isSprintingNet.Value = pedalSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isSprintingNet.Value;
            float moveSpeed = 6f * pedalSpeed;

            Quaternion lastRotation = Quaternion.LookRotation(joystickOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
            animator.speed = pedalAnimationSpeed;

            Vector3 newPosition = rb.position + moveSpeed * Time.deltaTime * joystickOffset.normalized;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            rb.MovePosition(newPosition);
        }
        else
        {
            isWalkingNet.Value = false;
            isSprintingNet.Value = false;
            animator.speed = 1.0f;
            isShowingBoostParticlesNet.Value = false;
        }

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

    public override void LateUpdate()
    {
        base.LateUpdate();
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
    }
 
    [ServerRpc]
    public void InitializeBalloonsServerRpc(FixedString64Bytes color)
    {
        balloonsNet.Value = new BalloonState(2, color);
    }

    [ClientRpc]
    private void PlayTagSoundClientRpc()
    {
        tagAudioSource.pitch = UnityEngine.Random.Range(0.7f, 1.3f);
        tagAudioSource?.PlayOneShot(popClip);
    }


    /// <summary>
    /// Set targeted player as tagged on server, and disable their movement.
    /// Also update time tagging player has been tagged.
    /// </summary>
    /// <param name="victimId"></param> Target player ID.
    [ServerRpc]
    private void TagPlayerServerRpc(ulong victimId)
    {
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId].GetComponent<PlayerBalloonTag>();
        BalloonState localBalloons = this.balloonsNet.Value;
        BalloonState victimBalloons = victim.balloonsNet.Value;
        if (victimBalloons.count <= 0) return;

        localBalloons.SetColor(localBalloons.count, victimBalloons.GetColor(victimBalloons.count - 1));
        localBalloons.count++;
        victimBalloons.count--;
        this.balloonsNet.Value = localBalloons;
        victim.balloonsNet.Value = victimBalloons;

        // Add timediff to current player and prevent tagging again for another .7 seconds
        this.timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
        this.lastTagTimeNet.Value = serverTime;
        PlayTagSoundClientRpc();
    }

    /// <summary>
    /// RPC that prevents spamming of tag when NOT hitting a target.
    /// </summary>
    [ServerRpc]
    private void TagServerRpc()
    {
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        this.lastTagTimeNet.Value = serverTime;
    }

    /// <summary>
    /// Re-enable user actions after freeze period.
    /// </summary>
    [ServerRpc]
    private void UnfreezePlayerServerRpc()
    {
        isFrozen.Value = false;
    }
}
