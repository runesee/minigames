using UnityEngine;

public class ButtonSoundGenerator : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private ButtonCircle.ButtonType buttonType;
    [SerializeField] private float volume = 0.3f;
    [SerializeField] private float duration = 0.15f;

    private AudioSource audioSource;
    private ButtonCircle buttonCircle;

    private void Awake()
    {
        buttonCircle = GetComponent<ButtonCircle>();
        SetupAudioSource();
        GenerateButtonSound();
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    private void GenerateButtonSound()
    {
        float frequency = GetFrequencyForButton(buttonType);
        AudioClip clip = GenerateTone(frequency, duration); 
        if (buttonCircle != null) AssignSoundToButton(clip);
    }

    private float GetFrequencyForButton(ButtonCircle.ButtonType type)
    {
        switch (type)
        {
            case ButtonCircle.ButtonType.X:
                return 261.63f;
            case ButtonCircle.ButtonType.Y:
                return 329.63f;
            case ButtonCircle.ButtonType.A:
                return 293.66f;
            case ButtonCircle.ButtonType.B:
                return 349.23f;
            default:
                return 440f;
        }
    }

    private AudioClip GenerateTone(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        
        AudioClip clip = AudioClip.Create("ButtonTone", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];
        
        float attackTime = 0.01f;
        float releaseTime = 0.05f;
        int attackSamples = Mathf.FloorToInt(sampleRate * attackTime);
        int releaseSamples = Mathf.FloorToInt(sampleRate * releaseTime);
        
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float sineWave = Mathf.Sin(2f * Mathf.PI * frequency * time);
            
            float envelope = 1f;
            if (i < attackSamples)
            {
                envelope = i / (float)attackSamples;
            }
            else if (i > sampleCount - releaseSamples)
            {
                envelope = (sampleCount - i) / (float)releaseSamples;
            }
            
            samples[i] = sineWave * envelope * 0.5f;
        }
        
        clip.SetData(samples, 0);
        return clip;
    }

    private void AssignSoundToButton(AudioClip clip)
    {
        var buttonCircleType = typeof(ButtonCircle);
        var buttonSoundField = buttonCircleType.GetField("buttonSound", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        buttonSoundField?.SetValue(buttonCircle, clip);

        var audioSourceField = buttonCircleType.GetField("audioSource", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        audioSourceField?.SetValue(buttonCircle, audioSource);
    }
}
