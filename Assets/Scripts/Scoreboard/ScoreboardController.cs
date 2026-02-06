using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;

public class ScoreboardController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        UpdateScoreboard(SessionManager.Instance.PlayerDataList);
        UpdateScoreboardClientRpc(SessionManager.Instance.PlayerDataList.ToArray());
    }

    public void UpdateScoreboard(List<SessionManager.PlayerData> scores)
    {
        var playerCard = Object.FindObjectsByType<PlayerCard>(FindObjectsSortMode.None);
        for (int i = 0; i < scores.Count; i++)
        {
            playerCard[i].transform.SetSiblingIndex(1+i);
            playerCard[i].nicknameText.text = scores[i].Guid.ToSafeString();
            playerCard[i].scoreText.text = scores[i].Score.ToSafeString();
        }
    }

    [ClientRpc]
    private void UpdateScoreboardClientRpc(SessionManager.PlayerData[] scores)
    {
        UpdateScoreboard(new List<SessionManager.PlayerData>(scores));
    }
}

