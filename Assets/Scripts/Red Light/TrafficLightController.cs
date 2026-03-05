using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    [Header("Light GameObjects")]
    [SerializeField] private GameObject redLight;
    [SerializeField] private GameObject yellowLight;
    [SerializeField] private GameObject greenLight;

    public void SetLightState(bool isRed)
    {
        redLight.SetActive(isRed);
        yellowLight.SetActive(false);
        greenLight.SetActive(!isRed);
    }

    public void SetYellowWarning(bool isYellow)
    {
        redLight.SetActive(false);
        yellowLight.SetActive(isYellow);
        greenLight.SetActive(!isYellow);
    }
}
