using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MusicManager : NetworkBehaviour
{
    public static MusicManager Instance { get; private set; }
    public AudioClip lobbyMusic;
    public AudioClip tutorialMusic;
    public AudioClip tagMusic;
    public AudioClip focusFlowMusic;
    public AudioClip scoreboardMusic;
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void PlaySong(MinigameManager.MinigameScene scene)
    {
        switch (scene)
        {
            case MinigameManager.MinigameScene.TagTutorial:
                PlayTutorialMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.FocusFlowTutorial:
                PlayTutorialMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.Scoreboard:
                PlayScoreboardMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.Tag:
                PlayTagMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.FocusFlow:
                PlayFocusFlowMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.CaptureTheFlag:
                PlayFocusFlowMusicClientRpc();
                break;
            default:
                PlayLobbyMusicClientRpc();
                break;
        }
    }

    [ClientRpc]
    private void PlayTutorialMusicClientRpc()
    {
        PlayMusic(tutorialMusic);
    }

    [ClientRpc]
    private void PlayTagMusicClientRpc()
    {
        PlayMusic(tagMusic);
    }

    [ClientRpc]
    private void PlayLobbyMusicClientRpc()
    {
        PlayMusic(lobbyMusic);
    }

    [ClientRpc]
    private void PlayFocusFlowMusicClientRpc()
    {
        PlayMusic(focusFlowMusic);
    }

    [ClientRpc]
    private void PlayScoreboardMusicClientRpc()
    {
        audioSource.Stop();
        StartCoroutine(PlayScoreboardSoundbyte());
    }

    private void PlayMusic(AudioClip audioClip)
    {
        audioSource.Stop();
        audioSource?.PlayOneShot(audioClip);
    }

    private IEnumerator PlayScoreboardSoundbyte()
    {
        yield return new WaitForSeconds(3f); // Roughly syncs up with '+'-text
        audioSource?.PlayOneShot(scoreboardMusic);
    }
}
