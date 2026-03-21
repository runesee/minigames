using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using PlayPulse.Api.Utils;

public class PlayerTagMovement : TagPlayer
{
    public AudioSource boostAudioSource;
    public AudioClip boostClip;

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
    private NetworkVariable<float> staminaNet = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private InputAction resetTaggedPlayerDebug;
    private bool isBoosting;
    private new readonly float sprintSpeedThreshold = 3.3f;
    private readonly float boostSpeed = 8f;
    private readonly float maxStamina = 100f;
    private readonly float staminaDrainRate = 20f;
    private readonly float staminaRegenRateFast = 15f;
    private readonly float exhaustedSpeedMultiplier = 0.3f;
    private readonly float minStaminaToBoost = 5f;
    private readonly float taggedStaminaBoostMultiplier = 1.5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        resetTaggedPlayerDebug = InputSystem.actions.FindAction("Crouch");
        resetTaggedPlayerDebug.Enable();
        if (IsHost && IsOwner) StartCoroutine(WaitForPlayerConnect());
    }

    private System.Collections.IEnumerator WaitForPlayerConnect()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < MinigameManager.PLAYER_COUNT || TagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        TagGameState.Instance.SetGameStateServerRpc(GameState.Running);
        SetInitialTaggedPlayer();
    }

    public override PlayerData GetPlayerData()
    {
        double totalTime = timeSpentTaggedNet.Value;
        if (NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
        {
            var gameTimer = FindAnyObjectByType<GameTimer>();
            double serverTime = gameTimer != null ? gameTimer.GameEndServerTime : NetworkManager.Singleton.ServerTime.FixedTime;
            totalTime += serverTime - lastTagTimeNet.Value;
        }
        return new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            (float) totalTime,
            Team.None
        );
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
            else ToggleHitPlayerServerRpc();
        }

        // Parse InputInteractions
        var (joystickOffset, input) = ParseInput();
        isPunchingNet.Value = isPunching && isTaggedNet.Value;
        bool wasBoosting = isBoosting;
        isBoosting = (sprintAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.RightTrigger)) && staminaNet.Value >= minStaminaToBoost;
        
        if (isBoosting && !wasBoosting) boostAudioSource?.PlayOneShot(boostClip); // Start playing boost audio
        else if (!isBoosting) boostAudioSource?.Stop(); // Stopped boosting, stop current audio

        // Set target player as isHit if punched by punching player
        if (isPunching && isTaggedNet.Value && !isFrozenNet.Value)
        {
            PlayerTagMovement target = PlayerUtils.FindClosestPlayerInRange<PlayerTagMovement>(2.5f, this.gameObject, this.transform);
            tagAudioSource?.PlayOneShot(tagClip);
            if (target != null)
            {
                TagPlayerServerRpc(target.NetworkObjectId); // Set target as tagged and hit (can tag others, play animation)
            }
        }
        else if (isFrozenNet.Value)
        {
            double timeSinceTagged = serverTime - lastTagTimeNet.Value;
            if (timeSinceTagged >= 0.7f) UnfreezePlayerServerRpc();
        }

        // Handle animations and update position based on input actions
        var (pedalSpeed, pedalAnimationSpeed) = GetSmoothedPedalSpeed();
        float moveSpeed = (isBoosting ? boostSpeed : walkSpeed) * (staminaNet.Value <= 0f ? exhaustedSpeedMultiplier : 1f) * pedalSpeed;

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            HandleMovement(joystickOffset, moveSpeed, pedalSpeed, pedalAnimationSpeed, false);
            UpdateStamina(isBoosting);

            // Play running animation if movement speed above threshold
            isSprintingNet.Value = moveSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isBoosting && (MinigameManager.USING_PLAYPULSE ? pedalSpeed > 0f : input.sqrMagnitude > 0.1f);
        }
        else HandleMovement();
        HandleTaunting();
        if (resetTaggedPlayerDebug.WasPressedThisFrame() && IsHost) SetInitialTaggedPlayer();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        animator.SetBool("isHit", isHitNet.Value);
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
        this.lastTagTimeNet.Value = serverTime;
        TagGameState.Instance.taggedPlayerIdNet.Value = victimId;
        this.isFrozenNet.Value = true;
        PlayTagSoundClientRpc();
    }

    [ClientRpc]
    private void PlayTagSoundClientRpc()
    {
        if (!tagAudioSource.isPlaying) tagAudioSource?.PlayOneShot(tagClip);
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

    [ServerRpc]
    protected virtual void ToggleHitPlayerServerRpc()
    {
        isHitNet.Value = false;
    }
}
