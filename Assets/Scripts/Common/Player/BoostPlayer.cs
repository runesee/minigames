using Unity.Netcode;
using UnityEngine;

public abstract class BoostPlayer : MovementPlayer
{
    protected NetworkVariable<bool> isShowingBoostParticlesNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public ParticleSystem sprintParticleEffect;
    protected float smoothedPedalSpeed = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (sprintParticleEffect != null)
        {
            var main = sprintParticleEffect.main;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            sprintParticleEffect.Stop();
        }
        isShowingBoostParticlesNet.OnValueChanged += OnSprintParticlesChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isShowingBoostParticlesNet.OnValueChanged -= OnSprintParticlesChanged;
    }

    public virtual void OnSprintParticlesChanged(bool previousValue, bool newValue)
    {
        if (sprintParticleEffect == null) return;
        if (newValue && !sprintParticleEffect.isPlaying) sprintParticleEffect.Play();
        else if (sprintParticleEffect.isPlaying) sprintParticleEffect.Stop();
    }
    
}
