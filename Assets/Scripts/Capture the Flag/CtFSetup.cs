using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class CtFSetup : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab; 
    public GameObject setupPanel;
    public List<CtFSetupCard> playerCards;
    private List<SetupData> setupData = new List<SetupData>();

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
                SessionManager.Instance.PlayerDataList[i].Guid
                ));
        }
        ShowSetupClientRpc(this.setupData.ToArray());
        CtFGameState.Instance.SetGameStateServerRpc(GameState.Setup);
    }

    private void UpdateCanvas(List<SetupData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            playerCards[i].gameObject.SetActive(true);
            playerCards[i].nicknameText.text = data[i].nickname.ToString();
            UnityEngine.ColorUtility.TryParseHtmlString(data[i].color.ToString(), out var playerColor);
            playerCards[i].nicknameText.color = playerColor;

            if (i % 2 == 0)
            {
                if (IsHost) CtFGameState.Instance.greenPrefabs.Add(data[i].Guid);
                playerCards[i].teamText.text = "Green";
                playerCards[i].teamText.color = Color.green;
            } 
            else
            {
                if (IsHost) CtFGameState.Instance.bluePrefabs.Add(data[i].Guid);
                playerCards[i].teamText.text = "Blue";
                playerCards[i].teamText.color = Color.blue;
            } 
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        GameObject playerInstance = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private IEnumerator DisplaySetupPanel()
    {
        yield return new WaitForSeconds(2f);
        setupPanel.SetActive(true);
        yield return new WaitForSeconds(8f);
        setupPanel.SetActive(false);
        if (IsServer) foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) SpawnPlayer(clientId);
    }

    [ClientRpc]
    private void ShowSetupClientRpc(SetupData[] data)
    {
        UpdateCanvas(data.ToList());
    }

    private struct SetupData : INetworkSerializable
    {
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public FixedString64Bytes Guid;

        public SetupData(FixedString64Bytes nickname, FixedString64Bytes color, FixedString64Bytes guid)
        {
            this.nickname = nickname;
            this.color = color;
            this.Guid = guid;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref Guid);
        }
    }
}
