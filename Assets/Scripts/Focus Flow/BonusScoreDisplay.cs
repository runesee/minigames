using System.Collections;
using UnityEngine;

public class BonusScoreDisplay : MonoBehaviour
{
    private const float DisplayDuration = 1.0f;
    private const float FadeOutDuration = 0.5f;

    private TextMesh textMesh;
    private Color originalColor;
    private Coroutine displayCoroutine;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        originalColor = textMesh.color;
        textMesh.text = "";
    }

    public void ShowBonus(int bonusPoints)
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        displayCoroutine = StartCoroutine(DisplayBonusCoroutine(bonusPoints));
    }

    private IEnumerator DisplayBonusCoroutine(int bonusPoints)
    {
        textMesh.text = $"+{bonusPoints}";
        textMesh.color = originalColor;

        yield return new WaitForSeconds(DisplayDuration - FadeOutDuration);

        float elapsedTime = 0f;
        while (elapsedTime < FadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / FadeOutDuration);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        textMesh.text = "";
        textMesh.color = originalColor;
        displayCoroutine = null;
    }
}
