using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using static TagSessionManager;
using PlayPulse.Api.Utils;
using UnityEngine.UIElements;
using Unity.Netcode.Components;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTagMovement : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;
    public float RotateSpeed = 30f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRateSlow = 5f;
    public float staminaRegenRateFast = 15f;
    public float sprintSpeedThreshold = 0.5f;
    public float walkSpeedThreshold = 0.2f;
    public float exhaustedSpeedMultiplier = 0.3f;
    public float minStaminaToSprint = 5f;
    public float taggedStaminaBoostMultiplier = 1.5f;

    [Header("Map Boundaries")]
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -13f;
    public float maxZ = 12f;

    [Header("Player Customization")]
    public List<string> playerColors = new List<string>()
    {
        "#D6877F",
        "#7fb3d6",
        "#92d67f",
        "#d6d37f",
    };

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

    private NetworkVariable<float> staminaNet = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private InputAction attackAction;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private Animator animator;
    private Rigidbody rb;

    private bool isSprinting;
    private bool isPunching;
    private bool isTaunting;
    private bool canTaunt;
    private Vector3 savedPosition = default;
    private float currentSpeed;
    private bool wasTaggedLastFrame;
    private float pedalResistance;
    private bool USING_PLAYPULSE = false; // Flag for dev/bike movement toggling.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start() {
        if (!USING_PLAYPULSE) return;
        // Initialize connection with PP-service
        try
        {
            if (!PlayPulse.PlayPulseService.IsInitialized) {
                PlayPulse.PlayPulseService.Initialize(
                    string.Empty,
                    connectToBikeService: true,
                    appSocketPathOverride: "127.0.0.1:13337",
                    shellSocketPathOverride: "127.0.0.1:13337",
                    useTcpSocket: true
                );
                // Reset resistance
                pedalResistance = 0.2f; 
                PlayPulse.Input.Input.ResistanceSetPoint = pedalResistance;
            }
        }
        catch {USING_PLAYPULSE = false;} // Bike connection failed, overriding to use keyboard instead
    }

    public override void OnNetworkSpawn()
    {
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;

        // Init key bindings
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        attackAction = InputSystem.actions.FindAction("Attack");
        interactAction = InputSystem.actions.FindAction("Interact");
        moveAction.Enable();
        sprintAction.Enable();
        attackAction.Enable();
        interactAction.Enable();

        // Subscribe to color changes
        colorNet.OnValueChanged += OnSkinColorChanged;
        
        // Apply initial color
        if (IsOwner)
        {
            // For local player, use PlayerPrefs and sync to server
            string color = PlayerPrefs.GetString("Color");
            SetSkinColor(color);
            UpdateColorServerRpc(color);
            staminaNet.Value = maxStamina;
        }
        else
        {
            // For remote players, use the networked color value
            SetSkinColor(new string(colorNet.Value.Value));
        }

        if (!IsOwner) return;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
        colorNet.OnValueChanged -= OnSkinColorChanged;
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(new string(newValue.Value));
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
        catch (KeyNotFoundException) { }
    }

    /// <summary>
    /// Saves a disconnecting player's data as long as the host keeps running.
    /// Used to resync player data on reconnect.
    /// </summary>
    /// <param name="clientId">Required, not used.</param>
    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;
        var position = transform.position;

        PlayerData playerData = new PlayerData(
            PlayerPrefs.GetString("Guid"),
            position.x,
            position.z,
            timeSpentTaggedNet.Value,
            lastTagTimeNet.Value,
            isTaggedNet.Value
        );
        SaveDataServerRpc(playerData);
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
                GUILayout.TextArea($"{player.OwnerClientId}: {displayTime:F1}s");
            }
        }
        GUILayout.EndArea();

        // Draw menu items
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        if (GUILayout.Button("Shutdown"))
        {
            if (NetworkManager.Singleton.IsHost) TagGameState.Instance.SetGameStateServerRpc(TagGameState.GameState.Stopped);
            NetworkManager.Singleton.Shutdown();
        }

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && TagGameState.Instance.gameState.Value == TagGameState.GameState.Idling && NetworkManager.Singleton.IsHost)
        {
            if (GUILayout.Button("Start Game"))
            {
                TagGameState.Instance.SetGameStateServerRpc(TagGameState.GameState.Running);
                // We have to do a lot of parsing as custom class objects are not serializable with Netcode (currently)
                var players = NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values
                .Select(playerObject => playerObject.GetComponent<PlayerTagMovement>())
                .Where(player => player != null)
                .ToList();
                var random = UnityEngine.Random.Range(0, players.Count);
                var selectedPlayer = players[random];
                SetInitialTaggedPlayerServerRpc(selectedPlayer.NetworkObjectId);
            }
        }
        GUILayout.EndArea();

        if (IsOwner)
        {
            DrawStaminaBar();
        }
    }

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

    private void Update()
    {
        if (TagGameState.Instance != null && TagGameState.Instance.gameState.Value != TagGameState.GameState.Running) return;

        // TODO : add flag instead of constant checks
        if (savedPosition.magnitude != 0f)
        {
            rb.MovePosition(savedPosition);
            savedPosition = default;
            return;
        }
        // Attempt to change gears if user presses right or left trigger
        if (USING_PLAYPULSE)
        {
            float deltaResistance = PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.RightTrigger) ? 0.2f : -0.2f;
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.LeftTrigger) 
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

        if (isTaggedNet.Value != wasTaggedLastFrame)
        {
            OnTagStatusChanged(isTaggedNet.Value);
            wasTaggedLastFrame = isTaggedNet.Value;
        }

        // Parse InputInteractions
        Vector2 input = moveAction.ReadValue<Vector2>();

        bool wantsToSprint = sprintAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.B);
        Vector3 movement = new Vector3(input.x, 0, input.y);
        isTaunting = interactAction.IsPressed() || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isPunching = attackAction.WasPerformedThisFrame() || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);
        isPunchingNet.Value = isPunching && isTaggedNet.Value;

        bool canSprint = wantsToSprint && staminaNet.Value >= minStaminaToSprint;
        isSprinting = canSprint;
        isSprintingNet.Value = isSprinting;

        // Set target player as isHit if punched by punching player
        if (isPunching && isTaggedNet.Value)
        {
            PlayerTagMovement target = FindClosestPlayerInRange(2.5f);

            if (target != null)
            {
                // Set target as tagged and hit (can tag others, play animation)
                TagPlayerServerRpc(target.NetworkObjectId);
            }
        }

        // Handle animations and update position based on input actions
        float pedalSpeed = USING_PLAYPULSE ? Math.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f) : 1f;
        float pedalAnimationSpeed = USING_PLAYPULSE ? 2 * pedalSpeed : 1f;
        movement = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ? 
        new Vector3((-1)*PlayPulse.Input.Input.JoystickX, 0, (-1)*PlayPulse.Input.Input.JoystickY) : movement;
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion lastRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
            float currentSpeed = animator.GetCurrentAnimatorStateInfo(0).speed;
            animator.speed = currentSpeed * pedalAnimationSpeed;
            isWalkingNet.Value = !isSprinting;
        }
        else
        {
            isWalkingNet.Value = false;
            isSprintingNet.Value = false;
            animator.speed = 1.0f; // Only use custom speed for walking and running anims
        }
        float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (staminaNet.Value <= 0f)
        {
            moveSpeed *= exhaustedSpeedMultiplier;
        }

        Vector3 newPosition = rb.position + movement * pedalSpeed * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        rb.MovePosition(newPosition);

        UpdateStamina(isSprinting);

        // Lastly, if neither moving or tagging, check if taunting.
        // Sets both trigger and bool value in Animator.
        // Limits animation to loop once if key is held down,
        // otherwise cancels on other actions or letting go of key
        canTaunt = !isWalkingNet.Value && !isSprinting && !isPunching;
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

    /// <summary>
    /// Sets player model color to specified hexcode.
    /// </summary>
    /// <param name="color">New player model color.</param>
    [ServerRpc]
    public void UpdateColorServerRpc(string color)
    {
        colorNet.Value = new FixedString64Bytes(color);
    }

    /// <summary>
    /// Saves Tag game data tied to a GUID on the host.
    /// Used to re-construct game state upon reconnect.
    /// </summary>
    /// <param name="playerData">Tag-specific data to save on disconnect.</param>
    [ServerRpc]
    private void SaveDataServerRpc(PlayerData playerData)
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

    private void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isHit", isHitNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
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
                taggedPlayer = (PlayerTagMovement) player;
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

    private void OnTagStatusChanged(bool isNowTagged)
    {
        float currentMaxStamina = GetCurrentMaxStamina();

        if (staminaNet.Value > currentMaxStamina)
        {
            staminaNet.Value = currentMaxStamina;
        }
    }
}
