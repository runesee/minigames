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

    private InputAction moveAction;
    private InputAction jumpAction;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }
        moveAction = InputSystem.actions.FindAction("Move");
        //jumpAction = InputSystem.actions.FindAction("Jump");

        moveAction.Enable();
        //jumpAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        //bool jumpPressed = jumpAction.IsPressed();

        Vector3 movement = new Vector3(input.x, 0, input.y) * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        /*if (jumpPressed && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }*/
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
}
