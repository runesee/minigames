using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenController : MonoBehaviour
{
    private const string NoPlayerName = "-";
    private const string ScoreFormat = "{0} pts";

    [SerializeField] private GameObject[] playerSlots;

    private void Start()
    {
        List<SessionManager.PlayerData> sortedPlayers = SessionManager.Instance.PlayerDataList
            .OrderByDescending(p => p.Score)
            .ToList();

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];

            CharacterPreview characterPreview = slot.transform.Find("AgaEndScreen")?.GetComponent<CharacterPreview>();
            Text nameText = slot.transform.Find("Nametag/NameText")?.GetComponent<Text>();
            Text scoreText = slot.transform.Find("Nametag/ScoreText")?.GetComponent<Text>();

            if (i < sortedPlayers.Count)
            {
                SessionManager.PlayerData playerData = sortedPlayers[i];

                if (characterPreview != null && ColorUtility.TryParseHtmlString(playerData.color.ToString(), out Color playerColor))
                {
                    characterPreview.SetColor(playerColor);
                }

                if (nameText != null) nameText.text = playerData.nickname.ToString();
                if (scoreText != null) scoreText.text = string.Format(ScoreFormat, playerData.Score);
            }
            else
            {
                if (nameText != null) nameText.text = NoPlayerName;
                if (scoreText != null) scoreText.text = string.Format(ScoreFormat, 0);
            }
        }
    }
}
