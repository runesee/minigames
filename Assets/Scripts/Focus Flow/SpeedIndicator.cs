using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    private const float MinYPosition = -0.4f;
    private const float MaxYPosition = 0.4f;
    private const int ZoneCount = 5;
    private const float BrightnessMultiplier = 1.8f;

    private MeshRenderer[] zoneRenderers;
    private Color[] baseColors;
    private Color[] brightColors;
    private int currentActiveZone = -1;

    private void Start()
    {
        InitializeZones();
    }

    private void InitializeZones()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        zoneRenderers = new MeshRenderer[ZoneCount];
        baseColors = new Color[ZoneCount];
        brightColors = new Color[ZoneCount];

        string[] zoneNames = { "Zone1_Green", "Zone2_YellowGreen", "Zone3_Yellow", "Zone4_Orange", "Zone5_Red" };

        for (int i = 0; i < ZoneCount; i++)
        {
            Transform zoneTransform = parent.Find(zoneNames[i]);
            if (zoneTransform != null)
            {
                zoneRenderers[i] = zoneTransform.GetComponent<MeshRenderer>();
                if (zoneRenderers[i] != null && zoneRenderers[i].material != null)
                {
                    baseColors[i] = zoneRenderers[i].material.color;
                    brightColors[i] = baseColors[i] * BrightnessMultiplier;
                    brightColors[i].a = 1f;
                }
            }
        }
    }

    private void Update()
    {
        float normalizedSpeed = Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
        float yPosition = Mathf.Lerp(MinYPosition, MaxYPosition, normalizedSpeed);

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            yPosition,
            transform.localPosition.z
        );

        UpdateZoneHighlight(normalizedSpeed);
    }

    private void UpdateZoneHighlight(float normalizedSpeed)
    {
        int activeZone = Mathf.FloorToInt(normalizedSpeed * ZoneCount);
        activeZone = Mathf.Clamp(activeZone, 0, ZoneCount - 1);

        if (activeZone != currentActiveZone)
        {
            if (currentActiveZone >= 0 && currentActiveZone < ZoneCount && zoneRenderers[currentActiveZone] != null)
            {
                zoneRenderers[currentActiveZone].material.color = baseColors[currentActiveZone];
            }

            currentActiveZone = activeZone;

            if (currentActiveZone >= 0 && currentActiveZone < ZoneCount && zoneRenderers[currentActiveZone] != null)
            {
                zoneRenderers[currentActiveZone].material.color = brightColors[currentActiveZone];
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < ZoneCount; i++)
        {
            if (zoneRenderers[i] != null && i < baseColors.Length)
            {
                zoneRenderers[i].material.color = baseColors[i];
            }
        }
    }
}
