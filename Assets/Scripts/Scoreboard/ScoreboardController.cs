using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public class ScoreboardController : NetworkBehaviour
{
    public GameObject panel;
    private readonly float[] slotY =
    {
        120.1f,
        13.5f,
        -93.1f,
        -199.7f
    };
    private List<PlayerCard> playerCards;

    private void Awake()
    {
        playerCards = new List<PlayerCard>(
            panel.GetComponentsInChildren<PlayerCard>(false)
        );
    }


    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        InitializeScoreboard(SessionManager.Instance.previousPlayerDataList);
        InitializeScoreboardClientRpc(SessionManager.Instance.previousPlayerDataList.ToArray());

        UpdateScoreboard(SessionManager.Instance.PlayerDataList);
        UpdateScoreboardClientRpc(SessionManager.Instance.PlayerDataList.ToArray());
        StartCoroutine(CallGameFinishedAfterDelay(10f));
    }

    public void InitializeScoreboard(List<SessionManager.PlayerData> scores)
    {
        scores.Reverse();
        var playerCards = Object.FindObjectsByType<PlayerCard>(FindObjectsSortMode.None);
        for (int i = 0; i < scores.Count; i++)
        {
            playerCards[i].nicknameText.text = scores[i].nickname.ToSafeString();
            playerCards[i].scoreText.text = scores[i].Score.ToSafeString();
            UnityEngine.ColorUtility.TryParseHtmlString(scores[i].color.ToSafeString(), out var skinColor);
            playerCards[i].nicknameText.color = skinColor;

            var card = playerCards[i];
            RectTransform rt = card.transform as RectTransform;
            Vector2 targetPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(-24.5f, slotY[i]);
        }
    }
    
    private IEnumerator CallGameFinishedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsHost)
        {
            MinigameManager.Instance.GameFinished();
        }
    }

    public void UpdateScoreboard(List<SessionManager.PlayerData> scores)
    {
        for (int i = 0; i < scores.Count; i++)
        {
            var card = playerCards[i];
            var data = scores[i];
            RectTransform rt = card.transform as RectTransform;
            card.nicknameText.text = data.nickname.ToSafeString();
            card.scoreText.text = data.Score.ToSafeString();

            UnityEngine.ColorUtility.TryParseHtmlString(
                data.color.ToSafeString(),
                out var skinColor
            );
            card.nicknameText.color = skinColor;

            rt.DOKill();
            Vector2 targetPos = rt.anchoredPosition;
            targetPos.y = slotY[i];
            rt.DOAnchorPos(targetPos, 8f)
            .SetEase(Ease.OutCubic);
        }
    }

    [ClientRpc]
    private void UpdateScoreboardClientRpc(SessionManager.PlayerData[] scores)
    {
        UpdateScoreboard(new List<SessionManager.PlayerData>(scores));
    }

    [ClientRpc]
    private void InitializeScoreboardClientRpc(SessionManager.PlayerData[] scores)
    {
        InitializeScoreboard(new List<SessionManager.PlayerData>(scores));
    }
}

