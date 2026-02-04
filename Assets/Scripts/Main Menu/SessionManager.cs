using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

/// <summary>
/// This Class should reside on an in-scene SessionManager Prefab/Game Object.
/// A general Game Manager class should update a player's score here on disconnect. 
/// Similarly, it should reconstruct a player's score on reconnect, if the game is in progress.
/// </summary>
public class SessionManager : NetworkBehaviour
{
    public NetworkList<PlayerData> PlayerDataList = new NetworkList<PlayerData>();
    public NetworkList<PlayerData> previousPlayerDataList = new NetworkList<PlayerData>();
    public static SessionManager Instance { get; private set; }

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public float Score;

        public PlayerData(FixedString64Bytes Guid, float Score)
        {
            this.Guid = Guid;
            this.Score = Score;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.Score = 0f;
        }

        public bool Equals(PlayerData other)
        {
            return Guid.Equals(other.Guid) && Score.Equals(other.Score);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref Score);
        }
    }

    public virtual void Start()
    {
        // New client connected, need to assign them a unique GUID
        if (PlayerPrefs.GetString("Guid") == "")
        {
            PlayerPrefs.SetString("Guid", System.Guid.NewGuid().ToString());
        }
    }

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        FixedString64Bytes guid = new FixedString64Bytes(PlayerPrefs.GetString("Guid"));
        PlayerData playerData = new PlayerData(guid);
        if (IsOwner)
        {
            PlayerDataList.Add(playerData);
            previousPlayerDataList.Add(playerData);
        }
    }

    public PlayerData GetDataByGuid(FixedString64Bytes guid)
    {
        for (int i = 0; i < PlayerDataList.Count; i++)
        {
            if (PlayerDataList[i].Guid.Equals(guid))
            {
                return PlayerDataList[i];
            }
        }
        return new PlayerData(guid);
    }

    [ServerRpc]
    public void SaveDataServerRpc(PlayerData newPlayerData)
    {
        for (int i = 0; i < PlayerDataList.Count; i++)
        {
            if (PlayerDataList[i].Guid.Equals(newPlayerData.Guid))
            {
                previousPlayerDataList[i] = PlayerDataList[i];
                PlayerDataList[i] = newPlayerData;
                return;
            }
        }
        PlayerDataList.Add(newPlayerData);
    }
}
