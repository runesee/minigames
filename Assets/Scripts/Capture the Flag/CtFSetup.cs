using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class CtFSetup : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab; 
    public GameObject setupPanel;
    public List<CtFSetupCard> playerCards;
    private List<SetupData> setupData = new List<SetupData>();
    public List<Vector3> greenSpawns = new List<Vector3>();
    public List<Vector3> blueSpawns = new List<Vector3>();
    private int greenSpawnTally = 0;
    private int blueSpawnTally = 0;

    public override void OnNetworkSpawn()
    {
        StartCoroutine(DisplaySetupPanel());
        if (!IsHost) return;
        
        // Requires other games to be played first (playerdatalist must be populated)
        for (int i = 0; i < SessionManager.Instance.PlayerDataList.Count; i++)
        {
            this.setupData.Add(new SetupData(
                SessionManager.Instance.PlayerDataList[i].nickname, 
                SessionManager.Instance.PlayerDataList[i].color, 
                SessionManager.Instance.PlayerDataList[i].Guid,
                Team.None
                ));
        }
        setupData.Sort((a, b) => string.Compare(a.Guid.ToString(), b.Guid.ToString(), StringComparison.Ordinal));
        ShowSetupClientRpc(this.setupData.ToArray());
    }

    private void UpdateCanvas(List<SetupData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            playerCards[i].gameObject.SetActive(true);
            if (data[i].team == Team.Green)
            {
                playerCards[i].teamText.text = "Green";
                playerCards[i].teamText.color = Color.green;
            } 
            else
            {
                playerCards[i].teamText.text = "Blue";
                playerCards[i].teamText.color = Color.blue;
            } 
        }
    }

    private void InitializeCanvas(List<SetupData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            playerCards[i].gameObject.SetActive(true);
            playerCards[i].nicknameText.text = data[i].nickname.ToString();
            UnityEngine.ColorUtility.TryParseHtmlString(data[i].color.ToString(), out var playerColor);
            playerCards[i].nicknameText.color = playerColor;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        GameObject playerInstance = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), UnityEngine.Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private IEnumerator DisplaySetupPanel()
    {
        yield return new WaitForSeconds(1.5f);
        setupPanel.SetActive(true);
        if (IsServer) foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) SpawnPlayer(clientId);
        yield return new WaitForSeconds(2f);
        if (IsServer) AssignTeams();
        yield return new WaitForSeconds(6f);
        setupPanel.SetActive(false);
    }

    private void AssignTeams()
    {
        setupData.Sort((a, b) => string.Compare(a.Guid.ToString(), b.Guid.ToString(), StringComparison.Ordinal));
        var players = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Select(obj => obj.GetComponent<PlayerCtF>()).Where(p => p != null).ToDictionary(p => p.guidNet.Value);
        int teamIndex = 0;
        foreach (var i in Enumerable.Range(0, setupData.Count))
        {
            var entry = setupData[i];
            if (!players.TryGetValue(entry.Guid, out var player)) continue;
            player.teamNet.Value = (teamIndex % 2 == 0) ? Team.Green : Team.Blue;
            var data = setupData[teamIndex];
            data.team = player.teamNet.Value;
            setupData[teamIndex] = data;

            Vector3 spawnPosition = new();
            if (player.teamNet.Value == Team.Green)
            {
                spawnPosition = greenSpawns[greenSpawnTally];
                greenSpawnTally++;
            }
            else
            {
                spawnPosition = blueSpawns[blueSpawnTally];
                blueSpawnTally++;
            }
            player.TeleportClientRpc(spawnPosition);
            teamIndex++;
        }
        UpdateCanvasClientRpc(setupData.ToArray());
    }

    [ClientRpc]
    private void UpdateCanvasClientRpc(SetupData[] data)
    {
        UpdateCanvas(data.ToList());
    }

    [ClientRpc]
    private void ShowSetupClientRpc(SetupData[] data)
    {
        InitializeCanvas(data.ToList());
    }

    private struct SetupData : INetworkSerializable
    {
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public FixedString64Bytes Guid;
        public Team team;

        public SetupData(FixedString64Bytes nickname, FixedString64Bytes color, FixedString64Bytes guid, Team team)
        {
            this.nickname = nickname;
            this.color = color;
            this.Guid = guid;
            this.team = team;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref team);
        }
    }
}
