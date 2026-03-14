using Unity.Netcode;
using UnityEngine.InputSystem;

public abstract class MovementPlayer : PrefabPlayer
{
    protected NetworkVariable<bool> isWalkingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected NetworkVariable<bool> isSprintingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected InputAction moveAction;
    protected InputAction sprintAction;
    protected readonly float sprintSpeedThreshold = 0.65f;
    protected readonly float walkSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        moveAction.Enable();
        sprintAction.Enable();
    }

    public virtual void LateUpdate()
    {
        animator.SetBool("isWalking", isWalkingNet.Value);
        animator.SetBool("isSprinting", isSprintingNet.Value);
    }
}
