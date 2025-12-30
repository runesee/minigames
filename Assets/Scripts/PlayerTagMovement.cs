using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTagMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;
    public float RotateSpeed = 30f;

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
    private NetworkVariable<double> timeSpentTagged = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<double> lastTagTime = new NetworkVariable<double>(//TODO : add net name standard?
        0,
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
    private double timeSpentTaggedClient;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        //if (IsOwner) return;
        
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;
        

        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
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

        timeSpentTaggedClient = 0;
        //networkManagerController.taggedPlayerIdNet.OnValueChanged += OnTaggedPlayerChanged;
    }

    void Start()
    {
        if (IsOwner) return;
        TagGameState.Instance.taggedPlayerIdNet.OnValueChanged += OnTaggedPlayerChanged;
    }

    void OnGUI()
    {
        //if (!IsOwner) return;
        if (!TagGameState.Instance) return;

        // Draw scoreboard
        GUILayout.BeginArea(new Rect(Screen.width-210, 10, 200, 300));
        GUILayout.TextArea("Scoreboard");
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerTagMovement>();
            if (!player) continue;

            double displayTime = player.timeSpentTagged.Value;

            if (player.NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
            {
                displayTime += timeSpentTaggedClient;
                //Debug.Log($"tagged time: {displayTime}");
            }
            GUILayout.TextArea($"{player.OwnerClientId}: {displayTime:F1}s");
        }
        GUILayout.EndArea();
    }

    private void Update()
    {
        timeSpentTaggedClient += Time.deltaTime;

        if (!IsOwner) return;

        // If tagged, client does nothing until roughly 1.8 seconds have passed
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        if (isHitNet.Value)
        {
            double timeSinceTagged = serverTime - lastTagTime.Value;
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
            PlayerTagMovement target = FindClosestPlayerInRange(2f);

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
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);

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
        timeSpentTagged.Value += serverTime - lastTagTime.Value;
        victim.lastTagTime.Value = serverTime;
        timeSpentTaggedClient = 0;
        TagGameState.Instance.taggedPlayerIdNet.Value = victimId;
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
        //foreach (var networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            //var player = networkObject.GetComponent<PlayerTagMovement>();
            if (player == this) continue;

            float dist = Vector3.Distance(transform.position, ((PlayerTagMovement) player).transform.position);

            if (dist < range && dist < shortest)
            {
                shortest = dist;
                closest = (PlayerTagMovement) player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= 45f;
            }
        }
        return isWithinBounds ? closest : null;
    }

    // TODO : add xml comment
    private void OnTaggedPlayerChanged(ulong oldId, ulong newId)
    {
        timeSpentTaggedClient = 0;
    }

    // TODO : Fix GUI errors on application exit
    // TODO : Fix timers initially not synced (until two tags)
    // TODO : Code cleanup
    // TODO : Migrate to Unity UI Toolkit
    // TODO : Scoreboard UI showing before game is started.
}
