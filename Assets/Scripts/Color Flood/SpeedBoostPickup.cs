using UnityEngine;

public class SpeedBoostPickup : MonoBehaviour
{
    public int pickupId;
    public float rotationSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        Vector3 pos = basePosition;
        pos.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = pos;
    }
}
