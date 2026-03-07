using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

[RequireComponent(typeof(Rigidbody))]
public class RedLightPlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speedMultiplier = 10f;
    [SerializeField] private float sprintSpeedThreshold = 5f;

    [Header("Penalty Settings")]
    [SerializeField] private float penaltyPushBackDistance = 3f;
    [SerializeField] private float penaltyFreezeDuration = 1.5f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private Transform trafficLight;
    [SerializeField] private float trafficLightOffset = 5f;
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip errorClip;

    private Rigidbody rb;
    private Animator animator;
    private bool isStopped = false;
    private bool isStandaloneMode = false;
    private bool isWalkingLocal = false;
    private bool isSprintingLocal = false;
    private bool isPenalized = false;
    private float penaltyTimer = 0f;
    private Color originalColor;
    private float flashTimer = 0f;
    private bool isFlashing = false;
    private bool usingPlayPulse = true;
    private float startPositionZ = 0f;

    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> isSprinting = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> isPenalizedNet = new NetworkVariable<bool>(
        false,
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

    public NetworkVariable<float> distanceTraveledNet = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        startPositionZ = transform.position.z;

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (usingPlayPulse)
        {
            try
            {
                if (!PlayPulse.PlayPulseService.IsInitialized)
                {
                    usingPlayPulse = false;
                }
            }
            catch
            {
                usingPlayPulse = false;
            }
        }

        colorNet.OnValueChanged += OnSkinColorChanged;
        isPenalizedNet.OnValueChanged += OnPenaltyStateChanged;

        string color = IsOwner ? PlayerPrefs.GetString("Color") : colorNet.Value.ToString();
        SetSkinColor(color);

        if (IsOwner)
        {
            string nickname = PlayerPrefs.GetString("Username", "Player");
            string guid = PlayerPrefs.GetString("Guid");
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(nickname);
            UpdateGuidServerRpc(guid);

            RedLightCameraFollow cameraFollow = Camera.main?.GetComponent<RedLightCameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        colorNet.OnValueChanged -= OnSkinColorChanged;
        isPenalizedNet.OnValueChanged -= OnPenaltyStateChanged;
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            isStandaloneMode = true;
            
            startPositionZ = transform.position.z;
            
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            originalColor = playerSkinRenderer.material.color;
        }
    }

    private void Update()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandlePenaltyTimer();
        HandleStopInput();
    }

    private void FixedUpdate()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandleMovement();
        CheckRedLightViolation();
        UpdateDistanceTraveled();
    }

    private void UpdateDistanceTraveled()
    {
        if (isStandaloneMode) return;

        distanceTraveledNet.Value = GetTraveledDistance();
    }

    private void HandlePenaltyTimer()
    {
        if (isPenalized)
        {
            penaltyTimer -= Time.deltaTime;
            if (penaltyTimer <= 0f)
            {
                isPenalized = false;
                
                if (IsServer)
                {
                    isPenalizedNet.Value = false;
                }
            }
        }
    }

    private void HandleFlashEffect()
    {
        if (!isFlashing) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            isFlashing = false;
            playerSkinRenderer.material.color = originalColor;
        }
        else
        {
            float flash = Mathf.PingPong(Time.time * 10f, 1f);
            playerSkinRenderer.material.color = Color.Lerp(Color.red, originalColor, flash);
        }
    }

    private void CheckRedLightViolation()
    {
        if (isPenalized || RedLightManager.Instance == null) return;

        bool isRedLight = RedLightManager.Instance.IsRedLight;
        float currentSpeed = Mathf.Abs(rb.linearVelocity.z);

        if (isRedLight && currentSpeed > movementThreshold)
        {
            ApplyPenalty();
        }
    }

    private void ApplyPenalty()
    {
        isPenalized = true;
        penaltyTimer = penaltyFreezeDuration;
        audioSource?.PlayOneShot(errorClip);

        Vector3 newPosition = transform.position;
        newPosition.z -= penaltyPushBackDistance;
        transform.position = newPosition;

        rb.linearVelocity = Vector3.zero;

        ApplyPenaltyServerRpc();
    }

    [ServerRpc]
    private void ApplyPenaltyServerRpc()
    {
        isPenalizedNet.Value = true;
        StartFlashEffectClientRpc();
    }

    [ClientRpc]
    private void StartFlashEffectClientRpc()
    {
        StartFlashEffect();
    }

    private void StartFlashEffect()
    {
        isFlashing = true;
        flashTimer = penaltyFreezeDuration;
    }

    private void LateUpdate()
    {
        bool walking = isStandaloneMode ? isWalkingLocal : isWalking.Value;
        bool sprinting = isStandaloneMode ? isSprintingLocal : isSprinting.Value;

        animator.SetBool("isWalking", walking);
        animator.SetBool("isSprinting", sprinting);

        UpdateTrafficLightPosition();
        HandleFlashEffect();
    }

    private void UpdateTrafficLightPosition()
    {
        if (trafficLight == null) return;

        Vector3 lightPosition = trafficLight.position;
        lightPosition.z = transform.position.z + trafficLightOffset;
        trafficLight.position = lightPosition;
    }

    private void HandleStopInput()
    {
        bool stopHeld;
        
        if (usingPlayPulse)
        {
            stopHeld = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.A);
        }
        else
        {
            stopHeld = Input.GetKey(KeyCode.Space);
        }

        isStopped = stopHeld;
    }

    private void HandleMovement()
    {
        if (isPenalized) return;

        float pedalInput = GetPedalInput();
        float speed = isStopped ? 0f : pedalInput * speedMultiplier;

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, speed);

        UpdateAnimationState(speed, pedalInput);
    }

    private void UpdateAnimationState(float speed, float pedalInput)
    {
        bool isMoving = pedalInput > 0.01f && !isStopped;
        bool shouldSprint = isMoving && speed > sprintSpeedThreshold;

        if (isStandaloneMode)
        {
            isWalkingLocal = isMoving && !shouldSprint;
            isSprintingLocal = shouldSprint;
        }
        else
        {
            isWalking.Value = isMoving && !shouldSprint;
            isSprinting.Value = shouldSprint;
        }

        animator.speed = isMoving ? pedalInput * 1.6f : 1.0f;
    }

    private float GetPedalInput()
    {
        if (usingPlayPulse)
        {
            return Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
        }
        else
        {
            return Input.GetKey(KeyCode.UpArrow) ? 0.5f : 0f;
        }
    }

    private void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(newValue.Value.ToString());
    }

    private void OnPenaltyStateChanged(bool previousValue, bool newValue)
    {
        if (newValue && !isFlashing)
        {
            StartFlashEffect();
        }
    }

    private void SetSkinColor(string color)
    {
        if (ColorUtility.TryParseHtmlString(color, out var skinColor))
        {
            playerSkinRenderer.material.color = skinColor;
            originalColor = skinColor;
        }
    }

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

    public void AssignTrafficLight(TrafficLightController light)
    {
        if (!IsServer) return;
        
        trafficLight = light.transform;
        AssignTrafficLightClientRpc(light.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ClientRpc]
    private void AssignTrafficLightClientRpc(ulong lightNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(lightNetworkObjectId, out var networkObject))
        {
            TrafficLightController controller = networkObject.GetComponent<TrafficLightController>();
            if (controller != null)
            {
                trafficLight = controller.transform;
            }
        }
    }

    public float GetTraveledDistance()
    {
        return transform.position.z - startPositionZ;
    }

    public float GetCurrentPosition()
    {
        return transform.position.z;
    }

    public RedLightGameState.PlayerData GetPlayerData()
    {
        float distance = isStandaloneMode ? GetTraveledDistance() : distanceTraveledNet.Value;
        
        return new RedLightGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            distance
        );
    }
}
