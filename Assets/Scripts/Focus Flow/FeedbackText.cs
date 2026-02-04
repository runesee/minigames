using UnityEngine;

public class FeedbackText : MonoBehaviour
{
    private TextMesh textMesh;
    private float animationDuration = 1.5f;
    private float moveDistance = 2.0f;
    private Vector3 startPosition;
    private float elapsedTime = 0f;

    public void Initialize(string message, Color color, Vector3 spawnPosition)
    {
        startPosition = spawnPosition;
        transform.position = startPosition;

        GameObject textObject = new GameObject("FeedbackTextMesh");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localRotation = Quaternion.identity;

        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = message;
        textMesh.fontSize = 100;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / animationDuration;

        transform.position = startPosition + Vector3.up * (moveDistance * progress);

        Color currentColor = textMesh.color;
        currentColor.a = 1f - progress;
        textMesh.color = currentColor;

        if (elapsedTime >= animationDuration)
        {
            Destroy(gameObject);
        }
    }
}
