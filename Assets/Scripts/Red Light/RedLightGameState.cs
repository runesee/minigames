using System;
using Unity.Collections;
using Unity.Netcode;

public class RedLightGameState : NetworkBehaviour
{
    public static RedLightGameState Instance { get; private set; }

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
}
