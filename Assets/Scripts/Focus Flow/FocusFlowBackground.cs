using UnityEngine;

public class FocusFlowBackground : MonoBehaviour
{
    private const int GradientTextureHeight = 128;
    private const int CircleTextureSize     = 64;
    private const int MaxParticles          = 80;
    private const float EmissionRate        = 10f;
    private const float BackgroundDepthZ    = 20f;
    private const float ParticleDepthZ      = 5f;
    private const float BackgroundWidth     = 72f;
    private const float BackgroundHeight    = 42f;

    private static readonly Color GradientTop    = new Color(0.04f, 0.05f, 0.18f);
    private static readonly Color GradientBottom = new Color(0.01f, 0.01f, 0.06f);

    [SerializeField] private Shader backgroundShader;
    [SerializeField] private Shader particleShader;

    private Material backgroundMaterial;
    private Material particleMaterial;
    private Texture2D gradientTexture;
    private Texture2D circleTexture;

    private void Awake()
    {
        gradientTexture = CreateGradientTexture(GradientTextureHeight);
        circleTexture   = CreateCircleTexture(CircleTextureSize);

        CreateGradientBackground();
        CreateParticleEffect();
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
        if (particleMaterial != null)   Destroy(particleMaterial);
        if (gradientTexture != null)    Destroy(gradientTexture);
        if (circleTexture != null)      Destroy(circleTexture);
    }

    private void CreateGradientBackground()
    {
        GameObject bgObj = new GameObject("BackgroundQuad");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = new Vector3(0f, 1f, BackgroundDepthZ);
        bgObj.transform.localScale    = new Vector3(BackgroundWidth, BackgroundHeight, 1f);

        MeshFilter   mf = bgObj.AddComponent<MeshFilter>();
        MeshRenderer mr = bgObj.AddComponent<MeshRenderer>();

        mf.sharedMesh = BuildQuadMesh();

        backgroundMaterial             = new Material(backgroundShader);
        backgroundMaterial.mainTexture = gradientTexture;
        mr.sharedMaterial              = backgroundMaterial;
    }

    private void CreateParticleEffect()
    {
        GameObject psObj = new GameObject("BackgroundParticles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 1f, ParticleDepthZ);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = true;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.30f, 0.55f, 1.00f, 0.80f),
            new Color(0.65f, 0.20f, 1.00f, 0.55f)
        );
        main.maxParticles     = MaxParticles;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = 0f;

        var emission = ps.emission;
        emission.rateOverTime = EmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(22f, 14f, 1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.10f, 0.40f);
        vel.z       = new ParticleSystem.MinMaxCurve(0f, 0f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0.00f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.85f, 1f),
            new Keyframe(1.00f, 0f)
        ));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0.00f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.85f),
                new GradientAlphaKey(0f, 1.00f),
            }
        );
        colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode   = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = -5;

        Shader particleShader = this.particleShader;

        particleMaterial             = new Material(particleShader);
        particleMaterial.mainTexture = circleTexture;
        renderer.sharedMaterial      = particleMaterial;
    }

    private static Mesh BuildQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices  = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
        };
        mesh.uv        = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Texture2D CreateGradientTexture(int height)
    {
        Texture2D tex = new Texture2D(1, height, TextureFormat.RGB24, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            tex.SetPixel(0, y, Color.Lerp(GradientBottom, GradientTop, t));
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float t     = Mathf.Clamp01(dist / center);
                float alpha = (1f - t) * (1f - t);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}
