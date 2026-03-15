using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PlayerBalloonTag : TagPlayer
{
    public AudioClip popClip;
    public List<GameObject> BalloonPrefabs;
    public NetworkVariable<BalloonState> balloonsNet = new NetworkVariable<BalloonState>(
        new BalloonState(2, "#D6877F"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        balloonsNet.OnValueChanged += OnBalloonsChanged;
        if (IsHost) StartCoroutine(WaitForPlayerConnect());
    }

    private System.Collections.IEnumerator WaitForPlayerConnect()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || BalloonTagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Running);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        balloonsNet.OnValueChanged -= OnBalloonsChanged;
    }
    
    void OnBalloonsChanged(BalloonState previousValue, BalloonState newValue)
    {
        for (int i = 0; i < BalloonPrefabs.Count; i++)
        {
            BalloonPrefabs[i].SetActive(i < newValue.count);
            UnityEngine.ColorUtility.TryParseHtmlString(newValue.GetColor(i).ToString(), out var color);
            BalloonPrefabs[i].GetComponentInChildren<MeshRenderer>().material.color = color;
        }
    }

    /// <summary>
    /// Helper method for changing a player model's color.
    /// </summary>
    /// <param name="color">Updated hex-code color.</param>
    protected override void SetSkinColor(string color)
    {
        base.SetSkinColor(color);
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        BalloonPrefabs[0].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        BalloonPrefabs[1].GetComponentInChildren<MeshRenderer>().material.color = skinColor;
        if (IsOwner) InitializeBalloonsServerRpc(color);
    }

    public override PlayerData GetPlayerData()
    {
        PlayerData playerData = new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            balloonsNet.Value.count,
            balloonsNet.Value.count
        );
        return playerData;
    }

    private void Update()
    {
        if (BalloonTagGameState.Instance != null && BalloonTagGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;

        // Parse InputInteractions
        var (joystickOffset, input) = ParseInput();
        isPunchingNet.Value = isPunching;

        if (isPunching && NetworkManager.Singleton.ServerTime.FixedTime - lastTagTimeNet.Value > 0.7)
        {
            PlayerBalloonTag target = PlayerUtils.FindClosestPlayerInRange<PlayerBalloonTag>(2.5f, this.gameObject, this.transform);
            tagAudioSource.pitch = 1f;
            tagAudioSource?.PlayOneShot(tagClip);
            if (target != null) TagPlayerServerRpc(target.NetworkObjectId);
            else TagServerRpc();
        }
        var (pedalSpeed, pedalAnimationSpeed) = GetSmoothedPedalSpeed();

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            float moveSpeed = 6f * pedalSpeed;
            HandleMovement(joystickOffset, moveSpeed, pedalSpeed, pedalAnimationSpeed, true);
        }
        else HandleMovement();
        HandleTaunting();
    }
 
    [ServerRpc]
    public void InitializeBalloonsServerRpc(FixedString64Bytes color)
    {
        balloonsNet.Value = new BalloonState(2, color);
    }

    [ClientRpc]
    private void PlayTagSoundClientRpc()
    {
        tagAudioSource.pitch = UnityEngine.Random.Range(0.7f, 1.3f);
        tagAudioSource?.PlayOneShot(popClip);
    }


    /// <summary>
    /// Set targeted player as tagged on server, and disable their movement.
    /// Also update time tagging player has been tagged.
    /// </summary>
    /// <param name="victimId"></param> Target player ID.
    [ServerRpc]
    private void TagPlayerServerRpc(ulong victimId)
    {
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        var victim = NetworkManager.Singleton.SpawnManager.SpawnedObjects[victimId].GetComponent<PlayerBalloonTag>();
        BalloonState localBalloons = this.balloonsNet.Value;
        BalloonState victimBalloons = victim.balloonsNet.Value;
        if (victimBalloons.count <= 0) return;

        localBalloons.SetColor(localBalloons.count, victimBalloons.GetColor(victimBalloons.count - 1));
        localBalloons.count++;
        victimBalloons.count--;
        this.balloonsNet.Value = localBalloons;
        victim.balloonsNet.Value = victimBalloons;

        // Add timediff to current player and prevent tagging again for another .7 seconds
        this.timeSpentTaggedNet.Value += serverTime - lastTagTimeNet.Value;
        this.lastTagTimeNet.Value = serverTime;
        PlayTagSoundClientRpc();
    }

    /// <summary>
    /// RPC that prevents spamming of tag when NOT hitting a target.
    /// </summary>
    [ServerRpc]
    private void TagServerRpc()
    {
        double serverTime = NetworkManager.Singleton.ServerTime.FixedTime;
        this.lastTagTimeNet.Value = serverTime;
    }
}
