using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class RedLightPlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speedMultiplier = 10f;
    [SerializeField] private float sprintSpeedThreshold = 5f;

    private Rigidbody rb;
    private Animator animator;
    private bool isStopped = false;
    private bool isStandaloneMode = false;
    private bool isWalkingLocal = false;
    private bool isSprintingLocal = false;

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

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            isStandaloneMode = true;
        }
    }

    private void Update()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandleStopInput();
    }

    private void FixedUpdate()
    {
        if (!isStandaloneMode && !IsOwner) return;

        HandleMovement();
    }

    private void LateUpdate()
    {
        bool walking = isStandaloneMode ? isWalkingLocal : isWalking.Value;
        bool sprinting = isStandaloneMode ? isSprintingLocal : isSprinting.Value;

        animator.SetBool("isWalking", walking);
        animator.SetBool("isSprinting", sprinting);
    }

    private void HandleStopInput()
    {
        bool stopPressed = Application.isEditor 
            ? Input.GetKeyDown(KeyCode.Space)
            : PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);

        if (stopPressed)
        {
            isStopped = !isStopped;
        }
    }

    private void HandleMovement()
    {
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
