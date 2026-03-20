using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenController : NetworkBehaviour
{
    private const string NoPlayerName = "-";
    private const string ScoreFormat = "{0} pts";
    private readonly List<string> medalColors = new List<string> { "#FFD700", "#C0C0C0", "#CD7F32", "#7e7d74" };

    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private TextMeshPro[] placementTexts;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            SessionManager.PlayerData[] allPlayers = SessionManager.Instance.PlayerDataList.ToArray();
            PopulatePlayerSlotsClientRpc(allPlayers);
        }
    }

    [ClientRpc]
    private void PopulatePlayerSlotsClientRpc(SessionManager.PlayerData[] players)
    {
        List<SessionManager.PlayerData> sortedPlayers = players.OrderByDescending(p => p.Score).ToList();

        int previousPlacement = 0;
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (i >= playerSlots.Length) break;

            int placement;
            if (i > 0 && Math.Abs(sortedPlayers[i].Score - sortedPlayers[i - 1].Score) < 0.001)
            {
                placement = previousPlacement;
            }
            else
            {
                placement = i + 1;
            }
            previousPlacement = placement;
            GameObject slot = playerSlots[i];
            if (i < placementTexts.Count())
            {
                UnityEngine.ColorUtility.TryParseHtmlString(medalColors[placement-1], out var medalColor);
                placementTexts[i].text = placement.ToString();
                placementTexts[i].color = medalColor;
            }

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
