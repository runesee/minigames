using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using TMPro;
using Unity.VisualScripting;

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

    private NetworkVariable<bool> isTaggedNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> isTauntingNet = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    private NetworkVariable<double> timeSpentTagged = new NetworkVariable<double>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    private NetworkVariable<double> lastTagTime = new NetworkVariable<double>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

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

        // At the moment, we set the initially tagged player here => Always Player 1.
        // TODO : change to on a start function etc. once all players have connected.
        
        // We have to do a lot of parsing as custom class objects are not serializable with Netcode (currently)
        var playerObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        var players = playerObjects.Values.ToList();
        var random  = UnityEngine.Random.Range(0, players.Count - 1);
        var selectedPlayer = players[random];
        var selectedPlayerId = playerObjects.FirstOrDefault(player => player.Value == selectedPlayer).Key;
        setInitialTaggedPlayerServerRpc(selectedPlayerId);

        // Init key bindings
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
        isPunchingNet.Value = isPunching && isTaggedNet.Value;
        isSprintingNet.Value = isSprinting;

        // Set target player as isHit if punched by punching player
        if (isPunching && isTaggedNet.Value)
        {
            PlayerTagMovement target = FindClosestPlayerInRange(2f);

            if (target != null)
            {
                double time = NetworkManager.Singleton.LocalTime.FixedTime;
                double servertime = NetworkManager.Singleton.ServerTime.FixedTime;
                //Debug.Log($"localtime: {time}");
                //Debug.Log($"servertime: {servertime}");
                //Debug.Log($"user time spent tagged: {timeSpentTagged.Value}");
                // Set target as tagged and hit (can tag others, and play anim)
                SendHitToServerRpc(target.NetworkObjectId);
            }
        }

        // Handle animations and update position based on input actions
        if (movement.sqrMagnitude > 0.1f)
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
    void SendHitToServerRpc(ulong victimId) // TODO :  give better name
    {   
        isTaggedNet.Value = false;
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId]
                    .GetComponent<PlayerTagMovement>();
        victim.isHitNet.Value = true;
        victim.isTaggedNet.Value = true;

        // Add timediff to current player
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        timeSpentTagged.Value += serverTime - lastTagTime.Value;
        victim.lastTagTime.Value = serverTime;
        // TODO : Use localtime to display constant timechange of tagee in game.
        // TODO : Then, update with actual serverTime on tag.

        StartCoroutine(StunRoutine(victim));
    }

    [ServerRpc]
    void setInitialTaggedPlayerServerRpc(ulong playerId)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerId]
                    .GetComponent<PlayerTagMovement>();
        playerObject.isTaggedNet.Value = true;
    }


    [ClientRpc]
    private void UpdateClientsClientRpc(Vector3 position)   // TODO :  give better name
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

    private PlayerTagMovement FindClosestPlayerInRange(float range)
    {
        PlayerTagMovement closest = null;
        float shortest = Mathf.Infinity;
        bool isWithinBounds = false;

        foreach (var player in FindObjectsByType(typeof(PlayerTagMovement), FindObjectsSortMode.None))
        {
            if (player == this) continue;

            float dist = Vector3.Distance(transform.position, ((PlayerTagMovement) player).transform.position);

            if (dist < range && dist < shortest)
            {
                shortest = dist;
                closest = (PlayerTagMovement) player;
                Vector3 targetVector = (closest.transform.position - transform.position).normalized;
                Vector3 playerVector = transform.forward;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                float angle;
                Vector3 axis;
                Quaternion.FromToRotation(playerVector, targetVector).ToAngleAxis(out angle, out axis);
                isWithinBounds = Mathf.Abs(angle) <= 45f;
            }
        }
        return isWithinBounds ? closest : null;
    }

    private IEnumerator StunRoutine(PlayerTagMovement victim)
    {
        yield return new WaitForSeconds(stunDuration);
        victim.isHitNet.Value = false; 
    }
}
