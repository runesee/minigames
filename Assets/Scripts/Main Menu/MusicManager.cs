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
    public AudioClip scoreboardMusic; // NB: this is just a single sound effect, not music
    public AudioClip endMusic;
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
            case MinigameManager.MinigameScene.EndScreen:
                PlayEndMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.Tag:
                PlayTagMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.FocusFlow:
                PlayFocusFlowIntenseMusicClientRpc(true);
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
    public void PlayFocusFlowMusicClientRpc()
    {
        StartCoroutine(FadeOutAndIn(focusFlowMusic, 3f));
    }

    [ClientRpc]
    public void PlayFocusFlowIntenseMusicClientRpc(bool sceneChange)
    {
        if (!sceneChange) StartCoroutine(FadeOutAndIn(tagMusic, 3f));
        else PlayMusic(tagMusic);
    }

    [ClientRpc]
    private void PlayScoreboardMusicClientRpc()
    {
        audioSource.Stop();
        audioSource?.PlayOneShot(scoreboardMusic);
    }

    [ClientRpc]
    private void PlayEndMusicClientRpc()
    {
        PlayMusic(endMusic);
    }

    private void PlayMusic(AudioClip audioClip)
    {
        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private IEnumerator FadeOutAndIn(AudioClip audioClip, float FadeTime) {
        float startVolume = 0.1f;
        while (audioSource.volume > 0) 
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.Play();

        while (audioSource.volume < startVolume) 
        {
            audioSource.volume += startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }
    }
}
