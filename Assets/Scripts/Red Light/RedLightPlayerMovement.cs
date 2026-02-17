using UnityEngine;
using Unity.Netcode;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;

        if (playerSkinRenderer != null)
        {
            originalColor = playerSkinRenderer.material.color;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            isStandaloneMode = true;
        }
    }

    private void Update()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandlePenaltyTimer();
        HandleStopInput();
        HandleFlashEffect();
    }

    private void FixedUpdate()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandleMovement();
        CheckRedLightViolation();
    }

    private void HandlePenaltyTimer()
    {
        if (isPenalized)
        {
            penaltyTimer -= Time.deltaTime;
            if (penaltyTimer <= 0f)
            {
                isPenalized = false;
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
            if (playerSkinRenderer != null)
            {
                playerSkinRenderer.material.color = originalColor;
            }
        }
        else
        {
            if (playerSkinRenderer != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                playerSkinRenderer.material.color = Color.Lerp(Color.red, originalColor, flash);
            }
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

        Vector3 newPosition = transform.position;
        newPosition.z -= penaltyPushBackDistance;
        transform.position = newPosition;

        rb.linearVelocity = Vector3.zero;

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
    }

    private void UpdateTrafficLightPosition()
    {
        Vector3 lightPosition = trafficLight.position;
        lightPosition.z = transform.position.z + trafficLightOffset;
        trafficLight.position = lightPosition;
    }

    private void HandleStopInput()
    {
        bool stopHeld = Application.isEditor
            ? Input.GetKey(KeyCode.Space)
            : PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.A);

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
        if (Application.isEditor)
        {
            return Input.GetKey(KeyCode.UpArrow) ? 0.5f : 0f;
        }

        return Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
    }
}
