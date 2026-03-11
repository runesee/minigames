using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerCtF : NetworkBehaviour
{
    public static PlayerCtF Local;
    [SerializeField] public SkinnedMeshRenderer playerSkinRenderer;
    [SerializeField] public GameObject playerShadow;
    [SerializeField] public Material playerShadowColor;
    [SerializeField] public GameObject flag;
    [SerializeField] public MeshRenderer flagColor;
    [SerializeField] public Material blueColor;
    [SerializeField] public Material greenColor;

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
    private NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(
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
    public NetworkVariable<double> lastRespawnTimeNet = new NetworkVariable<double>(
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
    public NetworkVariable<Team> teamNet = new NetworkVariable<Team>(
        Team.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<bool> isFlagActiveNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<Team> currentZoneNet = new NetworkVariable<Team>(
        Team.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> collectedFlagsNet = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public ParticleSystem sprintParticleEffect;
    public Team? currentStartZone;
    public Team? currentFlagZone;
    public AudioSource audioSource;
    public AudioClip tagClip;
    public AudioClip plopClip;
    public AudioClip scoreClip;
    public AudioClip enemyScoreClip;
    public AudioClip flagTakenClip;
    public AudioClip taggedClip;
    public AudioClip flagReturnedClip;
    public Camera mainCamera;
    private GameObject greenFlag;
    private GameObject blueFlag;
    private GameObject greenFlagFabric;
    private GameObject blueFlagFabric;
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
    private bool USING_PLAYPULSE = true; // Flag for dev/bike movement toggling.
    private readonly float sprintSpeedThreshold = 0.65f;

    public enum Team
    {
        None,
        Green,
        Blue,
    }

    public enum CtfClips
    {
        Tag,
        Plop,
        Score,
        Taken,
        Tagged,
        Returned,
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Local = this;
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;
        mainCamera = FindFirstObjectByType<Camera>();

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
        teamNet.OnValueChanged += OnTeamChanged;
        isFlagActiveNet.OnValueChanged += OnFlagChanged;
        flag.SetActive(false);

        greenFlag = GameObject.Find("GreenFlag");
        blueFlag = GameObject.Find("BlueFlag");
        greenFlagFabric = GameObject.Find("GreenFabric");
        blueFlagFabric = GameObject.Find("BlueFabric");
        currentFlagZone = Team.None;

        if (IsOwner)
        {
            playerShadow.SetActive(true);
            playerShadow.GetComponentInChildren<MeshRenderer>().material.color = playerShadowColor.color;
        } 

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
            StartCoroutine(ZoomCamera());
        }
        if (IsHost) StartCoroutine(WaitForPlayerConnect());
    }

    private System.Collections.IEnumerator WaitForPlayerConnect()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || CtFGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(8f);
        CtFGameState.Instance.SetGameStateServerRpc(GameState.Running);
    }

    private System.Collections.IEnumerator ZoomCamera()
    {
        yield return new WaitForSeconds(8f);
        mainCamera.orthographicSize = 10;
        mainCamera.transform.position = new Vector3(Math.Clamp(rb.position.x, -18f, 18f), 20f, -20f);
    }

    public override void OnNetworkDespawn()
    {
        colorNet.OnValueChanged -= OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
        isFlagActiveNet.OnValueChanged -= OnFlagChanged;
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(newValue.Value.ToString());
    }

    private void OnFlagChanged(bool previousValue, bool newValue)
    {
        flag.SetActive(newValue);
    }

    private void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
    }

    private void OnTeamChanged(Team previousValue, Team newValue)
    {
        playerSkinRenderer.material.color = newValue == Team.Green ? greenColor.color : blueColor.color;
        if (IsOwner)
        {
            playerSkinRenderer.material.color = playerSkinRenderer.material.color * 2f;
            playerSkinRenderer.material.EnableKeyword("_EMISSION");
            playerSkinRenderer.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            SetCurrentZoneServerRpc(newValue);
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner || NetworkManager.Singleton == null) return;
        var zone = other.GetComponent<TeamZone>();
        var startZone = other.GetComponent<StartZone>();
        var flagZone = other.GetComponent<FlagZone>();
        var currentZone = Team.None; 
        if (zone != null)
        {
            currentZone = zone.team;
            SetCurrentZoneServerRpc(currentZone);
        }
        if (startZone != null)
        {
            currentStartZone = startZone.zone;
            if (teamNet.Value == currentZoneNet.Value && teamNet.Value == currentStartZone && isFlagActiveNet.Value)
            {
                string enemyFlag = teamNet.Value == Team.Blue ? "GreenFlag" : "BlueFlag";
                ScoreFlagServerRpc(teamNet.Value, enemyFlag);
            }
        } 
        else if (flagZone != null)
        {
            currentFlagZone = flagZone.zone;
            GameObject currentFlag = teamNet.Value == Team.Green ? blueFlag : greenFlag;
            MeshRenderer currentFlagFabric = teamNet.Value == Team.Green ? blueFlagFabric.GetComponentInChildren<MeshRenderer>() : greenFlagFabric.GetComponentInChildren<MeshRenderer>();
            if (teamNet.Value != flagZone.zone && currentFlag.activeSelf)
            {
                Color baseColor = teamNet.Value == Team.Green ? blueColor.color : greenColor.color;
                currentFlagFabric.material.EnableKeyword("_EMISSION");
                currentFlagFabric.material.SetColor("_EmissionColor", baseColor * 2f);
                currentFlagFabric.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var flagZone = other.GetComponent<FlagZone>();
        if (flagZone != null)
        {
            currentFlagZone = Team.None;
            GameObject currentFlagFabric = teamNet.Value == Team.Green ? blueFlagFabric : greenFlagFabric;
            Material currentFlagFabricMaterial = currentFlagFabric.GetComponentInChildren<MeshRenderer>().material;
            currentFlagFabricMaterial.DisableKeyword("_EMISSION");
            currentFlagFabricMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
    }

    public CtFGameState.PlayerData GetTagData()
    {
        CtFGameState.PlayerData playerData = new CtFGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            teamNet.Value,
            collectedFlagsNet.Value,
            lastTagTimeNet.Value
        );
        return playerData;
    }

    private void Update()
    {
        if (CtFGameState.Instance != null && CtFGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        if (serverTime - lastRespawnTimeNet.Value >= 3f && isFrozen.Value && IsOwner) RespawnPlayerServerRpc();
        else if (isFrozen.Value) return;

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
            if (currentFlagZone != Team.None && currentFlagZone != teamNet.Value && currentZoneNet.Value != teamNet.Value)
            {
                string enemyFlag = teamNet.Value == Team.Blue ? "GreenFlag" : "BlueFlag";
                audioSource?.PlayOneShot(plopClip);
                TakeFlagServerRpc(enemyFlag);
            }
            else
            {
                PlayerCtF target = FindClosestPlayerInRange(2.5f);
                audioSource?.PlayOneShot(tagClip);
                if (target != null) TagPlayerServerRpc(target.NetworkObjectId);
            }
        }
        else if (isFrozen.Value)
        {
            double timeSinceTagged = serverTime - lastTagTimeNet.Value;
            if (timeSinceTagged >= 0.7f) UnfreezePlayerServerRpc();
        }

        // Handle animations and update position based on input actions
        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, Math.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f), smoothing);
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
            float moveSpeed = 10f * pedalSpeed;

            Quaternion lastRotation = Quaternion.LookRotation(joystickOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastRotation, 10f * Time.deltaTime);
            animator.speed = pedalAnimationSpeed;

            Vector3 newPosition = rb.position + moveSpeed * Time.deltaTime * joystickOffset.normalized;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            rb.MovePosition(newPosition);

            if (IsOwner)
            {
                Vector3 desiredPosition =  new Vector3(Math.Clamp(rb.position.x, -18f, 18f), 20f, -20f);
                Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, Time.deltaTime * moveSpeed);
                mainCamera.transform.position = smoothedPosition;
            } 
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
    public void SetCurrentZoneServerRpc(Team zone)
    {
        currentZoneNet.Value = zone;
    }

    [ServerRpc]
    public void UpdateGuidServerRpc(string guid)
    {
        guidNet.Value = guid;
    }

    /// <summary>
    /// Plays the tagged sound only for the individual player that was tagged.
    /// </summary>
    /// <param name="clientRpcParams"></param>()
    [ClientRpc]
    private void PlayTaggedSoundClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!audioSource.isPlaying) audioSource?.PlayOneShot(taggedClip);
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
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId].GetComponent<PlayerCtF>();
        ulong victimClientId = victim.OwnerClientId;

        if ((this.currentZoneNet.Value == this.teamNet.Value && victim.currentZoneNet.Value == this.teamNet.Value) || victim.isFlagActiveNet.Value)
        {
            victim.isRespawning.Value = true;
            victim.lastRespawnTimeNet.Value = serverTime;
            victim.currentZoneNet.Value = victim.teamNet.Value;
            if (victim.isFlagActiveNet.Value)
            {
                victim.isFlagActiveNet.Value = false;
                TogglePlacedFlagClientRpc(this.teamNet.Value.ToString() + "Flag", true);
                if (IsServer)
                {
                    CtFGameState.Instance.ToastMessageClientRpc(teamNet.Value, teamNet.Value.ToString() + " flag was returned!");
                    CtFGameState.Instance.PlaySoundClientRpc(teamNet.Value, CtfClips.Returned);
                }
            }
            victim.TeleportClientRpc(new Vector3(victim.teamNet.Value == Team.Blue ? -34.5f : 34.5f, victim.rb.position.y, victim.rb.position.z));
            PlayTaggedSoundClientRpc(new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = new ulong[] {victimClientId}}});
            PlaySoundClientRpc(CtfClips.Tag, Team.None);
        }
        if (IsOwner) StopAnimationsClientRpc();

        // Add timediff to current player and prevent tagging again for another .7 seconds
        this.isFrozen.Value = true;
        this.timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
        this.lastTagTimeNet.Value = serverTime;
    }

    [ClientRpc]
    private void PlaySoundClientRpc(CtfClips clip, Team team)
    {
        if (!audioSource.isPlaying)
        {
            switch(clip)
            {
                case CtfClips.Tag:
                    audioSource?.PlayOneShot(tagClip);
                    break;
                case CtfClips.Plop:
                    audioSource?.PlayOneShot(plopClip);
                    break;
                case CtfClips.Taken:
                    if (teamNet.Value != team) audioSource?.PlayOneShot(flagTakenClip);
                    break;
            }
        } 
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
    /// Re-enable user actions after freeze period.
    /// </summary>
    [ServerRpc]
    private void RespawnPlayerServerRpc()
    {
        isRespawning.Value = false;
    }

    [ClientRpc]
    void StopAnimationsClientRpc()
    {
        if (!IsOwner) return;
        isWalkingNet.Value = false;
        isSprintingNet.Value = false;
        isTauntingNet.Value = false;
        isPunchingNet.Value = false;
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position)
    {
        rb.position = position;
        rb.rotation = UnityEngine.Quaternion.Euler(0f, teamNet.Value == Team.Green ? -90f : 90f, 0f);
        if (IsOwner) mainCamera.transform.position = new Vector3(Math.Clamp(position.x, -18f, 18f), 20f, -20f);
    }

    [ClientRpc]
    public void TogglePlacedFlagClientRpc(string flagName, bool active)
    {
        if (flagName == "GreenFlag") greenFlag.SetActive(active);
        else blueFlag.SetActive(active);
        if (!active)
        {
            flagColor.material.EnableKeyword("_EMISSION");
            flagColor.material.SetColor("_EmissionColor", teamNet.Value == Team.Green ? blueColor.color * 1.25f : greenColor.color * 1.25f);
            flagColor.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    [ServerRpc]
    public void TakeFlagServerRpc(string flagName)
    {
        isFlagActiveNet.Value = true;
        TogglePlacedFlagClientRpc(flagName, false);
        Team _team = teamNet.Value == Team.Green ? Team.Blue : Team.Green;
        PlaySoundClientRpc(CtfClips.Taken, _team);
        if (IsServer) CtFGameState.Instance.ToastMessageClientRpc(_team, _team.ToString() + " flag was taken!");
    }

    [ServerRpc]
    public void ScoreFlagServerRpc(Team team, string flagName)
    {
        if (!isFlagActiveNet.Value) return;
        isFlagActiveNet.Value = false;
        this.collectedFlagsNet.Value++;
        
        int score;
        if (team == Team.Green)
        {
            CtFGameState.Instance.greenScore.Value++;
            score = CtFGameState.Instance.greenScore.Value;
            CtFGameState.Instance.PlaySoundClientRpc(Team.Green, CtfClips.Score);
        }
        else
        {
            CtFGameState.Instance.blueScore.Value++;
            score = CtFGameState.Instance.blueScore.Value;
            CtFGameState.Instance.PlaySoundClientRpc(Team.Blue, CtfClips.Score);
        }
        TogglePlacedFlagClientRpc(flagName, true);
        if (IsServer)
        {
            CtFGameState.Instance.UpdateScoreTextClientRpc(team, score);
            CtFGameState.Instance.ToastMessageClientRpc(team, team.ToString() + " scored a point!");
        } 
    }

    /// <summary>
    /// Helper function for getting the closest player within range and field of view, if any.
    /// </summary>
    /// <param name="range"></param> Limit for how far a player can tag.
    /// <returns></returns> A PlayerTagMovement object or null.
    private PlayerCtF FindClosestPlayerInRange(float range)
    {
        PlayerCtF closest = null;
        float shortest = Mathf.Infinity;
        bool isWithinBounds = false;
        GameObject _player = new GameObject();
        PlayerCtF taggedPlayer = this;
        foreach (var player in FindObjectsByType(typeof(PlayerCtF), FindObjectsSortMode.None))
        {
            PlayerCtF playerCtf = (PlayerCtF) player;
            if (player == this || playerCtf.teamNet.Value == this.teamNet.Value) continue;

            float distance = Vector3.Distance(transform.position, ((PlayerCtF)player).transform.position);

            if (distance < range && distance < shortest)
            {
                shortest = distance;
                closest = (PlayerCtF)player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= (distance > range / 2 ? 70f : 45f);
                taggedPlayer = (PlayerCtF)player;
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
