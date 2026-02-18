using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RedLightGameState : NetworkBehaviour
{
    public static RedLightGameState Instance { get; private set; }

    public enum GameState
    {
        Waiting,
        Running,
        Finished
    }

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Waiting,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public float Distance;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float Distance)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.Distance = Distance;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                Distance.Equals(other.Distance)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref Distance);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    public List<PlayerData> GetAllPlayerData()
    {
        List<PlayerData> playerDataList = new List<PlayerData>();

        RedLightPlayerMovement[] allPlayers = FindObjectsByType<RedLightPlayerMovement>(FindObjectsSortMode.None);
        
        foreach (RedLightPlayerMovement player in allPlayers)
        {
            if (player != null)
            {
                playerDataList.Add(player.GetPlayerData());
            }
        }

        return playerDataList;
    }

    public List<PlayerData> GetPlayerDataSortedByDistance()
    {
        List<PlayerData> playerDataList = GetAllPlayerData();
        return playerDataList.OrderByDescending(p => p.Distance).ToList();
    }

    public PlayerData GetWinner()
    {
        List<PlayerData> sortedPlayers = GetPlayerDataSortedByDistance();
        return sortedPlayers.Count > 0 ? sortedPlayers[0] : default;
    }
}

