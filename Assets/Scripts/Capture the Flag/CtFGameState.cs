using System.Collections;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CtFGameState : MinigameGameState
{
    public static CtFGameState Instance { get; private set; }
    public NetworkVariable<int> blueScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> greenScore = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public TMP_Text blueScoreText;
    public TMP_Text greenScoreText;
    public TMP_Text toastText;
    public Material blueColor;
    public Material greenColor;
    public float[] ctfScores = { 6f, 6f, 3f, 3f };
    private int tally = 0;

    private void Awake()
    {
        Instance = this;
    }

    protected override void SaveData()
    {
        var rankedPlayers = PlayerDataList.OrderByDescending(p => p.ordering).ToList();
        if (greenScore.Value < blueScore.Value) rankedPlayers.Reverse();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < ctfScores.Length ? ctfScores[i] : 0f;
            var player = rankedPlayers[i];
            var globalSessionData = SessionManager.Instance.GetDataByGuid(player.Guid);
            var scoredPlayerData = new SessionManager.PlayerData(
                player.Guid,
                player.nickname,
                player.color,
                score + globalSessionData.Score
            );
            SessionManager.Instance.SaveData(scoredPlayerData);
        }
    }

    [ClientRpc]
    public void UpdateScoreTextClientRpc(Team team, int score)
    {
        if (team == Team.Green) blueScoreText.text = score.ToString();
        else greenScoreText.text = score.ToString();
    }

    [ClientRpc]
    public void ToastMessageClientRpc(Team team, string message)
    {
        tally++;
        toastText.text = message;
        Color color = team == Team.Green ? greenColor.color : blueColor.color;
        if (team == Team.None) color = Color.white;
        toastText.color = color;
        StartCoroutine(DisplayToastMessage(tally));
    }

    // Some networked sounds (especially for opponents) may need to use the this Rpc to correctly determine team membership.
    [ClientRpc]
    public void PlaySoundClientRpc(Team scoringTeam, PlayerCtF.CtfClips clip)
    {
        var player = PlayerCtF.Local;
        if (player == null) return;
        switch(clip)
        {
            case PlayerCtF.CtfClips.Score:
                if (player.teamNet.Value == scoringTeam) player.tagAudioSource.PlayOneShot(player.scoreClip);
                else player.tagAudioSource.PlayOneShot(player.enemyScoreClip);
                break;
            case PlayerCtF.CtfClips.Returned:
                if (player.teamNet.Value == scoringTeam) player.tagAudioSource.PlayOneShot(player.flagReturnedClip);
                break;
        }
    }

    private IEnumerator DisplayToastMessage(int count)
    {
        yield return new WaitForSeconds(3f);
        if (count == tally) toastText.text = "";
    }

    public void SetScores(float[] scores)
    {
        this.scores = scores;
    }
}