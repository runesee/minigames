using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public class TagSessionManager : NetworkBehaviour
{
    public NetworkList<PlayerData> PlayerDataList = new NetworkList<PlayerData>();
    public static TagSessionManager Instance { get; private set; }

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public float XPos;
        public float ZPos;
        public double TimeSpentTagged;
        public double LastTagTime;
        public bool IsTagged;

        public PlayerData(FixedString64Bytes Guid, float XPos, float ZPos, double TimeSpentTagged, double LastTagTime, bool IsTagged)
        {
            this.Guid = Guid;
            this.XPos = XPos;
            this.ZPos = ZPos;
            this.TimeSpentTagged = TimeSpentTagged;
            this.LastTagTime = LastTagTime;
            this.IsTagged = IsTagged;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.XPos = 0f;
            this.ZPos = 0f;
            this.TimeSpentTagged = 0d;
            this.LastTagTime = 0d;
            this.IsTagged = false;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                XPos.Equals(other.XPos) &&
                ZPos.Equals(other.ZPos) &&
                TimeSpentTagged.Equals(other.TimeSpentTagged) &&
                LastTagTime.Equals(other.LastTagTime) && 
                IsTagged.Equals(other.IsTagged)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref XPos);
            serializer.SerializeValue(ref ZPos);
            serializer.SerializeValue(ref TimeSpentTagged);
            serializer.SerializeValue(ref LastTagTime);
            serializer.SerializeValue(ref IsTagged);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        FixedString64Bytes guid = new FixedString64Bytes(PlayerPrefs.GetString("Guid"));
        PlayerData playerData = new PlayerData(guid);
        if (IsOwner) SaveDataServerRpc(playerData);
    }

    public bool ContainsGuid(FixedString64Bytes guid)
    {
        for (int i = 0; i < PlayerDataList.Count; i++)
        {
            if (PlayerDataList[i].Guid.Equals(guid))
            {
                return true;
            }
        }
        return false;
    }

    public PlayerData? GetDataByGuid(FixedString64Bytes guid)
    {
        for (int i = 0; i < PlayerDataList.Count; i++)
        {
            if (PlayerDataList[i].Guid.Equals(guid))
            {
                return PlayerDataList[i];
            }
        }
        return null;
    }

    [ServerRpc]
    public void SaveDataServerRpc(PlayerData newPlayerData)
    {
        Debug.Log("Saving data TSM ServerRPC");
        for (int i = 0; i < PlayerDataList.Count; i++)
        {
            if (PlayerDataList[i].Guid.Equals(newPlayerData.Guid))
            {
                PlayerDataList[i] = newPlayerData;
                print(newPlayerData.Guid);
                print(newPlayerData.TimeSpentTagged);
                return;
            }
        }
        PlayerDataList.Add(newPlayerData);
    }
}
