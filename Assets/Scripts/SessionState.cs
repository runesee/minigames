using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

public class SessionState : NetworkBehaviour
{
    // TODO : MOVE and RENAME to TagSessionState etc.
    public static SessionState Instance { get; private set; }

    public NetworkVariable<Dictionary<FixedString64Bytes, PlayerData>> playerData = 
    new NetworkVariable<Dictionary<FixedString64Bytes, PlayerData>>(
        new Dictionary<FixedString64Bytes, PlayerData>(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public struct PlayerData : INetworkSerializable
    {
        public FixedString64Bytes Guid; // TODO : remove duplicate guid? Already covered by dict
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

    public void Start()
    {
        DontDestroyOnLoad(this);

        // New client connected, need to assign them a unique GUID
        if (PlayerPrefs.GetString("Guid") == "")
        {
            PlayerPrefs.SetString("Guid", System.Guid.NewGuid().ToString());

            // TODO : Set color and name depending on UI etc.
            PlayerPrefs.SetString("Color", "#7fb3d6");
            PlayerPrefs.SetString("Username", "Placeholder");
        }
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        FixedString64Bytes guid = new FixedString64Bytes(PlayerPrefs.GetString("Guid"));
        playerData.Value[guid] = default;
    }

    // TODO : XML comment
    [ServerRpc]
    public void SaveDataServerRpc(PlayerData data)
    {
            playerData.Value[data.Guid] = data;
    }
}
