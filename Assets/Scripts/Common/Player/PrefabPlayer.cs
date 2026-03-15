using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class PrefabPlayer : Player
{
    protected Animator animator;
    protected Rigidbody rb;

    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }
}
