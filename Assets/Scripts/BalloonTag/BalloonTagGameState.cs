public class BalloonTagGameState : MinigameGameState
{
    public static BalloonTagGameState Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}