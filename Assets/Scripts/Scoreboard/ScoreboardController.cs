using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;
using System.Collections;
using DG.Tweening;
using System.Linq;

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
    private Dictionary<string, PlayerCard> cardByGuid = new();
    private List<PlayerCard> playerCards;

    private void Awake()
    {
        playerCards = panel.GetComponentsInChildren<PlayerCard>(true).ToList();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        InitializeScoreboard(SessionManager.Instance.previousPlayerDataList);
        InitializeScoreboardClientRpc(SessionManager.Instance.previousPlayerDataList.ToArray());
        StartCoroutine(DelayedInit());
        StartCoroutine(CallGameFinishedAfterDelay(10f));
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(2f);

        if (IsHost)
        {
            UpdateScoreboard(SessionManager.Instance.PlayerDataList);
            UpdateScoreboardClientRpc(SessionManager.Instance.PlayerDataList.ToArray());
        }
    }

    public void InitializeScoreboard(List<SessionManager.PlayerData> scores)
    {
        for (int i = 0; i < 4; i++)
        {
            var card = playerCards[i];
            card.guid = scores[i].Guid.ToSafeString();
            cardByGuid[card.guid] = card;

            card.nicknameText.text = scores[i].nickname.ToSafeString();
            card.scoreText.text = scores[i].Score.ToSafeString();
            UnityEngine.ColorUtility.TryParseHtmlString(scores[i].color.ToSafeString(), out var skinColor);
            card.nicknameText.color = skinColor;

            RectTransform rectTransform = (RectTransform)card.transform;
            rectTransform.anchoredPosition = new Vector2(-24.5f, slotY[i]);
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
        scores = scores.OrderByDescending(s => s.Score).ToList();
        for (int i = 0; i < 4; i++)
        {
            var card = cardByGuid[scores[i].Guid.ToString()];
            card.scoreText.text = scores[i].Score.ToSafeString();

            RectTransform rectTransform = (RectTransform)card.transform;
            Vector2 target = new Vector2(-24.5f, slotY[i]);
            rectTransform.DOKill();
            rectTransform.DOAnchorPos(target, 2.8f).SetEase(Ease.OutCubic);
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

