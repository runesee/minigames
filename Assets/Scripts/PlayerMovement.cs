using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;

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
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();
        Vector3 movement = new Vector3(input.x, 0, input.y);
        
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
          isFightingNet.Value = CheckNearbyPlayers(3f); 
        } 
        float moveSpeed =  isSprinting ? sprintSpeed : walkSpeed;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
        SubmitPositionServerRpc(rb.position);
    }

    [ServerRpc]
    private void SubmitPositionServerRpc(Vector3 position)
    {
        transform.position = position;
        UpdateClientsClientRpc(position); // Send to all clients but owner
    }

    [ClientRpc]
    private void UpdateClientsClientRpc(Vector3 position)
    {
        if (IsOwner) return; // Owner already moved
        transform.position = position;
    }

    private void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isFighting", isFightingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
    }

    private bool CheckNearbyPlayers(float range)
    {
        foreach (NetworkPlayer other in FindObjectsByType(typeof(NetworkPlayer), FindObjectsSortMode.None))
        {
            if (other == this) continue;
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance <= range)
                return true;
        }
        return false;
    }
}
