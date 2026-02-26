using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    [SerializeField] private ScoreMultiplierManager multiplierManager;
    [SerializeField] private SpeedometerDisplay speedometerDisplay;

    /// <summary>Optional: skin renderer on the player character for color sync.</summary>
    public SkinnedMeshRenderer playerSkin;

    private const int ZoneCount = 5;
    private const float WalkThreshold = 0.1f;
    private const float SprintThreshold = 0.6f;

    private int currentActiveZone = -1;
    private Animator characterAnimator;

    /// <summary>
    /// Returns the current speed zone index mapped for ScoreMultiplierManager.
    /// Zone 0 = fastest, Zone 4 = slowest (inverted from internal zone ordering).
    /// </summary>
    public int GetCurrentZoneIndex()
    {
        return ZoneCount - 1 - currentActiveZone;
    }

    private void Start()
    {
        characterAnimator = GetComponentInChildren<Animator>();

        if (playerSkin != null && FocusFlowData.LocalInstance != null)
        {
            ColorUtility.TryParseHtmlString(
                FocusFlowData.LocalInstance.colorNet.Value.ToString(),
                out Color skinColor);
            playerSkin.material.color = skinColor;
        }
    }

    private void Update()
    {
        float normalizedSpeed = ReadNormalizedSpeed();

        if (speedometerDisplay != null)
        {
            speedometerDisplay.SetNormalizedSpeed(normalizedSpeed);
        }

        UpdateAnimator(normalizedSpeed);
        UpdateZoneHighlight(normalizedSpeed);
    }

    private float ReadNormalizedSpeed()
    {
        if (multiplierManager != null && multiplierManager.IsTracking)
        {
            return multiplierManager.GetAverageNormalizedSpeed();
        }

        return Mathf.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f) + 0.2f;
    }

    private void UpdateAnimator(float normalizedSpeed)
    {
        if (characterAnimator == null) return;

        bool isSprinting = normalizedSpeed > SprintThreshold;
        bool isWalking = !isSprinting && normalizedSpeed > WalkThreshold;

        characterAnimator.SetBool("isWalking", isWalking);
        characterAnimator.SetBool("isSprinting", isSprinting);
    }

    private void UpdateZoneHighlight(float normalizedSpeed)
    {
        int activeZone = Mathf.Clamp(
            Mathf.FloorToInt(normalizedSpeed * ZoneCount),
            0, ZoneCount - 1);

        if (activeZone == currentActiveZone) return;

        currentActiveZone = activeZone;

        if (speedometerDisplay != null)
        {
            speedometerDisplay.HighlightZone(currentActiveZone);
        }
    }
}
