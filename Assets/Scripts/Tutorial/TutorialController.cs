using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialController : NetworkBehaviour
{
    public Slider slider;
    public TMP_Text sliderText;
    public string relativeVideoPath;
    public VideoPlayer videoPlayer;
    private float sliderTime = 30f;

    private void Awake()
    {
        if (videoPlayer && relativeVideoPath != "")
        {
            try
            {
                videoPlayer.url = Application.streamingAssetsPath + "/" + relativeVideoPath;
            }
            catch { Debug.Log("Error parsing tutorial video file path."); }
        }
    }

    private void Start()
    {
        slider.value = 0f;
        StartCoroutine(UpdateWaitingSlider());
    }

    private IEnumerator UpdateWaitingSlider()
    {
        yield return new WaitForSeconds(1f);
        slider.value += 1f;
        sliderTime -= 1f;
        sliderText.text = $"Starting in {sliderTime}s";
        if (sliderTime > 0f) StartCoroutine(UpdateWaitingSlider());
        else if (IsHost) MinigameManager.Instance.SceneFinished();
    }
}
