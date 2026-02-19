using UnityEngine;

public class CrowdParticleSetup : MonoBehaviour
{
    [Header("Crowd Settings")]
    public int maxParticles = 1000;
    public float boxWidth = 2.5f;
    public float boxHeight = 0.8f;
    public float boxDepth = 150f;
    public Color startColor = Color.red;
    
    private void Start()
    {
        ConfigureParticleSystem();
    }
    
    private void ConfigureParticleSystem()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = startColor;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 200f;
        
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(boxWidth, boxHeight, boxDepth);
        
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(startColor, 0.0f), 
                new GradientColorKey(startColor * 1.2f, 0.5f),
                new GradientColorKey(startColor, 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(0.5f, 1.1f);
        curve.AddKey(1.0f, 1.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
        
        ps.Play();
    }
}
