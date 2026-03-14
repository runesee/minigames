public class FocusFlowGameState : MinigameGameState
{
    public static FocusFlowGameState Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}