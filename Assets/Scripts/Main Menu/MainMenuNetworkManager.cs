using UnityEngine;

public class MainMenuNetworkManager : MonoBehaviour
{
    public static MainMenuNetworkManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}
