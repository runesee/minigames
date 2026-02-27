using TMPro;
using UnityEngine;

public class PlayerCard : MonoBehaviour
{
    public string guid;
    public TMP_Text nicknameText;
    public TMP_Text scoreText;
    public TMP_Text bonusText;

    private void Awake()
    {
        // Auto-assign references if they're null
        if (nicknameText == null)
        {
            Transform nicknameTransform = transform.Find("Nickname");
            if (nicknameTransform != null)
            {
                nicknameText = nicknameTransform.GetComponent<TMP_Text>();
            }
        }

        if (scoreText == null)
        {
            Transform scoreTransform = transform.Find("Score");
            if (scoreTransform != null)
            {
                scoreText = scoreTransform.GetComponent<TMP_Text>();
            }
        }

        if (bonusText == null)
        {
            Transform bonusTransform = transform.Find("Bonus");
            if (bonusTransform != null)
            {
                bonusText = bonusTransform.GetComponent<TMP_Text>();
            }
        }
    }
}
