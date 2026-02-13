using UnityEngine;

public class ZoneMultiplierDisplay : MonoBehaviour
{
    [SerializeField] private IntervalTimer intervalTimer;
    
    private TextMesh[] zoneTexts;
    private bool previousIntervalPhase;

    private readonly string[] intervalMultipliers = { "x2.5", "x2.0", "x1.5", "x1.0", "x0.5" };
    private readonly string[] restMultipliers = { "x0.5", "x1.0", "x1.5", "x2.0", "x0.5" };

    private void Start()
    {
        zoneTexts = new TextMesh[5];
        
        string[] zoneNames = { "Zone5_Red", "Zone4_Orange", "Zone3_Yellow", "Zone2_YellowGreen", "Zone1_Green" };
        Transform parent = GameObject.Find("SpeedDisplayArea").transform;

        for (int i = 0; i < 5; i++)
        {
            Transform zoneTransform = parent.Find(zoneNames[i]);
            Transform textTransform = zoneTransform.Find("MultiplierText");
            zoneTexts[i] = textTransform.GetComponent<TextMesh>();
        }

        previousIntervalPhase = intervalTimer.IsIntervalPhase;
        UpdateMultipliers();
    }

    private void Update()
    {
        if (intervalTimer.IsIntervalPhase != previousIntervalPhase)
        {
            previousIntervalPhase = intervalTimer.IsIntervalPhase;
            UpdateMultipliers();
        }
    }

    private void UpdateMultipliers()
    {
        string[] currentMultipliers = intervalTimer.IsIntervalPhase ? intervalMultipliers : restMultipliers;

        for (int i = 0; i < 5; i++)
        {
            zoneTexts[i].text = currentMultipliers[i];
        }
    }
}
