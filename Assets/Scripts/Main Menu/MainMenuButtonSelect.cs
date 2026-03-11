using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuButtonSelect : MonoBehaviour, ISelectHandler
{
    public AudioSource audioSource;
    public AudioClip plonkClip;

    public void OnSelect(BaseEventData eventData)
    {
        audioSource?.PlayOneShot(plonkClip);
    }
}