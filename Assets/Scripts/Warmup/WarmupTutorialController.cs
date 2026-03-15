using UnityEngine;
using UnityEngine.Video;

public class WarmupTutorialController : MonoBehaviour
{
    public string relativeVideoPath;
    public VideoPlayer videoPlayer;

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
}
