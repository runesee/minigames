using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    private Animator animator;

    private NetworkVariable<bool> isWalkingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    public float RotateSpeed = 30f;

    private InputAction moveAction;

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
        moveAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 movement = new Vector3(input.x, 0, input.y);
        
        // Rotate prefab to match player movement direction
        // also set walking animation
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            isWalkingNet.Value = true;
        }
        else isWalkingNet.Value = false;
        

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
    }

}
