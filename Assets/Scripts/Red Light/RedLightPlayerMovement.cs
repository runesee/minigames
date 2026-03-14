using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class RedLightPlayerMovement : Player
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
    [SerializeField] private GameObject track;
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
        startPositionZ = transform.position.z;

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        colorNet.OnValueChanged += OnSkinColorChanged;
        isPenalizedNet.OnValueChanged += OnPenaltyStateChanged;

        var data = LocalPlayerStorage.Load();
        string color = IsOwner ? data.color : colorNet.Value.ToString();
        SetSkinColor(color);

        if (IsOwner)
        {
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(data.nickname);
            UpdateGuidServerRpc(data.guid);

            RedLightCameraFollow cameraFollow = Camera.main?.GetComponent<RedLightCameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
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

            originalColor = PlayerSkinRenderer.material.color;
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
            PlayerSkinRenderer.material.color = originalColor;
        }
        else
        {
            float flash = Mathf.PingPong(Time.time * 10f, 1f);
            PlayerSkinRenderer.material.color = Color.Lerp(Color.red, originalColor, flash);
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
        if (newPosition.z - penaltyPushBackDistance > -13f) newPosition.z -= penaltyPushBackDistance;
        else newPosition.z -= newPosition.z + 13f;
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
        
        if (MinigameManager.USING_PLAYPULSE)
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
        if (MinigameManager.USING_PLAYPULSE)
        {
            return Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
        }
        else
        {
            return Input.GetKey(KeyCode.UpArrow) ? 0.5f : 0f;
        }
    }

    private void OnPenaltyStateChanged(bool previousValue, bool newValue)
    {
        if (newValue && !isFlashing)
        {
            StartFlashEffect();
        }
    }

    protected override void SetSkinColor(string color)
    {
        base.SetSkinColor(color);
        if (ColorUtility.TryParseHtmlString(color, out var skinColor))
        {
            originalColor = skinColor;
        }
    }

    public void AssignTrafficLightAndTrack(TrafficLightController light, int playerIndex)
    {
        if (!IsServer) return;
        
        trafficLight = light.transform;
        AssignTrafficLightClientRpc(light.GetComponent<NetworkObject>().NetworkObjectId);
        HighlightTrackClientRpc(playerIndex);
    }

    [ClientRpc]
    private void HighlightTrackClientRpc(int playerIndex)
    {
        if (!IsOwner) return;
        this.track = RedLightPlayerSpawner.Instance.tracks[playerIndex];
        StartCoroutine(HighlightTrack());
    }

    private IEnumerator HighlightTrack()
    {
        yield return new WaitForSeconds(1f);
        Color trackColor = track.GetComponentInChildren<MeshRenderer>().material.color;
        ColorUtility.TryParseHtmlString(colorNet.Value.ToString(), out var skinColor);
        track.GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        yield return new WaitForSeconds(0.3f);
        track.GetComponentInChildren<MeshRenderer>().material.color = trackColor;
        yield return new WaitForSeconds(0.3f);
        track.GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        yield return new WaitForSeconds(0.3f);
        track.GetComponentInChildren<MeshRenderer>().material.color = trackColor;
        yield return new WaitForSeconds(0.3f);
        track.GetComponentInChildren<MeshRenderer>().material.color = skinColor;
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

    public override PlayerData GetPlayerData()
    {
        float distance = isStandaloneMode ? GetTraveledDistance() : distanceTraveledNet.Value;
        return new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            distance,
            distance
        );
    }
}
