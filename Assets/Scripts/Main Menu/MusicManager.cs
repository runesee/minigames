using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MusicManager : NetworkBehaviour
{
    public static MusicManager Instance { get; private set; }
    public AudioClip lobbyMusic;
    public AudioClip tutorialMusic;
    public AudioClip tagMusic;
    public AudioClip colorFloodMusic;
    public AudioClip ctfMusic;
    public AudioClip balloonMusic;
    public AudioClip redLightMusic;
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
            case MinigameManager.MinigameScene.RedLightTutorial:
                PlayTutorialMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.BalloonTagTutorial:
                PlayTutorialMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.ColorFloodTutorial:
                PlayTutorialMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.CaptureTheFlagTutorial:
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
            case MinigameManager.MinigameScene.ColorFlood:
                PlayColorFloodMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.CaptureTheFlag:
                PlayCtfMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.BalloonTag:
                PlayBalloonMusicClientRpc();
                break;
            case MinigameManager.MinigameScene.RedLight:
                PlayRedLightMusicClientRpc();
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
    private void PlayColorFloodMusicClientRpc()
    {
        PlayMusic(colorFloodMusic);
    }

    [ClientRpc]
    private void PlayCtfMusicClientRpc()
    {
        PlayMusic(ctfMusic);
    }

    [ClientRpc]
    private void PlayBalloonMusicClientRpc()
    {
        PlayMusic(balloonMusic);
    }

    [ClientRpc]
    private void PlayRedLightMusicClientRpc()
    {
        PlayMusic(redLightMusic);
    }


    [ClientRpc]
    public void PlayFocusFlowMusicClientRpc()
    {
        StartCoroutine(FadeOutAndIn(0.5f));
    }

    [ClientRpc]
    public void PlayFocusFlowIntenseMusicClientRpc(bool sceneChange)
    {
        if (!sceneChange) StartCoroutine(FadeOutAndIn(0.5f));
        else PlayMusic(focusFlowMusic);
    }

    [ClientRpc]
    public void ToggleRedLighMusicClientRpc()
    {
        StartCoroutine(FadeOutAndIn(0.5f));
    }

    [ClientRpc]
    private void PlayScoreboardMusicClientRpc()
    {
        audioSource.Stop();
        audioSource.volume = 0.15f;
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
        audioSource.volume = 0.15f;
    }

    private IEnumerator FadeOutAndIn(float FadeTime) {
        float startVolume = 0.15f;
        AudioSource secondaryAudioSource = GameObject.Find("Secondary Audio Source").GetComponent<AudioSource>();
        AudioSource currentAudioSource = audioSource.volume > 0f ? audioSource : secondaryAudioSource;
        AudioSource newAudioSource = audioSource.volume > 0f ? secondaryAudioSource : audioSource;

        while (currentAudioSource.volume > 0) 
        {
            currentAudioSource.volume -= startVolume * Time.deltaTime / FadeTime;
            newAudioSource.volume += startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }
    }
}
