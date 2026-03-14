public class RedLightGameState : MinigameGameState
{
    public static RedLightGameState Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}