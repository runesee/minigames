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

    [Header("Debug")]
    [SerializeField] private bool autoStartInEditor = true;

    private bool isRunning = false;
    private bool isInStandaloneMode = false;
    private bool currentLightIsRed = false;
    private double nextSwitchTime = 0.0;

    private NetworkVariable<bool> isRedLight = new NetworkVariable<bool>(
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
        OnLightStateChanged(false, isRedLight.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isRedLight.OnValueChanged -= OnLightStateChanged;
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
            if (Time.timeAsDouble >= nextSwitchTime)
            {
                SwitchLightStandalone();
            }
        }
        else if (IsServer)
        {
            if (NetworkManager.ServerTime.Time >= nextSwitchTimeNet.Value)
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
        ScheduleNextSwitchStandalone();
        UpdateAllLights();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        isInStandaloneMode = false;
        isRunning = true;
        isRedLight.Value = false;
        ScheduleNextSwitch();
    }

    public void StopGame()
    {
        isRunning = false;
    }

    private void SwitchLight()
    {
        isRedLight.Value = !isRedLight.Value;
        ScheduleNextSwitch();
    }

    private void SwitchLightStandalone()
    {
        currentLightIsRed = !currentLightIsRed;
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
        UpdateAllLights();
    }

    private void UpdateAllLights()
    {
        bool showRed = isInStandaloneMode ? currentLightIsRed : isRedLight.Value;

        foreach (var trafficLight in trafficLights)
        {
            trafficLight.SetLightState(showRed);
        }
    }
}
