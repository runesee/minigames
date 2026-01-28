using UnityEngine;

public class LobbyCompilationTest : MonoBehaviour
{
    private void TestCompilation()
    {
        var lobbyManager = GetComponent<LobbyManager>();
        var networkHandler = GetComponent<LobbyNetworkHandler>();
        var characterPreview = GetComponent<CharacterPreview>();
        
        Debug.Log("Lobby scripts compile successfully!");
    }
}
