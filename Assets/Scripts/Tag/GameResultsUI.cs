using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using Unity.Collections;

public class GameResultsUI : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI resultsText;
    [SerializeField] private ParticleSystem fireworksLeft;
    [SerializeField] private ParticleSystem fireworksRight;

    [Header("Animation Settings")]
    [SerializeField] private float panelAnimationDuration = 0.8f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public CanvasGroup panelCanvasGroup;
    private List<PlayerCard> playerCards;
    private readonly List<string> medalColors = new List<string> { "#FFD700", "#C0C0C0", "#CD7F32", "#7e7d74" };
    private readonly List<float> cardYPositions = new List<float> { 100, 0, -100, -200 };
    private List<PlayerResult> results = new List<PlayerResult>();

    private void Awake()
    {
        playerCards = resultsPanel.GetComponentsInChildren<PlayerCard>(true).ToList();
    }

    private void Start()
    {
        resultsPanel?.SetActive(false);

        if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.Tag)
        {
            if (TagGameState.Instance != null)
            {
                TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForTagGameState());
            }
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.FocusFlow)
        {
            if (FocusFlowGameState.Instance != null)
            {
                FocusFlowGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForFocusFlowGameState());
            }
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.RedLight)
        {
            if (RedLightGameState.Instance != null)
            {
                RedLightGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForRedLightGameState());
            }
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.BalloonTag)
        {
            if (BalloonTagGameState.Instance != null)
            {
                BalloonTagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForBalloonTagGameState());
            }
        }
    }

    private System.Collections.IEnumerator WaitForTagGameState()
    {
        while (TagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    private System.Collections.IEnumerator WaitForFocusFlowGameState()
    {
        while (FocusFlowGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        FocusFlowGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    private System.Collections.IEnumerator WaitForRedLightGameState()
    {
        while (RedLightGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        RedLightGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    private System.Collections.IEnumerator WaitForBalloonTagGameState()
    {
        while (BalloonTagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        BalloonTagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (TagGameState.Instance != null)
        {
            TagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
        else if (FocusFlowGameState.Instance != null)
        {
            FocusFlowGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
        else if (RedLightGameState.Instance != null)
        {
            RedLightGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
        else if (BalloonTagGameState.Instance != null)
        {
            BalloonTagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState == GameState.Stopped)
        {
            if (!IsHost) return;
            BuildResultsText();
            StartCoroutine(ShowResultsWithAnimation());
            ShowResultsClientRpc(results.ToArray());
        }
        else
        {
            HideResults();
        }
    }

    private void HideResults()
    {
        resultsPanel?.SetActive(false);

        if (fireworksLeft != null) fireworksLeft.Stop();
        if (fireworksRight != null) fireworksRight.Stop();
    }

    private IEnumerator ShowResultsWithAnimation()
    {
        if (resultsPanel == null || resultsText == null || NetworkManager.Singleton == null)
        {
            yield break;
        }

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

    private List<PlayerResult> AddTagResults(List<PlayerResult> results)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerTagMovement>();
            if (player == null) continue;

            double totalTime = player.timeSpentTaggedNet.Value;

            if (player.NetworkObjectId == TagGameState.Instance.taggedPlayerIdNet.Value)
            {
                var gameTimer = FindAnyObjectByType<GameTimer>();
                double serverTime = gameTimer != null
                    ? gameTimer.GameEndServerTime
                    : NetworkManager.Singleton.ServerTime.FixedTime;
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
                score = totalTime,
                nickname = nickname,
                color = player.colorNet.Value
            });
        }
        return results;
    }

    private List<PlayerResult> AddFocusFlowResults(List<PlayerResult> results)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            FocusFlowData data = obj.GetComponent<FocusFlowData>();
            if (data == null) continue;
            results.Add(new PlayerResult
            {
                clientId = data.OwnerClientId,
                score = data.totalScoreNet.Value,
                nickname = data.nicknameNet.Value.ToSafeString(),
                color = data.colorNet.Value
            });
        }
        return results;
    }

    private List<PlayerResult> AddBalloonTagResults(List<PlayerResult> results)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerBalloonTag>();
            if (player == null) continue;
            results.Add(new PlayerResult
            {
                clientId = player.OwnerClientId,
                score = player.balloonsNet.Value.count,
                nickname = player.nicknameNet.Value.ToSafeString(),
                color = player.colorNet.Value
            });
        }
        return results;
    }

    private List<PlayerResult> AddRedLightResults(List<PlayerResult> results)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            RedLightPlayerMovement player = obj.GetComponent<RedLightPlayerMovement>();
            if (player == null) continue;

            results.Add(new PlayerResult
            {
                clientId = player.OwnerClientId,
                score = player.distanceTraveledNet.Value,
                nickname = player.nicknameNet.Value
            });
        }
        return results;
    }

    private void BuildResultsText()
    {
        if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.Tag)
        {
            results = AddTagResults(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.FocusFlow)
        {
            results = AddFocusFlowResults(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.RedLight)
        {
            results = AddRedLightResults(results);
        }

        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.BalloonTag)
        {
            results = AddBalloonTagResults(results);
        }
        results = results.OrderBy(r => r.score).ToList();

        if (MinigameManager.Instance.currentGameState != MinigameManager.MinigameScene.Tag ||
            MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.RedLight)
        {
            results.Reverse();
        }

        UpdateCanvas(results);
    }

    private void UpdateCanvas(List<PlayerResult> results)
    {
        foreach (var card in playerCards)
        {
            card.gameObject.SetActive(false);
        }

        for (int i = 0; i < results.Count; i++)
        {
            if (i >= playerCards.Count) break;

            string playerName = results[i].nickname.Value;
            double score = results[i].score;
            var currentState = MinigameManager.Instance.currentGameState;

            string time;
            if (currentState == MinigameManager.MinigameScene.Tag)
                time = score.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "s";
            else if (currentState == MinigameManager.MinigameScene.RedLight)
                time = score.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "m";
            else
                time = ((int)score).ToString(System.Globalization.CultureInfo.InvariantCulture);

            var card = playerCards[i];
            card.nicknameText.text = playerName;
            card.scoreText.text = time;
            RectTransform rectTransform = (RectTransform)card.transform;
            card.gameObject.SetActive(true);

            UnityEngine.ColorUtility.TryParseHtmlString(medalColors[i], out var medalColor);
            UnityEngine.ColorUtility.TryParseHtmlString(results[i].color.Value, out var playerColor);
            card.bonusText.color = medalColor;
            card.nicknameText.color = playerColor;
            card.bonusText.text = "#" + (1 + i).ToString();
            rectTransform.anchoredPosition = new Vector2(0, cardYPositions[i]);
        }
    }

    [ClientRpc]
    private void ShowResultsClientRpc(PlayerResult[] playerResults)
    {
        this.results = playerResults.ToList();
        UpdateCanvas(this.results);
        StartCoroutine(ShowResultsWithAnimation());
    }

    private struct PlayerResult : INetworkSerializable
    {
        public ulong clientId;
        public double score;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref score);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
        }
    }
}
