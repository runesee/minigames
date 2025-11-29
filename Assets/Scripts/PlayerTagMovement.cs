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

    private NetworkVariable<bool> isFightingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isSprintingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private InputAction attackAction;

    private bool isPunching;

    private NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isHitNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public float RotateSpeed = 30f;

    private InputAction moveAction;
    private InputAction sprintAction;

    private bool isSprinting;

    private Rigidbody rb;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        animator = GetComponentInChildren<Animator>();
        
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.Enable();
        moveAction.Enable();
        attackAction = InputSystem.actions.FindAction("Attack");
        attackAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();
        Vector3 movement = new Vector3(input.x, 0, input.y);
        
        isPunching = attackAction.WasPerformedThisFrame();
        isPunchingNet.Value = isPunching;
        if (isPunching)
        {
            PlayerTagMovement target = FindClosestPlayerInRange(2f);

            if (target != null)
            {
                Debug.Log($"target: {target}");
                SendHitToServerRpc(target.NetworkObjectId);
            }
        }

        // Handle animations and update position based on input actions
        isSprintingNet.Value = isSprinting;
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            isWalkingNet.Value = !isSprinting;
            isFightingNet.Value = false;  
        }
        else
        {
          isWalkingNet.Value = false;
          isSprintingNet.Value = false;
          isFightingNet.Value = !isPunching && CheckNearbyPlayers(2f); 
        } 
        float moveSpeed =  isSprinting ? sprintSpeed : walkSpeed;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
        SubmitPositionServerRpc(rb.position);
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
        animator.SetBool("isFighting", isFightingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isHit", isHitNet.Value);
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
        Debug.Log($"closest player: {closest}");
        return closest;
    }

    private IEnumerator StunRoutine(PlayerTagMovement victim)
    {
        rb.linearVelocity = Vector3.zero; // TODO: use actual movement stunning and not this
        yield return new WaitForSeconds(stunDuration);
        victim.isHitNet.Value = false; 
    }

}
