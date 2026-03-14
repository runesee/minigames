using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class TagPlayer : BoostPlayer
{
    protected NetworkVariable<bool> isPunchingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected NetworkVariable<bool> isFrozenNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    protected NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    protected NetworkVariable<double> timeSpentTaggedNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    protected NetworkVariable<double> lastTagTimeNet = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public AudioSource tagAudioSource;
    public AudioClip tagClip;
    protected InputAction attackAction;
    protected InputAction interactAction;
    protected bool isPunching;
    protected bool isTaunting;
    protected bool canTaunt;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        attackAction = InputSystem.actions.FindAction("Attack");
        interactAction = InputSystem.actions.FindAction("Interact");
        attackAction.Enable();
        interactAction.Enable();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        animator.SetBool("isPunching", isPunchingNet.Value);
        animator.SetBool("isTaunting", isTauntingNet.Value);
    }

    [ServerRpc]
    protected virtual void UnfreezePlayerServerRpc()
    {
        isFrozenNet.Value = false;
    }

    [ClientRpc]
    protected virtual void StopAnimationsClientRpc()
    {
        if (!IsOwner) return;
        isWalkingNet.Value = false;
        isSprintingNet.Value = false;
        isTauntingNet.Value = false;
        isPunchingNet.Value = false;
    }
}
