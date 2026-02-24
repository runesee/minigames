using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This Class should reside on an in-scene SessionManager Prefab/Game Object.
/// A general Game Manager class should update a player's score here on disconnect. 
/// Similarly, it should reconstruct a player's score on reconnect, if the game is in progress.
/// </summary>
public class SessionManager : NetworkBehaviour
{
    public List<PlayerData> PlayerDataList = new List<PlayerData>();
    public List<PlayerData> previousPlayerDataList = new List<PlayerData>();
    public static SessionManager Instance { get; private set; }

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public float Score;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float Score)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.Score = Score;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.nickname = "";
            this.color = "";
            this.Score = 0f;
        }

        public bool Equals(PlayerData other)
        {
            return Guid.Equals(other.Guid) && nickname.Equals(other.nickname) && color.Equals(other.color) && Score.Equals(other.Score);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref Score);
        }
    }

    public virtual void Start()
    {
        // New client connected, need to assign them a unique GUID
        var data = LocalPlayerStorage.Load();
        data ??= new LocalPlayerData();
        if (string.IsNullOrEmpty(data.guid))
        {
            data.guid = System.Guid.NewGuid().ToString();
            LocalPlayerStorage.Save(data);
        }
    }

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        var data = LocalPlayerStorage.Load();
        FixedString64Bytes guid = new FixedString64Bytes(data.guid);
        PlayerData playerData = new PlayerData(guid);
        if (IsOwner)
        {
            if (!PlayerDataList.Any(p => p.Guid.Equals(guid)))
            {
                PlayerDataList.Add(playerData);
                previousPlayerDataList.Add(playerData);
            }
        }

    }

    public PlayerData GetDataByGuid(FixedString64Bytes guid)
    {
        foreach (var playerData in PlayerDataList)
            if (playerData.Guid.Equals(guid)) return playerData;
        return new PlayerData(guid, "player", "", 0f);
    }

    public void SaveData(PlayerData newPlayerData)
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
        previousPlayerDataList.Add(new PlayerData(newPlayerData.Guid, newPlayerData.nickname, newPlayerData.color, 0f));
    }
}
