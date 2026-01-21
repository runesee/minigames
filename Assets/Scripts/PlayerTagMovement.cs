using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using System;
using static TagSessionState;
using PlayPulse.Api.Utils;
using UnityEngine.UIElements;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTagMovement : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;
    public float RotateSpeed = 30f;
    
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        // Set player color to the one determined by PlayerPrefs
        colorNet.OnValueChanged += OnSkinColorChanged;
        string color = PlayerPrefs.GetString("Color");
        SetSkinColor(color);

        if (!IsOwner) return;
        UpdateColorServerRpc(color);
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;  // IDEA / TODO : Try owner or not
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(new string(newValue.Value));
    }

    private void SetSkinColor(string color)
    {
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        playerSkinRenderer.material.color = skinColor;
    }

    // TODO : xml comment & move
    [ServerRpc]
    public void UpdateColorServerRpc(string color)
    {
        colorNet.Value = new FixedString64Bytes(color);
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
        colorNet.OnValueChanged -= OnSkinColorChanged;
    }

    private void OnClientConnect(ulong clientId)
    {
        if (!TagSessionState.Instance) return;

        try
        {
            FixedString64Bytes guid = new FixedString64Bytes(PlayerPrefs.GetString("Guid"));
            if (TagSessionState.Instance.playerData.Value.Keys.Contains(guid)) {
                PlayerData playerData = TagSessionState.Instance.playerData.Value[guid];
                savedPosition = new Vector3(playerData.XPos, 1f, playerData.ZPos);
                ResyncPlayerDataServerRpc(playerData);
            }
        }
        catch (KeyNotFoundException) {}
    }

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

    [ServerRpc]
    private void SaveDataServerRpc(PlayerData playerData)
    {
        TagSessionState.Instance.SaveDataServerRpc(playerData);
    }

    // There is arguably a lot of logic in onGUI, which runs often.
    // TODO : Move this logic when a better UI solution is in place.
    void OnGUI()
    {
        if (!TagGameState.Instance || !NetworkManager.Singleton) return;

        // Draw scoreboard
        GUILayout.BeginArea(new Rect(Screen.width-210, 10, 200, 300));
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
                var random  = UnityEngine.Random.Range(0, players.Count);
                var selectedPlayer = players[random];
                SetInitialTaggedPlayerServerRpc(selectedPlayer.NetworkObjectId);
            }
        }
        GUILayout.EndArea();
    }

    private void Update()
    {
        

        if (TagGameState.Instance != null && TagGameState.Instance.gameState.Value != TagGameState.GameState.Running)
        {
            return;
        }

        if (savedPosition.magnitude != 0f) {
            rb.MovePosition(savedPosition);
            Debug.Log(savedPosition.x);
            savedPosition = default;
            return;
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
        isSprinting = sprintAction.IsPressed();
        Vector3 movement = new Vector3(input.x, 0, input.y);
        isTaunting = interactAction.ReadValue<float>() > 0f;
        isPunching = attackAction.WasPerformedThisFrame();
        isPunchingNet.Value = isPunching && isTaggedNet.Value;
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
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion lastRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
            isWalkingNet.Value = !isSprinting;
        }
        else
        {
          isWalkingNet.Value = false;
          isSprintingNet.Value = false;
        } 
        float moveSpeed =  isSprinting ? sprintSpeed : walkSpeed;
        Vector3 newPosition = rb.position + movement * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        rb.MovePosition(newPosition);

        // Lastly, if neither moving or tagging, check if taunting.
        // Sets both trigger and bool value in Animator.
        // Limits animation to loop once if key is held down,
        // otherwise cancels on other actions or letting go of key
        canTaunt = !isWalkingNet.Value && !isSprinting && !isPunching;
        if (interactAction.WasPressedThisFrame() && canTaunt)
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


    // TODO : XML comment
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
        TagGameState.Instance.taggedPlayerIdNet.Value = victimId; // TODO : change victimID to GUID
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
        foreach (var player in FindObjectsByType(typeof(PlayerTagMovement), FindObjectsSortMode.None))
        {
            if (player == this) continue;

            float distance = Vector3.Distance(transform.position, ((PlayerTagMovement) player).transform.position);

            if (distance < range && distance < shortest)
            {
                shortest = distance;
                closest = (PlayerTagMovement) player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= (distance > range / 2 ? 70f : 45f);
            }
        }
        return isWithinBounds ? closest : null;
    }
}
