using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

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
    private float currentSpeed;
    private StaminaBarUI staminaBarUI;

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

        // Give each player model a unique color
        // TODO : Use GUIDs once implemented to re-assign upon reconnect.
        try
        {
            var skinMaterial = playerSkinRenderer.material;
            UnityEngine.ColorUtility.TryParseHtmlString(playerColors[(int) OwnerClientId % playerColors.Count], out var skinColor);
            skinMaterial.color = skinColor;            
        } catch{}
        
        if (IsOwner)
        {
            staminaNet.Value = maxStamina;
        }
        
        CreateStaminaBar();
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
        if (!IsOwner) return;

        if (TagGameState.Instance != null && TagGameState.Instance.gameState.Value != TagGameState.GameState.Running)
        {
            return;
        }

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
        
        currentSpeed = movement.magnitude * moveSpeed / sprintSpeed;
        UpdateStamina(currentSpeed);

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
        
        if (staminaBarUI != null)
        {
            staminaBarUI.UpdateStamina(staminaNet.Value / maxStamina);
        }
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
    
    private void UpdateStamina(float normalizedSpeed)
    {
        if (!IsOwner) return;
        
        if (normalizedSpeed > sprintSpeedThreshold)
        {
            staminaNet.Value = Mathf.Max(0f, staminaNet.Value - staminaDrainRate * Time.deltaTime);
        }
        else if (normalizedSpeed > walkSpeedThreshold)
        {
            staminaNet.Value = Mathf.Min(maxStamina, staminaNet.Value + staminaRegenRateSlow * Time.deltaTime);
        }
        else
        {
            staminaNet.Value = Mathf.Min(maxStamina, staminaNet.Value + staminaRegenRateFast * Time.deltaTime);
        }
    }
    
    private void CreateStaminaBar()
    {
        if (!IsOwner) return;
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("StaminaBarsCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        GameObject staminaBarObject = new GameObject("StaminaBar");
        staminaBarObject.transform.SetParent(canvas.transform, false);
        
        RectTransform barRect = staminaBarObject.AddComponent<RectTransform>();
        barRect.sizeDelta = new Vector2(200f, 20f);
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(1f, 0f);
        barRect.anchoredPosition = new Vector2(-20f, 20f);
        
        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(staminaBarObject.transform, false);
        RectTransform bgRect = backgroundObject.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image bgImage = backgroundObject.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(staminaBarObject.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image fillImage = fillObject.AddComponent<UnityEngine.UI.Image>();
        fillImage.type = UnityEngine.UI.Image.Type.Filled;
        fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fillImage.color = Color.green;
        
        staminaBarUI = staminaBarObject.AddComponent<StaminaBarUI>();
        
        System.Reflection.FieldInfo fillImageField = typeof(StaminaBarUI).GetField("fillImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fillImageField != null)
        {
            fillImageField.SetValue(staminaBarUI, fillImage);
        }
    }
}
