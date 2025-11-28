using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerTag : NetworkBehaviour
{
    private Animator animator;
    private Rigidbody rb;

    private InputAction attackAction;

    private bool isPunching;

    private NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

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
        attackAction = InputSystem.actions.FindAction("Attack");
        attackAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        isPunching = attackAction.WasPerformedThisFrame();
        isPunchingNet.Value = isPunching;
    }

    private void LateUpdate()
    {
        animator.SetBool("isPunching", isPunchingNet.Value);
    }
}
