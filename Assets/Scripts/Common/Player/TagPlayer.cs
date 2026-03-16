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

    protected virtual void HandleTaunting()
    {
        // Lastly, if neither moving or tagging, check if taunting.
        // Sets both trigger and bool value in Animator.
        // Limits animation to loop once if key is held down,
        // otherwise cancels on other actions or letting go of key
        canTaunt = !isWalkingNet.Value && !isSprintingNet.Value && !isPunching;
        if (isTaunting && canTaunt)
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

    protected override (Vector3 joystickOffset, Vector3 input) ParseInput()
    {
        isTaunting = (interactAction.ReadValue<float>() > 0f && interactAction.WasPressedThisFrame()) || PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.Y);
        isPunching = attackAction.WasPerformedThisFrame() || PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A);
        return base.ParseInput();
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
