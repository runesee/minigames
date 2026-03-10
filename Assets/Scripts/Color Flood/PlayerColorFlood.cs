using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerColorFlood : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

    [Header("Map Boundaries")]
    public float minX = -39.5f;
    public float maxX = 39.5f;
    public float minZ = -19.5f;
    public float maxZ = 19.5f;

    private readonly float walkSpeed = 5f;
    private readonly float sprintSpeedThreshold = 0.65f;

    // --- NetworkVariables ---

    private NetworkVariable<bool> isWalkingNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isSprintingNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isShowingBoostParticlesNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString64Bytes> colorNet = new NetworkVariable<FixedString64Bytes>(
        "#D6877F", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> nicknameNet = new NetworkVariable<FixedString64Bytes>(
        "Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> guidNet = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ColorFloodGameState.Team> teamNet = new NetworkVariable<ColorFloodGameState.Team>(
        ColorFloodGameState.Team.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- Component refs ---

    public ParticleSystem sprintParticleEffect;
    private InputAction moveAction;
    private InputAction sprintAction;
    private Animator animator;
    private Rigidbody rb;

    private float smoothedPedalSpeed = 0f;
    private bool usingPlayPulse = true;

    private float speedBoostTimer;
    private const float SpeedBoostDuration = 5f;
    private const float SpeedBoostMultiplier = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (!usingPlayPulse) return;
        try
        {
            if (!PlayPulse.PlayPulseService.IsInitialized)
                PlayPulse.Input.Input.ResistanceSetPoint = 0.2f;
        }
        catch { usingPlayPulse = false; }
    }

    public override void OnNetworkSpawn()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        if (sprintParticleEffect != null)
        {
            var main = sprintParticleEffect.main;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            sprintParticleEffect.Stop();
        }

        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        moveAction.Enable();
        sprintAction?.Enable();

        colorNet.OnValueChanged += OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;
        teamNet.OnValueChanged += OnTeamChanged;

        var data = LocalPlayerStorage.Load();
        string color = IsOwner ? data.color : colorNet.Value.ToString();
        SetSkinColor(color);

        if (teamNet.Value != ColorFloodGameState.Team.None)
        {
            ApplyTeamTint(teamNet.Value);
        }

        if (IsOwner)
        {
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(data.nickname);
            UpdateGuidServerRpc(data.guid);
        }
    }

    public override void OnNetworkDespawn()
    {
        colorNet.OnValueChanged -= OnSkinColorChanged;
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
        teamNet.OnValueChanged -= OnTeamChanged;
    }

    private void Update()
    {
        if (ColorFloodGameState.Instance != null &&
            ColorFloodGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;

        if (speedBoostTimer > 0)
        {
            speedBoostTimer -= Time.deltaTime;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 joystickOffset = new Vector3(input.x, 0, input.y);

        float smoothing = 1f - Mathf.Exp(-10f * Time.deltaTime);
        float inputSpeed = Mathf.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f);
        smoothedPedalSpeed = Mathf.Lerp(smoothedPedalSpeed, inputSpeed, smoothing);
        float pedalSpeed = usingPlayPulse ? smoothedPedalSpeed : 0.5f;
        float pedalAnimationSpeed = usingPlayPulse ? 1.6f * pedalSpeed : 1f;

        if (System.Math.Abs(PlayPulse.Input.Input.JoystickX) > 0.1f ||
            System.Math.Abs(PlayPulse.Input.Input.JoystickY) > 0.1f)
        {
            joystickOffset = new Vector3(
                -PlayPulse.Input.Input.JoystickX, 0, -PlayPulse.Input.Input.JoystickY);
        }

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            bool isBoosted = speedBoostTimer > 0;
            isSprintingNet.Value = isBoosted || pedalSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isBoosted || pedalSpeed > sprintSpeedThreshold;

            float moveSpeed = walkSpeed * pedalSpeed;
            if (speedBoostTimer > 0)
            {
                moveSpeed *= SpeedBoostMultiplier;
            }
            Quaternion targetRotation = Quaternion.LookRotation(joystickOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            if (animator != null) animator.speed = pedalAnimationSpeed;

            Vector3 newPosition = rb.position + moveSpeed * Time.deltaTime * joystickOffset.normalized;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            rb.MovePosition(newPosition);
        }
        else
        {
            bool isBoosted = speedBoostTimer > 0;
            isWalkingNet.Value = false;
            isSprintingNet.Value = isBoosted;
            isShowingBoostParticlesNet.Value = isBoosted;
            if (animator != null) animator.speed = 1f;
        }
    }

    private void LateUpdate()
    {
        if (animator == null) return;
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (ColorFloodGameState.Instance == null ||
            ColorFloodGameState.Instance.gameState.Value != GameState.Running) return;

        SpeedBoostPickup pickup = other.GetComponent<SpeedBoostPickup>();
        if (pickup != null)
        {
            PowerUpSpawner.Instance.CollectSpeedBoostServerRpc(pickup.pickupId);
            return;
        }

        if (teamNet.Value == ColorFloodGameState.Team.None) return;

        ColorFloodTile tile = other.GetComponent<ColorFloodTile>();
        if (tile == null) return;

        TileGrid.Instance.PaintTileServerRpc(tile.tileIndex, teamNet.Value);
    }

    [ClientRpc]
    public void GrantSpeedBoostClientRpc()
    {
        if (IsOwner)
        {
            speedBoostTimer = SpeedBoostDuration;
        }
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(newValue.ToString());
        if (teamNet.Value != ColorFloodGameState.Team.None)
        {
            ApplyTeamTint(teamNet.Value);
        }
    }

    private void SetSkinColor(string color)
    {
        if (playerSkinRenderer == null) return;
        if (ColorUtility.TryParseHtmlString(color, out Color skinColor))
            playerSkinRenderer.material.color = skinColor;
    }

    private void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (!newValue && sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
    }

    private void OnTeamChanged(ColorFloodGameState.Team previousValue, ColorFloodGameState.Team newValue)
    {
        ApplyTeamTint(newValue);
    }

    private void ApplyTeamTint(ColorFloodGameState.Team team)
    {
        if (playerSkinRenderer == null) return;
        Color tint = team switch
        {
            ColorFloodGameState.Team.Green => new Color(0.5f, 1f, 0.5f),
            ColorFloodGameState.Team.Blue => new Color(0.5f, 0.7f, 1f),
            _ => Color.white,
        };
        playerSkinRenderer.material.color = tint;
    }

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

    [ServerRpc]
    public void UpdateGuidServerRpc(string guid)
    {
        guidNet.Value = new FixedString64Bytes(guid);
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, ColorFloodGameState.Team team)
    {
        rb.position = position;
        rb.rotation = Quaternion.Euler(0f, team == ColorFloodGameState.Team.Green ? 90f : -90f, 0f);
    }

    public ColorFloodGameState.PlayerData GetPlayerData()
    {
        int tilesOwned = teamNet.Value == ColorFloodGameState.Team.Green
            ? ColorFloodGameState.Instance.greenTileCount.Value
            : ColorFloodGameState.Instance.blueTileCount.Value;

        return new ColorFloodGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            teamNet.Value,
            tilesOwned
        );
    }
}
