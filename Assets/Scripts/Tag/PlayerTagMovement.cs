using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using System;
using PlayPulse.Api.Utils;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTagMovement : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

    [Header("Map Boundaries")]
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -13f;
    public float maxZ = 12f;

    [Header("Audio settings")]
    public AudioSource tagAudioSource;
    public AudioSource boostAudioSource;
    public AudioClip tagClip;
    public AudioClip boostClip;

    private NetworkVariable<bool> isWalkingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<bool> isSprintingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<bool> isHitNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<bool> isTaggedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
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
    public NetworkVariable<FixedString64Bytes> colorNet = new NetworkVariable<FixedString64Bytes>(
    "#D6877F",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> nicknameNet = new NetworkVariable<FixedString64Bytes>(
    "Player",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> guidNet = new NetworkVariable<FixedString64Bytes>(
    "",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> staminaNet = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public ParticleSystem sprintParticleEffect;
    private InputAction attackAction;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction resetTaggedPlayerDebug;
    private Animator animator;
    private Rigidbody rb;

    private bool isPunching;
    private bool isTaunting;
    private bool canTaunt;
    private bool isBoosting;
    private float smoothedPedalSpeed = 0f;
    private readonly float walkSpeed = 5f;
    private readonly float boostSpeed = 8f;
    private readonly float maxStamina = 100f;
    private readonly float staminaDrainRate = 20f;
    private readonly float staminaRegenRateFast = 15f;
    private readonly float sprintSpeedThreshold = 3.3f;
    private readonly float exhaustedSpeedMultiplier = 0.3f;
    private readonly float minStaminaToBoost = 5f;
    private readonly float taggedStaminaBoostMultiplier = 1.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
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
        resetTaggedPlayerDebug = InputSystem.actions.FindAction("Crouch");
        moveAction.Enable();
        sprintAction.Enable();
        attackAction.Enable();
        interactAction.Enable();
        resetTaggedPlayerDebug.Enable();

        // Subscribe to color and sprint particle changes
        colorNet.OnValueChanged += OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;

        // Apply initial player-selected color
        var data = LocalPlayerStorage.Load();
        string color = IsOwner ? data.color : colorNet.Value.ToString();
        SetSkinColor(color);

        if (IsOwner)
        {
            // Apply player-selected nickname and color
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(data.nickname);
            UpdateGuidServerRpc(data.guid);
        }
        if (IsHost && IsOwner) StartCoroutine(WaitForPlayerConnect());
    }

    private System.Collections.IEnumerator WaitForPlayerConnect()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || TagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        TagGameState.Instance.SetGameStateServerRpc(GameState.Running);
        SetInitialTaggedPlayer();
    }

    public override void OnNetworkDespawn()
    {
        colorNet.OnValueChanged -= OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(newValue.Value.ToString());
    }

    private void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
    }

    /// <summary>
    /// Helper method for changing a player model's color.
    /// </summary>
    /// <param name="color">Updated hex-code color.</param>
    private void SetSkinColor(string color)
    {
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        playerSkinRenderer.material.color = skinColor;
    }

    public TagGameState.PlayerData GetTagData()
    {
        var position = transform.position;
        double totalTime = timeSpentTaggedNet.Value;
        if (NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
        {
            double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
            totalTime += serverTime - lastTagTimeNet.Value;
        }

        TagGameState.PlayerData playerData = new TagGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            position.x,
            position.z,
            totalTime,
            lastTagTimeNet.Value,
            isTaggedNet.Value
        );
        return playerData;
    }

    void OnGUI()
    {
        if (!IsOwner) return;

        float barWidth = 200f;
        float barHeight = 20f;
        float padding = 20f;
        float xPos = Screen.width - barWidth - padding;
        float yPos = Screen.height - barHeight - padding;

        Rect backgroundRect = new Rect(xPos, yPos, barWidth, barHeight);
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);

        float currentMaxStamina = GetCurrentMaxStamina();
        float staminaPercent = staminaNet.Value / currentMaxStamina;
        Rect fillRect = new Rect(xPos, yPos, barWidth * staminaPercent, barHeight);

        Color fillColor = Color.Lerp(Color.red, Color.green, staminaPercent);
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void Update()
    {
        if (TagGameState.Instance != null && TagGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;
        
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        if (isHitNet.Value)
        {
            double timeSinceTagged = serverTime - lastTagTimeNet.Value;
            if (timeSinceTagged < 1.8f) return;
            else UnfreezePlayerServerRpc();
        }

        // Parse InputInteractions
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 joystickOffset = new Vector3(input.x, 0, input.y);
        isTaunting = (interactAction.ReadValue<float>() > 0f && interactAction.WasPressedThisFrame()) ||
            PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isTaunting = interactAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isPunching = attackAction.WasPerformedThisFrame() || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);
        isPunchingNet.Value = isPunching && isTaggedNet.Value;
        bool wasBoosting = isBoosting;
        isBoosting = (sprintAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.RightTrigger)) && staminaNet.Value >= minStaminaToBoost;
        
        if (isBoosting && !wasBoosting) boostAudioSource?.PlayOneShot(boostClip); // Start playing boost audio
        else if (!isBoosting) boostAudioSource?.Stop(); // Stopped boosting, stop current audio

        // Set target player as isHit if punched by punching player
        if (isPunching && isTaggedNet.Value)
        {
            PlayerTagMovement target = PlayerUtils.FindClosestPlayerInRange<PlayerTagMovement>(2.5f, this.gameObject, this.transform);
            tagAudioSource?.PlayOneShot(tagClip);
            if (target != null)
            {
                TagPlayerServerRpc(target.NetworkObjectId); // Set target as tagged and hit (can tag others, play animation)
            }
        }

        // Handle animations and update position based on input actions
        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        float inputSpeed = Math.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, inputSpeed, smoothing);
        float pedalSpeed = MinigameManager.USING_PLAYPULSE ? smoothedPedalSpeed : 0.4f;
        float pedalAnimationSpeed = MinigameManager.USING_PLAYPULSE ? 1.6f * pedalSpeed : 1f;
        joystickOffset = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ?
        new Vector3((-1) * PlayPulse.Input.Input.JoystickX, 0, (-1) * PlayPulse.Input.Input.JoystickY) : joystickOffset;
        float moveSpeed = (isBoosting ? boostSpeed : walkSpeed) * (staminaNet.Value <= 0f ? exhaustedSpeedMultiplier : 1f) * pedalSpeed;

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            Quaternion lastRotation = Quaternion.LookRotation(joystickOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
            animator.speed = pedalAnimationSpeed;

            Vector3 newPosition = rb.position + moveSpeed * Time.deltaTime * joystickOffset.normalized;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            rb.MovePosition(newPosition);
            UpdateStamina(isBoosting);

            // Play running animation if movement speed above threshold
            // TODO : use a range instead of ONE value to prevent jitter!
            isSprintingNet.Value = moveSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isBoosting && (MinigameManager.USING_PLAYPULSE ? pedalSpeed > 0f : input.sqrMagnitude > 0.1f);
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

        if (resetTaggedPlayerDebug.WasPressedThisFrame() && IsHost) {
            SetInitialTaggedPlayer();
        }
    }

    private void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isHit", isHitNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
    }

    /// <summary>
    /// Helper function for allocating one player as tagged.
    /// </summary>
    private void SetInitialTaggedPlayer() {
        // We have to do a lot of parsing as custom class objects are not serializable with Netcode (currently)
        var players = NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values
        .Select(playerObject => playerObject.GetComponent<PlayerTagMovement>())
        .Where(player => player != null)
        .ToList();
        var random = UnityEngine.Random.Range(0, players.Count);
        var selectedPlayer = players[random];
        SetInitialTaggedPlayerServerRpc(selectedPlayer.NetworkObjectId);
    }

    /// <summary>
    /// Sets player model color to specified hexcode.
    /// </summary>
    /// <param name="color">New player model color.</param>
    [ServerRpc]
    public void UpdateColorServerRpc(string color)
    {
        colorNet.Value = new FixedString64Bytes(color);
    }

    [ServerRpc]
    public void UpdateNicknameServerRpc(string nickname)
    {
        nicknameNet.Value = nickname;
    }

    [ServerRpc]
    public void UpdateGuidServerRpc(string guid)
    {
        guidNet.Value = guid;
    }

    /// <summary>
    /// Re-enable user actions after freeze period.
    /// </summary>
    [ServerRpc]
    private void UnfreezePlayerServerRpc()
    {
        isHitNet.Value = false;
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

        isTaggedNet.Value = false;
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId].GetComponent<PlayerTagMovement>();
        victim.isHitNet.Value = true;
        victim.isTaggedNet.Value = true;
        if (IsOwner) StopAnimationsClientRpc();

        // Add timediff to current player
        timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
        victim.lastTagTimeNet.Value = serverTime;
        TagGameState.Instance.taggedPlayerIdNet.Value = victimId;
        PlayTagSoundClientRpc();
    }

    [ClientRpc]
    private void PlayTagSoundClientRpc()
    {
        if (!tagAudioSource.isPlaying) tagAudioSource?.PlayOneShot(tagClip);
    }

    [ClientRpc]
    void StopAnimationsClientRpc()
    {
        if (!IsOwner) return;
        isWalkingNet.Value = false;
        isSprintingNet.Value = false;
        isTauntingNet.Value = false;
    }

    /// <summary>
    /// Set a player as tagged on the server.
    /// Used for initializing the game state.
    /// </summary>
    /// <param name="playerId"></param> ID of player that starts tagged.
    [ServerRpc]
    private void SetInitialTaggedPlayerServerRpc(ulong playerId)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerId]
                    .GetComponent<PlayerTagMovement>();
        playerObject.isTaggedNet.Value = true;
        playerObject.lastTagTimeNet.Value = NetworkManager.Singleton.ServerTime.FixedTime;
        TagGameState.Instance.taggedPlayerIdNet.Value = playerId;
    }

    /// <summary>
    /// Helper function for reducing current stamina while sprinting.
    /// </summary>
    /// <param name="isCurrentlySprinting"></param>
    private void UpdateStamina(bool isCurrentlySprinting)
    {
        if (!IsOwner) return;

        float currentMaxStamina = GetCurrentMaxStamina();
        float regenMultiplier = isTaggedNet.Value ? taggedStaminaBoostMultiplier : 1f;

        if (isCurrentlySprinting)
        {
            staminaNet.Value = Mathf.Max(0f, staminaNet.Value - staminaDrainRate * Time.deltaTime);
        }
        else
        {
            staminaNet.Value = Mathf.Min(currentMaxStamina, staminaNet.Value + staminaRegenRateFast * regenMultiplier * Time.deltaTime);
        }
    }

    private float GetCurrentMaxStamina()
    {
        return isTaggedNet.Value ? maxStamina * taggedStaminaBoostMultiplier : maxStamina;
    }
}
