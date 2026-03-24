using UnityEngine;
using Unity.Netcode;

public class RedLightManager : NetworkBehaviour
{
    public static RedLightManager Instance { get; private set; }

    [Header("Traffic Lights")]
    [SerializeField] private TrafficLightController[] trafficLights;

    [Header("Duration Settings")]
    [SerializeField] private float greenLightMinDuration = 3f;
    [SerializeField] private float greenLightMaxDuration = 8f;
    [SerializeField] private float redLightMinDuration = 2f;
    [SerializeField] private float redLightMaxDuration = 5f;

    [Header("Audio Settings")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip audioClip;
    private const float YellowWarningDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool autoStartInEditor = true;

    private bool isRunning = false;
    private bool isInStandaloneMode = false;
    private bool currentLightIsRed = false;
    private bool currentLightIsYellow = false;
    private double nextSwitchTime = 0.0;

    private NetworkVariable<bool> isRedLight = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isYellowLight = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<double> nextSwitchTimeNet = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsRedLight => isInStandaloneMode ? currentLightIsRed : isRedLight.Value;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isRedLight.OnValueChanged += OnLightStateChanged;
        isYellowLight.OnValueChanged += OnLightStateChanged;
        OnLightStateChanged(false, isRedLight.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isRedLight.OnValueChanged -= OnLightStateChanged;
        isYellowLight.OnValueChanged -= OnLightStateChanged;
    }

    private void Start()
    {
        if (autoStartInEditor && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening))
        {
            StartGameStandalone();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        if (isInStandaloneMode)
        {
            double currentTime = Time.timeAsDouble;

            if (!currentLightIsRed && !currentLightIsYellow && currentTime >= nextSwitchTime - YellowWarningDuration)
            {
                currentLightIsYellow = true;
                UpdateAllLights();
            }

            if (currentTime >= nextSwitchTime)
            {
                SwitchLightStandalone();
            }
        }
        else if (IsServer)
        {
            double currentTime = NetworkManager.ServerTime.Time;

            if (!isRedLight.Value && !isYellowLight.Value && currentTime >= nextSwitchTimeNet.Value - YellowWarningDuration)
            {
                isYellowLight.Value = true;
                MusicManager.Instance.ToggleRedLighMusicClientRpc();
            }

            if (currentTime >= nextSwitchTimeNet.Value)
            {
                SwitchLight();
            }
        }
    }

    private void StartGameStandalone()
    {
        isInStandaloneMode = true;
        isRunning = true;
        currentLightIsRed = false;
        currentLightIsYellow = false;
        ScheduleNextSwitchStandalone();
        UpdateAllLights();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        isInStandaloneMode = false;
        isRunning = true;
        isRedLight.Value = false;
        isYellowLight.Value = false;
        ScheduleNextSwitch();
    }

    public void StopGame()
    {
        isRunning = false;
    }

    private void SwitchLight()
    {
        if (isRedLight.Value) MusicManager.Instance.ToggleRedLighMusicClientRpc();
        isRedLight.Value = !isRedLight.Value;
        isYellowLight.Value = false;
        ScheduleNextSwitch();
    }

    private void PlayLightSound()
    {
        if (isYellowLight.Value) audioSource.pitch = 1f;
        else if (isRedLight.Value) audioSource.pitch = 2f;
        else audioSource.pitch = 0.5f;
        audioSource.PlayOneShot(audioClip);
    }

    private void SwitchLightStandalone()
    {
        currentLightIsRed = !currentLightIsRed;
        currentLightIsYellow = false;
        audioSource.pitch = currentLightIsRed ? 2f : 0.5f;
        audioSource.PlayOneShot(audioClip);
        ScheduleNextSwitchStandalone();
        UpdateAllLights();
    }

    private void ScheduleNextSwitch()
    {
        float duration = isRedLight.Value 
            ? Random.Range(redLightMinDuration, redLightMaxDuration)
            : Random.Range(greenLightMinDuration, greenLightMaxDuration);

        nextSwitchTimeNet.Value = NetworkManager.ServerTime.Time + duration;
    }

    private void ScheduleNextSwitchStandalone()
    {
        float duration = currentLightIsRed 
            ? Random.Range(redLightMinDuration, redLightMaxDuration)
            : Random.Range(greenLightMinDuration, greenLightMaxDuration);

        nextSwitchTime = Time.timeAsDouble + duration;
    }

    private void OnLightStateChanged(bool previousState, bool newState)
    {
        PlayLightSound();
        UpdateAllLights();
    }

    private void UpdateAllLights()
    {
        bool showRed = isInStandaloneMode ? currentLightIsRed : isRedLight.Value;
        bool showYellow = isInStandaloneMode ? currentLightIsYellow : isYellowLight.Value;

        foreach (var trafficLight in trafficLights)
        {
            if (showYellow)
            {
                trafficLight.SetYellowWarning(true);
                if (isInStandaloneMode)
                {
                    audioSource.pitch = 1f;
                    audioSource.PlayOneShot(audioClip);
                }
            }
            else trafficLight.SetLightState(showRed);
        }
    }
}
