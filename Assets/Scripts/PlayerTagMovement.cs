using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTagMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;

    public float stunDuration = 3f;

    private Animator animator;

    private NetworkVariable<bool> isWalkingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isSprintingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private InputAction attackAction;

    private bool isPunching;
    private bool isTaunting;
    private bool canTaunt;

    private NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isHitNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    public float RotateSpeed = 30f;

    private InputAction moveAction;
    private InputAction sprintAction;

    private InputAction interactAction;

    private bool isSprinting;

    private Rigidbody rb;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;
        
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }

        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        attackAction = InputSystem.actions.FindAction("Attack");
        interactAction = InputSystem.actions.FindAction("Interact");
        moveAction.Enable();
        sprintAction.Enable();
        attackAction.Enable();
        interactAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Parse InputInteractions
        Vector2 input = moveAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();
        Vector3 movement = new Vector3(input.x, 0, input.y);
        isTaunting = interactAction.ReadValue<float>() > 0f;
        isPunching = attackAction.WasPerformedThisFrame();
        isPunchingNet.Value = isPunching;
        isSprintingNet.Value = isSprinting;

        // Set target player as isHit if punched by punching player
        if (isPunching) // TODO: Check if player also 'has it'
        {
            PlayerTagMovement target = FindClosestPlayerInRange(2f);

            if (target != null)
            {
                SendHitToServerRpc(target.NetworkObjectId);
            }
        }

        // Handle animations and update position based on input actions
        if (movement.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            isWalkingNet.Value = !isSprinting;
        }
        else
        {
          isWalkingNet.Value = false;
          isSprintingNet.Value = false;
        } 
        float moveSpeed =  isSprinting ? sprintSpeed : walkSpeed;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
        SubmitPositionServerRpc(rb.position);

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

    [ServerRpc]
    private void SubmitPositionServerRpc(Vector3 position)
    {
        transform.position = position;
        UpdateClientsClientRpc(position);
    }

    [ServerRpc]
    void SendHitToServerRpc(ulong victimId)
    {
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId]
                    .GetComponent<PlayerTagMovement>();
        victim.isHitNet.Value = true;
        StartCoroutine(StunRoutine(victim));
    }

    [ClientRpc]
    private void UpdateClientsClientRpc(Vector3 position)
    {
        if (IsOwner) return;
        transform.position = position;
    }

    private void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isHit", isHitNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
    }

    private bool CheckNearbyPlayers(float range)
    {
        foreach (PlayerTagMovement other in FindObjectsByType(typeof(PlayerTagMovement), FindObjectsSortMode.None))
        {
            if (other == this) continue;
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance <= range)
                return true;
        }
        return false;
    }


    private PlayerTagMovement FindClosestPlayerInRange(float range)
    {
        PlayerTagMovement closest = null;
        float shortest = Mathf.Infinity;

        foreach (var player in FindObjectsByType(typeof(PlayerTagMovement), FindObjectsSortMode.None))
        {
            if (player == this) continue;

            float dist = Vector3.Distance(transform.position, ((PlayerTagMovement) player).transform.position);
            if (dist < range && dist < shortest)
            {
                shortest = dist;
                closest = (PlayerTagMovement) player;
            }
        }
        return closest;
    }

    private IEnumerator StunRoutine(PlayerTagMovement victim)
    {
        yield return new WaitForSeconds(stunDuration);
        victim.isHitNet.Value = false; 
    }
}
