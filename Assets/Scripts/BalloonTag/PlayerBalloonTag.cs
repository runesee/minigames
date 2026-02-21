using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerBalloonTag : NetworkBehaviour
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
    public NetworkVariable<FixedString64Bytes> colorNet = new NetworkVariable<FixedString64Bytes>(
    "#D6877F",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public NetworkVariable<BalloonState> balloonsNet = new NetworkVariable<BalloonState>(
    new BalloonState(2, "#D6877F"),
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

    public ParticleSystem sprintParticleEffect;
    public List<GameObject> BalloonPrefabs;
    private InputAction attackAction;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private Animator animator;
    private Rigidbody rb;

    private bool isPunching;
    private bool isTaunting;
    private bool canTaunt;
    private float smoothedPedalSpeed = 0f;
    private bool USING_PLAYPULSE = false; // Flag for dev/bike movement toggling.
    private readonly float sprintSpeedThreshold = 0.65f;

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
        moveAction.Enable();
        sprintAction.Enable();
        attackAction.Enable();
        interactAction.Enable();

        // Subscribe to color and sprint particle changes
        colorNet.OnValueChanged += OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;
        balloonsNet.OnValueChanged += OnBalloonsChanged;

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
    }

    public override void OnNetworkDespawn()
    {
        colorNet.OnValueChanged -= OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
        balloonsNet.OnValueChanged -= OnBalloonsChanged;
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
    private void SetSkinColor(string color)
    {
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        playerSkinRenderer.material.color = skinColor;
        BalloonPrefabs[0].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        BalloonPrefabs[1].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        if (IsOwner) InitializeBalloonsServerRpc(color);
    }

    public BalloonTagGameState.PlayerData GetTagData()
    {
        BalloonTagGameState.PlayerData playerData = new BalloonTagGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            balloonsNet.Value.count,
            lastTagTimeNet.Value
        );
        return playerData;
    }

    // There is arguably a lot of logic in onGUI, which runs often.
    // TODO : Move this logic when a better UI solution is in place.
    void OnGUI()
    {
        if (!BalloonTagGameState.Instance || !NetworkManager.Singleton) return;

        // Draw scoreboard
        GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 300));
        if (BalloonTagGameState.Instance.gameState.Value == GameState.Running)
        {
            GUILayout.TextArea("Scoreboard");
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                var player = obj.GetComponent<PlayerBalloonTag>();
                if (!player) continue;
                
                string playerName = player.nicknameNet.Value.ToString();
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = $"Player{player.OwnerClientId}";
                }
                var balloons = player.balloonsNet.Value.count;
                var balloon_label = (balloons != 1) ? "Balloons" : "Balloon";
                GUILayout.TextArea($"{playerName}: {balloons.ToString():F1} {balloon_label}");
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && BalloonTagGameState.Instance.gameState.Value == GameState.Idling && NetworkManager.Singleton.IsHost)
        {
            if (GUILayout.Button("Start Game"))
            {
                BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Running);
            }
        }
        GUILayout.EndArea();
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

        if (isPunching && !isFrozen.Value)
        {
            PlayerBalloonTag target = FindClosestPlayerInRange(2.5f);
            if (target != null) TagPlayerServerRpc(target.NetworkObjectId);
        }
        else if (isFrozen.Value)
        {
            double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
            double timeSinceTagged = serverTime - lastTagTimeNet.Value;
            if (timeSinceTagged >= 0.7f) UnfreezePlayerServerRpc();
        }

        // Handle animations and update position based on input actions
        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, PlayPulse.Input.Input.Speed, smoothing);
        float pedalSpeed = USING_PLAYPULSE ? smoothedPedalSpeed : 0.4f;
        float pedalAnimationSpeed = USING_PLAYPULSE ? 1.6f * pedalSpeed : 1f;
        joystickOffset = (Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f || Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f) ?
        new Vector3((-1) * PlayPulse.Input.Input.JoystickX, 0, (-1) * PlayPulse.Input.Input.JoystickY) : joystickOffset;

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            // Play running animation if movement speed above threshold
            isSprintingNet.Value = pedalSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isSprintingNet.Value;
            float moveSpeed = 5f * pedalSpeed;

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

    private void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
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
 
    [ServerRpc]
    public void InitializeBalloonsServerRpc(FixedString64Bytes color)
    {
        balloonsNet.Value = new BalloonState(2, color);
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
        this.isFrozen.Value = true;
        this.timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
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

    /// <summary>
    /// Helper function for getting the closest player within range and field of view, if any.
    /// </summary>
    /// <param name="range"></param> Limit for how far a player can tag.
    /// <returns></returns> A PlayerTagMovement object or null.
    private PlayerBalloonTag FindClosestPlayerInRange(float range)
    {
        PlayerBalloonTag closest = null;
        float shortest = Mathf.Infinity;
        bool isWithinBounds = false;
        GameObject _player = new GameObject();
        PlayerBalloonTag taggedPlayer = this;
        foreach (var player in FindObjectsByType(typeof(PlayerBalloonTag), FindObjectsSortMode.None))
        {
            if (player == this) continue;

            float distance = Vector3.Distance(transform.position, ((PlayerBalloonTag)player).transform.position);

            if (distance < range && distance < shortest)
            {
                shortest = distance;
                closest = (PlayerBalloonTag)player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= (distance > range / 2 ? 70f : 45f);
                taggedPlayer = (PlayerBalloonTag)player;
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
}
