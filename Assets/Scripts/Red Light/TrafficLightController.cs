using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    [Header("Light GameObjects")]
    [SerializeField] private GameObject redLight;
    [SerializeField] private GameObject greenLight;

    public void SetLightState(bool isRed)
    {
        redLight.SetActive(isRed);
        greenLight.SetActive(!isRed);
    }
}
