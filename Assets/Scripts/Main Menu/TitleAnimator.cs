using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TitleAnimator : MonoBehaviour
{
    [Header("Wave Animation")]
    [SerializeField] private float waveAmplitude = 8f;
    [SerializeField] private float waveFrequency = 2f;
    [SerializeField] private float waveCharacterOffset = 0.3f;

    [Header("Glow Effect")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.3f, 0.8f);
    [SerializeField] private float glowOffset = 0.05f;
    [SerializeField] private float glowInner = 0.1f;
    [SerializeField] private float glowOuter = 0.3f;
    [SerializeField] private float glowPowerMin = 0.3f;
    [SerializeField] private float glowPowerMax = 0.8f;
    [SerializeField] private float glowPulseSpeed = 1.5f;

    [Header("Scale Breathing")]
    [SerializeField] private float scaleMin = 0.97f;
    [SerializeField] private float scaleMax = 1.03f;
    [SerializeField] private float scaleSpeed = 1.2f;

    private TextMeshProUGUI titleText;
    private Material titleMaterial;
    private Vector3 originalScale;
    private bool hasForceUpdated;

    private void Awake()
    {
        titleText = GetComponent<TextMeshProUGUI>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        SetupGlow();
    }

    private void Update()
    {
        AnimateWave();
        AnimateGlowPulse();
        AnimateScaleBreathing();
    }

    private void OnDestroy()
    {
        if (titleMaterial != null)
        {
            titleMaterial.DisableKeyword(ShaderUtilities.Keyword_Glow);
        }
    }

    private void SetupGlow()
    {
        titleMaterial = titleText.fontMaterial;

        titleMaterial.EnableKeyword(ShaderUtilities.Keyword_Glow);
        titleMaterial.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
        titleMaterial.SetFloat(ShaderUtilities.ID_GlowOffset, glowOffset);
        titleMaterial.SetFloat(ShaderUtilities.ID_GlowInner, glowInner);
        titleMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, glowOuter);
        titleMaterial.SetFloat(ShaderUtilities.ID_GlowPower, glowPowerMax);
    }

    private void AnimateWave()
    {
        titleText.ForceMeshUpdate();
        TMP_TextInfo textInfo = titleText.textInfo;

        if (textInfo == null || textInfo.characterCount == 0)
        {
            return;
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
            {
                continue;
            }

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float waveOffset = Mathf.Sin(Time.time * waveFrequency + i * waveCharacterOffset) * waveAmplitude;

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j].y += waveOffset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            titleText.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void AnimateGlowPulse()
    {
        if (titleMaterial == null)
        {
            return;
        }

        float glowPower = Mathf.Lerp(glowPowerMin, glowPowerMax,
            (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);

        titleMaterial.SetFloat(ShaderUtilities.ID_GlowPower, glowPower);
    }

    private void AnimateScaleBreathing()
    {
        float scaleFactor = Mathf.Lerp(scaleMin, scaleMax,
            (Mathf.Sin(Time.time * scaleSpeed + Mathf.PI * 0.5f) + 1f) * 0.5f);

        transform.localScale = originalScale * scaleFactor;
    }
}
