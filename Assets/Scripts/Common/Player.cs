using Unity.Netcode;

public abstract class Player : NetworkBehaviour
{
    public abstract PlayerData GetPlayerData();
}
