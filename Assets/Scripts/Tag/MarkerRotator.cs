using UnityEngine;

public class MarkerRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool rotateOnYAxis = true;
    [SerializeField] private bool bobUpDown = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.2f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        if (rotateOnYAxis)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        if (bobUpDown)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
}
