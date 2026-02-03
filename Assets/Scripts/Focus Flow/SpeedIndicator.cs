using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    private const float MinYPosition = -0.4f;
    private const float MaxYPosition = 0.4f;

    private void Update()
    {
        float normalizedSpeed = Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
        float yPosition = Mathf.Lerp(MinYPosition, MaxYPosition, normalizedSpeed);

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            yPosition,
            transform.localPosition.z
        );
    }
}
