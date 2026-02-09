using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    private const float MinYPosition = -0.5f;
    private const float MaxYPosition = 0.5f;
    private const int ZoneCount = 5;
    private const float BrightnessMultiplier = 1.8f;
    private const float WalkThreshold = 0.1f;
    private const float SprintThreshold = 0.6f;

    private MeshRenderer[] zoneRenderers;
    private Color[] baseColors;
    private int currentActiveZone = -1;
    private Animator characterAnimator;

    private void Start()
    {
        zoneRenderers = new MeshRenderer[ZoneCount];
        baseColors = new Color[ZoneCount];

        string[] zoneNames = { "Zone1_Green", "Zone2_YellowGreen", "Zone3_Yellow", "Zone4_Orange", "Zone5_Red" };

        for (int i = 0; i < ZoneCount; i++)
        {
            Transform zoneTransform = transform.parent.Find(zoneNames[i]);
            if (zoneTransform != null)
            {
                zoneRenderers[i] = zoneTransform.GetComponent<MeshRenderer>();
                if (zoneRenderers[i] != null)
                {
                    baseColors[i] = zoneRenderers[i].material.color;
                }
            }
        }

        characterAnimator = GetComponentInChildren<Animator>();
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

        if (characterAnimator != null)
        {
            bool isSprinting = normalizedSpeed > SprintThreshold;
            bool isWalking = !isSprinting && normalizedSpeed > WalkThreshold;

            characterAnimator.SetBool("isWalking", isWalking);
            characterAnimator.SetBool("isSprinting", isSprinting);
        }

        UpdateZoneHighlight(normalizedSpeed);
    }

    private void UpdateZoneHighlight(float normalizedSpeed)
    {
        int activeZone = Mathf.Clamp(Mathf.FloorToInt(normalizedSpeed * ZoneCount), 0, ZoneCount - 1);

        if (activeZone != currentActiveZone)
        {
            if (currentActiveZone >= 0 && zoneRenderers[currentActiveZone] != null)
            {
                zoneRenderers[currentActiveZone].material.color = baseColors[currentActiveZone];
            }

            currentActiveZone = activeZone;

            if (zoneRenderers[currentActiveZone] != null)
            {
                zoneRenderers[currentActiveZone].material.color = baseColors[currentActiveZone] * BrightnessMultiplier;
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < ZoneCount; i++)
        {
            if (zoneRenderers[i] != null)
            {
                zoneRenderers[i].material.color = baseColors[i];
            }
        }
    }
}
