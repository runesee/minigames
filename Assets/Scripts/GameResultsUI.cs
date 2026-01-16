using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class GameResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI resultsText;

    private void Start()
    {
        if (resultsPanel != null)
        {
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
            ShowResults();
        }
        else
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
        }
    }

    private void ShowResults()
    {
        if (resultsPanel == null || resultsText == null || NetworkManager.Singleton == null)
        {
            return;
        }

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

            results.Add(new PlayerResult
            {
                clientId = player.OwnerClientId,
                timeTagged = totalTime
            });
        }

        results = results.OrderBy(r => r.timeTagged).ToList();

        string resultText = "<size=48><b>GAME OVER</b></size>\n\n<size=36><b>Results:</b></size>\n\n";
        
        for (int i = 0; i < results.Count; i++)
        {
            string rank = (i + 1).ToString();
            string playerName = $"Player {results[i].clientId}";
            string time = results[i].timeTagged.ToString("F2") + "s";
            
            if (i == 0)
            {
                resultText += $"<color=yellow><size=32>{rank}. {playerName}: {time} - WINNER!</size></color>\n";
            }
            else
            {
                resultText += $"<size=28>{rank}. {playerName}: {time}</size>\n";
            }
        }

        resultText += "\n<size=24><i>Press Shutdown to return to menu</i></size>";

        resultsText.text = resultText;
        resultsPanel.SetActive(true);
    }

    private struct PlayerResult
    {
        public ulong clientId;
        public double timeTagged;
    }
}
