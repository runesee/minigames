using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using System;
using static TagSessionManager;
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
    private Vector3 savedPosition = default;
    private float pedalResistance;
    private bool USING_PLAYPULSE = true; // Flag for dev/bike movement toggling.
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

    /* TODO :
    Currently, this file feels bloated and hard to follow.
    Drawing scoreboard etc. should not reside here, but in separate files.
    */ 
    void Start()
    {
        if (!USING_PLAYPULSE) return;
        // Initialize connection with PP-service, which should already by started.
        try
        {
            if (!PlayPulse.PlayPulseService.IsInitialized)
            {
                // Reset resistance
                pedalResistance = 0.2f;
                PlayPulse.Input.Input.ResistanceSetPoint = pedalResistance;
            }
        }
        catch { USING_PLAYPULSE = false; } // Bike connection failed, overriding to use keyboard instead
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
        string color = IsOwner ? PlayerPrefs.GetString("Color") : colorNet.Value.ToString();
        SetSkinColor(color);

        if (IsOwner)
        {
            // Apply player-selected nickname
            string nickname = PlayerPrefs.GetString("Username", "Player");
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(nickname);

            // Attempt to store per-player session data on disconnect
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            TagGameState.Instance.gameState.OnValueChanged += OnTagGameStateChanged;
        } 
    }

    private void OnTagGameStateChanged(TagGameState.GameState previousValue, TagGameState.GameState newValue)
    {
        if (newValue == TagGameState.GameState.Stopped)
        {
            SaveTagData(); // If the game has ended, all player data should be saved and stored appropriately.
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
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

    /// <summary>
    /// Reconstructs a disconnected player's data, if saved on host.
    /// </summary>
    /// <param name="clientId">Required, not used.</param>
    private void OnClientConnect(ulong clientId)
    {
        if (!TagSessionManager.Instance) return;
        try
        {
            FixedString64Bytes guid = new FixedString64Bytes(PlayerPrefs.GetString("Guid"));
            if (TagSessionManager.Instance.ContainsGuid(guid))
            {
                PlayerData playerData = (PlayerData)TagSessionManager.Instance.GetDataByGuid(guid); // Already know Guid exists, ignore null case
                savedPosition = new Vector3(playerData.XPos, 1f, playerData.ZPos);
                ResyncPlayerDataServerRpc(playerData);
            }
        }
        catch (KeyNotFoundException) {}
    }

    /// <summary>
    /// Saves a disconnecting player's data as long as the host keeps running.
    /// Used to resync player data on reconnect.
    /// </summary>
    /// <param name="clientId">Required, not used.</param>
    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;
        SaveTagData();
    }

    private void SaveTagData()
    {
        var position = transform.position;
        double totalTime = timeSpentTaggedNet.Value;
        if (NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
        {
            double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
            totalTime += serverTime - lastTagTimeNet.Value;
        }

        PlayerData playerData = new PlayerData(
            PlayerPrefs.GetString("Guid"),
            position.x,
            position.z,
            totalTime,
            lastTagTimeNet.Value,
            isTaggedNet.Value
        );
        SaveTagDataServerRpc(playerData);
    }

    // There is arguably a lot of logic in onGUI, which runs often.
    // TODO : Move this logic when a better UI solution is in place.
    void OnGUI()
    {
        if (!TagGameState.Instance || !NetworkManager.Singleton) return;

        // Draw scoreboard
        GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 300));
        if (TagGameState.Instance.gameState.Value == TagGameState.GameState.Running)
        {
            GUILayout.TextArea("Scoreboard");
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                var player = obj.GetComponent<PlayerTagMovement>();
                if (!player) continue;

                double displayTime = player.timeSpentTaggedNet.Value;

                if (player.NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
                {
                    double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
                    displayTime += serverTime - player.lastTagTimeNet.Value;
                }
                
                string playerName = player.nicknameNet.Value.ToString();
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = $"Player{player.OwnerClientId}";
                }
                GUILayout.TextArea($"{playerName}: {displayTime:F1}s");
            }
        }
        GUILayout.EndArea();

        // Draw menu items
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        if (GUILayout.Button("Shutdown"))
        {
            if (NetworkManager.Singleton.IsHost)
            {
                MinigameManager.Instance.TerminateConnection();
            }
        }

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && TagGameState.Instance.gameState.Value == TagGameState.GameState.Idling && NetworkManager.Singleton.IsHost)
        {
            if (GUILayout.Button("Start Game"))
            {
                TagGameState.Instance.SetGameStateServerRpc(TagGameState.GameState.Running);
                SetInitialTaggedPlayer();
            }
        }
        GUILayout.EndArea();

        if (IsOwner) {
            DrawStaminaBar();
        }
    }

    private void Update()
    {
        if (TagGameState.Instance != null && TagGameState.Instance.gameState.Value != TagGameState.GameState.Running) return;

        // TODO : add flag instead of constant checks
        /*if (savedPosition.magnitude != 0f)
        {
            rb.MovePosition(savedPosition);
            savedPosition = default;
            return;
        }*/

        // Attempt to change gears if user presses right or left trigger
        if (USING_PLAYPULSE)
        {
            float deltaResistance = PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.RightTrigger) ? 0.2f : -0.2f;
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.RightTrigger)
            || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.LeftTrigger))
            {
                pedalResistance = Math.Clamp(pedalResistance + deltaResistance, 0.0f, 1.0f);
                PlayPulse.Input.Input.ResistanceSetPoint = pedalResistance;
            }
        }

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
        isBoosting = (sprintAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.B)) && staminaNet.Value >= minStaminaToBoost;

        // Set target player as isHit if punched by punching player
        if (isPunching && isTaggedNet.Value)
        {
            PlayerTagMovement target = FindClosestPlayerInRange(2.5f);

            if (target != null)
            {
                TagPlayerServerRpc(target.NetworkObjectId); // Set target as tagged and hit (can tag others, play animation)
            }
        }

        // Handle animations and update position based on input actions
        float pedalSpeed = USING_PLAYPULSE ? Math.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f) : 0.4f;
        float pedalAnimationSpeed = USING_PLAYPULSE ? 1.6f * pedalSpeed : 1f;
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
            isShowingBoostParticlesNet.Value = isBoosting;
        }
        else
        {
            isWalkingNet.Value = false;
            isSprintingNet.Value = false;
            animator.speed = 1.0f;
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
    /// Helper function for rendering the stamina bar in OnGui.
    /// </summary>
    private void DrawStaminaBar()
    {
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
        nicknameNet.Value = new FixedString64Bytes(nickname);
    }

    /// <summary>
    /// Saves Tag game data tied to a GUID on the host.
    /// Used to re-construct game state upon reconnect.
    /// </summary>
    /// <param name="playerData">Tag-specific data to save on disconnect.</param>
    [ServerRpc]
    private void SaveTagDataServerRpc(TagSessionManager.PlayerData playerData)
    {
        TagSessionManager.Instance.SaveDataServerRpc(playerData);
    }

    /// <summary>
    /// Reads saved Tag game data tied to a GUID, and reconstructs that player's netvars.
    /// </summary>
    /// <param name="playerData">Tag-specific data read on reconnect.</param>
    [ServerRpc]
    public void ResyncPlayerDataServerRpc(PlayerData playerData)
    {
        timeSpentTaggedNet.Value = playerData.TimeSpentTagged;
        lastTagTimeNet.Value = playerData.LastTagTime;
        isTaggedNet.Value = playerData.IsTagged;
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
        victim.isWalkingNet.Value = false;
        victim.isSprintingNet.Value = false;
        victim.isTauntingNet.Value = false;

        // Add timediff to current player
        timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
        victim.lastTagTimeNet.Value = serverTime;
        TagGameState.Instance.taggedPlayerIdNet.Value = victimId;
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
    /// Helper function for getting the closest player within range and field of view, if any.
    /// </summary>
    /// <param name="range"></param> Limit for how far a player can tag.
    /// <returns></returns> A PlayerTagMovement object or null.
    private PlayerTagMovement FindClosestPlayerInRange(float range)
    {
        PlayerTagMovement closest = null;
        float shortest = Mathf.Infinity;
        bool isWithinBounds = false;
        GameObject _player = new GameObject();
        PlayerTagMovement taggedPlayer = this;
        foreach (var player in FindObjectsByType(typeof(PlayerTagMovement), FindObjectsSortMode.None))
        {
            if (player == this) continue;

            float distance = Vector3.Distance(transform.position, ((PlayerTagMovement)player).transform.position);

            if (distance < range && distance < shortest)
            {
                shortest = distance;
                closest = (PlayerTagMovement)player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= (distance > range / 2 ? 70f : 45f);
                taggedPlayer = (PlayerTagMovement)player;
                _player = player.GameObject();
            }
        }
        // Need to check whether a GameObject is blocking the player's view (e.g. a Cube)
        if (Physics.Linecast(transform.position, taggedPlayer.transform.position, out RaycastHit hit))
        {
            if (hit.collider.gameObject != _player)
            {
                isWithinBounds = false;
            }
        }
        return isWithinBounds ? closest : null;
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
