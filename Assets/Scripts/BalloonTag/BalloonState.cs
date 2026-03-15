using Unity.Collections;
using Unity.Netcode;

public struct BalloonState : INetworkSerializable
{
    public int count;
    public FixedString64Bytes c0;
    public FixedString64Bytes c1;
    public FixedString64Bytes c2;
    public FixedString64Bytes c3;
    public FixedString64Bytes c4;
    public FixedString64Bytes c5;
    public FixedString64Bytes c6;
    public FixedString64Bytes c7;

    public BalloonState(int count, FixedString64Bytes color)
    {
        this.count = count;
        c0 = c1 = c2 = c3 = c4 = c5 = c6 = c7 = color;
    }

    public readonly FixedString64Bytes GetColor(int index)
    {
        return index switch
        {
            0 => c0,
            1 => c1,
            2 => c2,
            3 => c3,
            4 => c4,
            5 => c5,
            6 => c6,
            7 => c7,
            _ => default
        };
    }

    public void SetColor(int index, FixedString64Bytes color)
    {
        switch (index)
        {
            case 0: c0 = color; break;
            case 1: c1 = color; break;
            case 2: c2 = color; break;
            case 3: c3 = color; break;
            case 4: c4 = color; break;
            case 5: c5 = color; break;
            case 6: c6 = color; break;
            case 7: c7 = color; break;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref count);
        serializer.SerializeValue(ref c0);
        serializer.SerializeValue(ref c1);
        serializer.SerializeValue(ref c2);
        serializer.SerializeValue(ref c3);
        serializer.SerializeValue(ref c4);
        serializer.SerializeValue(ref c5);
        serializer.SerializeValue(ref c6);
        serializer.SerializeValue(ref c7);
    }
}
