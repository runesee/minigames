using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;

public class ScoreboardController : NetworkBehaviour
{
    private static float ANIMATION_SPEED = 3.25f;
    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        UpdateScoreboard(SessionManager.Instance.PlayerDataList);
        UpdateScoreboardClientRpc(SessionManager.Instance.PlayerDataList.ToArray());
        StartCoroutine(CallGameFinishedAfterDelay(20f));
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
        var playerCards = Object.FindObjectsByType<PlayerCard>(FindObjectsSortMode.None);
        for (int i = 0; i < scores.Count; i++)
        {
            playerCards[i].nicknameText.text = scores[i].nickname.ToSafeString();
            playerCards[i].scoreText.text = scores[i].Score.ToSafeString();
            UnityEngine.ColorUtility.TryParseHtmlString(scores[i].color.ToSafeString(), out var skinColor);
            playerCards[i].nicknameText.color = skinColor;

            RectTransform rectTransform = playerCards[i].transform as RectTransform;
            LayoutElement layoutElement = playerCards[i].GetComponent<LayoutElement>();
            Vector2 oldPos = rectTransform.anchoredPosition;
            rectTransform.SetSiblingIndex(1+i);

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                rectTransform.parent as RectTransform
            );
            Vector2 newPos = rectTransform.anchoredPosition;
            layoutElement.ignoreLayout = true;  // Have to toggle this, otherwise it literally just breaks any animation
            rectTransform.anchoredPosition = oldPos;
            StartCoroutine(MoveTo(rectTransform, newPos, layoutElement));
        }
    }

    IEnumerator MoveTo(RectTransform rectTransform, Vector2 target, LayoutElement layoutElement)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime / ANIMATION_SPEED;
            rectTransform.anchoredPosition = Vector2.Lerp(start, target, time);
            yield return null;
        }
        rectTransform.anchoredPosition = target;
        layoutElement.ignoreLayout = false;
    }

    [ClientRpc]
    private void UpdateScoreboardClientRpc(SessionManager.PlayerData[] scores)
    {
        UpdateScoreboard(new List<SessionManager.PlayerData>(scores));
    }
}

