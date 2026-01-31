using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class GameResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI resultsText;
    [SerializeField] private ParticleSystem fireworksLeft;
    [SerializeField] private ParticleSystem fireworksRight;

    [Header("Animation Settings")]
    [SerializeField] private float panelAnimationDuration = 0.8f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup panelCanvasGroup;

    private void Start()
    {
        if (resultsPanel != null)
        {
            panelCanvasGroup = resultsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = resultsPanel.AddComponent<CanvasGroup>();
            }

            resultsPanel.SetActive(false);
        }

        if (TagGameState.Instance != null)
        {
            TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
        }
        else
        {
            StartCoroutine(WaitForTagGameState());
        }
    }

    private System.Collections.IEnumerator WaitForTagGameState()
    {
        while (TagGameState.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (TagGameState.Instance != null)
        {
            TagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(TagGameState.GameState previousState, TagGameState.GameState newState)
    {
        if (newState == TagGameState.GameState.Stopped)
        {
            StartCoroutine(ShowResultsWithAnimation());
        }
        else
        {
            HideResults();
        }
    }

    private void HideResults()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (fireworksLeft != null) fireworksLeft.Stop();
        if (fireworksRight != null) fireworksRight.Stop();
    }

    private IEnumerator ShowResultsWithAnimation()
    {
        if (resultsPanel == null || resultsText == null || NetworkManager.Singleton == null)
        {
            yield break;
        }

        BuildResultsText();

        resultsPanel.SetActive(true);

        if (fireworksLeft != null) fireworksLeft.Play();
        if (fireworksRight != null) fireworksRight.Play();

        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (elapsed < panelAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / panelAnimationDuration;
            float curveValue = scaleCurve.Evaluate(t);

            if (resultsPanel.transform is RectTransform rectTransform)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = curveValue;
            }

            yield return null;
        }

        if (resultsPanel.transform is RectTransform finalRect)
        {
            finalRect.localScale = targetScale;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
        }

        StartCoroutine(AnimateResultsText());
    }

    private IEnumerator AnimateResultsText()
    {
        if (resultsText == null) yield break;

        while (true)
        {
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            resultsText.fontSize = 36 + pulse * 4f;
            yield return null;
        }
    }

    private void BuildResultsText()
    {
        List<PlayerResult> results = new List<PlayerResult>();

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerTagMovement>();
            if (player == null) continue;

            double totalTime = player.timeSpentTaggedNet.Value;

            if (player.NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
            {
                double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
                totalTime += serverTime - player.lastTagTimeNet.Value;
            }

            string nickname = new string(player.nicknameNet.Value.Value);
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = $"Player{player.OwnerClientId}";
            }

            results.Add(new PlayerResult
            {
                clientId = player.OwnerClientId,
                timeTagged = totalTime,
                nickname = nickname
            });
        }

        results = results.OrderBy(r => r.timeTagged).ToList();

        string resultText = "<size=48><b>*** GAME OVER ***</b></size>\n\n<size=36><b>Final Results:</b></size>\n\n";

        for (int i = 0; i < results.Count; i++)
        {
            string rank = (i + 1).ToString();
            string playerName = results[i].nickname;
            string time = results[i].timeTagged.ToString("F2") + "s";

            if (i == 0)
            {
                resultText += $"<color=yellow><size=32> {rank}. {playerName}: {time} - WINNER! </size></color>\n";
            }
            else if (i == 1)
            {
                resultText += $"<color=#C0C0C0><size=28> {rank}. {playerName}: {time} </size></color>\n";
            }
            else if (i == 2)
            {
                resultText += $"<color=#CD7F32><size=28> {rank}. {playerName}: {time} </size></color>\n";
            }
            else
            {
                resultText += $"<size=28>{rank}. {playerName}: {time}</size>\n";
            }
        }

        resultText += "\n<size=24><i>Press Shutdown to return to menu</i></size>";

        resultsText.text = resultText;
    }

    private struct PlayerResult
    {
        public ulong clientId;
        public double timeTagged;
        public string nickname;
    }
}
