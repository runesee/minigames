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
    [SerializeField] private TextMeshProUGUI winnerText;
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
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.CaptureTheFlag)
        {
            if (CtFGameState.Instance != null)
            {
                CtFGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForCtFGameState());
            }
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.ColorFlood)
        {
            if (ColorFloodGameState.Instance != null)
            {
                ColorFloodGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            }
            else
            {
                StartCoroutine(WaitForColorFloodGameState());
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

    private System.Collections.IEnumerator WaitForCtFGameState()
    {
        while (CtFGameState.Instance == null)  yield return new WaitForSeconds(0.1f);
        CtFGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
    }

    private System.Collections.IEnumerator WaitForColorFloodGameState()
    {
        while (ColorFloodGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        ColorFloodGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
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
        else if (CtFGameState.Instance != null)
        {
            CtFGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
        else if (ColorFloodGameState.Instance != null)
        {
            ColorFloodGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState == GameState.Stopped)
        {
            if (!IsHost) return;
            MinigameManager.MinigameScene currentScene = BuildResultsText();
            StartCoroutine(ShowResultsWithAnimation());
            ShowResultsClientRpc(results.ToArray(), currentScene);
            if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.CaptureTheFlag)
            {
                if (CtFGameState.Instance.blueScore.Value > CtFGameState.Instance.greenScore.Value)
                {
                    DisplayWinnerTextClientRpc(CtFGameState.Instance.blueColor.color, Team.Blue);
                }
                else DisplayWinnerTextClientRpc(CtFGameState.Instance.greenColor.color, Team.Green);
            }
            else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.ColorFlood)
            {
                if (ColorFloodGameState.Instance.blueTileCount.Value > ColorFloodGameState.Instance.greenTileCount.Value)
                {
                    DisplayWinnerTextClientRpc(PlayerColorManager.GetColor(1), Team.Blue);
                }
                else DisplayWinnerTextClientRpc(PlayerColorManager.GetColor(2), Team.Green);
            }
        }
        else if (newState == GameState.Idling || newState == GameState.Initializing)
        {
            HideResults();
        }
    }

    private void HideResults()
    {
        resultsPanel?.SetActive(false);
        fireworksLeft?.Stop();
        fireworksRight?.Stop();
    }

    private IEnumerator ShowResultsWithAnimation()
    {
        if (resultsPanel == null || resultsText == null || NetworkManager.Singleton == null)
        {
            yield break;
        }

        resultsPanel.SetActive(true);
        fireworksLeft?.Play();
        fireworksRight?.Play();

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

    private static List<PlayerResult> AddResults<T>(List<PlayerResult> results) where T : Player
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<T>();
            if (player == null) continue;
            var data = player.GetPlayerData();
            results.Add(new PlayerResult
            {
                clientId = player.OwnerClientId,
                score = data.score,
                nickname = data.nickname.ToSafeString(),
                color = data.color,
                team = data.team
            });
        }
        return results;
    }

    private MinigameManager.MinigameScene BuildResultsText()
    {
        if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.Tag)
        {
            results = AddResults<PlayerTagMovement>(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.FocusFlow)
        {
            results = AddResults<FocusFlowData>(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.RedLight)
        {
            results = AddResults<RedLightPlayerMovement>(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.BalloonTag)
        {
            results = AddResults<PlayerBalloonTag>(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.CaptureTheFlag)
        {
            results = AddResults<PlayerCtF>(results);
        }
        else if (MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.ColorFlood)
        {
            results = AddResults<PlayerColorFlood>(results);
        }
        results = results.OrderBy(r => r.score).ToList();

        if (MinigameManager.Instance.currentGameState != MinigameManager.MinigameScene.Tag ||
            MinigameManager.Instance.currentGameState == MinigameManager.MinigameScene.RedLight)
        {
            results.Reverse();
        }
        UpdateCanvas(results, MinigameManager.Instance.currentGameState);
        return MinigameManager.Instance.currentGameState;
    }

    private void UpdateCanvas(List<PlayerResult> results, MinigameManager.MinigameScene currentScene)
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
            string time;

            if (currentScene == MinigameManager.MinigameScene.Tag)
                time = score.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s";
            else if (currentScene == MinigameManager.MinigameScene.RedLight)
                time = score.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "m";
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
            Team team = results[i].team;
            if (team != Team.None) card.scoreText.color = team == Team.Green ? PlayerColorManager.GetColor(2) : PlayerColorManager.GetColor(1);
           
            int firstIndexOfScore = i;
            while (firstIndexOfScore > 0 && results[firstIndexOfScore - 1].score == results[i].score) firstIndexOfScore--;
            int rank = firstIndexOfScore + 1;
            card.bonusText.text = "#" + rank.ToString();
            rectTransform.anchoredPosition = new Vector2(0, cardYPositions[i]);
        }
    }

    [ClientRpc]
    private void ShowResultsClientRpc(PlayerResult[] playerResults, MinigameManager.MinigameScene currentScene)
    {
        this.results = playerResults.ToList();
        UpdateCanvas(this.results, currentScene);
        StartCoroutine(ShowResultsWithAnimation());
    }

    [ClientRpc]
    private void DisplayWinnerTextClientRpc(Color color, Team team)
    {
        UpdateWinnerText(color, team);
    }

    private void UpdateWinnerText(Color color, Team team)
    {
        winnerText.text = team.ToString() + " team wins!";
        winnerText.color = color;
    }

    public struct PlayerResult : INetworkSerializable
    {
        public ulong clientId;
        public double score;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public Team team;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref score);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref team);
        }
    }
}
